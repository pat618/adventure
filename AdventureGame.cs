namespace AdventureGame;

public class AdventureGame
{
    public readonly string GO_NORTH = "W";
    public readonly string GO_SOUTH = "S";
    public readonly string GO_EAST = "D";
    public readonly string GO_WEST = "A";
    public readonly string GET_LAMP = "L";
    public readonly string GET_KEY = "K";
    public readonly string OPEN_CHEST = "O";
    public readonly string QUIT = "Q";

    private Adventurer adventurer = null!;
    private Room?[,] dungeon = null!;
    private int aRow, aCol;
    private int gRow, gCol;
    private int exitRow, exitCol;
    private bool isChestOpen;
    private bool hasPlayerQuit;
    private bool isAdventureAlive;
    private bool hasReachedExit;
    private bool isGruePursuing;
    private string lastMessage = string.Empty;
    private Random rng = new Random();



    private const int ROOM_W = 23;
    private const int ROOM_H = 9;
    private const int COR_W = 7;
    private const int VCOR_H = 3;

    private (int r, int c)[] allCells = Array.Empty<(int, int)>();

    public void Start()
    {
        Init();
        ShowGameStartScreen();

        string input;
        do
        {
            Redraw();
            do
            {
                ShowInputOptions();
                input = GetInput();
            }
            while (!IsValidInput(input));

            lastMessage = string.Empty;
            ProcessInput(input);
            UpdateGameState();
        }
        while (!IsGameOver());

        Redraw();
        ShowGameOverScreen();
    }

    private void Init()
    {
        adventurer = new Adventurer();
        adventurer.SetLamp(false);

        string[] layout =
        {
            "R.R.R.R#",
            "v###v#v#",
            "R.R#R#R#",
            "v#v###v#",
            "R#R.R.R#",
            "##v#v###",
            "##R.R###",
            "########",
        };

        int gridRows = layout.Length;
        int gridCols = layout[0].Length;
        dungeon = new Room?[gridRows, gridCols];

        for (int r = 0; r < gridRows; r++)
            for (int c = 0; c < gridCols; c++)
                if (layout[r][c] != '#')
                    dungeon[r, c] = new Room();

        for (int r = 0; r < gridRows; r++)
            for (int c = 0; c < gridCols; c++)
            {
                Room? cell = dungeon[r, c];
                if (cell == null) continue;
                bool isRoom = (r % 2 == 0 && c % 2 == 0);
                cell.SetLit(isRoom);
                cell.SetNorth(IsOpen(r - 1, c));
                cell.SetSouth(IsOpen(r + 1, c));
                cell.SetWest(IsOpen(r, c - 1));
                cell.SetEast(IsOpen(r, c + 1));
                cell.SetDescription(isRoom ? "room" : "passage");
            }

        dungeon[0, 0]!.SetExit(true);
        dungeon[0, 0]!.SetDescription("Room 1  [EXIT]");
        dungeon[0, 2]!.SetKey(true);
        dungeon[0, 2]!.SetDescription("Room 2  [KEY]");
        dungeon[0, 4]!.SetDescription("Room 3");
        dungeon[0, 6]!.SetDescription("Room 4");
        dungeon[2, 0]!.SetLamp(true);
        dungeon[2, 0]!.SetDescription("Room 5  [LAMP]");
        dungeon[2, 2]!.SetDescription("Room 6");
        dungeon[2, 4]!.SetDescription("Room 7");
        dungeon[2, 6]!.SetDescription("Room 8");
        dungeon[4, 0]!.SetDescription("Room 9");
        dungeon[4, 2]!.SetDescription("Room 10");
        dungeon[4, 4]!.SetDescription("Room 11");
        dungeon[4, 6]!.SetDescription("Room 12");
        dungeon[6, 2]!.SetChest(true);
        dungeon[6, 2]!.SetDescription("Room 13 [CHEST]");
        dungeon[6, 4]!.SetDescription("Room 14");

        allCells = AllWalkable();

        aRow = 2; aCol = 0;
        (gRow, gCol) = RandomCellExcluding(aRow, aCol);

        exitRow = 0; exitCol = 0;
        isChestOpen = false;
        hasPlayerQuit = false;
        isAdventureAlive = true;
        hasReachedExit = false;
        isGruePursuing = false;
        lastMessage = string.Empty;
    }

