using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text.Json.Serialization;

namespace BlackjackShared;

public enum GameState
{
    Betting,
    Playing,
    DealerTurn,
    GameOver
}

public class Table
{
    public int Id { get; set; }
    public string Name { get; set; }
    public Deck Deck { get; set; }
    public Hand DealerHand { get; set; }
    public List<Seat> Seats { get; set; }
    public int MaxPlayers => Seats.Count;
    public int PlayerCount => Seats.Count(s => s.Player != null);

    public GameState State { get; set; } = GameState.Betting;
    public int CurrentTurnSeatIndex { get; set; } = -1; 

    [JsonConstructor]
    public Table() { }
    
    public Table(int id, string name, int seatCount, int players = 0)
    {
        Id = id;
        Name = name;
        Deck = new Deck(5);
        DealerHand = new Hand();
        Seats = new List<Seat>();
        
        for (int i = 0; i < seatCount; i++)
        {
            Seats.Add(new Seat(i + 1));
        }
    }
    
    public string Sit(Profile player, int seatIndex)
    {
        if (seatIndex < 0 || seatIndex >= Seats.Count) return "Selected seat is invalid.";
        if (Seats[seatIndex].IsTaken) return "Seat is already taken.";

        if (Seats.Any(s => s.Player == player)) return "You are already sitting at this table.";

        Seats[seatIndex].SitDown(player);
        return "OK";
    }

    public string StandUp(Profile player)
    {
        var seat = GetSeatOf(player);
        if (seat == null) return "You are not sitting at a table.";
        seat.StandUp();

        if (State == GameState.Betting && !Seats.Any(s => s.IsTaken)) 
        {
            ResetTable();
            return "OK";
        }
        
        bool wasActiveTurn = (State == GameState.Playing && CurrentTurnSeatIndex == seat.SeatNumber - 1);
        if (wasActiveTurn) NextTurn();
        else if(State == GameState.Betting) CheckStartRound();
        
        return "OK";
    }
    
    public string PlaceBet(Profile player, int amount)
    {
        if (amount <= 0) return "Bet must be positive.";
        
        var seat = GetSeatOf(player);
        if (seat == null) return "You must sit to bet.";
        if (State != GameState.Betting) return "Game is in progress.";
        if (player.Balance < amount) return "Insufficient funds.";

        player.Balance -= amount;
        seat.AddBet(amount);

        CheckStartRound();
        return "OK";
    }

    public string Hit(Profile player)
    {
        var seat = GetSeatOf(player);
        if (seat == null) return "You are not seated.";

        if (State != GameState.Playing) return "Wait for the round to start.";
        if (CurrentTurnSeatIndex != seat.SeatNumber - 1) return "It is not your turn.";

        seat.Hand.AddCard(Deck.Draw());

        if (seat.Hand.IsBust || seat.Hand.Score() == 21)
        {
            NextTurn();
        }

        return "OK";
    }

    public string Stand(Profile player)
    {
        var seat = GetSeatOf(player);
        if (seat == null) return "You are not sitting at this table.";

        if (State != GameState.Playing) return "Wait for the round to start.";
        if (CurrentTurnSeatIndex != seat.SeatNumber - 1) return "It is not your turn.";

        NextTurn();
        return "OK";
    }

    private void CheckStartRound()
    {
        var players = Seats.Where(s => s.IsTaken).ToList();
        if (players.Count > 0 && players.All(s => s.CurrentBet > 0))
        {
            Console.WriteLine($"Starting round at table {Id}.");
            StartGame();
        }
    }
    
    private void StartGame()
    {
        State = GameState.Playing;
        DealerHand = new Hand();

        foreach (var seat in Seats.Where(s => s.IsTaken))
        {
            seat.ResetHand();
            seat.Hand.AddCard(Deck.Draw());
            seat.Hand.AddCard(Deck.Draw());
        }
        
        DealerHand.AddCard(Deck.Draw());
        DealerHand.AddCard(Deck.Draw(false));

        NextTurn();
    }
    

    private void NextTurn()
    {
        int next = CurrentTurnSeatIndex + 1;
        Console.WriteLine($"Turn for seat {next}");

        while (next < Seats.Count)
        {
            if (Seats[next].IsTaken && Seats[next].CurrentBet > 0 && Seats[next].Hand.Score() < 21)
            {
                CurrentTurnSeatIndex = next;
                return;
            }
            next++;
        }

        if (State == GameState.Playing && Seats.All(s => s.Hand.IsBust || s.Hand.IsBlackjack))
        {
            ResolvePayouts();
        }
        else
        {
            State = GameState.DealerTurn;
            CurrentTurnSeatIndex = 99;
        }
    }

    public bool ExecuteDealerStep()
    {
        if (State != GameState.DealerTurn) return false;

        if (DealerHand.HasHiddenCard)
        {
            foreach (var c in DealerHand.Cards) c.IsFaceUp = true;
            return true;
        }

        int score = DealerHand.Score();
        if (Math.Abs(score) < 17)
        {
            DealerHand.AddCard(Deck.Draw());
            return true;
        }

        ResolvePayouts();
        return false;
    }

    
    private void ResolvePayouts()
    {
        State = GameState.GameOver;
        int dScore = DealerHand.AbsScore;

        foreach (var seat in Seats.Where(s => s.IsTaken && s.CurrentBet > 0))
        {
            int pScore = seat.Hand.AbsScore;
            if (!seat.Hand.IsBust)
            {
                if(seat.Hand.IsBlackjack)
                    seat.Player.Balance += seat.CurrentBet * 2.5;
                else if (DealerHand.IsBust || pScore > dScore)
                    seat.Player.Balance += seat.CurrentBet * 2;
                else if (pScore == dScore)
                    seat.Player.Balance += seat.CurrentBet;
            }
        }
        
        ResetTable();
    }
    
    private void ResetTable()
    {
        State = GameState.Betting;
        Deck.CheckEmpty();
        CurrentTurnSeatIndex = -1;
        DealerHand = new Hand();

        foreach (var s in Seats) 
        {
            s.ResetHand();
            s.ResetBet();
        }
    }
    public Seat? GetSeatOf(Profile user)
    {
        return Seats.FirstOrDefault(s => s.Player?.Name == user.Name);
    }
}