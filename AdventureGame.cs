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

    // ── state ─────────────────────────────────────────────────────────
    private Adventurer adventurer = null!;
    private Room?[,] dungeon = null!;
    private int aRow, aCol;           // adventurer position
    private int gRow, gCol;           // grue position
    private int exitRow, exitCol;
    private bool isChestOpen;
    private bool hasPlayerQuit;
    private bool isAdventureAlive;
    private bool hasReachedExit;
    private bool isGruePursuing;      // true after chest opened → grue chases
    private string lastMessage = string.Empty;
    private Random rng = new Random();

    //  5×5 grid layout:
    //
    //  Col:  0      1      2      3      4
    //  Row0: R      h      R      #      #
    //  Row1: v      #      v      #      #
    //  Row2: R      h      R      h      R
    //  Row3: #      #      v      #      v
    //  Row4: #      #      R      h      R
    //
    //  Rooms (even,even): [0,0]=EXIT  [0,2]=KEY  [2,0]=LAMP+START
    //                     [2,2]=empty [2,4]=empty [4,2]=CHEST [4,4]=empty
    //  Corridors: h=horizontal(even,odd)  v=vertical(odd,even)  #=wall(null)
    //
    //  GRUE wanders always. Without lamp the player CANNOT see it.
    //  Sharing any cell with the Grue = instant death.
    //  Picking up the lamp teleports the Grue to a random cell != player.

    private const int ROOM_W = 23;
    private const int ROOM_H = 9;
    private const int COR_W = 7;
    private const int VCOR_H = 3;

    // ── all walkable cells (built once after dungeon is ready) ─────────
    private (int r, int c)[] allCells = Array.Empty<(int, int)>();

    // ───────────────────────────────────────────────────────────────────
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
        adventurer.SetLamp(false);   // must pick up!

        string[] layout =
        {
            "R.R##",
            "v#v##",
            "R.R.R",
            "##v#v",
            "##R.R",
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

        // Room labels & items
        dungeon[0, 0]!.SetExit(true);
        dungeon[0, 0]!.SetDescription("Room 1  [EXIT]");
        dungeon[0, 2]!.SetKey(true);
        dungeon[0, 2]!.SetDescription("Room 2  [KEY]");
        dungeon[2, 0]!.SetLamp(true);
        dungeon[2, 0]!.SetDescription("Room 3  [LAMP]");
        dungeon[2, 2]!.SetDescription("Room 4");
        dungeon[2, 4]!.SetDescription("Room 5");
        dungeon[4, 2]!.SetChest(true);
        dungeon[4, 2]!.SetDescription("Room 6  [CHEST]");
        dungeon[4, 4]!.SetDescription("Room 7");

        // Build list of all walkable cells
        allCells = AllWalkable();

        // Adventurer starts in Room 3
        aRow = 2; aCol = 0;

        // Grue starts at a random cell that is NOT the player's start room
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

    // ───────────────────────────────────────────────────────────────────
    //  RENDER
    // ───────────────────────────────────────────────────────────────────

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

        Console.WriteLine("  ╔══════════════════════════════════════════════════════════╗");
        Console.WriteLine("  ║              D U N G E O N   M A P                      ║");
        Console.WriteLine("  ╚══════════════════════════════════════════════════════════╝");
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
            lines[i] += new string('▓', w);
    }

    private void BuildRoom(string[] lines, Room r, int row, int col)
    {
        int iw = ROOM_W;
        bool hasLamp = adventurer.HasLamp();

        // top border
        if (r.HasNorth()) { int s = (iw - 5) / 2; lines[0] += "+" + new string('─', s) + "[ N ]" + new string('─', iw - s - 5) + "+"; }
        else lines[0] += "+" + new string('─', iw) + "+";

        // room name
        lines[1] += "│" + PadCenter(r.GetDescription(), iw) + "│";

        // items
        lines[2] += "│" + PadCenter(BuildItemStr(r), iw) + "│";

        // characters (E/W doors on line 3)
        string chars = BuildChars(row, col, hasLamp);
        string wBrd = r.HasWest() ? "  " : "│ ";
        string eBrd = r.HasEast() ? "  " : " │";
        lines[3] += wBrd + PadCenter(chars, iw - wBrd.Length - eBrd.Length) + eBrd;

        for (int i = 4; i <= 7; i++)
            lines[i] += "│" + new string(' ', iw) + "│";

        // bottom border
        if (r.HasSouth()) { int s = (iw - 5) / 2; lines[8] += "+" + new string('─', s) + "[ S ]" + new string('─', iw - s - 5) + "+"; }
        else lines[8] += "+" + new string('─', iw) + "+";
    }

    private void BuildHCorridor(string[] lines, int row, int col)
    {
        int iw = COR_W;
        bool hasLamp = adventurer.HasLamp();
        string mid = BuildChars(row, col, hasLamp);
        if (mid == string.Empty) mid = hasLamp ? "───────" : " ? ";

        lines[0] += new string('─', iw + 2);
        for (int i = 1; i < ROOM_H - 1; i++)
            lines[i] += " " + PadCenter(i == ROOM_H / 2 ? mid : string.Empty, iw) + " ";
        lines[ROOM_H - 1] += new string('─', iw + 2);
    }

    private void BuildVCorridor(string[] lines, int row, int col)
    {
        int iw = ROOM_W;
        bool hasLamp = adventurer.HasLamp();
        string chars = BuildChars(row, col, hasLamp);
        if (chars == string.Empty && !hasLamp) chars = "?";

        lines[0] += "│" + new string(' ', iw) + "│";
        lines[1] += "│" + PadCenter(chars, iw) + "│";
        lines[2] += "│" + new string(' ', iw) + "│";
    }

    // Builds the character string for a cell.
    // Grue is only shown if player has the lamp OR is in the same cell.
    private string BuildChars(int row, int col, bool hasLamp)
    {
        bool hasAdv = (row == aRow && col == aCol);
        bool grueIsHere = (row == gRow && col == gCol);
        // Can see grue only with lamp, or if it's in the same cell (then you die anyway)
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
        Console.WriteLine("  ┌──────────────────────────────────────────────────────────────────┐");
        Console.WriteLine("  │ ( @ )=You  <<GRUE>>=Grue (lamp needed to see it!)               │");
        Console.WriteLine("  │ [Lamp]=Lamp  [Key]=Key  [Chest]=Chest  <<EXIT>>=Exit            │");
        Console.WriteLine("  │ [ N ]/[ S ]=door  open side=E/W door  ?=dark (Grue may lurk!)  │");
        Console.WriteLine("  └──────────────────────────────────────────────────────────────────┘");
    }

    private static string PadCenter(string s, int width)
    {
        if (width <= 0) return string.Empty;
        if (s.Length >= width) return s[..width];
        int pad = width - s.Length;
        return new string(' ', pad / 2) + s + new string(' ', pad - pad / 2);
    }

    // ───────────────────────────────────────────────────────────────────
    //  HUD / INPUT
    // ───────────────────────────────────────────────────────────────────

    private void ShowGameStartScreen()
    {
        Console.Clear();
        Console.WriteLine();
        Console.WriteLine("  ╔════════════════════════════════════════════════════════╗");
        Console.WriteLine("  ║           A D V E N T U R E   G A M E                 ║");
        Console.WriteLine("  ╠════════════════════════════════════════════════════════╣");
        Console.WriteLine("  ║  You wake up in Room 3. A lamp glows on the floor.    ║");
        Console.WriteLine("  ║                                                        ║");
        Console.WriteLine("  ║  WARNING: The Grue is already loose in the dungeon!   ║");
        Console.WriteLine("  ║   Without the lamp you CANNOT see it coming.          ║");
        Console.WriteLine("  ║   If it enters your room — you die.                   ║");
        Console.WriteLine("  ║                                                        ║");
        Console.WriteLine("  ║  Goals:                                                ║");
        Console.WriteLine("  ║    1. Pick up LAMP  [L]  (Room 3, where you start).   ║");
        Console.WriteLine("  ║    2. Find the KEY  [K]  (Room 2, top-right).         ║");
        Console.WriteLine("  ║    3. Open the CHEST [O] (Room 6, bottom-centre).     ║");
        Console.WriteLine("  ║    4. Reach the EXIT     (Room 1, top-left).          ║");
        Console.WriteLine("  ║                                                        ║");
        Console.WriteLine("  ║  Opening the chest makes the Grue actively chase you! ║");
        Console.WriteLine("  ╚════════════════════════════════════════════════════════╝");
        Console.WriteLine();
        Console.Write("  Press ENTER to start...");
        Console.ReadLine();
    }

    private void ShowScene()
    {
        Room? r = dungeon[aRow, aCol];
        Console.WriteLine("  ══════════════════════════════════════════════════════");
        Console.WriteLine($"  Location : {r?.GetDescription() ?? "?"}");

        if (!adventurer.HasLamp())
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write("  Inventory: [NO LAMP]");
            Console.ResetColor();
            Console.WriteLine("  ← pick it up [L] — the Grue is out there!");
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
            Console.WriteLine("  !! THE GRUE IS HUNTING YOU — REACH THE EXIT !!");
            Console.ResetColor();
        }
        else if (!adventurer.HasLamp())
        {
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.WriteLine("  ~ The Grue wanders nearby... you cannot see it ~");
            Console.ResetColor();
        }

        Console.WriteLine("  ══════════════════════════════════════════════════════");
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

    // ───────────────────────────────────────────────────────────────────
    //  GAME LOGIC
    // ───────────────────────────────────────────────────────────────────

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
        // Sharing a cell with the Grue = death (even without lamp)
        CheckGrueEncounter();
    }

    private void GetLamp()
    {
        Room? r = dungeon[aRow, aCol];
        if (r?.HasLamp() ?? false)
        {
            adventurer.SetLamp(true);
            r!.SetLamp(false);
            // Teleport Grue to a random cell away from the player
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
                lastMessage = "You seize the TREASURE! The Grue roars — RUN to the exit!";
            }
            else lastMessage = "The chest is locked. Find the KEY first.";
        }
        else lastMessage = isChestOpen ? "The chest is already open." : "No chest here.";
    }

    private void Quit() { lastMessage = "You quit."; hasPlayerQuit = true; }

    private void UpdateGameState()
    {
        if (!isAdventureAlive || hasPlayerQuit || hasReachedExit) return;

        // Grue always moves every turn (wanders or pursues)
        if (isGruePursuing)
            MoveGrueBFS();
        else
            MoveGrueRandom();

        CheckGrueEncounter();
        if (!isAdventureAlive) return;

        if (isChestOpen && aRow == exitRow && aCol == exitCol)
            hasReachedExit = true;
    }

    // Grue wanders: picks a random neighbouring cell
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

    // BFS: Grue moves one step toward the player
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
            Console.WriteLine("  ╔══════════════════════════════════════════════════╗");
            Console.WriteLine("  ║   YOU ESCAPED THE DUNGEON WITH THE TREASURE!    ║");
            Console.WriteLine("  ║                 *** YOU WIN ***                 ║");
            Console.WriteLine("  ╚══════════════════════════════════════════════════╝");
            Console.ResetColor();
        }
        else if (!isAdventureAlive)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("  ╔══════════════════════════════════════════════════╗");
            Console.WriteLine("  ║      The dungeon claims another soul...         ║");
            Console.WriteLine("  ║              *** GAME  OVER ***                 ║");
            Console.WriteLine("  ╚══════════════════════════════════════════════════╝");
            Console.ResetColor();
        }
        else Console.WriteLine("  You slipped away quietly. Goodbye.");
    }
}