    private bool IsOpen(int r, int c)
        => r >= 0 && r < dungeon.GetLength(0)
        && c >= 0 && c < dungeon.GetLength(1)
        && dungeon[r, c] != null;

    private (int r, int c)[] AllWalkable()
    {
        var list = new System.Collections.Generic.List<(int, int)>();
        for (int r = 0; r < dungeon.GetLength(0); r++)
            for (int c = 0; c < dungeon.GetLength(1); c++)
                if (dungeon[r, c] != null)
                    list.Add((r, c));
        return list.ToArray();
    }

    private (int r, int c) RandomCellExcluding(int exR, int exC)
    {
        var candidates = allCells.Where(p => !(p.r == exR && p.c == exC)).ToArray();
        return candidates[rng.Next(candidates.Length)];
    }

    private void Redraw()
    {
        Console.Clear();
        ShowMap();
        ShowScene();
    }

    private void ShowMap()
    {
        int gridRows = dungeon.GetLength(0);
        int gridCols = dungeon.GetLength(1);

        Console.WriteLine("  \u2554\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2557");
        Console.WriteLine("  \u2551              D U N G E O N   M A P                      \u2551");
        Console.WriteLine("  \u255a\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u255d");
        Console.WriteLine();

        for (int row = 0; row < gridRows; row++)
        {
            bool isRoomRow = (row % 2 == 0);
            int cellH = isRoomRow ? ROOM_H : VCOR_H;

            string[] lines = new string[cellH];
            for (int i = 0; i < cellH; i++) lines[i] = "  ";

            for (int col = 0; col < gridCols; col++)
            {
                Room? cell = dungeon[row, col];
                bool isRoomCol = (col % 2 == 0);

                if (cell == null)
                    BuildNull(lines, isRoomRow, isRoomCol);
                else if (isRoomRow && isRoomCol)
                    BuildRoom(lines, cell, row, col);
                else if (isRoomRow && !isRoomCol)
                    BuildHCorridor(lines, row, col);
                else
                    BuildVCorridor(lines, row, col);
            }

            foreach (var line in lines)
                Console.WriteLine(line);
        }

        Console.WriteLine();
        ShowLegend();
        Console.WriteLine();
    }

    private void BuildNull(string[] lines, bool isRoomRow, bool isRoomCol)
    {
        int w = isRoomCol ? (ROOM_W + 2) : (COR_W + 2);
        for (int i = 0; i < lines.Length; i++)
            lines[i] += new string('\u2593', w);
    }

    private void BuildRoom(string[] lines, Room r, int row, int col)
    {
        int iw = ROOM_W;
        bool hasLamp = adventurer.HasLamp();

        if (r.HasNorth()) { int s = (iw - 5) / 2; lines[0] += "+" + new string('\u2500', s) + "[ N ]" + new string('\u2500', iw - s - 5) + "+"; }
        else lines[0] += "+" + new string('\u2500', iw) + "+";

        lines[1] += "\u2502" + PadCenter(r.GetDescription(), iw) + "\u2502";
        lines[2] += "\u2502" + PadCenter(BuildItemStr(r), iw) + "\u2502";

        string chars = BuildChars(row, col, hasLamp);
        string wBrd = r.HasWest() ? "  " : "\u2502 ";
        string eBrd = r.HasEast() ? "  " : " \u2502";
        lines[3] += wBrd + PadCenter(chars, iw - wBrd.Length - eBrd.Length) + eBrd;

        for (int i = 4; i <= 7; i++)
            lines[i] += "\u2502" + new string(' ', iw) + "\u2502";

        if (r.HasSouth()) { int s = (iw - 5) / 2; lines[8] += "+" + new string('\u2500', s) + "[ S ]" + new string('\u2500', iw - s - 5) + "+"; }
        else lines[8] += "+" + new string('\u2500', iw) + "+";
    }

    private void BuildHCorridor(string[] lines, int row, int col)
    {
        int iw = COR_W;
        bool hasLamp = adventurer.HasLamp();
        string mid = BuildChars(row, col, hasLamp);
        if (mid == string.Empty) mid = hasLamp ? "\u2500\u2500\u2500\u2500\u2500\u2500\u2500" : " ? ";

        lines[0] += new string('\u2500', iw + 2);
        for (int i = 1; i < ROOM_H - 1; i++)
            lines[i] += " " + PadCenter(i == ROOM_H / 2 ? mid : string.Empty, iw) + " ";
        lines[ROOM_H - 1] += new string('\u2500', iw + 2);
    }

