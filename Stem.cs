﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Malfoy
{
    public static class Stem
    {
        private static string _outputHashPath = "";
        private static string _outputDictPath = "";

        private static readonly object _lock = new object();

        private const int TaskCount = 16;

        public static async Task Process(string currentDirectory, string[] args)
        {
            var arg = args[0];
            var source = args[1];

            //Get user hashes / json input path
            var fileEntries = Directory.GetFiles(currentDirectory, arg);

            if (fileEntries.Length == 0)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("No input file(s) found.");
                Console.ResetColor();
                return;
            }

            var sourceFiles = Directory.GetFiles(currentDirectory, source);

            if (sourceFiles.Count() == 0)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                if (sourceFiles.Count() == 0) Console.WriteLine("No source files found.");

                Console.ResetColor();
                return;
            }

            Console.WriteLine($"Started at {DateTime.Now.ToShortTimeString()}.");

            //Load the firstnames or other items used for stemming into a hashset
            var lookups = new HashSet<string>();
            var lineCount = 0;

            var size = Common.GetFileEntriesSize(sourceFiles);
            var progressTotal = 0L;

            using (var progress = new ProgressBar(false))
            {
                foreach (var lookupPath in sourceFiles)
                {
                    using (var reader = new StreamReader(lookupPath))
                    {
                        while (!reader.EndOfStream)
                        {
                            lineCount++;

                            var line = reader.ReadLine();

                            if (line.Length >= 4) lookups.Add(line.ToLower());

                            //Update the percentage
                            progress.Report((double)progressTotal / size);
                        }
                    }
                }
            }

            size = Common.GetFileEntriesSize(fileEntries);
            progressTotal = 0L;
            lineCount = 0;

            using (var progress = new ProgressBar(false))
            { 
                foreach (var filePath in fileEntries)
                {
                    progress.WriteLine($"Processing {filePath}.");

                    //Create a version based on the file size, so that the hash and dict are bound together
                    var fileInfo = new FileInfo(filePath);
                    var version = Common.GetSerial(fileInfo,"s");                    

                    var fileName = Path.GetFileNameWithoutExtension(filePath);
                    var filePathName = $"{currentDirectory}\\{fileName}";
                    _outputHashPath = $"{filePathName}.{version}.hash";
                    _outputDictPath = $"{filePathName}.{version}.dict";

                    //Check that there are no output files
                    if (!Common.CheckForFiles(new string[] { _outputHashPath, _outputDictPath }))
                    {
                        progress.Pause();

                        Console.ForegroundColor = ConsoleColor.DarkYellow;
                        Console.WriteLine($"Skipping {filePathName}.");
                        Console.ResetColor();

                        progressTotal += fileInfo.Length;

                        progress.Resume();
                        progress.Report((double)progressTotal / size);

                        continue;
                    }

                    var inputs = new List<string>();

                    //Loop through and check if each email contains items from the lookup, if so add them
                    using (var reader = new StreamReader(filePath))
                    {
                        while (!reader.EndOfStream)
                        {
                            var line = reader.ReadLine();

                            inputs.Add(line);

                            lineCount++;
                            progressTotal += line.Length;

                            //Update the percentage
                            progress.Report((double)progressTotal / size);
                        }
                    }

                    var itemCount = (inputs.Count() / TaskCount) + 1;
                    var splitLists = Common.SplitList<string>(inputs, itemCount);
                    var tasks = new List<Task>();

                    foreach (var splitList in splitLists)
                    {
                        tasks.Add(Task.Run(() => DoStem(lookups, splitList)));
                    }

                    progressTotal = 0;

                    //Wait for tasks to complate
                    while (tasks.Count > 0)
                    {
                        var task = await Task.WhenAny(tasks);
                        tasks.Remove(task);

                        progress.Report(progressTotal / TaskCount);
                    }
                }

                progress.WriteLine($"Completed at {DateTime.Now.ToShortTimeString()}.");
            }
        }

        private static Task DoStem(HashSet<string> lookups, List<string> lines)
        {
            var hashes = new List<string>();
            var dicts = new List<string>();

            foreach (var line in lines)
            {
                var splits = line.Split(':');

                if (splits.Length == 2 || splits.Length == 3)
                {
                    var email = splits[0].ToLower();

                    //Validate the email is valid
                    var subsplits = email.Split('@');
                    var name = subsplits[0].ToLower();

                    //Remove any +
                    name = name.Split('+')[0];

                    if (subsplits.Length == 2)
                    {
                        var finals = new HashSet<string>();

                        //Try split on .
                        var names = name.Split('.');
                        foreach (var subname in names)
                        {
                            //Rule out initials
                            if (subname.Length > 1 && subname.Length < 70) finals.Add(subname);
                        }

                        foreach (var entry in lookups)
                        {
                            if (name == entry || name.StartsWith(entry)) finals.Add(entry);
                        }

                        foreach (var final in finals)
                        {
                            hashes.Add(line);
                            dicts.Add(final);
                        }
                    }
                }
            }

            if (hashes.Count > 0)
            {
                lock (_lock)
                {
                    File.AppendAllLines(_outputHashPath, hashes);
                    File.AppendAllLines(_outputDictPath, dicts);
                }
            }

            return Task.CompletedTask;
        }
    }
}
