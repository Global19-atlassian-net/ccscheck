using System;
using System.IO;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using System.Linq;
using System.Threading;

using Bio;
using Bio.IO.PacBio;
using Bio.Variant;
using Bio.BWA.MEM;
using Bio.BWA;
using Bio.Extensions;


namespace ccscheck
{
    class MainClass
    {
        public static void Main (string[] args)
        {
            try {
                Console.WriteLine(System.Environment.CurrentDirectory);
                PlatformManager.Services.MaxSequenceSize = int.MaxValue;
                PlatformManager.Services.DefaultBufferSize = 4096;
                PlatformManager.Services.Is64BitProcessType = true;

                if (args.Length > 4) {
                    Console.WriteLine ("Too many arguments");
                    DisplayHelp ();
                } else if (args.Length < 2) {
                    Console.WriteLine("Not enough arguments");
                    DisplayHelp();
                }else if (args [0] == "h" || args [0] == "help" || args [0] == "?" || args [0] == "-h") {
                    DisplayHelp ();
                } else {
                    string bam_name = args [0];
                    string out_dir = args [1];
                    string ref_name = args.Length > 2 ? args [2] : null;

                    IEnumerable<PacBioCCSRead> reader;
                    PacBioCCSBamReader bamreader = new PacBioCCSBamReader ();

                    if(Directory.Exists(bam_name)) {
                        reader = getReader(bam_name);
                    } else if(File.Exists(bam_name)) {
                        reader = bamreader.Parse(bam_name);
                    } else {
                        Console.WriteLine ("Can't find file or folder: " + bam_name);
                        return;
                    }

                    if (ref_name != null && !File.Exists (ref_name)) {
                        Console.WriteLine ("Can't find file: " + ref_name);
                        return;
                    }
                    if (Directory.Exists (out_dir)) {
                        Console.WriteLine ("The output directory already exists, please specify a new directory or delete the old one.");
                        //return;
                    }

                    Directory.CreateDirectory (out_dir);

                    List<CCSReadMetricsOutputter> outputters = new List<CCSReadMetricsOutputter> () { 
                        new ZmwOutputFile(out_dir),
                        new ZScoreOutputter(out_dir),
                        new VariantOutputter(out_dir),
                        new SNROutputFile(out_dir),
                        new QVCalibration(out_dir)};

                    BWAPairwiseAligner bwa = null;
                    bool callVariants = ref_name != null;
                    if (callVariants) {
                        bwa = new BWAPairwiseAligner(ref_name, false); 
                    }
                    VariantFilter filter = args.Length > 3 ? new VariantFilter(args[3]) : null;
                    int excludedVariants = 0;
                    int excludedVariantsAtEnd = 0;

                    // Produce aligned reads with variants called in parallel.
                    var reads = new BlockingCollection<Tuple<PacBioCCSRead, BWAPairwiseAlignment, List<Variant>>>();
                    Task producer = Task.Factory.StartNew(() =>
                        {
                            try 
                            {
                                Parallel.ForEach(reader, z => {
                                    try {
                                        if(z.HoleNumber != 7528) {return;}
                                        BWAPairwiseAlignment aln = null;
                                        List<Variant> variants = null;
                                        if (callVariants) {
                                            aln = bwa.AlignRead(z.Sequence) as BWAPairwiseAlignment;
                                            Console.WriteLine(z.Sequence.ConvertToString());
                                            Console.WriteLine(aln.AlignedRefSeq.ConvertToString());
                                            Console.WriteLine(aln.AlignedQuerySeq.ConvertToString());
                                            var qs = (aln.AlignedQuerySeq as QualitativeSequence);
                                            var qstr = new String(qs.GetQualityScores().Select(x=> (char)(x+33)).ToArray());
                                            Console.WriteLine(qstr);
                                            if (aln != null) {
                                                variants = VariantCaller.CallVariants(aln);
                                                variants.ForEach( p => {
                                                    p.StartPosition += aln.AlignedSAMSequence.Pos;
                                                    p.RefName = aln.Reference;
                                                    });
                                                //Filter out end variants.
                                                if (variants.Count > 0) {
                                                    int startCount = variants.Count;
                                                    variants = variants.Where(v => !v.AtEndOfAlignment).ToList();
                                                    int endCount = variants.Count;
                                                    if (endCount != startCount) {
                                                        var dif = startCount - endCount;
                                                        Interlocked.Add(ref excludedVariantsAtEnd, dif);
                                                    }
                                                }
                                                // Filter out any crappy variants
                                                if (filter != null && variants.Count > 0) {
                                                    int startCount = variants.Count;
                                                    variants = variants.Where(v => !filter.ContainsVariantPosition(v)).ToList();
                                                    int endCount = variants.Count;
                                                    if (endCount != startCount) {
                                                        var dif = startCount - endCount;
                                                        Interlocked.Add(ref excludedVariants, dif);
                                                    }
                                                }
                                            }
                                        }
                                        var res = new Tuple<PacBioCCSRead, BWAPairwiseAlignment, List<Variant>>(z, aln, variants);
                                        reads.Add(res);
                                    }
                                    catch(Exception thrown) {
                                        Console.WriteLine("CCS READ FAIL: " + z.Sequence.ID);
                                        Console.WriteLine(thrown.Message);
                                    } });
                            } catch(Exception thrown) {
                                Console.WriteLine("Could not parse BAM file: " + thrown.Message);
                                while(thrown.InnerException != null) {
                                    Console.WriteLine(thrown.InnerException.Message);
                                    thrown = thrown.InnerException;
                                }
                            }
                            reads.CompleteAdding();
                        });                  


                    // Consume them into output files.
                    foreach(var r in reads.GetConsumingEnumerable()) {
                        foreach(var outputter in outputters) {
                            outputter.ConsumeCCSRead(r.Item1, r.Item2, r.Item3);
                        }
                    }

                    // throw any exceptions (this should be used after putting the consumer on a separate thread)
                    producer.Wait();
                    // Close the files
                    outputters.ForEach(z => z.Finish());
                    if(filter != null) {
                        Console.WriteLine("Filtered out " + excludedVariants.ToString() +" variants based on VCF file");
                    }
                    Console.WriteLine("Filtered out " + excludedVariantsAtEnd.ToString() +" variants for being at end of alignment");
            }
            }
            catch(Exception thrown) {
                Console.WriteLine ("Error thrown when attempting to generate the CCS results");
                Console.WriteLine ("Error: " + thrown.Message);
                Console.WriteLine (thrown.StackTrace);
                while (thrown.InnerException != null) {
                    Console.WriteLine ("Inner Exception: " + thrown.InnerException.Message);
                    thrown = thrown.InnerException;
                }
            }
        }

        static IEnumerable<PacBioCCSRead> getReader(string directoryName) {
            var fastqs = Directory.EnumerateFiles (directoryName).Where (z => z.EndsWith (".fastq"));
            foreach (var f in fastqs) {
                foreach (var s in CCSFastqParser.Parse(f)) {
                    yield return s;
                }                
            }
        }

       static void DisplayHelp() {
            Console.WriteLine ("ccscheck INPUT OUTDIR REF[optional]");
            Console.WriteLine ("INPUT - the input ccs.bam file");
            Console.WriteLine ("OUTDIR - directory to output results into");
            Console.WriteLine ("REF - A fasta file with the references (optional)");
            Console.WriteLine ("VCF - A VCF file with variant positions to exclude (optional)");
        }                   
    }
}