    private void BuildVCorridor(string[] lines, int row, int col)
    {
        int iw = ROOM_W;
        bool hasLamp = adventurer.HasLamp();
        string chars = BuildChars(row, col, hasLamp);
        if (chars == string.Empty && !hasLamp) chars = "?";

        lines[0] += "\u2502" + new string(' ', iw) + "\u2502";
        lines[1] += "\u2502" + PadCenter(chars, iw) + "\u2502";
        lines[2] += "\u2502" + new string(' ', iw) + "\u2502";
    }

    private string BuildChars(int row, int col, bool hasLamp)
    {
        bool hasAdv = (row == aRow && col == aCol);
        bool grueIsHere = (row == gRow && col == gCol);
        bool grueVisible = grueIsHere && (hasLamp || hasAdv);

        if (hasAdv && grueVisible) return "( @ ) <<GRUE>>";
        if (hasAdv) return "( @ )";
        if (grueVisible) return "<<GRUE>>";
        return string.Empty;
    }

    private string BuildItemStr(Room r)
    {
        var parts = new System.Collections.Generic.List<string>();
        if (r.HasLamp()) parts.Add("[Lamp]");
        if (r.HasKey()) parts.Add("[Key]");
        if (r.HasChest()) parts.Add(isChestOpen ? "(open)" : "[Chest]");
        if (r.IsExit()) parts.Add("<<EXIT>>");
        return string.Join("  ", parts);
    }

    private void ShowLegend()
    {
        Console.WriteLine("  \u250c\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2510");
        Console.WriteLine("  \u2502 ( @ )=You  <<GRUE>>=Grue (lamp needed to see it!)               \u2502");
        Console.WriteLine("  \u2502 [Lamp]=Lamp  [Key]=Key  [Chest]=Chest  <<EXIT>>=Exit            \u2502");
        Console.WriteLine("  \u2502 [ N ]/[ S ]=door  open side=E/W door  ?=dark (Grue may lurk!)  \u2502");
        Console.WriteLine("  \u2514\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2518");
    }

    private static string PadCenter(string s, int width)
    {
        if (width <= 0) return string.Empty;
        if (s.Length >= width) return s[..width];
        int pad = width - s.Length;
        return new string(' ', pad / 2) + s + new string(' ', pad - pad / 2);
    }

    private void ShowGameStartScreen()
    {
        Console.Clear();
        Console.WriteLine();
        Console.WriteLine("  \u2554\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2557");
        Console.WriteLine("  \u2551           A D V E N T U R E   G A M E                 \u2551");
        Console.WriteLine("  \u2560\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2563");
        Console.WriteLine("  \u2551  You wake up in Room 5. A lamp glows on the floor.    \u2551");
        Console.WriteLine("  \u2551                                                        \u2551");
        Console.WriteLine("  \u2551  WARNING: The Grue is already loose in the dungeon!   \u2551");
        Console.WriteLine("  \u2551   Without the lamp you CANNOT see it coming.          \u2551");
        Console.WriteLine("  \u2551   If it enters your room \u2014 you die.                   \u2551");
        Console.WriteLine("  \u2551                                                        \u2551");
        Console.WriteLine("  \u2551  Goals:                                                \u2551");
        Console.WriteLine("  \u2551    1. Pick up LAMP  [L]  (Room 5, where you start).   \u2551");
        Console.WriteLine("  \u2551    2. Find the KEY  [K]  (Room 2, top area).          \u2551");
        Console.WriteLine("  \u2551    3. Open the CHEST [O] (Room 13, bottom centre).    \u2551");
        Console.WriteLine("  \u2551    4. Reach the EXIT     (Room 1, top-left).          \u2551");
        Console.WriteLine("  \u2551                                                        \u2551");
        Console.WriteLine("  \u2551  Opening the chest makes the Grue actively chase you! \u2551");
        Console.WriteLine("  \u255a\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u255d");
        Console.WriteLine();
        Console.Write("  Press ENTER to start...");
        Console.ReadLine();
    }

