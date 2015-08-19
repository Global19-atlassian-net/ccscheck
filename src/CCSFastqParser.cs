// using System;
// using System.IO;
// using System.Collections.Generic;
// using System.Linq;
//
// using Bio;
// using Bio.IO.PacBio;
// using Bio.Variant;
// using Bio.BWA;
// using Bio.BWA.MEM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;

using Bio;
using Bio.IO.PacBio;
using Bio.IO.SAM;
using Bio.IO.FastQ;
using Bio.Extensions;

namespace ccscheck
{
    public static class CCSFastqParser
    {
        public static IEnumerable<PacBioCCSRead> Parse(string filename) {
            if (!File.Exists (filename)) {
                throw new FileNotFoundException ("Could not find the FASTQ file", filename);
            }
            var fqp = new FastQParser ();
            var newFile = filename.Replace (".fastq", ".csv");
            Dictionary<string, CCScsvLine> csvData = null;
            if (File.Exists (newFile)) {
                csvData = LoadCSVFile (newFile);
            }
            string rsString = "i,0,0,0,0,0";
            foreach (var seq in fqp.Parse(filename)) {
                var sam = new SAMAlignedSequence ();
                sam.QuerySequence = seq;
                SAMOptionalField of = new SAMOptionalField () { Tag = "zm", Value = seq.ID.Split ('/') [1] };
                sam.OptionalFields.Add (of);                        
                if (csvData != null && csvData.ContainsKey (seq.ID)) {
                    var cd = csvData [seq.ID];
                     of = new SAMOptionalField () 
                    { Tag = "sn", Value = String.Join(",", "f", cd.SnrA, cd.SnrC, cd.SnrG, cd.SnrT) };
                    sam.OptionalFields.Add (of);
                    var options = new string[] { "pq", "za", "rs", "np", "RG" };
                    var values = new string[] {cd.PredictedAccuracy, "0",
                        rsString, cd.NumPasses, "NA"};
                    for(int i=0; i < options.Length; i++) {
                        of = new SAMOptionalField () { Tag = options[i], Value = values[i]};
                        sam.OptionalFields.Add (of);
                    }
                    of = new SAMOptionalField () { Tag = "zs", Value = "0,0" };
                    sam.OptionalFields.Add (of);
                           
                } else {
                    of = new SAMOptionalField () { Tag = "sn", Value = "f,0,0,0,0" };
                    sam.OptionalFields.Add (of);
                    var naOptions = new string[] { "pq", "za", "np", "RG" };
                    of = new SAMOptionalField () { Tag = "rs", Value = rsString };
                    sam.OptionalFields.Add (of);
                    foreach (var s in naOptions) {
                        of = new SAMOptionalField () { Tag = s, Value = "0" };
                        sam.OptionalFields.Add (of);
                    }
                    of = new SAMOptionalField () { Tag = "zs", Value = "0,0" };
                    sam.OptionalFields.Add (of);
                }
                yield return new PacBioCCSRead(sam);
            }
        }
      
        static Dictionary<string, CCScsvLine> LoadCSVFile(string fname) {
            return File.ReadAllLines (fname).Skip (1).Select (z => new CCScsvLine (z)).ToDictionary(
                k => k.DictionaryKey,
                p => p);
        }
    }

    public class CCScsvLine 
    {
        public string SnrA, SnrC, SnrT, SnrG;
        public string NumPasses;
        public string PredictedAccuracy;
        public string CCSReadLength;
        public string MovieName;
        public string HoleNumber;
        public string DictionaryKey;
        public CCScsvLine(string line) {
            var sp = line.Trim ().Split (',');
            MovieName = sp [0];
            HoleNumber = sp [1];
            SnrT = sp [3];
            SnrG = sp [4];
            SnrA = sp [5];
            SnrC = sp [6];
            CCSReadLength = sp[7];
            NumPasses = sp [8];
            PredictedAccuracy = sp [9];
            DictionaryKey = sp [0] + "/" + sp [1] + "/ccs";

        }
        
    }
}

