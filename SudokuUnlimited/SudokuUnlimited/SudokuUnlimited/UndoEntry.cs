using System;
using System.Collections.Generic;
using System.Text;

namespace SudokuUnlimited
{
    public class UndoEntry
    {
        public List<CellSnapshot> Before { get; set; } = new();
        public List<CellSnapshot> After { get; set; } = new();
    }

}
