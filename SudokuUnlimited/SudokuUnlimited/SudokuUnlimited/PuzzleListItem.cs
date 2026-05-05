using System;
using System.Collections.Generic;
using System.Text;

namespace SudokuUnlimited
{
    public class PuzzleListItem
    {
        public int LineNumber { get; set; }
        public string Label => $"Puzzle {LineNumber}";
        public bool IsSolved { get; set; }
        public string PuzzleString { get; set; }
    }
}
