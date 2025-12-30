using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Text.Json;
using BlackjackShared;

namespace BlackjackClient;

public static class ConsoleRenderer
{
    private const int Width = 80;
    private const int TableWidth = 50;
    
    private static readonly System.Threading.Lock LogLock = new ();
    
    private static readonly List<string> GameLog = new List<string>();

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
                    if (ClientState.CurrentTable != null)
                    {
                        DrawDealer(ClientState.CurrentTable.DealerHand);
                        Console.WriteLine();
                        if (ClientState.UserProfile != null)
                            DrawSeats(ClientState.CurrentTable.Seats, ClientState.UserProfile);
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
                if (i < GameLog.Count) 
                    Console.WriteLine($" {GameLog[i].PadRight(Width - 2)} ");
                else 
                    Console.WriteLine(new string(' ', Width));
            }
        }
        DrawSeparator("=");
    }

    private static void DrawHeader(Profile p)
    {
        DrawSeparator("=");

        string content = $"\tUSER: {p.Name,-8} | BALANCE: ${p.Balance,-8} | LVL: {p.Level} ({p.Xp} / 1000 XP)";
        Console.WriteLine(content.PadRight(Width));
        DrawSeparator("=");
    }

    private static void DrawDealer(Hand dealerHand)
    {

        string handStr = dealerHand.ToString();
        string scoreStr = $"Score: {dealerHand.StringScore} {dealerHand.Status}";
        string label = "DEALER";

        int padding = (Width - TableWidth) / 2;
        string padStr = new string(' ', padding);

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
        const string emptySpace = "    ";
        
        for (var i = 0; i < activeSeats.Count; i += seatsPerRow)
        {
            var rowSeats = activeSeats.Skip(i).Take(seatsPerRow).ToList();

            Console.Write(" ");
            foreach (var seat in rowSeats)
            {
                var name = (seat.Player == myProfile) ? "YOU" : seat.Player.Name;
                var moveIndicator = seat.SeatNumber - 1 == ClientState.CurrentTable.CurrentTurnSeatIndex ? " (!)" : "";
                var title = $" {name} (Seat {seat.SeatNumber}){moveIndicator} ";
                
                var dashes = boxWidth - 4 - title.Length;
                if (dashes < 0) dashes = 0;
            
                string topBorder = $"┌──{title}{new string('─', dashes)}┐";
                Console.Write(topBorder + emptySpace);
            }
            Console.WriteLine();
            Console.Write(" ");
            foreach (var seat in rowSeats)
            {
                string cards = string.Join(" ", seat.Hand.Cards.Select(c => c.Symbol));

                if (cards.Length > boxWidth - 4) cards = cards.Substring(0, boxWidth - 7) + "...";

                Console.Write($"│ {cards.PadRight(boxWidth - 4)} │{emptySpace}");
            }
            Console.WriteLine();

            Console.Write(" ");
            foreach (var seat in rowSeats)
            {
                string content = $"Score: {seat.Hand.StringScore} {seat.Hand.Status}";
                Console.Write($"│ {content.PadRight(boxWidth - 4)} │{emptySpace}");
            }
            Console.WriteLine();
            
            Console.Write(" ");
            foreach (var seat in rowSeats)
            {
                Console.Write($"└{new string('─', boxWidth - 2)}┘{emptySpace}");
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
            Console.WriteLine("\tNo tables found. Type 'create <name>' to start.".PadRight(Width));
        }
        else
        {
            Console.WriteLine($"\t{"ID", -4} | {"TABLE NAME", -30} | PLAYERS".PadRight(Width));
            Console.WriteLine(new string('-', Width));
            
            foreach (var t in ClientState.LobbyList.Take(10)) 
            {
                string row = $"\t{t.Id,-4} | {t.Name,-30} | {t.PlayerCount}/{t.MaxPlayers}";
                Console.WriteLine(row.PadRight(Width));
            }
        }

        Console.WriteLine();
        DrawSeparator("-");
        Console.WriteLine("\tCOMMANDS: list, join <id>, create <name>, logout, exit".PadRight(Width));
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
            int sideLen = (Width - title.Length) / 2;
            string side = new string(c[0], sideLen);

            string result = $"{side}{title}{side}";
            Console.WriteLine(result.PadRight(Width, c[0]));
        }
    }
    
    private static string GenerateProgressBar(double current, int max)
    {
        int barLength = 30;
        if (max == 0) max = 1;
        double percent = (double)current / max;
        if (percent < 0) percent = 0;
        if (percent > 1) percent = 1;

        int filled = (int)(barLength * percent);
        int empty = barLength - filled;

        return new string('█', filled) + new string('.', empty);
    }
}