using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace SudokuUnlimited
{
    public class BoardState
    {
        [JsonPropertyName("cells")]
        public CellState[][] Cells { get; set; }

        [JsonPropertyName("collectionId")]
        public string CollectionId { get; set; }

        [JsonPropertyName("lineNumber")]
        public int LineNumber { get; set; }

        [JsonPropertyName("puzzleString")]
        public string PuzzleString { get; set; }
    }

    public class CellState
    {
        [JsonPropertyName("value")]
        public int Value { get; set; }

        [JsonPropertyName("isGiven")]
        public bool IsGiven { get; set; }

        [JsonPropertyName("isNakedPair")]
        public bool IsNakedPair { get; set; }

        [JsonPropertyName("isError")]
        public bool IsError { get; set; }

        [JsonPropertyName("candidates")]
        public List<int> Candidates { get; set; } = new();
    }
}
