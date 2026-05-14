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

    // ── state ───────────────────────────────────────────────────────
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
    private string lastDirection = string.Empty;
    private string lastMessage = string.Empty;

    // ── cell sizes ──────────────────────────────────────────────────
    //
    //  5×5 grid:
    //
    //  Col:  0      1      2      3      4
    //  Row0: R      h      R      #      #
    //  Row1: v      #      v      #      #
    //  Row2: R      h      R      h      R
    //  Row3: #      #      v      #      v
    //  Row4: #      #      R      h      R
    //
    //  R=room  h=H-corridor  v=V-corridor  #=wall
    //
    //  [0,0]=EXIT      [0,2]=KEY room
    //  [2,0]=LAMP+START  [2,2]=CHEST  [2,4]=empty
    //  [4,2]=empty       [4,4]=Grue start
    //
    //  Connections:
    //    [0,0]─h─[0,2]   [0,0]─v─[2,0]   [0,2]─v─[2,2]
    //    [2,0]─h─[2,2]   [2,2]─h─[2,4]
    //    [2,2]─v─[4,2]   [2,4]─v─[4,4]   [4,2]─h─[4,4]
    //
    //  Corridors are DARK — entering without lamp = instant Grue death.

    private const int ROOM_W = 23;
    private const int ROOM_H = 9;
    private const int COR_W = 7;
    private const int VCOR_H = 3;

    // ───────────────────────────────────────────────────────────────
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
        adventurer.SetLamp(false);   // must be picked up!

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
        {
            for (int c = 0; c < gridCols; c++)
            {
                Room? cell = dungeon[r, c];
                if (cell == null) continue;

                bool isRoom = (r % 2 == 0 && c % 2 == 0);
                cell.SetLit(isRoom);       // rooms lit, corridors dark
                cell.SetNorth(IsOpen(r - 1, c));
                cell.SetSouth(IsOpen(r + 1, c));
                cell.SetWest(IsOpen(r, c - 1));
                cell.SetEast(IsOpen(r, c + 1));
                cell.SetDescription(isRoom ? "room" : "passage");
            }
        }

        // Room assignments
        dungeon[0, 0]!.SetExit(true);
        dungeon[0, 0]!.SetDescription("Room 1  [EXIT]");

        dungeon[0, 2]!.SetKey(true);
        dungeon[0, 2]!.SetDescription("Room 2  [KEY]");

        // Room 3: start room — lamp is here, but chest is in room 4
        dungeon[2, 0]!.SetLamp(true);
        dungeon[2, 0]!.SetDescription("Room 3  [LAMP]");

        // chest moved to Room 7
        dungeon[2, 2]!.SetDescription("Room 4");

        dungeon[2, 4]!.SetDescription("Room 5");
        dungeon[4, 2]!.SetDescription("Room 6");
        dungeon[4, 4]!.SetChest(true);
        dungeon[4, 4]!.SetDescription("Room 7  [CHEST]");

        aRow = 2; aCol = 0;   // start: Room 3 (has lamp)
        gRow = 4; gCol = 4;   // grue:  Room 7

        exitRow = 0; exitCol = 0;

        isChestOpen = false;
        hasPlayerQuit = false;
        isAdventureAlive = true;
        hasReachedExit = false;
        isGruePursuing = false;
        lastDirection = string.Empty;
        lastMessage = string.Empty;
    }

    private bool IsOpen(int r, int c)
        => r >= 0 && r < dungeon.GetLength(0)
        && c >= 0 && c < dungeon.GetLength(1)
        && dungeon[r, c] != null;

    // ───────────────────────────────────────────────────────────────
    //  RENDER
    // ───────────────────────────────────────────────────────────────

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
                    BuildHCorridor(lines, cell, row, col);
                else
                    BuildVCorridor(lines, cell, row, col);
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

        // line 0 – top border
        if (r.HasNorth())
        {
            int s = (iw - 5) / 2;
            lines[0] += "+" + new string('─', s) + "[ N ]" + new string('─', iw - s - 5) + "+";
        }
        else lines[0] += "+" + new string('─', iw) + "+";

        // line 1 – room name
        lines[1] += "│" + PadCenter(r.GetDescription(), iw) + "│";

        // line 2 – items
        lines[2] += "│" + PadCenter(BuildItemStr(r), iw) + "│";

        // line 3 – E/W doors + characters
        string chars = BuildChars(row, col);
        string wBrd = r.HasWest() ? "  " : "│ ";
        string eBrd = r.HasEast() ? "  " : " │";
        lines[3] += wBrd + PadCenter(chars, iw - wBrd.Length - eBrd.Length) + eBrd;

        // lines 4-7 – padding
        for (int i = 4; i <= 7; i++)
            lines[i] += "│" + new string(' ', iw) + "│";

        // line 8 – bottom border
        if (r.HasSouth())
        {
            int s = (iw - 5) / 2;
            lines[8] += "+" + new string('─', s) + "[ S ]" + new string('─', iw - s - 5) + "+";
        }
        else lines[8] += "+" + new string('─', iw) + "+";
    }

    private void BuildHCorridor(string[] lines, Room cell, int row, int col)
    {
        int iw = COR_W;
        bool hasAdv = (row == aRow && col == aCol);
        bool hasGrue = isGruePursuing && (row == gRow && col == gCol);

        // Dark corridor — show '???' unless player is here with lamp or grue is here
        string mid;
        if (hasAdv && hasGrue) mid = "@+G";
        else if (hasAdv) mid = adventurer.HasLamp() ? " @ " : "???";
        else if (hasGrue) mid = " G ";
        else if (!adventurer.HasLamp()) mid = " ? ";   // unknown dark passage
        else mid = "───────";

        lines[0] += new string('─', iw + 2);
        for (int i = 1; i < ROOM_H - 1; i++)
            lines[i] += " " + PadCenter(i == ROOM_H / 2 ? mid : string.Empty, iw) + " ";
        lines[ROOM_H - 1] += new string('─', iw + 2);
    }

    private void BuildVCorridor(string[] lines, Room cell, int row, int col)
    {
        int iw = ROOM_W;
        bool hasAdv = (row == aRow && col == aCol);
        bool hasGrue = isGruePursuing && (row == gRow && col == gCol);

        string chars;
        if (hasAdv && hasGrue) chars = "( @ ) <<GRUE>>";
        else if (hasAdv) chars = adventurer.HasLamp() ? "( @ )" : "???";
        else if (hasGrue) chars = "<<GRUE>>";
        else if (!adventurer.HasLamp()) chars = "?";
        else chars = string.Empty;

        lines[0] += "│" + new string(' ', iw) + "│";
        lines[1] += "│" + PadCenter(chars, iw) + "│";
        lines[2] += "│" + new string(' ', iw) + "│";
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

    private string BuildChars(int row, int col)
    {
        bool hasAdv = (row == aRow && col == aCol);
        bool hasGrue = isGruePursuing && (row == gRow && col == gCol);
        if (hasAdv && hasGrue) return "( @ ) <<GRUE>>";
        if (hasAdv) return "( @ )";
        if (hasGrue) return "<<GRUE>>";
        return string.Empty;
    }

    private void ShowLegend()
    {
        Console.WriteLine("  ┌──────────────────────────────────────────────────────────────────┐");
        Console.WriteLine("  │ ( @ )=You   <<GRUE>>=Grue   [Lamp]=Lamp   [Key]=Key             │");
        Console.WriteLine("  │ [Chest]=Chest   <<EXIT>>=Exit   [ N ]/[ S ]=door N/S            │");
        Console.WriteLine("  │ open side border = door E/W    ? = dark passage (deadly!)       │");
        Console.WriteLine("  └──────────────────────────────────────────────────────────────────┘");
    }

    private static string PadCenter(string s, int width)
    {
        if (width <= 0) return string.Empty;
        if (s.Length >= width) return s[..width];
        int pad = width - s.Length;
        return new string(' ', pad / 2) + s + new string(' ', pad - pad / 2);
    }

    // ───────────────────────────────────────────────────────────────
    //  HUD / INPUT
    // ───────────────────────────────────────────────────────────────

    private void ShowGameStartScreen()
    {
        Console.Clear();
        Console.WriteLine();
        Console.WriteLine("  ╔═══════════════════════════════════════════════════════╗");
        Console.WriteLine("  ║            A D V E N T U R E   G A M E               ║");
        Console.WriteLine("  ╠═══════════════════════════════════════════════════════╣");
        Console.WriteLine("  ║  You wake up in Room 3. A lamp glows on the floor.   ║");
        Console.WriteLine("  ║                                                       ║");
        Console.WriteLine("  ║  *** PICK UP THE LAMP before leaving this room! ***  ║");
        Console.WriteLine("  ║      The corridors are pitch black.                  ║");
        Console.WriteLine("  ║      Step into darkness without it = instant death.  ║");
        Console.WriteLine("  ║                                                       ║");
        Console.WriteLine("  ║  Goals:                                               ║");
        Console.WriteLine("  ║    1. Pick up LAMP  (press L while in Room 3).       ║");
        Console.WriteLine("  ║    2. Find the KEY  (Room 2, top-right).             ║");
        Console.WriteLine("  ║    3. Reach Room 7 (bottom-right) to open the CHEST.              ║");
        Console.WriteLine("  ║    4. Reach the EXIT (Room 1, top-left).             ║");
        Console.WriteLine("  ║                                                       ║");
        Console.WriteLine("  ║  Opening the chest wakes the Grue — it hunts you!   ║");
        Console.WriteLine("  ╚═══════════════════════════════════════════════════════╝");
        Console.WriteLine();
        Console.Write("  Press ENTER to start...");
        Console.ReadLine();
    }

    private void ShowScene()
    {
        Room? r = dungeon[aRow, aCol];
        Console.WriteLine("  ══════════════════════════════════════════════════════");
        Console.WriteLine($"  Location : {r?.GetDescription() ?? "?"}");

        // Inventory line — warn loudly when lamp is missing
        if (!adventurer.HasLamp())
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write("  Inventory: [NO LAMP]");
            Console.ResetColor();
            Console.WriteLine("  ← pick it up with [L] before moving!");
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
            // Grue-death messages in red
            if (lastMessage.Contains("Grue") || lastMessage.Contains("dark"))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"  >> {lastMessage}");
                Console.ResetColor();
            }
            else Console.WriteLine($"  >> {lastMessage}");
        }

        if (isGruePursuing)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("  !! THE GRUE IS ON YOUR TRAIL — GET TO THE EXIT !!");
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

    // ───────────────────────────────────────────────────────────────
    //  GAME LOGIC
    // ───────────────────────────────────────────────────────────────

    private void ProcessInput(string input)
    {
        if (input == GO_NORTH) Move(-1, 0, GO_SOUTH);
        else if (input == GO_SOUTH) Move(1, 0, GO_NORTH);
        else if (input == GO_WEST) Move(0, -1, GO_EAST);
        else if (input == GO_EAST) Move(0, 1, GO_WEST);
        else if (input == GET_LAMP) GetLamp();
        else if (input == GET_KEY) GetKey();
        else if (input == OPEN_CHEST) OpenChest();
        else Quit();
    }

    private void Move(int dr, int dc, string backDir)
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

        aRow += dr; aCol += dc; lastDirection = backDir;

        // Check if new cell is a dark corridor without the lamp
        Room? dest = dungeon[aRow, aCol];
        bool destLit = dest?.IsLit() ?? true;
        if (!destLit && !adventurer.HasLamp())
        {
            lastMessage = "It is pitch black. You are eaten by a Grue!";
            isAdventureAlive = false;
        }
    }

    private void GetLamp()
    {
        Room? r = dungeon[aRow, aCol];
        if (r?.HasLamp() ?? false)
        {
            adventurer.SetLamp(true);
            r!.SetLamp(false);
            lastMessage = "You pick up the LAMP. The darkness retreats!";
        }
        else lastMessage = adventurer.HasLamp() ? "You already carry the lamp." : "There is no lamp here.";
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
                lastMessage = "You seized the TREASURE! The Grue stirs — RUN to the exit!";
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
        {
            MoveGrueBFS();
            if (gRow == aRow && gCol == aCol)
            {
                lastMessage = "The Grue has caught you! DEVOURED.";
                isAdventureAlive = false;
                return;
            }
        }

        if (isChestOpen && aRow == exitRow && aCol == exitCol)
            hasReachedExit = true;
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
                Room? cur = dungeon[r, c];
                bool door = d == 0 ? (cur?.HasNorth() ?? false)
                          : d == 1 ? (cur?.HasSouth() ?? false)
                          : d == 2 ? (cur?.HasWest() ?? false)
                          : (cur?.HasEast() ?? false);
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
