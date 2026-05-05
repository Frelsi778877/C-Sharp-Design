using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace SudokuUnlimited
{
    /// <summary>
    /// A fully customizable WPF Sudoku grid control.
    /// Dynamically renders a 9x9 Sudoku board with support for solved values,
    /// candidate mini-grids, highlighting, validation, and givens protection.
    /// </summary>
    public class SudokuGrid : ContentControl
    {
        // -------------------------------------------------------------------------
        // Private State
        // -------------------------------------------------------------------------
        private readonly Border[,] _nakedPairBorders = new Border[9, 9];
        private readonly TextBlock[,] _nakedPairTextBlocks = new TextBlock[9, 9];
        private readonly HashSet<(int row, int col)> _selectedCells = new HashSet<(int, int)>();
        private bool _isDragging = false;
        private readonly SudokuCellModel[,] _cells = new SudokuCellModel[9, 9];
        private int[,] _solution = null;
        private Grid _rootGrid;
        private readonly Stack<UndoEntry> _undoStack = new();
        private readonly Stack<UndoEntry> _redoStack = new();
        private Button _undoButton;
        private Button _redoButton;

        // Visual cell containers: _cellBorders[row, col] is the outer Border for that cell
        private readonly Border[,] _cellBorders = new Border[9, 9];

        // For solved mode: the TextBlock inside each cell
        private readonly TextBlock[,] _solvedTextBlocks = new TextBlock[9, 9];

        // For candidate mode: the 9 mini TextBlocks per cell [row, col, candidateIndex 0-8]
        private readonly TextBlock[,,] _candidateTextBlocks = new TextBlock[9, 9, 9];

        // The inner Grid used for candidate layout per cell
        private readonly Grid[,] _candidateGrids = new Grid[9, 9];

        // -------------------------------------------------------------------------
        // Events
        // -------------------------------------------------------------------------

        /// <summary>
        /// Fired when a cell is clicked. Provides row, column, value, and cell type info.
        /// </summary>
        public event EventHandler<CellClickedEventArgs> CellClicked;

        // -------------------------------------------------------------------------
        // Dependency Properties
        // -------------------------------------------------------------------------

        public static readonly DependencyProperty CellWidthProperty =
            DependencyProperty.Register(nameof(CellWidth), typeof(double), typeof(SudokuGrid),
                new FrameworkPropertyMetadata(60.0, OnLayoutPropertyChanged));

        public static readonly DependencyProperty CellHeightProperty =
            DependencyProperty.Register(nameof(CellHeight), typeof(double), typeof(SudokuGrid),
                new FrameworkPropertyMetadata(60.0, OnLayoutPropertyChanged));

        public static readonly DependencyProperty CellBackgroundProperty =
            DependencyProperty.Register(nameof(CellBackground), typeof(Brush), typeof(SudokuGrid),
                new FrameworkPropertyMetadata(Brushes.White, OnVisualPropertyChanged));

        public static readonly DependencyProperty TextColorProperty =
            DependencyProperty.Register(nameof(TextColor), typeof(Brush), typeof(SudokuGrid),
                new FrameworkPropertyMetadata(Brushes.Black, OnVisualPropertyChanged));

        public static readonly DependencyProperty OuterBorderThicknessProperty =
            DependencyProperty.Register(nameof(OuterBorderThickness), typeof(double), typeof(SudokuGrid),
                new FrameworkPropertyMetadata(3.0, OnLayoutPropertyChanged));

        public static readonly DependencyProperty OuterBorderColorProperty =
            DependencyProperty.Register(nameof(OuterBorderColor), typeof(Brush), typeof(SudokuGrid),
                new FrameworkPropertyMetadata(Brushes.Black, OnVisualPropertyChanged));

        public static readonly DependencyProperty BoxBorderThicknessProperty =
            DependencyProperty.Register(nameof(BoxBorderThickness), typeof(double), typeof(SudokuGrid),
                new FrameworkPropertyMetadata(2.0, OnLayoutPropertyChanged));

        public static readonly DependencyProperty BoxBorderColorProperty =
            DependencyProperty.Register(nameof(BoxBorderColor), typeof(Brush), typeof(SudokuGrid),
                new FrameworkPropertyMetadata(Brushes.Black, OnVisualPropertyChanged));

        public static readonly DependencyProperty InnerBorderThicknessProperty =
            DependencyProperty.Register(nameof(InnerBorderThickness), typeof(double), typeof(SudokuGrid),
                new FrameworkPropertyMetadata(0.5, OnLayoutPropertyChanged));

        public static readonly DependencyProperty InnerBorderColorProperty =
            DependencyProperty.Register(nameof(InnerBorderColor), typeof(Brush), typeof(SudokuGrid),
                new FrameworkPropertyMetadata(Brushes.Gray, OnVisualPropertyChanged));

        public static readonly DependencyProperty SolvedFontSizeProperty =
            DependencyProperty.Register(nameof(SolvedFontSize), typeof(double), typeof(SudokuGrid),
                new FrameworkPropertyMetadata(28.0, OnVisualPropertyChanged));

        public static readonly DependencyProperty CandidateFontSizeProperty =
            DependencyProperty.Register(nameof(CandidateFontSize), typeof(double), typeof(SudokuGrid),
                new FrameworkPropertyMetadata(10.0, OnVisualPropertyChanged));

        public static readonly DependencyProperty MatchHighlightColorProperty =
            DependencyProperty.Register(nameof(MatchHighlightColor), typeof(Brush), typeof(SudokuGrid),
                new FrameworkPropertyMetadata(new SolidColorBrush(Color.FromRgb(255, 220, 100)), OnVisualPropertyChanged));

        public static readonly DependencyProperty VisibilityHighlightColorProperty =
            DependencyProperty.Register(nameof(VisibilityHighlightColor), typeof(Brush), typeof(SudokuGrid),
                new FrameworkPropertyMetadata(new SolidColorBrush(Color.FromRgb(100, 200, 220)), OnVisualPropertyChanged));

        public static readonly DependencyProperty ErrorColorProperty =
            DependencyProperty.Register(nameof(ErrorColor), typeof(Brush), typeof(SudokuGrid),
                new FrameworkPropertyMetadata(new SolidColorBrush(Color.FromRgb(255, 80, 80)), OnVisualPropertyChanged));

        public static readonly DependencyProperty MatchContextHighlightColorProperty =
    DependencyProperty.Register(nameof(MatchContextHighlightColor), typeof(Brush), typeof(SudokuGrid),
        new FrameworkPropertyMetadata(new SolidColorBrush(Color.FromArgb(80, 255, 220, 100)), OnVisualPropertyChanged));

        public static readonly DependencyProperty VisibilitySelectedColorProperty =
    DependencyProperty.Register(nameof(VisibilitySelectedColor), typeof(Brush), typeof(SudokuGrid),
        new FrameworkPropertyMetadata(new SolidColorBrush(Color.FromRgb(60, 160, 180)), OnVisualPropertyChanged));

        public static readonly DependencyProperty MatchCandidateHighlightColorProperty =
    DependencyProperty.Register(nameof(MatchCandidateHighlightColor), typeof(Brush), typeof(SudokuGrid),
        new FrameworkPropertyMetadata(new SolidColorBrush(Color.FromRgb(180, 230, 230)), OnVisualPropertyChanged));
        public static readonly DependencyProperty KeepUndoHistoryProperty =
    DependencyProperty.Register(nameof(KeepUndoHistory), typeof(bool), typeof(SudokuGrid),
        new FrameworkPropertyMetadata(false));

        public static readonly DependencyProperty ShowUndoControlsProperty =
            DependencyProperty.Register(nameof(ShowUndoControls), typeof(bool), typeof(SudokuGrid),
                new FrameworkPropertyMetadata(false, OnShowUndoControlsChanged));




        // -------------------------------------------------------------------------
        // CLR Property Wrappers
        // -------------------------------------------------------------------------

        public bool KeepUndoHistory
        {
            get => (bool)GetValue(KeepUndoHistoryProperty);
            set => SetValue(KeepUndoHistoryProperty, value);
        }

        public bool ShowUndoControls
        {
            get => (bool)GetValue(ShowUndoControlsProperty);
            set => SetValue(ShowUndoControlsProperty, value);
        }

        public double CellWidth
        {
            get => (double)GetValue(CellWidthProperty);
            set => SetValue(CellWidthProperty, value);
        }

        public double CellHeight
        {
            get => (double)GetValue(CellHeightProperty);
            set => SetValue(CellHeightProperty, value);
        }

        public Brush CellBackground
        {
            get => (Brush)GetValue(CellBackgroundProperty);
            set => SetValue(CellBackgroundProperty, value);
        }

        public Brush TextColor
        {
            get => (Brush)GetValue(TextColorProperty);
            set => SetValue(TextColorProperty, value);
        }

        public double OuterBorderThickness
        {
            get => (double)GetValue(OuterBorderThicknessProperty);
            set => SetValue(OuterBorderThicknessProperty, value);
        }

        public Brush OuterBorderColor
        {
            get => (Brush)GetValue(OuterBorderColorProperty);
            set => SetValue(OuterBorderColorProperty, value);
        }

        public double BoxBorderThickness
        {
            get => (double)GetValue(BoxBorderThicknessProperty);
            set => SetValue(BoxBorderThicknessProperty, value);
        }

        public Brush BoxBorderColor
        {
            get => (Brush)GetValue(BoxBorderColorProperty);
            set => SetValue(BoxBorderColorProperty, value);
        }

        public double InnerBorderThickness
        {
            get => (double)GetValue(InnerBorderThicknessProperty);
            set => SetValue(InnerBorderThicknessProperty, value);
        }

        public Brush InnerBorderColor
        {
            get => (Brush)GetValue(InnerBorderColorProperty);
            set => SetValue(InnerBorderColorProperty, value);
        }

        public double SolvedFontSize
        {
            get => (double)GetValue(SolvedFontSizeProperty);
            set => SetValue(SolvedFontSizeProperty, value);
        }

        public double CandidateFontSize
        {
            get => (double)GetValue(CandidateFontSizeProperty);
            set => SetValue(CandidateFontSizeProperty, value);
        }

        public Brush MatchHighlightColor
        {
            get => (Brush)GetValue(MatchHighlightColorProperty);
            set => SetValue(MatchHighlightColorProperty, value);
        }

        public Brush VisibilityHighlightColor
        {
            get => (Brush)GetValue(VisibilityHighlightColorProperty);
            set => SetValue(VisibilityHighlightColorProperty, value);
        }

        public Brush ErrorColor
        {
            get => (Brush)GetValue(ErrorColorProperty);
            set => SetValue(ErrorColorProperty, value);
        }

        public Brush MatchContextHighlightColor
        {
            get => (Brush)GetValue(MatchContextHighlightColorProperty);
            set => SetValue(MatchContextHighlightColorProperty, value);
        }

        public Brush VisibilitySelectedColor
        {
            get => (Brush)GetValue(VisibilitySelectedColorProperty);
            set => SetValue(VisibilitySelectedColorProperty, value);
        }

        public Brush MatchCandidateHighlightColor
        {
            get => (Brush)GetValue(MatchCandidateHighlightColorProperty);
            set => SetValue(MatchCandidateHighlightColorProperty, value);
        }

        // -------------------------------------------------------------------------
        // Constructor & Initialization
        // -------------------------------------------------------------------------

        static SudokuGrid()
        {
            DefaultStyleKeyProperty.OverrideMetadata(
                typeof(SudokuGrid),
                new FrameworkPropertyMetadata(typeof(SudokuGrid)));
        }

        public SudokuGrid()
        {
            // Initialize cell models
            for (int r = 0; r < 9; r++)
                for (int c = 0; c < 9; c++)
                    _cells[r, c] = new SudokuCellModel();

            Loaded += (s, e) => BuildGrid();
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            BuildGrid();
        }

        // -------------------------------------------------------------------------
        // Grid Construction
        // -------------------------------------------------------------------------

        private void BuildGrid()
        {
            // Create root border for outer boundary
            var outerBorder = new Border
            {
                BorderThickness = new Thickness(OuterBorderThickness),
                BorderBrush = OuterBorderColor
            };

            _rootGrid = new Grid();
            _rootGrid.MouseLeave += (s, e) => _isDragging = false;

            // Define 9 rows and 9 columns
            for (int i = 0; i < 9; i++)
            {
                _rootGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(CellHeight) });
                _rootGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(CellWidth) });
            }

            // Build each cell
            for (int row = 0; row < 9; row++)
            {
                for (int col = 0; col < 9; col++)
                {
                    var cellBorder = BuildCellBorder(row, col);
                    _cellBorders[row, col] = cellBorder;

                    int r = row, c = col;
                    cellBorder.MouseLeftButtonDown += (s, e) => OnCellMouseDown(r, c, e);
                    cellBorder.MouseEnter += (s, e) => OnCellMouseEnter(r, c, e);
                    cellBorder.MouseLeftButtonUp += (s, e) => OnCellMouseUp(r, c, e);
                    cellBorder.MouseRightButtonUp += (s, e) => OnCellRightClicked(r, c, e);

                    Grid.SetRow(cellBorder, row);
                    Grid.SetColumn(cellBorder, col);
                    _rootGrid.Children.Add(cellBorder);
                }
            }

            outerBorder.Child = _rootGrid;

            // Undo/Redo buttons
            _undoButton = BuildUndoRedoButton("↩ Undo", () => Undo());
            _redoButton = BuildUndoRedoButton("Redo ↪", () => Redo());

            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 6, 0, 0),
                Visibility = ShowUndoControls ? Visibility.Visible : Visibility.Collapsed
            };

            buttonPanel.Children.Add(_undoButton);
            buttonPanel.Children.Add(_redoButton);

            var rootStack = new StackPanel { Orientation = Orientation.Vertical };
            rootStack.Children.Add(outerBorder);
            rootStack.Children.Add(buttonPanel);

            this.Content = rootStack;
            UpdateUndoButtonStates();
        }

        private void EliminateNakedGroupCandidatesFromPeers(List<(int row, int col)> groupCells)
        {
            // Get the full set of candidates across all locked cells
            var groupCandidates = new HashSet<int>();
            foreach (var (r, c) in groupCells)
                foreach (var candidate in _cells[r, c].Candidates)
                    groupCandidates.Add(candidate);

            // Determine shared visibility — cells must be in same row, col, or box
            // We eliminate from each peer zone that ALL group cells share
            bool allSameRow = groupCells.All(c => c.row == groupCells[0].row);
            bool allSameCol = groupCells.All(c => c.col == groupCells[0].col);
            bool allSameBox = groupCells.All(c =>
                (c.row / 3) == (groupCells[0].row / 3) &&
                (c.col / 3) == (groupCells[0].col / 3));

            for (int r = 0; r < 9; r++)
            {
                for (int c = 0; c < 9; c++)
                {
                    // Skip the locked cells themselves
                    if (groupCells.Contains((r, c))) continue;

                    // Skip givens and non-candidate cells
                    if (_cells[r, c].IsGiven) continue;
                    if (!_cells[r, c].IsInCandidateMode) continue;

                    bool inSameRow = allSameRow && r == groupCells[0].row;
                    bool inSameCol = allSameCol && c == groupCells[0].col;
                    bool inSameBox = allSameBox &&
                                     (r / 3) == (groupCells[0].row / 3) &&
                                     (c / 3) == (groupCells[0].col / 3);

                    if (inSameRow || inSameCol || inSameBox)
                    {
                        bool changed = false;
                        foreach (var candidate in groupCandidates)
                        {
                            if (_cells[r, c].Candidates.Contains(candidate))
                            {
                                if (_cells[r, c].IsNakedPair)
                                {
                                    var pairCandidates = new HashSet<int>(_cells[r, c].Candidates);
                                    _cells[r, c].IsNakedPair = false;
                                    _cells[r, c].Candidates.Remove(candidate);
                                    ResetNakedGroupPartners(r, c, pairCandidates);
                                }
                                else
                                {
                                    _cells[r, c].Candidates.Remove(candidate);
                                }
                                changed = true;
                            }
                        }
                        if (changed) RefreshCell(r, c);
                    }
                }
            }
        }

        private Button BuildUndoRedoButton(string label, Action onClick)
        {
            var btn = new Button
            {
                Margin = new Thickness(4, 0, 0, 0),
                Cursor = Cursors.Hand,
                IsEnabled = false
            };

            var template = new ControlTemplate(typeof(Button));
            var border = new FrameworkElementFactory(typeof(Border));
            border.SetValue(Border.CornerRadiusProperty, new CornerRadius(6));
            border.SetValue(Border.BorderThicknessProperty, new Thickness(2));
            border.SetValue(Border.PaddingProperty, new Thickness(12, 6, 12, 6));
            border.Name = "Bd";

            var text = new FrameworkElementFactory(typeof(TextBlock));
            text.SetValue(TextBlock.TextProperty, label);
            text.SetValue(TextBlock.FontSizeProperty, 13.0);
            text.SetValue(TextBlock.FontWeightProperty, FontWeights.SemiBold);
            text.SetValue(TextBlock.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            text.SetValue(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Center);
            text.Name = "Lbl";

            border.AppendChild(text);
            template.VisualTree = border;

            // Enabled state trigger
            var enabledTrigger = new Trigger { Property = Button.IsEnabledProperty, Value = true };
            enabledTrigger.Setters.Add(new Setter(Border.BackgroundProperty,
                Brushes.White, "Bd"));
            enabledTrigger.Setters.Add(new Setter(Border.BorderBrushProperty,
                new SolidColorBrush(Color.FromRgb(75, 163, 195)), "Bd"));
            enabledTrigger.Setters.Add(new Setter(TextBlock.ForegroundProperty,
                new SolidColorBrush(Color.FromRgb(75, 163, 195)), "Lbl"));
            template.Triggers.Add(enabledTrigger);

            // Disabled state trigger
            var disabledTrigger = new Trigger { Property = Button.IsEnabledProperty, Value = false };
            disabledTrigger.Setters.Add(new Setter(Border.BackgroundProperty,
                new SolidColorBrush(Color.FromRgb(224, 224, 224)), "Bd"));
            disabledTrigger.Setters.Add(new Setter(Border.BorderBrushProperty,
                new SolidColorBrush(Color.FromRgb(192, 192, 192)), "Bd"));
            disabledTrigger.Setters.Add(new Setter(TextBlock.ForegroundProperty,
                new SolidColorBrush(Color.FromRgb(160, 160, 160)), "Lbl"));
            template.Triggers.Add(disabledTrigger);

            // Hover trigger
            var hoverTrigger = new Trigger { Property = Button.IsMouseOverProperty, Value = true };
            hoverTrigger.Setters.Add(new Setter(Border.BackgroundProperty,
                new SolidColorBrush(Color.FromRgb(75, 163, 195)), "Bd"));
            hoverTrigger.Setters.Add(new Setter(TextBlock.ForegroundProperty,
                Brushes.White, "Lbl"));
            template.Triggers.Add(hoverTrigger);

            // Pressed trigger
            var pressedTrigger = new Trigger { Property = Button.IsPressedProperty, Value = true };
            pressedTrigger.Setters.Add(new Setter(Border.BackgroundProperty,
                new SolidColorBrush(Color.FromRgb(23, 86, 118)), "Bd"));
            pressedTrigger.Setters.Add(new Setter(Border.BorderBrushProperty,
                new SolidColorBrush(Color.FromRgb(23, 86, 118)), "Bd"));
            pressedTrigger.Setters.Add(new Setter(TextBlock.ForegroundProperty,
                Brushes.White, "Lbl"));
            template.Triggers.Add(pressedTrigger);

            btn.Template = template;
            btn.Click += (s, e) => onClick();
            return btn;
        }

        private UndoEntry _currentEntry;

        private void BeginUndoEntry(params (int row, int col)[] cells)
        {
            if (!KeepUndoHistory) return;
            _currentEntry = new UndoEntry();
            foreach (var (r, c) in cells)
                _currentEntry.Before.Add(CellSnapshot.Capture(_cells[r, c], r, c));
        }

        private void BeginUndoEntryAllChanged(List<(int row, int col)> cells)
        {
            if (!KeepUndoHistory) return;
            _currentEntry = new UndoEntry();
            foreach (var (r, c) in cells)
                _currentEntry.Before.Add(CellSnapshot.Capture(_cells[r, c], r, c));
        }

        private void CommitUndoEntry(params (int row, int col)[] cells)
        {
            if (!KeepUndoHistory || _currentEntry == null) return;
            foreach (var (r, c) in cells)
                _currentEntry.After.Add(CellSnapshot.Capture(_cells[r, c], r, c));

            // Only push if something actually changed
            bool changed = false;
            for (int i = 0; i < _currentEntry.Before.Count; i++)
            {
                var before = _currentEntry.Before[i];
                var after = _currentEntry.After.FirstOrDefault(a => a.Row == before.Row && a.Col == before.Col);
                if (after == null) continue;
                if (before.Value != after.Value || before.IsNakedPair != after.IsNakedPair ||
                    before.IsError != after.IsError || !before.Candidates.SetEquals(after.Candidates))
                {
                    changed = true;
                    break;
                }
            }

            if (changed)
            {
                _undoStack.Push(_currentEntry);
                _redoStack.Clear();
            }

            _currentEntry = null;
            UpdateUndoButtonStates();
        }

        public void Undo()
        {
            if (_undoStack.Count == 0) return;
            var entry = _undoStack.Pop();
            ApplySnapshots(entry.Before);
            _redoStack.Push(entry);
            UpdateUndoButtonStates();
        }

        public void Redo()
        {
            if (_redoStack.Count == 0) return;
            var entry = _redoStack.Pop();
            ApplySnapshots(entry.After);
            _undoStack.Push(entry);
            UpdateUndoButtonStates();
        }

        private void ApplySnapshots(List<CellSnapshot> snapshots)
        {
            foreach (var snap in snapshots)
            {
                var cell = _cells[snap.Row, snap.Col];
                cell.Value = snap.Value;
                cell.IsGiven = snap.IsGiven;
                cell.IsNakedPair = snap.IsNakedPair;
                cell.IsError = snap.IsError;
                cell.Candidates = new HashSet<int>(snap.Candidates);
                RefreshCell(snap.Row, snap.Col);
            }
            RefreshAllCellBackgrounds();
        }

        private void UpdateUndoButtonStates()
        {
            if (_undoButton != null)
                _undoButton.IsEnabled = _undoStack.Count > 0;
            if (_redoButton != null)
                _redoButton.IsEnabled = _redoStack.Count > 0;
        }

        private void UpdateUndoControlsVisibility()
        {
            // Find the button panel — it's the second child of the root StackPanel
            if (this.Content is StackPanel rootStack && rootStack.Children.Count > 1)
                rootStack.Children[1].Visibility = ShowUndoControls
                    ? Visibility.Visible : Visibility.Collapsed;
        }

        private void CommitUndoEntryForCells(List<(int row, int col)> cells)
        {
            if (!KeepUndoHistory || _currentEntry == null) return;
            foreach (var (r, c) in cells)
                _currentEntry.After.Add(CellSnapshot.Capture(_cells[r, c], r, c));

            _undoStack.Push(_currentEntry);
            _redoStack.Clear();
            _currentEntry = null;
            UpdateUndoButtonStates();
        }

        private Border BuildCellBorder(int row, int col)
        {
            var thickness = GetCellBorderThickness(row, col);

            var cellBorder = new Border
            {
                BorderThickness = thickness,
                BorderBrush = GetCellBorderBrush(row, col),
                Background = CellBackground,
                Width = CellWidth,
                Height = CellHeight,
                Cursor = Cursors.Hand
            };

            // Inner container to hold either solved text or candidate grid
            var innerContainer = new Grid();

            // --- Solved TextBlock ---
            var solvedText = new TextBlock
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                TextAlignment = TextAlignment.Center,
                FontSize = SolvedFontSize,
                Foreground = TextColor,
                FontWeight = FontWeights.Normal,
                Visibility = Visibility.Collapsed
            };
            _solvedTextBlocks[row, col] = solvedText;
            innerContainer.Children.Add(solvedText);

            // --- Candidate Grid (3x3 mini-grid) ---
            var candidateGrid = new Grid
            {
                Visibility = Visibility.Collapsed,
                Margin = new Thickness(2)
            };

            for (int i = 0; i < 3; i++)
            {
                candidateGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
                candidateGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            }

            for (int miniRow = 0; miniRow < 3; miniRow++)
            {
                for (int miniCol = 0; miniCol < 3; miniCol++)
                {
                    int candidateIndex = miniRow * 3 + miniCol; // 0-8
                    int candidateValue = candidateIndex + 1;    // 1-9

                    var miniText = new TextBlock
                    {
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center,
                        TextAlignment = TextAlignment.Center,
                        FontSize = CandidateFontSize,
                        Foreground = TextColor,
                        Text = string.Empty
                    };

                    _candidateTextBlocks[row, col, candidateIndex] = miniText;

                    Grid.SetRow(miniText, miniRow);
                    Grid.SetColumn(miniText, miniCol);
                    candidateGrid.Children.Add(_candidateTextBlocks[row, col, candidateIndex]);
                }
            }

            _candidateGrids[row, col] = candidateGrid;
            innerContainer.Children.Add(candidateGrid);

            // Naked Pair display
            var nakedPairText = new TextBlock
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                TextAlignment = TextAlignment.Center,
                FontSize = CandidateFontSize ,
                Foreground = TextColor,
                FontWeight = FontWeights.Regular,
                TextWrapping = TextWrapping.Wrap
            };

            var nakedPairBorder = new Border
            {
                BorderBrush = new SolidColorBrush(Color.FromRgb(200, 200, 200)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(3),
                Padding = new Thickness(4),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Visibility = Visibility.Collapsed,
                Child = nakedPairText
            };

            _nakedPairBorders[row, col] = nakedPairBorder;
            _nakedPairTextBlocks[row, col] = nakedPairText;
            innerContainer.Children.Add(nakedPairBorder);

            cellBorder.Child = innerContainer;

            

            return cellBorder;
        }

        private static void OnShowUndoControlsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is SudokuGrid grid)
                grid.UpdateUndoControlsVisibility();
        }

        private void OnCellRightClicked(int row, int col, MouseButtonEventArgs e)
        {
            var selected = _selectedCells.ToList();

            // Must have at least 2 cells selected
            if (selected.Count < 2) return;

            // Clicked cell must be in selection
            if (!selected.Contains((row, col))) return;

            // Filter out solved and given cells — only work with candidate cells
            var candidateSelected = selected
                .Where(s => !_cells[s.row, s.col].IsGiven && _cells[s.row, s.col].Value == 0)
                .ToList();

            if (candidateSelected.Count < 2) return;

            var selectedCellModels = candidateSelected.Select(s => _cells[s.row, s.col]).ToList();

            // Check if all candidate cells are locked
            bool allLocked = selectedCellModels.All(c => c.IsNakedPair);

            // Capture selected cells + all potential peer cells for undo
            var allAffected = new List<(int row, int col)>();
            allAffected.AddRange(candidateSelected.Select(s => (s.row, s.col)));

            var groupCandidates = new HashSet<int>();
            foreach (var s in candidateSelected)
                foreach (var candidate in _cells[s.row, s.col].Candidates)
                    groupCandidates.Add(candidate);

            bool willBeAllSameRow = candidateSelected.All(c => c.row == candidateSelected[0].row);
            bool willBeAllSameCol = candidateSelected.All(c => c.col == candidateSelected[0].col);
            bool willBeAllSameBox = candidateSelected.All(c =>
                (c.row / 3) == (candidateSelected[0].row / 3) &&
                (c.col / 3) == (candidateSelected[0].col / 3));

            for (int r = 0; r < 9; r++)
            {
                for (int c = 0; c < 9; c++)
                {
                    if (candidateSelected.Contains((r, c))) continue;
                    if (_cells[r, c].IsGiven || !_cells[r, c].IsInCandidateMode) continue;

                    bool inSameRow = willBeAllSameRow && r == candidateSelected[0].row;
                    bool inSameCol = willBeAllSameCol && c == candidateSelected[0].col;
                    bool inSameBox = willBeAllSameBox &&
                                     (r / 3) == (candidateSelected[0].row / 3) &&
                                     (c / 3) == (candidateSelected[0].col / 3);

                    if ((inSameRow || inSameCol || inSameBox) &&
                        _cells[r, c].Candidates.Any(cand => groupCandidates.Contains(cand)))
                        allAffected.Add((r, c));
                }
            }

            var affectedForUndo = allAffected;
            BeginUndoEntryAllChanged(affectedForUndo);

            if (allLocked)
            {
                // Unlock all candidate cells
                foreach (var (r, c) in candidateSelected)
                {
                    _cells[r, c].IsNakedPair = false;
                    RefreshCell(r, c);
                }

                CommitUndoEntryForCells(affectedForUndo);
                e.Handled = true;
                return;
            }

            // All candidate cells must be in candidate mode
            if (!selectedCellModels.All(c => c.IsInCandidateMode))
            {
                _currentEntry = null;
                return;
            }

            // Unique candidate count must match candidate cell count
            if (groupCandidates.Count != candidateSelected.Count)
            {
                _currentEntry = null;
                return;
            }

            // Lock all candidate cells
            foreach (var (r, c) in candidateSelected)
            {
                _cells[r, c].IsNakedPair = true;
                RefreshCell(r, c);
            }

            // Eliminate matching candidates from peers
            EliminateNakedGroupCandidatesFromPeers(candidateSelected.Select(s => (s.row, s.col)).ToList());

            CommitUndoEntryForCells(affectedForUndo);
            e.Handled = true;
        }

        private bool _isDraggingWithCtrl = false;

        public BoardState ExportState()
        {
            var state = new BoardState();

            state.Cells = new CellState[9][];

            for (int i = 0; i < 9; i++)
            {
                state.Cells[i] = new CellState[9];
            }

            for (int r = 0; r < 9; r++)
            {
                for (int c = 0; c < 9; c++)
                {
                    state.Cells[r][c] = new CellState
                    {
                        Value = _cells[r, c].Value,
                        IsGiven = _cells[r, c].IsGiven,
                        IsNakedPair = _cells[r, c].IsNakedPair,
                        IsError = _cells[r, c].IsError,
                        Candidates = _cells[r, c].Candidates.ToList()
                    };
                }
            }

            return state;
        }

        public void ImportState(BoardState state)
        {
            for (int r = 0; r < 9; r++)
            {
                for (int c = 0; c < 9; c++)
                {
                    var cs = state.Cells[r][c];
                    _cells[r, c].Value = cs.Value;
                    _cells[r, c].IsGiven = cs.IsGiven;
                    _cells[r, c].IsNakedPair = cs.IsNakedPair;
                    _cells[r, c].IsError = cs.IsError;
                    _cells[r, c].Candidates = new HashSet<int>(cs.Candidates);
                    RefreshCell(r, c);
                }
            }

            _undoStack.Clear();
            _redoStack.Clear();
            UpdateUndoButtonStates();
            RefreshAllCellBackgrounds();
        }

        private void OnCellMouseDown(int row, int col, MouseButtonEventArgs e)
        {
            bool isCtrl = Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl);

            if (isCtrl)
            {
                // Ctrl+click: toggle individual cell
                if (_selectedCells.Contains((row, col)))
                    _selectedCells.Remove((row, col));
                else
                    _selectedCells.Add((row, col));

                _isDragging = true;
                _isDraggingWithCtrl = true; // remember ctrl was held at drag start
            }
            else
            {
                // Regular click: clear and select just this cell
                _selectedCells.Clear();
                _selectedCells.Add((row, col));
                _isDragging = true;
                _isDraggingWithCtrl = false;
            }

            ApplySelectionHighlight();
            FireCellClicked(row, col);
            e.Handled = true;
        }

        private void OnCellMouseEnter(int row, int col, MouseEventArgs e)
        {
            if (!_isDragging) return;
            if (_selectedCells.Contains((row, col))) return;

            _selectedCells.Add((row, col));
            ApplySelectionHighlight();
        }

        private void OnCellMouseUp(int row, int col, MouseButtonEventArgs e)
        {
            _isDragging = false;
            _isDraggingWithCtrl = false;
        }

        private void FireCellClicked(int row, int col)
        {
            OnCellClicked(row, col);
            var cell = _cells[row, col];
            CellClicked?.Invoke(this, new CellClickedEventArgs(
                row, col, cell.Value, cell.IsGiven, cell.IsInCandidateMode));
        }

        private void ApplySelectionHighlight()
        {
            // Clear all non-error highlights
            for (int r = 0; r < 9; r++)
                for (int c = 0; c < 9; c++)
                {
                    _cells[r, c].IsHighlighted = false;
                    _cells[r, c].HighlightType = HighlightType.None;
                }

            // Apply selection highlight to all selected cells
            foreach (var (r, c) in _selectedCells)
            {
                _cells[r, c].IsHighlighted = true;
                _cells[r, c].HighlightType = HighlightType.VisibilitySelected;
            }

            RefreshAllCellBackgrounds();
        }

        public HashSet<(int row, int col)> GetSelectedCells()
        {
            return new HashSet<(int, int)>(_selectedCells);
        }

        public bool IsMultiSelect => _selectedCells.Count > 1;

        private Thickness GetCellBorderThickness(int row, int col)
        {
            double inner = InnerBorderThickness;
            double box = BoxBorderThickness;

            // Left border
            double left = (col % 3 == 0) ? 0 : inner; // box left handled by outer or box border
            // Top border
            double top = (row % 3 == 0) ? 0 : inner;
            // Right border
            double right = (col % 3 == 2) ? box : inner;
            // Bottom border
            double bottom = (row % 3 == 2) ? box : inner;

            // The leftmost and topmost cell of each box gets a box border on left/top
            if (col % 3 == 0) left = box;
            if (row % 3 == 0) top = box;

            // The outer border is drawn by the outerBorder control, so suppress box borders on edges
            if (col == 0) left = 0;
            if (row == 0) top = 0;
            if (col == 8) right = 0;
            if (row == 8) bottom = 0;

            // Re-add inner borders for non-box-edge interior cells
            if (col > 0 && col % 3 != 0) left = inner;
            if (row > 0 && row % 3 != 0) top = inner;

            return new Thickness(left, top, right, bottom);
        }

        private Brush GetCellBorderBrush(int row, int col)
        {
            // Use box color for box boundaries, inner color for inner lines
            bool isBoxEdge = (row % 3 == 0) || (col % 3 == 0) || (row % 3 == 2) || (col % 3 == 2);
            return isBoxEdge ? BoxBorderColor : InnerBorderColor;
        }

        // -------------------------------------------------------------------------
        // Public API
        // -------------------------------------------------------------------------

        /// <summary>
        /// Initializes the board with a SudokuPuzzle (givens + optional solution).
        /// Clears all existing state before applying.
        /// </summary>
        public void Initialize(SudokuPuzzle puzzle)
        {
            if (puzzle == null) throw new ArgumentNullException(nameof(puzzle));

            _solution = puzzle.Solution;

            for (int r = 0; r < 9; r++)
            {
                for (int c = 0; c < 9; c++)
                {
                    _cells[r, c].Clear();
                    int given = puzzle.Givens[r, c];
                    if (given > 0)
                    {
                        _cells[r, c].Value = given;
                        _cells[r, c].IsGiven = true;
                    }
                }
            }

            _undoStack.Clear();
            _redoStack.Clear();
            _selectedCells.Clear();
            _currentEntry = null;
            UpdateUndoButtonStates();

            ClearHighlights();
            RefreshAllCells();
        }

        public void SetCandidatesBulk(List<(int row, int col, HashSet<int> candidates)> changes)
        {
            if (changes == null || changes.Count == 0) return;

            var affected = changes.Select(c => (c.row, c.col)).ToList();
            BeginUndoEntryAllChanged(affected);

            foreach (var (row, col, candidates) in changes)
            {
                if (_cells[row, col].IsGiven) continue;
                if (_cells[row, col].IsNakedPair) continue;

                _cells[row, col].Value = 0;
                _cells[row, col].IsError = false;
                _cells[row, col].Candidates = new HashSet<int>(candidates);
                RefreshCell(row, col);
            }

            CommitUndoEntryForCells(affected);
        }

        /// <summary>
        /// Sets a player-entered solved value for a cell.
        /// Ignored if the cell is a given. Triggers validation if solution is available.
        /// </summary>
        public void SetCell(int row, int col, int value)
        {
            ValidateCoords(row, col);
            if (_cells[row, col].IsGiven) return;

            // Capture all affected cells for undo
            var affected = GetPeerCellsWithCandidate(row, col, value);
            affected.Insert(0, (row, col));
            BeginUndoEntryAllChanged(affected);

            // Just clear naked pair on the solved cell itself — don't reset partners
            if (_cells[row, col].IsNakedPair)
                _cells[row, col].IsNakedPair = false;

            _cells[row, col].Value = value;
            _cells[row, col].Candidates = new HashSet<int>();
            _cells[row, col].IsError = false;

            if (value > 0)
            {
                ValidateCell(row, col);
                RemoveCandidateFromPeers(row, col, value);
            }

            RefreshCell(row, col);

            CommitUndoEntryForCells(affected);

            if (value > 0)
            {
                _selectedCells.Clear();
                _selectedCells.Add((row, col));
                OnCellClicked(row, col);
                RefreshAllCellBackgrounds();
            }
        }

        private List<(int row, int col)> GetPeerCellsWithCandidate(int row, int col, int value)
        {
            var peers = new List<(int, int)>();
            int boxStartRow = (row / 3) * 3;
            int boxStartCol = (col / 3) * 3;

            for (int r = 0; r < 9; r++)
                for (int c = 0; c < 9; c++)
                {
                    if (r == row && c == col) continue;
                    bool inRow = r == row;
                    bool inCol = c == col;
                    bool inBox = r >= boxStartRow && r < boxStartRow + 3 &&
                                 c >= boxStartCol && c < boxStartCol + 3;
                    if ((inRow || inCol || inBox) && _cells[r, c].IsInCandidateMode &&
                        _cells[r, c].Candidates.Contains(value))
                        peers.Add((r, c));
                }
            return peers;
        }

        private void ResetNakedGroupPartners(int solvedRow, int solvedCol, HashSet<int> groupCandidates)
        {
            for (int r = 0; r < 9; r++)
                for (int c = 0; c < 9; c++)
                {
                    if (r == solvedRow && c == solvedCol) continue;
                    if (_cells[r, c].IsNakedPair && _cells[r, c].Candidates.IsSubsetOf(groupCandidates))
                    {
                        _cells[r, c].IsNakedPair = false;
                        RefreshCell(r, c);
                    }
                }
        }

        private void RemoveCandidateFromPeers(int row, int col, int value)
        {
            int boxStartRow = (row / 3) * 3;
            int boxStartCol = (col / 3) * 3;

            for (int r = 0; r < 9; r++)
            {
                for (int c = 0; c < 9; c++)
                {
                    if (r == row && c == col) continue;

                    bool inRow = r == row;
                    bool inCol = c == col;
                    bool inBox = r >= boxStartRow && r < boxStartRow + 3 &&
                                 c >= boxStartCol && c < boxStartCol + 3;

                    if (inRow || inCol || inBox)
                    {
                        var cell = _cells[r, c];
                        if ((cell.IsInCandidateMode || cell.IsNakedPair) && cell.Candidates.Contains(value))
                        {
                            cell.Candidates.Remove(value);
                            RefreshCell(r, c);
                        }
                    }
                }
            }
        }

        public void SetCandidatesOnLocked(List<(int row, int col, HashSet<int> candidates)> changes)
        {
            if (changes == null || changes.Count == 0) return;

            var affected = changes.Select(c => (c.row, c.col)).ToList();
            BeginUndoEntryAllChanged(affected);

            foreach (var (row, col, candidates) in changes)
            {
                if (_cells[row, col].IsGiven) continue;
                _cells[row, col].Candidates = new HashSet<int>(candidates);
                RefreshCell(row, col);
            }

            CommitUndoEntryForCells(affected);
        }

        /// <summary>
        /// Sets the candidates for a cell. Clears the solved value.
        /// Ignored if the cell is a given.
        /// </summary>
        public void SetCandidates(int row, int col, IEnumerable<int> candidates)
        {
            ValidateCoords(row, col);
            if (_cells[row, col].IsGiven) return;
            if (_cells[row, col].IsNakedPair) return;

            BeginUndoEntry((row, col));
            _cells[row, col].Value = 0;
            _cells[row, col].IsError = false;
            _cells[row, col].Candidates = new HashSet<int>(candidates);
            RefreshCell(row, col);
            CommitUndoEntry((row, col));
        }

        /// <summary>
        /// Clears a cell's value and candidates. Ignored if the cell is a given.
        /// </summary>
        public void ClearCell(int row, int col)
        {
            ValidateCoords(row, col);
            if (_cells[row, col].IsGiven) return;

            BeginUndoEntry((row, col));
            _cells[row, col].Value = 0;
            _cells[row, col].Candidates = new HashSet<int>();
            _cells[row, col].IsError = false;
            RefreshCell(row, col);
            CommitUndoEntry((row, col));
        }

        /// <summary>
        /// Sets a per-cell font size override for the solved number display.
        /// Pass 0 to revert to the global SolvedFontSize.
        /// </summary>
        public void SetFontSize(int row, int col, double fontSize)
        {
            ValidateCoords(row, col);
            _cells[row, col].FontSizeOverride = fontSize;
            RefreshCell(row, col);
        }

        /// <summary>
        /// Clears all highlights from every cell.
        /// </summary>
        public void ClearHighlights()
        {
            for (int r = 0; r < 9; r++)
                for (int c = 0; c < 9; c++)
                {
                    _cells[r, c].IsHighlighted = false;
                    _cells[r, c].HighlightType = HighlightType.None;
                }

            RefreshAllCellBackgrounds();
        }

        // -------------------------------------------------------------------------
        // Click Handling & Highlight Logic
        // -------------------------------------------------------------------------

        private void OnCellClicked(int row, int col)
        {
            // Only apply match/visibility highlights for single cell selection
            if (_selectedCells.Count != 1) return;

            var cell = _cells[row, col];

            if (cell.IsSolved || cell.IsGiven)
                ApplyMatchHighlight(row, col, cell.Value);
            else
                ApplyVisibilityHighlight(row, col);

            RefreshAllCellBackgrounds();
        }

        private void ApplyMatchHighlight(int clickedRow, int clickedCol, int value)
        {
            if (value <= 0) return;

            for (int r = 0; r < 9; r++)
            {
                for (int c = 0; c < 9; c++)
                {
                    var cell = _cells[r, c];

                    if (cell.Value == value)
                    {
                        // Solved or given cell with matching value
                        cell.IsHighlighted = true;
                        cell.HighlightType = HighlightType.Match;
                    }
                    else if (cell.IsInCandidateMode && cell.Candidates.Contains(value))
                    {
                        // Candidate cell containing the matching value
                        cell.IsHighlighted = true;
                        cell.HighlightType = HighlightType.MatchCandidate;
                    }
                    else if (r == clickedRow || c == clickedCol)
                    {
                        cell.IsHighlighted = true;
                        cell.HighlightType = HighlightType.MatchContext;
                    }
                }
            }
        }

        private void ApplyVisibilityHighlight(int clickedRow, int clickedCol)
        {
            int boxStartRow = (clickedRow / 3) * 3;
            int boxStartCol = (clickedCol / 3) * 3;

            for (int r = 0; r < 9; r++)
            {
                for (int c = 0; c < 9; c++)
                {
                    bool isClickedCell = r == clickedRow && c == clickedCol;
                    bool inRow = r == clickedRow;
                    bool inCol = c == clickedCol;
                    bool inBox = r >= boxStartRow && r < boxStartRow + 3 &&
                                 c >= boxStartCol && c < boxStartCol + 3;

                    if (isClickedCell)
                    {
                        _cells[r, c].IsHighlighted = true;
                        _cells[r, c].HighlightType = HighlightType.VisibilitySelected;
                    }
                    else if (inRow || inCol || inBox)
                    {
                        _cells[r, c].IsHighlighted = true;
                        _cells[r, c].HighlightType = HighlightType.Visibility;
                    }
                }
            }
        }

        // -------------------------------------------------------------------------
        // Validation
        // -------------------------------------------------------------------------

        private void ValidateCell(int row, int col)
        {
            if (_solution == null) return;

            var cell = _cells[row, col];
            if (cell.Value <= 0 || cell.IsGiven) return;

            int expected = _solution[row, col];
            cell.IsError = (expected > 0 && cell.Value != expected);
        }

        // -------------------------------------------------------------------------
        // Visual Refresh
        // -------------------------------------------------------------------------

        private void RefreshAllCells()
        {
            for (int r = 0; r < 9; r++)
                for (int c = 0; c < 9; c++)
                    RefreshCell(r, c);
        }

        private void RefreshCell(int row, int col)
        {
            if (_cellBorders[row, col] == null) return;

            var cell = _cells[row, col];
            var border = _cellBorders[row, col];
            var solvedText = _solvedTextBlocks[row, col];
            var candidateGrid = _candidateGrids[row, col];

            // --- Background ---
            RefreshCellBackground(row, col);

            // --- Mode: Solved or Given ---
            if (cell.IsSolved || cell.IsGiven)
            {
                solvedText.Visibility = Visibility.Visible;
                candidateGrid.Visibility = Visibility.Collapsed;
                _nakedPairBorders[row, col].Visibility = Visibility.Collapsed;
                solvedText.Text = cell.Value.ToString();
                solvedText.Foreground = TextColor;
                solvedText.FontSize = cell.FontSizeOverride > 0
                    ? cell.FontSizeOverride
                    : SolvedFontSize;
                solvedText.FontWeight = cell.IsGiven ? FontWeights.Bold : FontWeights.Bold;
            }
            // --- Mode: Candidate ---
            else if (cell.IsInCandidateMode)
            {
                solvedText.Visibility = Visibility.Collapsed;
                candidateGrid.Visibility = Visibility.Visible;

                for (int i = 0; i < 9; i++)
                {
                    int val = i + 1;
                    var miniText = _candidateTextBlocks[row, col, i];
                    miniText.Text = cell.Candidates.Contains(val) ? val.ToString() : string.Empty;
                    miniText.FontSize = CandidateFontSize;
                    miniText.Foreground = TextColor;
                }
            }



            // --- Mode: Empty ---
            else
            {
                solvedText.Visibility = Visibility.Collapsed;
                candidateGrid.Visibility = Visibility.Collapsed;
            }

            if (cell.IsNakedPair)
            {
                solvedText.Visibility = Visibility.Collapsed;
                candidateGrid.Visibility = Visibility.Collapsed;
                _nakedPairBorders[row, col].Visibility = Visibility.Visible;

                var sorted = cell.Candidates.OrderBy(x => x);
                _nakedPairTextBlocks[row, col].Text = string.Join(" ", sorted);
                _nakedPairTextBlocks[row, col].FontSize = CandidateFontSize ;
                _nakedPairTextBlocks[row, col].Foreground = TextColor;
            }
            else
            {
                _nakedPairBorders[row, col].Visibility = Visibility.Collapsed;
            }
        }

        private void RefreshCellBackground(int row, int col)
        {
            if (_cellBorders[row, col] == null) return;

            var cell = _cells[row, col];
            var border = _cellBorders[row, col];

            if (cell.IsError)
                border.Background = ErrorColor;
            else if (cell.IsHighlighted)
            {
                border.Background = cell.HighlightType switch
                {
                    HighlightType.Match => MatchHighlightColor,
                    HighlightType.MatchCandidate => MatchCandidateHighlightColor,
                    HighlightType.MatchContext => MatchContextHighlightColor,
                    HighlightType.Visibility => VisibilityHighlightColor,
                    HighlightType.VisibilitySelected => VisibilitySelectedColor,
                    _ => CellBackground
                };
            }
            else
                border.Background = CellBackground;
        }

        private void RefreshAllCellBackgrounds()
        {
            for (int r = 0; r < 9; r++)
                for (int c = 0; c < 9; c++)
                    RefreshCellBackground(r, c);
        }

        // -------------------------------------------------------------------------
        // Property Change Callbacks
        // -------------------------------------------------------------------------

        private static void OnLayoutPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is SudokuGrid grid && grid._rootGrid != null)
            {
                // Rebuild the grid from scratch on layout changes
                grid.Content = null;
                grid.BuildGrid();
                grid.RefreshAllCells();
            }
        }

        private static void OnVisualPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is SudokuGrid grid && grid._rootGrid != null)
            {
                grid.RefreshAllCells();
                grid.RefreshAllCellBackgrounds();
            }
        }

        // -------------------------------------------------------------------------
        // Helpers
        // -------------------------------------------------------------------------

        private static void ValidateCoords(int row, int col)
        {
            if (row < 0 || row > 8) throw new ArgumentOutOfRangeException(nameof(row), "Row must be 0-8.");
            if (col < 0 || col > 8) throw new ArgumentOutOfRangeException(nameof(col), "Column must be 0-8.");
        }

        public HashSet<int> GetCandidates(int row, int col)
        {
            ValidateCoords(row, col);
            return new HashSet<int>(_cells[row, col].Candidates);
        }

        public int GetCellValue(int row, int col)
        {
            ValidateCoords(row, col);
            return _cells[row, col].Value;
        }

        public bool IsGiven(int row, int col)
        {
            ValidateCoords(row, col);
            return _cells[row, col].IsGiven;
        }

        public bool IsNumberComplete(int value)
        {
            int count = 0;
            for (int r = 0; r < 9; r++)
                for (int c = 0; c < 9; c++)
                    if (_cells[r, c].Value == value && !_cells[r, c].IsError) count++;
            return count == 9;
        }

        public void LockCompletedNumber(int value)
        {
            for (int r = 0; r < 9; r++)
                for (int c = 0; c < 9; c++)
                {
                    if (_cells[r, c].Value == value && _cells[r, c].IsError)
                    {
                        // Clear the invalid duplicate and its highlight
                        _cells[r, c].Value = 0;
                        _cells[r, c].IsError = false;
                        _cells[r, c].IsHighlighted = false;
                        _cells[r, c].HighlightType = HighlightType.None;
                        RefreshCell(r, c);
                    }
                    else if (_cells[r, c].Value == value && !_cells[r, c].IsGiven)
                    {
                        // Lock the valid ones
                        _cells[r, c].IsGiven = true;
                        RefreshCell(r, c);
                    }
                }
        }

        public bool IsGridComplete()
        {
            for (int r = 0; r < 9; r++)
                for (int c = 0; c < 9; c++)
                    if (_cells[r, c].Value == 0 || _cells[r, c].IsError)
                        return false;
            return true;
        }

        public bool IsNakedPair(int row, int col)
        {
            ValidateCoords(row, col);
            return _cells[row, col].IsNakedPair;
        }


    }

}
