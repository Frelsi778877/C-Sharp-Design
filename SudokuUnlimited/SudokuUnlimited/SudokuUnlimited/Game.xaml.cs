using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace SudokuUnlimited
{
    /// <summary>
    /// Interaction logic for Game.xaml
    /// </summary>
    public partial class Game : Window
    {
        private int _selectedRow = -1;
        private int _selectedCol = -1;

        private SudokuPuzzle _puzzle;
        private SudokuCollection _collection;
        private int _puzzleLineNumber;
        private BoardState _savedState;

        public Game(SudokuPuzzle puzzle, SudokuCollection collection, int lineNumber, BoardState savedState = null)
        {
            InitializeComponent();
            _collection = collection;
            _puzzleLineNumber = lineNumber;
            _savedState = savedState;

            KeyDown += Game_KeyDown;

            MySudokuGrid.Loaded += (s, e) =>
            {
                MySudokuGrid.Initialize(puzzle);
                CheckCompletedNumbers();

                // Restore saved state AFTER grid is initialized
                if (_savedState != null)
                {
                    MySudokuGrid.ImportState(_savedState);
                    CheckCompletedNumbers();
                }
            };
        }

        private void Game_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                SaveProgress();
                var loadGame = new LoadGame();
                loadGame.Show();
                this.Close();
            }
        }

        private void SaveProgress()
        {
            var state = MySudokuGrid.ExportState();
            state.CollectionId = _collection?.Id;
            state.LineNumber = _puzzleLineNumber;
            CollectionStore.SaveBoardState(state);
        }


        private void MySudokuGrid_CellClicked(object sender, CellClickedEventArgs e)
        {
            _selectedRow = e.Row;
            _selectedCol = e.Column;

            Title = $"Clicked: Row {e.Row}, Col {e.Column}, Value: {e.Value}";
        }

        private void NumberButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedRow < 0 || _selectedCol < 0) return;
            int number = int.Parse(((Button)sender).Tag.ToString());

            if (MySudokuGrid.IsMultiSelect)
            {
                // Multi-select: always treat as candidate toggle
                ToggleCandidateOnSelection(number);
            }
            else
            {
                // Single select: left click = solved number toggle
                var currentValue = MySudokuGrid.GetCellValue(_selectedRow, _selectedCol);
                if (currentValue == number)
                    MySudokuGrid.ClearCell(_selectedRow, _selectedCol);
                else
                    MySudokuGrid.SetCell(_selectedRow, _selectedCol, number);
            }

            CheckCompletedNumbers();
            CheckPuzzleSolved();
        }

        private void NumberButton_RightClick(object sender, MouseButtonEventArgs e)
        {
            if (_selectedRow < 0 || _selectedCol < 0) return;
            int number = int.Parse(((Button)sender).Tag.ToString());

            var selected = MySudokuGrid.GetSelectedCells();

            // Check if any selected cells are locked naked groups
            bool anyLocked = selected.Any(s => MySudokuGrid.IsNakedPair(s.row, s.col));

            if (anyLocked)
            {
                // Remove candidate from locked cells directly
                var changes = new List<(int row, int col, HashSet<int> candidates)>();
                foreach (var (r, c) in selected)
                {
                    if (!MySudokuGrid.IsNakedPair(r, c)) continue;
                    var candidates = MySudokuGrid.GetCandidates(r, c);
                    if (candidates.Contains(number))
                    {
                        candidates.Remove(number);
                        changes.Add((r, c, candidates));
                    }
                }
                if (changes.Count > 0)
                    MySudokuGrid.SetCandidatesOnLocked(changes);
            }
            else
            {
                ToggleCandidateOnSelection(number);
            }

            e.Handled = true;
            CheckCompletedNumbers();
        }

        private void ToggleCandidateOnSelection(int number)
        {
            var selected = MySudokuGrid.GetSelectedCells();
            if (selected.Count == 0) return;

            bool allHave = true;
            foreach (var (r, c) in selected)
            {
                if (MySudokuGrid.IsGiven(r, c)) continue;
                if (MySudokuGrid.IsNakedPair(r, c)) continue;
                if (MySudokuGrid.GetCellValue(r, c) > 0) continue;
                var candidates = MySudokuGrid.GetCandidates(r, c);
                if (!candidates.Contains(number)) { allHave = false; break; }
            }

            var changes = new List<(int row, int col, HashSet<int> candidates)>();

            foreach (var (r, c) in selected)
            {
                if (MySudokuGrid.IsGiven(r, c)) continue;
                if (MySudokuGrid.IsNakedPair(r, c)) continue;
                if (MySudokuGrid.GetCellValue(r, c) > 0) continue;

                var candidates = MySudokuGrid.GetCandidates(r, c);
                if (allHave)
                    candidates.Remove(number);
                else
                    candidates.Add(number);

                changes.Add((r, c, candidates));
            }

            if (changes.Count > 0)
                MySudokuGrid.SetCandidatesBulk(changes);
        }

        private void CheckPuzzleSolved()
        {
            if (MySudokuGrid.IsGridComplete())
            {
                SolvedOverlay.Visibility = Visibility.Visible;

                if (_collection != null && _puzzleLineNumber > 0)
                    CollectionStore.MarkSolved(_collection.Id, _puzzleLineNumber);
            }
        }

        private void SolvedBackButton_Click(object sender, RoutedEventArgs e)
        {
            CollectionStore.ClearBoardState();
            var loadGame = new LoadGame();
            loadGame.Show();
            this.Close();
        }

        

        private void CheckCompletedNumbers()
        {
            foreach (var child in NumberPanel.Children)
            {
                if (child is Button btn)
                {
                    int number = int.Parse(btn.Tag.ToString());
                    if (MySudokuGrid.IsNumberComplete(number))
                    {
                        MySudokuGrid.LockCompletedNumber(number);
                        btn.IsEnabled = false;
                    }
                }
            }
        }
    }
}
