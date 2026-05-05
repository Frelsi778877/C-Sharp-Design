using System;
using System.Collections.Generic;
using System.Text;

namespace SudokuUnlimited
{
    public class SudokuPuzzle
    {
        /// <summary>
        /// The given (pre-filled) values for the puzzle.
        /// 0 = empty cell. Values 1-9 are given clues and will be protected from editing.
        /// </summary>
        public int[,] Givens { get; set; } = new int[9, 9];

        /// <summary>
        /// The full solution for the puzzle. Optional.
        /// If provided, player entries will be validated against this solution.
        /// 0 = not set. Values 1-9 are the correct answers.
        /// </summary>
        public int[,] Solution { get; set; } = null;

        public SudokuPuzzle()
        {
            Givens = new int[9, 9];
        }

        public SudokuPuzzle(int[,] givens, int[,] solution = null)
        {
            if (givens == null) throw new ArgumentNullException(nameof(givens));
            if (givens.GetLength(0) != 9 || givens.GetLength(1) != 9)
                throw new ArgumentException("Givens must be a 9x9 array.", nameof(givens));

            if (solution != null && (solution.GetLength(0) != 9 || solution.GetLength(1) != 9))
                throw new ArgumentException("Solution must be a 9x9 array.", nameof(solution));

            Givens = givens;
            Solution = solution;
        }
    }

}
