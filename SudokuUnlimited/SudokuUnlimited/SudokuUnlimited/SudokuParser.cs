using System;
using System.Collections.Generic;
using System.Text;

namespace SudokuUnlimited
{
    public static class SudokuParser
    {
        public static SudokuPuzzle FromString(string puzzleString)
        {
            if (puzzleString == null || puzzleString.Trim().Length != 81)
                throw new ArgumentException("Puzzle string must be exactly 81 characters.");

            int[,] givens = new int[9, 9];

            for (int i = 0; i < 81; i++)
            {
                int r = i / 9;
                int c = i % 9;
                givens[r, c] = puzzleString[i] - '0';
            }

            int[,] solution = SudokuSolver.Solve(givens);

            return new SudokuPuzzle(givens, solution);
        }

        public static SudokuPuzzle GivensOnly(string puzzleString)
        {
            if (puzzleString == null || puzzleString.Trim().Length != 81)
                throw new ArgumentException("Puzzle string must be exactly 81 characters.");

            int[,] givens = new int[9, 9];

            for (int i = 0; i < 81; i++)
            {
                int r = i / 9;
                int c = i % 9;
                givens[r, c] = puzzleString[i] - '0';
            }

            return new SudokuPuzzle(givens); // no solution
        }
    }

    public static class SudokuSolver
    {
        public static int[,] Solve(int[,] givens)
        {
            int[,] board = (int[,])givens.Clone();
            TrySolve(board);
            return board;
        }

        private static bool TrySolve(int[,] board)
        {
            for (int r = 0; r < 9; r++)
            {
                for (int c = 0; c < 9; c++)
                {
                    if (board[r, c] != 0) continue;

                    for (int num = 1; num <= 9; num++)
                    {
                        if (IsValid(board, r, c, num))
                        {
                            board[r, c] = num;
                            if (TrySolve(board)) return true;
                            board[r, c] = 0;
                        }
                    }

                    return false;
                }
            }

            return true;
        }

        private static bool IsValid(int[,] board, int row, int col, int num)
        {
            for (int c = 0; c < 9; c++)
                if (board[row, c] == num) return false;

            for (int r = 0; r < 9; r++)
                if (board[r, col] == num) return false;

            int boxRow = (row / 3) * 3;
            int boxCol = (col / 3) * 3;
            for (int r = boxRow; r < boxRow + 3; r++)
                for (int c = boxCol; c < boxCol + 3; c++)
                    if (board[r, c] == num) return false;

            return true;
        }

        public static int CountSolutions(int[,] givens, int limit = 2)
        {
            int[,] board = (int[,])givens.Clone();
            int count = 0;
            CountSolutions(board, ref count, limit);
            return count;
        }

        private static void CountSolutions(int[,] board, ref int count, int limit)
        {
            for (int r = 0; r < 9; r++)
            {
                for (int c = 0; c < 9; c++)
                {
                    if (board[r, c] != 0) continue;

                    for (int num = 1; num <= 9; num++)
                    {
                        if (IsValid(board, r, c, num))
                        {
                            board[r, c] = num;
                            CountSolutions(board, ref count, limit);
                            board[r, c] = 0;

                            if (count >= limit) return;
                        }
                    }

                    return;
                }
            }

            count++;
        }
    }
}