    private void ShowScene()
    {
        Room? r = dungeon[aRow, aCol];
        Console.WriteLine("  \u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550");
        Console.WriteLine($"  Location : {r?.GetDescription() ?? "?"}");

        if (!adventurer.HasLamp())
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write("  Inventory: [NO LAMP]");
            Console.ResetColor();
            Console.WriteLine("  \u2190 pick it up [L] \u2014 the Grue is out there!");
        }
        else
        {
            string inv = "  Inventory: [Lamp]";
            if (adventurer.HasKey()) inv += "  [Key]";
            if (isChestOpen) inv += "  [Treasure]";
            Console.WriteLine(inv);
        }

        if (!string.IsNullOrEmpty(lastMessage))
        {
            bool danger = lastMessage.Contains("Grue") || lastMessage.Contains("dark")
                       || lastMessage.Contains("DEVOURED") || lastMessage.Contains("black");
            if (danger) Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"  >> {lastMessage}");
            if (danger) Console.ResetColor();
        }

        if (isGruePursuing)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("  !! THE GRUE IS HUNTING YOU \u2014 REACH THE EXIT !!");
            Console.ResetColor();
        }
        else if (!adventurer.HasLamp())
        {
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.WriteLine("  ~ The Grue wanders nearby... you cannot see it ~");
            Console.ResetColor();
        }

        Console.WriteLine("  \u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550");
    }

    private void ShowInputOptions()
        => Console.Write("  [W]North [S]South [A]West [D]East [L]Lamp [K]Key [O]Chest [Q]Quit\n  > ");

    private string GetInput() => Console.ReadLine()!.Trim().ToUpper();

    private bool IsValidInput(string input)
    {
        string[] valid = { GO_NORTH, GO_SOUTH, GO_EAST, GO_WEST, GET_LAMP, GET_KEY, OPEN_CHEST, QUIT };
        if (!valid.Contains(input))
        {
            lastMessage = "Unknown command. Use W/A/S/D/L/K/O/Q.";
            Redraw();
            return false;
        }
        return true;
    }

    private void ProcessInput(string input)
    {
        if (input == GO_NORTH) Move(-1, 0);
        else if (input == GO_SOUTH) Move(1, 0);
        else if (input == GO_WEST) Move(0, -1);
        else if (input == GO_EAST) Move(0, 1);
        else if (input == GET_LAMP) GetLamp();
        else if (input == GET_KEY) GetKey();
        else if (input == OPEN_CHEST) OpenChest();
        else Quit();
    }

    private void Move(int dr, int dc)
    {
        Room? r = dungeon[aRow, aCol];
        bool canGo = (dr == -1 && (r?.HasNorth() ?? false))
                  || (dr == 1 && (r?.HasSouth() ?? false))
                  || (dc == -1 && (r?.HasWest() ?? false))
                  || (dc == 1 && (r?.HasEast() ?? false));

        if (!canGo)
        {
            string dir = dr == -1 ? "north" : dr == 1 ? "south" : dc == -1 ? "west" : "east";
            lastMessage = $"No passage to the {dir}.";
            return;
        }

        aRow += dr; aCol += dc;
        CheckGrueEncounter();
    }

    private void GetLamp()
    {
        Room? r = dungeon[aRow, aCol];
        if (r?.HasLamp() ?? false)
        {
            adventurer.SetLamp(true);
            r!.SetLamp(false);
            (gRow, gCol) = RandomCellExcluding(aRow, aCol);
            lastMessage = "You grab the LAMP! Somewhere in the dark, the Grue snarls...";
        }
        else lastMessage = adventurer.HasLamp() ? "You already carry the lamp." : "No lamp here.";
    }

    private void GetKey()
    {
        Room? r = dungeon[aRow, aCol];
        if (r?.HasKey() ?? false)
        {
            adventurer.SetKey(true);
            r!.SetKey(false);
            lastMessage = "You picked up the KEY!";
        }
        else lastMessage = adventurer.HasKey() ? "You already carry the key." : "No key here.";
    }

    private void OpenChest()
    {
        Room? r = dungeon[aRow, aCol];
        if (r?.HasChest() ?? false)
        {
            if (adventurer.HasKey())
            {
                isChestOpen = true;
                isGruePursuing = true;
                lastMessage = "You seize the TREASURE! The Grue roars \u2014 RUN to the exit!";
            }
            else lastMessage = "The chest is locked. Find the KEY first.";
        }
        else lastMessage = isChestOpen ? "The chest is already open." : "No chest here.";
    }

    private void Quit() { lastMessage = "You quit."; hasPlayerQuit = true; }

    private void UpdateGameState()
    {
        if (!isAdventureAlive || hasPlayerQuit || hasReachedExit) return;

        if (isGruePursuing)
            MoveGrueBFS();
        else
            MoveGrueRandom();

        CheckGrueEncounter();
        if (!isAdventureAlive) return;

        if (isChestOpen && aRow == exitRow && aCol == exitCol)
            hasReachedExit = true;
    }

    private void MoveGrueRandom()
    {
        var neighbours = new System.Collections.Generic.List<(int r, int c)>();
        int[] dr = { -1, 1, 0, 0 };
        int[] dc = { 0, 0, -1, 1 };

        Room? cur = dungeon[gRow, gCol];
        bool[] doors = {
            cur?.HasNorth() ?? false,
            cur?.HasSouth() ?? false,
            cur?.HasWest()  ?? false,
            cur?.HasEast()  ?? false,
        };

        for (int d = 0; d < 4; d++)
        {
            if (!doors[d]) continue;
            int nr = gRow + dr[d], nc = gCol + dc[d];
            if (IsOpen(nr, nc)) neighbours.Add((nr, nc));
        }

        if (neighbours.Count > 0)
        {
            var next = neighbours[rng.Next(neighbours.Count)];
            gRow = next.r; gCol = next.c;
        }
    }

    private void MoveGrueBFS()
    {
        int rows = dungeon.GetLength(0);
        int cols = dungeon.GetLength(1);
        var prev = new (int r, int c)[rows, cols];
        for (int r = 0; r < rows; r++)
            for (int c = 0; c < cols; c++)
                prev[r, c] = (-1, -1);

        var queue = new System.Collections.Generic.Queue<(int r, int c)>();
        queue.Enqueue((gRow, gCol));
        prev[gRow, gCol] = (gRow, gCol);

        int[] dr = { -1, 1, 0, 0 };
        int[] dc = { 0, 0, -1, 1 };

        while (queue.Count > 0)
        {
            var (r, c) = queue.Dequeue();
            if (r == aRow && c == aCol) break;
            for (int d = 0; d < 4; d++)
            {
                int nr = r + dr[d], nc = c + dc[d];
                if (nr < 0 || nr >= rows || nc < 0 || nc >= cols) continue;
                if (dungeon[nr, nc] == null || prev[nr, nc].r != -1) continue;
                Room? curRoom = dungeon[r, c];
                bool door = d == 0 ? (curRoom?.HasNorth() ?? false)
                          : d == 1 ? (curRoom?.HasSouth() ?? false)
                          : d == 2 ? (curRoom?.HasWest() ?? false)
                          : (curRoom?.HasEast() ?? false);
                if (!door) continue;
                prev[nr, nc] = (r, c);
                queue.Enqueue((nr, nc));
            }
        }

        if (prev[aRow, aCol].r == -1) return;
        (int r, int c) step = (aRow, aCol);
        while (prev[step.r, step.c] != (gRow, gCol))
            step = prev[step.r, step.c];
        gRow = step.r; gCol = step.c;
    }

    private void CheckGrueEncounter()
    {
        if (gRow == aRow && gCol == aCol)
        {
            lastMessage = "The Grue is here! You are DEVOURED in the darkness!";
            isAdventureAlive = false;
        }
    }

    private bool IsGameOver() => hasReachedExit || hasPlayerQuit || !isAdventureAlive;

    private void ShowGameOverScreen()
    {
        Console.WriteLine();
        if (hasReachedExit)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("  \u2554\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2557");
            Console.WriteLine("  \u2551   YOU ESCAPED THE DUNGEON WITH THE TREASURE!    \u2551");
            Console.WriteLine("  \u2551                 *** YOU WIN ***                 \u2551");
            Console.WriteLine("  \u255a\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u255d");
            Console.ResetColor();
        }
        else if (!isAdventureAlive)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("  \u2554\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2557");
            Console.WriteLine("  \u2551      The dungeon claims another soul...         \u2551");
            Console.WriteLine("  \u2551              *** GAME  OVER ***                 \u2551");
            Console.WriteLine("  \u2554\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2557");
            Console.ResetColor();
        }
        else Console.WriteLine("  You slipped away quietly. Goodbye.");
    }
}
