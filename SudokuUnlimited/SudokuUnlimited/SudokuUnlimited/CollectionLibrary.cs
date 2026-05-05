using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json.Serialization;

namespace SudokuUnlimited
{
    public class CollectionLibrary
    {
        [JsonPropertyName("collections")]
        public List<SudokuCollection> Collections { get; set; } = new();
    }

    public class SudokuCollection
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("filename")]
        public string Filename { get; set; }

        [JsonPropertyName("difficulty")]
        public string Difficulty { get; set; }

        [JsonPropertyName("solved")]
        public List<int> Solved { get; set; } = new();

        [JsonIgnore]
        public int SolvedCount => Solved?.Count ?? 0;

        [JsonIgnore]
        public int PuzzleCount { get; set; }

        public void LoadPuzzleCount(string basePath)
        {
            string fullPath = System.IO.Path.Combine(basePath, Filename);
            if (File.Exists(fullPath))
                PuzzleCount = File.ReadLines(fullPath).Count(l => l.Trim().Length == 81);
        }
    }
}
