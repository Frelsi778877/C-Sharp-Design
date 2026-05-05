using System;
using System.Collections.Generic;
using System.Text;

namespace SudokuUnlimited
{
    public class CellClickedEventArgs : EventArgs
    {
        /// <summary>
        /// The zero-based row index of the clicked cell (0-8).
        /// </summary>
        public int Row { get; }

        /// <summary>
        /// The zero-based column index of the clicked cell (0-8).
        /// </summary>
        public int Column { get; }

        /// <summary>
        /// The current display value of the clicked cell.
        /// 0 if the cell is empty or in candidate mode.
        /// </summary>
        public int Value { get; }

        /// <summary>
        /// Whether the clicked cell is a given (protected pre-filled clue).
        /// </summary>
        public bool IsGiven { get; }

        /// <summary>
        /// Whether the clicked cell is in candidate mode.
        /// </summary>
        public bool IsCandidate { get; }

        public CellClickedEventArgs(int row, int column, int value, bool isGiven, bool isCandidate)
        {
            Row = row;
            Column = column;
            Value = value;
            IsGiven = isGiven;
            IsCandidate = isCandidate;
        }
    }

}
