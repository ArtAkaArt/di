﻿using System.Diagnostics;
using System.Text;
using TagsCloudContainer.Interfaces;

namespace TagsCloudContainer
{
    public class FileToDictionaryConverter : IConverter
    {
        private readonly IWordsFilter filter;
        private readonly IDocParser parser;

        public FileToDictionaryConverter(IWordsFilter filter, IDocParser parser)
        {
            this.filter = filter;
            this.parser = parser;
        }

        public Dictionary<string, int> GetWordsInFile(ICustomOptions options)
        {
            var inputWordPath = Path.Combine(options.WorkingDir, options.WordsFileName);
            var bufferedWords = new List<string>();

            if (options.WordsFileName[options.WordsFileName.LastIndexOf('.')..] != ".txt")
                bufferedWords = parser.ParseDoc(inputWordPath);
            else
                bufferedWords = File.ReadAllLines(inputWordPath)
                    .ToList();
            if (bufferedWords.Count == 0)
                throw new AggregateException("Words file are empty");
            bufferedWords = bufferedWords
                .Select(x => x.ToLower())
                .ToList();
            var tmpFilePath = Path.Combine(options.WorkingDir, "tmp.txt");
            File.WriteAllLines(tmpFilePath, bufferedWords);

            var cmd = $"mystem.exe -nig {tmpFilePath}";

            var proc = new ProcessStartInfo
            {
                UseShellExecute = false,
                WorkingDirectory = Path.Combine(options.WorkingDir),
                FileName = @"C:\Windows\System32\cmd.exe",
                Arguments = "/C" + cmd,
                RedirectStandardOutput = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                StandardOutputEncoding = Encoding.UTF8,
            };
            var p = Process.Start(proc);

            var taggedWords = p.StandardOutput
                .ReadToEnd()
                .Split("\r\n")
                .ToList();

            File.Delete(tmpFilePath);

            var boringWords = File.ReadAllLines(Path.Combine(options.WorkingDir, options.BoringWordsName))
                .Select(x => x.ToLower())
                .ToList();

            boringWords.Sort();

            var boringWordsSet = boringWords.ToHashSet();

            var filteredWords = filter.FilterWords(taggedWords, options, boringWordsSet);

            var result = new Dictionary<string, int>();
            filteredWords.ForEach(x =>
            {
                if (result.ContainsKey(x))
                    result[x] += 1;

                else result.Add(x, 1);
            });

            return result
                .ToList()
                .OrderByDescending(x => x.Value)
                .ToDictionary(x => x.Key, x => x.Value);
        }
    }
}