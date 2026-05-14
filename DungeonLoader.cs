namespace AdventureGame;

public static class DungeonLoader
{
    private const char Wall = '#';

    public static Room[,] Load(string filePath)
    {
        string[] lines = File.ReadAllLines(filePath);

        int rows = int.Parse(lines[0]);
        int cols = int.Parse(lines[1]);

        int exitRow = int.Parse(lines[2]);
        int exitCol = int.Parse(lines[3]);
        int lampRow = int.Parse(lines[4]);
        int lampCol = int.Parse(lines[5]);
        int keyRow = int.Parse(lines[6]);
        int keyCol = int.Parse(lines[7]);
        int chestRow = int.Parse(lines[8]);
        int chestCol = int.Parse(lines[9]);

        int layoutStart = 12;
        int descriptionsStart = layoutStart + rows;

        if (lines.Length < descriptionsStart)
            throw new FormatException("File does not contain enough layout rows.");

        Room[,] dungeon = new Room[rows, cols];
        List<(int row, int col)> traversableTiles = new();

        for (int row = 0; row < rows; row++)
        {
            string layoutLine = lines[layoutStart + row];

            if (layoutLine.Length != cols)
                throw new FormatException($"Layout row {row} must contain exactly {cols} characters.");

            for (int col = 0; col < cols; col++)
            {
                if (layoutLine[col] != Wall)
                {
                    dungeon[row, col] = new Room();
                    traversableTiles.Add((row, col));
                }
            }
        }

        int descriptionCount = lines.Length - descriptionsStart;

        if (descriptionCount != traversableTiles.Count)
        {
            throw new FormatException(
                    $"Description count ({descriptionCount}) must match traversable tile count ({traversableTiles.Count})."
            );
        }

        for (int i = 0; i < traversableTiles.Count; i++)
        {
            string[] parts = lines[descriptionsStart + i].Split('|', 2);

            if (parts.Length != 2)
                throw new FormatException($"Invalid room description line: {lines[descriptionsStart + i]}");

            bool isLit = parts[0] switch
            {
                "1" => true,
                "0" => false,
                _ => throw new FormatException("Room lit value must be 1 or 0.")
            };

            string description = parts[1];

            var (row, col) = traversableTiles[i];
            Room room = dungeon[row, col];

            room.SetLit(isLit);
            room.SetDescription(description);

            room.SetLamp(row == lampRow && col == lampCol);
            room.SetKey(row == keyRow && col == keyCol);
            room.SetChest(row == chestRow && col == chestCol);
            room.SetExit(row == exitRow && col == exitCol);

            room.SetNorth(IsTraversable(dungeon, row - 1, col));
            room.SetSouth(IsTraversable(dungeon, row + 1, col));
            room.SetEast(IsTraversable(dungeon, row, col + 1));
            room.SetWest(IsTraversable(dungeon, row, col - 1));
        }

        ValidateTraversableTile(dungeon, exitRow, exitCol, "exit");

        return dungeon;
    }

    private static bool IsTraversable(Room[,] dungeon, int row, int col)
    {
        return row >= 0 &&
                     row < dungeon.GetLength(0) &&
                     col >= 0 &&
                     col < dungeon.GetLength(1) &&
                     dungeon[row, col] != null;
    }

    private static void ValidateTraversableTile(Room[,] dungeon, int row, int col, string name)
    {
        if (!IsTraversable(dungeon, row, col))
            throw new FormatException($"The {name} position must be on a traversable tile.");
    }
}
