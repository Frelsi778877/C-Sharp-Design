using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;

namespace SudokuUnlimited
{
    public static class CollectionStore
    {
        private static readonly string Path = "./GameCollections/Collections.json";

        public static CollectionLibrary Load()
        {
            if (!File.Exists(Path))
                return new CollectionLibrary();

            string json = File.ReadAllText(Path);
            return JsonSerializer.Deserialize<CollectionLibrary>(json);
        }

        private static readonly string SaveStatePath = "savestate.json";

        public static void SaveBoardState(BoardState state)
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            File.WriteAllText(SaveStatePath, JsonSerializer.Serialize(state, options));
        }

        public static BoardState LoadBoardState()
        {
            if (!File.Exists(SaveStatePath)) return null;
            string json = File.ReadAllText(SaveStatePath);
            return JsonSerializer.Deserialize<BoardState>(json);
        }

        public static void ClearBoardState()
        {
            if (File.Exists(SaveStatePath))
                File.Delete(SaveStatePath);
        }

        public static void Save(CollectionLibrary library)
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            File.WriteAllText(Path, JsonSerializer.Serialize(library, options));
        }

        public static void MarkSolved(string collectionId, int lineNumber)
        {
            var library = Load();
            var collection = library.Collections.FirstOrDefault(c => c.Id == collectionId);
            if (collection == null) return;

            if (!collection.Solved.Contains(lineNumber))
            {
                collection.Solved.Add(lineNumber);
                collection.Solved.Sort(); // keep it ordered
                Save(library);
            }
        }
    }
}
