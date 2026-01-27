using BlackjackShared;

namespace BlackjackClient;

public static class ConsoleRenderer
{
    private const int Width = 80;
    private const int TableWidth = 50;
    
    private static readonly Lock LogLock = new ();
    
    private static readonly List<string> GameLog = [];

    public static void AddLog(string message)
    {
        lock (LogLock)
        {
            GameLog.Add($"[{DateTime.Now:HH:mm:ss}] {message}");
            if (GameLog.Count > 5) GameLog.RemoveAt(0);
        }
    }
    
    public static void Draw(bool hasInput = false)
    {
        try
        {
            Console.SetCursorPosition(0, 0);
            Console.Clear();

            switch (ClientState.View)
            {
                case ViewState.Welcome:
                    DrawWelcome();
                    break;
                case ViewState.Lobby:
                    if (ClientState.UserProfile != null) DrawHeader(ClientState.UserProfile);
                    DrawLobby();
                    break;
                case ViewState.Table:
                    if (ClientState.UserProfile != null) DrawHeader(ClientState.UserProfile);
                    if (ClientState.CurrentTable == null) break;

                    DrawDealer(ClientState.CurrentTable.DealerHand);
                    Console.WriteLine();
                    if (ClientState.UserProfile != null)
                        DrawSeats(ClientState.CurrentTable.Seats, ClientState.UserProfile);
                    
                    if (ClientState.CurrentTable.TimerEnd.HasValue)
                    {
                        var timeLeft = (ClientState.CurrentTable.TimerEnd.Value.ToUniversalTime() - DateTime.UtcNow).TotalSeconds;
                        if (timeLeft < 0) timeLeft = 0;

                        var label = $" {ClientState.CurrentTable.TimerLabel}: {timeLeft:0}s ";
                        DrawSeparator("=", label);

                        var bar = GenerateProgressBar(timeLeft, ClientState.CurrentTable.TimerDuration);
                        Console.WriteLine($"\n{bar}");
                    }
                    break;
            }
            Console.WriteLine();
            DrawLogAndFooter();
        }
        catch (Exception)
        {
            Console.Clear();
            Console.WriteLine("[UI Error] Resize terminal or check connection.");
        }
        finally
        {
            if (!hasInput)
            {
                Console.Write(" > ");
            } 
        }
    }

    private static void DrawWelcome()
    {
        Console.Clear();
        DrawSeparator("=");
        Console.WriteLine("\tWELCOME TO TERMINAL BLACKJACK".PadRight(Width));
        DrawSeparator("=");
        Console.WriteLine("\n");
        Console.WriteLine("\tType 'login <username> <password>' to log into your account.");
        Console.WriteLine("\tType 'register <username> <password>' to create a new account.");
        Console.WriteLine("\tType 'guest' to play as a guest user.");
        Console.WriteLine("\tType 'exit' to exit the game.");
    }

    private static void DrawLogAndFooter()
    {
        DrawSeparator("-", " SYSTEM LOG ");

        lock (LogLock)
        {
            for (var i = 0; i < 6; i++)
            {
                var line = i < GameLog.Count ? GameLog[i].PadRight(Width - 2) : new string(' ', Width);
                Console.WriteLine($"{line}");
            }
        }
        DrawSeparator("=");
    }

    private static void DrawHeader(Profile p)
    {
        DrawSeparator("=");

        var content = $"\tUSER: {p.Name,-8} | BALANCE: ${p.Balance,-8} | LVL: {p.Level} ({p.Xp} / 1000 XP)";
        Console.WriteLine(content.PadRight(Width));
        DrawSeparator("=");
    }

    private static void DrawDealer(Hand dealerHand)
    {

        var handStr = dealerHand.ToString();
        var scoreStr = $"Score: {dealerHand.StringScore} {dealerHand.Status}";
        const string label = "DEALER";

        const int padding = (Width - TableWidth) / 2;
        var padStr = new string(' ', padding);

        Console.WriteLine($"{padStr}┌────────────────── {label} ───────────────────┐");
        Console.WriteLine($"{padStr}│  {handStr,-43}│");
        Console.WriteLine($"{padStr}│  {scoreStr,-43}│");
        Console.WriteLine($"{padStr}└─────────────────────────────────────────────┘");
    }

