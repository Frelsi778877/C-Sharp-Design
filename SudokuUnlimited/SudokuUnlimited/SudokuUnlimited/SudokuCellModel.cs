using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;

namespace SudokuUnlimited
{
    public class SudokuCellModel : INotifyPropertyChanged
    {
        private int _value;
        private bool _isGiven;
        private bool _isHighlighted;
        private HighlightType _highlightType;
        private bool _isError;
        private double _fontSizeOverride;
        private HashSet<int> _candidates;

        /// <summary>
        /// The solved value of this cell. 0 = no solved value.
        /// </summary>
        public int Value
        {
            get => _value;
            set { _value = value; OnPropertyChanged(); }
        }

        public bool IsNakedPair { get; set; }

        /// <summary>
        /// Whether this cell is a given (protected pre-filled clue).
        /// </summary>
        public bool IsGiven
        {
            get => _isGiven;
            set { _isGiven = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Whether this cell is currently highlighted.
        /// </summary>
        public bool IsHighlighted
        {
            get => _isHighlighted;
            set { _isHighlighted = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// The type of highlight currently applied to this cell.
        /// </summary>
        public HighlightType HighlightType
        {
            get => _highlightType;
            set { _highlightType = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Whether this cell has an invalid player entry (shows error color).
        /// </summary>
        public bool IsError
        {
            get => _isError;
            set { _isError = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Per-cell font size override for solved mode. 0 = use global default.
        /// </summary>
        public double FontSizeOverride
        {
            get => _fontSizeOverride;
            set { _fontSizeOverride = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// The set of candidate values for this cell. Empty = no candidates set.
        /// </summary>
        public HashSet<int> Candidates
        {
            get => _candidates;
            set { _candidates = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// True if this cell is in candidate mode (has candidates and no solved value).
        /// </summary>
        public bool IsInCandidateMode => Value == 0 && _candidates != null && _candidates.Count > 0;

        /// <summary>
        /// True if this cell is solved (has a value > 0).
        /// </summary>
        public bool IsSolved => Value > 0;

        /// <summary>
        /// True if this cell is completely empty (no value, no candidates).
        /// </summary>
        public bool IsEmpty => Value == 0 && (_candidates == null || _candidates.Count == 0);

        public SudokuCellModel()
        {
            _candidates = new HashSet<int>();
            _fontSizeOverride = 0;
        }

        public void Clear()
        {
            Value = 0;
            IsGiven = false;
            IsHighlighted = false;
            HighlightType = HighlightType.None;
            IsError = false;
            FontSizeOverride = 0;
            Candidates = new HashSet<int>();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }

    /// <summary>
    /// The type of highlight applied to a cell.
    /// </summary>
    public enum HighlightType
    {
        /// <summary>No highlight.</summary>
        None,

        /// <summary>
        /// Match highlight — shown on cells that share the same value as the clicked solved cell.
        /// </summary>
        Match,
        MatchCandidate,

        /// <summary>
        /// Visibility highlight — shown on cells in the same row, column, or house as the clicked cell.
        /// </summary>
        Visibility,

        MatchContext,

        VisibilitySelected
    }

}
