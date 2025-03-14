﻿using CommandLine;
using System.Collections.Generic;

namespace Metacrack
{
    [Verb("lookup", HelpText = "Lookup meta data for a list of keys.")]
    public class LookupOptions
    {        
        [Value(0, Required = true, MetaName = "InputPath", HelpText = "The input path to process .txt files from.")]
        public string InputPath { get; set; }

        [Value(1, Required = true, MetaName = "SourceFolder", HelpText = "The source catalog to lookup data in.")]
        public string SourceFolder { get; set; }

        [Option("prefix", Default = "Password")]
        public string Prefix { get; set;}

        [Option("hash", Default = -1)]
        public int Hash { get; set; }

        [Option("tokenize", HelpText = "Attempts to turn each value into further sub values.")]
        public bool Tokenize { get; set; }

        [Option("stem", HelpText = "Adds a stemmed version of the password to the output.")]
        public bool Stem { get; set; }

        [Option("stem-only", HelpText = "Outputs a stemmed version of the password only.")]
        public bool StemOnly { get; set; }

        [Option("export", HelpText = "Creates an  email:data file for export instead or a word and hash list.")]
        public bool Export { get; set; }

        [Option("filter", HelpText = "Filters a cluster of words using the rule file in the path provided.")]
        public string Filter { get; set; }

        [Option("infer", HelpText = "Adds cross references to output.")]
        public bool UseInference { get; set; }
    }
}