    private static void DrawSeats(List<Seat> seats, Profile myProfile)
    {
        var activeSeats = seats.Where(s => s.IsTaken).ToList();

        if (activeSeats.Count == 0)
        {
            Console.WriteLine("Waiting for players...".PadLeft(Width / 2 + 10));
            return;
        }
        
        const int seatsPerRow = 2;
        const int boxWidth = 36;
        const int innerWidth = boxWidth - 4;
        const string emptySpace = "    ";
        
        for (var i = 0; i < activeSeats.Count; i += seatsPerRow)
        {
            var rowSeats = activeSeats.Skip(i).Take(seatsPerRow).ToList();

            Console.Write(" ");
            foreach (var seat in rowSeats)
            {
                var name = (seat.Player == myProfile) ? "YOU" : seat.Player?.Name;
                var moveIndicator = seat.SeatNumber - 1 == ClientState.CurrentTable?.CurrentTurnSeatIndex ? " (!)" : "";
                var title = $" {name} (Seat {seat.SeatNumber}){moveIndicator} ";
                
                var dashes = boxWidth - 4 - title.Length;
                if (dashes < 0) dashes = 0;
            
                var topBorder = $"┌──{title}{new string('─', dashes)}┐";
                Console.Write(topBorder + emptySpace);
            }
            Console.WriteLine();
            Console.Write(" ");
            foreach (var seat in rowSeats)
            {
                var cards = string.Join(" ", seat.Hand.Cards.Select(c => c.Symbol));

                if (cards.Length > innerWidth) cards = cards[..(innerWidth - 4)] + "...";

                Console.Write($"│ {cards, -innerWidth} │{emptySpace}");
            }
            Console.WriteLine();

            Console.Write(" ");
            foreach (var seat in rowSeats)
            {
                var content = $"Score: {seat.Hand.StringScore} {seat.Hand.Status}";
                Console.Write($"│ {content, -innerWidth} │{emptySpace}");
            }
            Console.WriteLine();
            
            Console.Write(" ");
            foreach (var _ in rowSeats)
            {
                Console.Write($"└{new string('─', innerWidth + 2)}┘{emptySpace}");
            }
            Console.WriteLine("\n");
        }
    }

    public static void DrawExit()
    {
        Console.Clear();
        Console.WriteLine("Exiting game...");
    }

    private static void DrawLobby()
    {
        Console.WriteLine();
        DrawSeparator("=", " LOBBY ");

        if (ClientState.LobbyList.Count == 0)
        {
            Console.WriteLine("\tNo tables found. Type 'create <name> <max_players>' to start.".PadRight(Width));
        }
        else
        {
            Console.WriteLine($"\t{"ID", -4} | {"TABLE NAME", -30} | PLAYERS".PadRight(Width));
            Console.WriteLine(new string('-', Width));
            
            foreach (var t in ClientState.LobbyList.Take(10)) 
            {
                var row = $"\t{t.Id,-4} | {t.Name,-30} | {t.PlayerCount}/{t.MaxPlayers}";
                Console.WriteLine(row.PadRight(Width));
            }
        }

        Console.WriteLine();
        DrawSeparator("-");
        Console.WriteLine("\tCOMMANDS: list, join <id/name>, create <name> <max_players>, logout, exit".PadRight(Width));
        DrawSeparator("=");
    }

    private static void DrawSeparator(string c, string title = "")
    {
        if (string.IsNullOrEmpty(title))
        {
            Console.WriteLine(new string(c[0], Width));
        }
        else
        {
            var sideLen = (Width - title.Length - 2) / 2;
            var side = new string(c[0], sideLen);

            var result = $"{side} {title} {side}";
            Console.WriteLine(result.PadRight(Width, c[0]));
        }
    }
    
    private static string GenerateProgressBar(double current, int max)
    {
        var barLength = Width;
        if (max == 0) max = 1;
        var percent = current / max;
        if (percent < 0) percent = 0;
        if (percent > 1) percent = 1;

        var filled = (int)(barLength * percent);
        var empty = barLength - filled;

        return new string('█', filled) + new string('.', empty);
    }
}