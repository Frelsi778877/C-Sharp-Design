using System;
using System.Collections.Generic;
using System.Text;

namespace SudokuUnlimited
{
    public class CellSnapshot
    {
        public int Row { get; set; }
        public int Col { get; set; }
        public int Value { get; set; }
        public bool IsGiven { get; set; }
        public bool IsNakedPair { get; set; }
        public bool IsError { get; set; }
        public HashSet<int> Candidates { get; set; }

        public static CellSnapshot Capture(SudokuCellModel cell, int row, int col)
        {
            return new CellSnapshot
            {
                Row = row,
                Col = col,
                Value = cell.Value,
                IsGiven = cell.IsGiven,
                IsNakedPair = cell.IsNakedPair,
                IsError = cell.IsError,
                Candidates = new HashSet<int>(cell.Candidates)
            };
        }
    }
}
