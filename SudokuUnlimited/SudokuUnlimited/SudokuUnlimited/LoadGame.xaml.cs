using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace SudokuUnlimited
{
    /// <summary>
    /// Interaction logic for LoadGame.xaml
    /// </summary>
    public partial class LoadGame : Window
    {
        private List<string> _currentPuzzleLines = new();
        private SudokuCollection _currentCollection;
        private ScrollViewer _puzzleScrollViewer;

        private int _currentPage = 0;
        private const int PageSize = 100;
        private ObservableCollection<PuzzleListItem> _puzzleItems = new();

        public LoadGame()
        {
            InitializeComponent();

            // Load the collections from the JSON file
            var library = CollectionStore.Load();

            string basePath = "GameCollections"; // folder where your puzzle files live

            foreach (var collection in library.Collections)
                collection.LoadPuzzleCount(basePath);

            CollectionsTable.ItemsSource = library.Collections;

            KeyDown += Game_KeyDown;
        }

        private void Game_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                var main = new MainWindow();
                main.Show();
                this.Close();
            }
        }

        private SudokuCollection _selectedCollection;

        private void CollectionsTable_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (CollectionsTable.SelectedItem is SudokuCollection collection)
            {
                _selectedCollection = collection;
                CollectionsPanel.Visibility = Visibility.Collapsed;
                PuzzlesPanel.Visibility = Visibility.Visible;

                LoadPuzzlesPanel(collection);
            }
        }

        private void LoadPuzzlesPanel(SudokuCollection collection)
        {
            _currentCollection = collection;
            PuzzlesHeader.Text = collection.Name;

            string path = System.IO.Path.Combine("GameCollections", collection.Filename);
            _currentPuzzleLines = File.ReadLines(path)
                .Where(l => l.Trim().Length == 81)
                .ToList();

            _currentPage = 0;
            _puzzleItems = new ObservableCollection<PuzzleListItem>();
            PuzzleListView.ItemsSource = _puzzleItems;

            // Detach previous scroll handler
            if (_puzzleScrollViewer != null)
                _puzzleScrollViewer.ScrollChanged -= PuzzleListScrollChanged;

            LoadNextPage();

            Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Loaded, new Action(() =>
            {
                _puzzleScrollViewer = GetScrollViewer(PuzzleListView);
                if (_puzzleScrollViewer != null)
                {
                    _puzzleScrollViewer.ScrollChanged -= PuzzleListScrollChanged;
                    _puzzleScrollViewer.ScrollChanged += PuzzleListScrollChanged;
                    _puzzleScrollViewer.ScrollToTop(); // reset scroll position
                }
            }));
        }

        private void PuzzleListScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (sender is ScrollViewer sv)
            {
                if (sv.VerticalOffset >= sv.ScrollableHeight - 200)
                {
                    int totalLoaded = _currentPage * PageSize;
                    if (totalLoaded < _currentPuzzleLines.Count)
                        LoadNextPage();
                }
            }
        }


        private void LoadNextPage()
        {
            var nextItems = _currentPuzzleLines
                .Skip(_currentPage * PageSize)
                .Take(PageSize)
                .Select((line, index) => new PuzzleListItem
                {
                    LineNumber = (_currentPage * PageSize) + index + 1,
                    PuzzleString = line,
                    IsSolved = _currentCollection.Solved.Contains((_currentPage * PageSize) + index + 1)
                });

            foreach (var item in nextItems)
                _puzzleItems.Add(item);

            _currentPage++;
        }

        private ScrollViewer GetScrollViewer(DependencyObject obj)
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(obj); i++)
            {
                var child = VisualTreeHelper.GetChild(obj, i);
                if (child is ScrollViewer sv) return sv;
                var result = GetScrollViewer(child);
                if (result != null) return result;
            }
            return null;
        }

        private void PuzzleListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (PuzzleListView.SelectedItem is PuzzleListItem item)
            {
                PreviewLabel.Text = item.Label;
                var puzzle = SudokuParser.GivensOnly(item.PuzzleString);
                PreviewGrid.Initialize(puzzle);
            }
        }

        private void PuzzleListView_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (PuzzleListView.SelectedItem is PuzzleListItem item)
            {
                var savedState = CollectionStore.LoadBoardState();
                var puzzle = SudokuParser.FromString(item.PuzzleString);

                BoardState stateToRestore = null;
                if (savedState != null &&
                    savedState.CollectionId == _currentCollection.Id &&
                    savedState.LineNumber == item.LineNumber)
                {
                    stateToRestore = savedState;
                }

                Game gameWindow = new Game(puzzle, _currentCollection, item.LineNumber, stateToRestore);
                gameWindow.Show();
                this.Hide();
            }
        }

        private void ImportButton_Click(object sender, RoutedEventArgs e)
        {
            
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            PuzzlesPanel.Visibility = Visibility.Collapsed;
            CollectionsPanel.Visibility = Visibility.Visible;
        }

    }
}
