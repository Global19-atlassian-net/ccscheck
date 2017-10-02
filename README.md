### ccscheck - Evaluate the empirical accuracy and diagnose any issues with PacBio CCS sequencing.


```
    ccscheck input.ccs.bam out_folder_name any_size_ref.fasta
```



ccscheck will produce the following data files in the output folder specified when it completes.


| CSV File Name      | Data Contained                                                                                     |
| :-------          | :-----------                                                                                       |
| qv_calibration | Empirical counts of the correct and incorrect basecalls divided by insertions, deletions and SNPs. |
| snrs       | The SNR values for the emitted ZMWs.                                                               |
| variants   | A detailed description of all variants found when aligning the reads.                              |
| zmws       | A detailed description and associated statistics for each emitted ZMW.                             |
| zscores    | Z-scores for each ZMW/subread combination.                                                         |

This tool is an internal tool that we are making available for external users, but it is not officially supported by PacBio.

##Stand alone binaries
Are only available as betas right now but can be downloaded for:
* [Mac OSX El Capitan](http://www.evolvedmicrobe.com/ccscheck/ccscheck_macosx.tar.gz)
* [Ubuntu 14.04] (http://www.evolvedmicrobe.com/ccscheck/ccscheck.ubuntu14.tar.gz)


##Build requirements

* POSIX/64-bit/Little-Endian
* git
* C Compiler
* C# Compiler with CLR available.


##Build instructions

```
    git clone https://github.com/evolvedmicrobe/ccscheck.git
    cd ccscheck
    ./build.sh
```

Disclaimer
----------
THIS WEBSITE AND CONTENT AND ALL SITE-RELATED SERVICES, INCLUDING ANY DATA, ARE PROVIDED "AS IS," WITH ALL FAULTS, WITH NO REPRESENTATIONS OR WARRANTIES OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING, BUT NOT LIMITED TO, ANY WARRANTIES OF MERCHANTABILITY, SATISFACTORY QUALITY, NON-INFRINGEMENT OR FITNESS FOR A PARTICULAR PURPOSE. YOU ASSUME TOTAL RESPONSIBILITY AND RISK FOR YOUR USE OF THIS SITE, ALL SITE-RELATED SERVICES, AND ANY THIRD PARTY WEBSITES OR APPLICATIONS. NO ORAL OR WRITTEN INFORMATION OR ADVICE SHALL CREATE A WARRANTY OF ANY KIND. ANY REFERENCES TO SPECIFIC PRODUCTS OR SERVICES ON THE WEBSITES DO NOT CONSTITUTE OR IMPLY A RECOMMENDATION OR ENDORSEMENT BY PACIFIC BIOSCIENCES.
