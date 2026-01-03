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
    public int Id { get; init; }
    public string Name { get; init; } = "";
    [JsonIgnore]
    private Deck Deck { get; init; } = new Deck(5);
    public Hand DealerHand { get; set; } = new Hand();
    public List<Seat> Seats { get; init; } = [];
    [JsonIgnore]
    public int MaxPlayers => Seats.Count;
    [JsonIgnore]
    public int PlayerCount => Seats.Count(s => s.Player != null);

    public GameState State { get; set; } = GameState.Betting;
    public int CurrentTurnSeatIndex { get; set; } = -1; 
    
    private DateTime _dealerActionStart = DateTime.MinValue;
    
    public DateTime? TimerEnd { get; set; }
    public int TimerDuration { get; set; }
    public string TimerLabel { get; set; } = "";

    [JsonConstructor]
    public Table() { }
    
    public Table(int id, string name, int seatCount)
    {
        Id = id;
        Name = name;
        Deck = new Deck(5);
        DealerHand = new Hand();
        Seats = new List<Seat>();
        
        for (var i = 0; i < seatCount; i++)
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
        
        var wasActiveTurn = (State == GameState.Playing && CurrentTurnSeatIndex == seat.SeatNumber - 1);
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
        

        if (seat.Hand.AbsScore >= 21)
            NextTurn();
        else 
            SetTimer(30, $"Player {player.Name}'s Turn");
        
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
        var readyCount = players.Count(p => p.CurrentBet > 0);
        
        if (readyCount == players.Count && players.Count > 0)
        {
            SetTimer(5, "Game starting in");
        }
        else if (readyCount == 1)
        {
            SetTimer(30, "Place your bets");
        }
    }
    
    private void SetTimer(int seconds, string label)
    {
        TimerEnd = DateTime.UtcNow.AddSeconds(seconds);
        TimerDuration = seconds;
        TimerLabel = label;
    }
    
    private void ClearTimer()
    {
        TimerEnd = null;
        TimerLabel = "";
    }
    public bool Heartbeat()
    {
        if (TimerEnd.HasValue && DateTime.UtcNow >= TimerEnd.Value)
        {
            HandleTimeout();
            return true;
        }
        if (State != GameState.DealerTurn) return false;
        
        if (DateTime.UtcNow < _dealerActionStart) return false;

        var act = ExecuteDealerStep();
        if(act) _dealerActionStart = DateTime.UtcNow.AddSeconds(1);
        return act;
    }

    private void HandleTimeout()
    {
        ClearTimer();

        switch (State)
        {
            case GameState.Betting:
                var players = Seats.Where(s => s.IsTaken).ToList();
                if (players.Count > 0 && players.Any(s => s.CurrentBet > 0))
                {
                    Console.WriteLine($"Starting round at table {Id}.");
                    StartGame();
                }
                break;
            case GameState.Playing:
                if (CurrentTurnSeatIndex >= 0 && CurrentTurnSeatIndex < Seats.Count)
                {
                    NextTurn();
                }
                break;
            case GameState.GameOver:
                ResetTable();
                break;
            case GameState.DealerTurn:
            default: break;
        }
    }
    
    private void StartGame()
    {
        ClearTimer();
        State = GameState.Playing;
        DealerHand = new Hand();
        _dealerActionStart = DateTime.MinValue;

        for(var i = 0; i < 2; ++i) 
            foreach (var s in Seats.Where(s => s is {IsTaken: true, CurrentBet: > 0})) s.Hand.AddCard(Deck.Draw());
        
        DealerHand.AddCard(Deck.Draw());
        DealerHand.AddCard(Deck.Draw(false));

        NextTurn();
    }
    

    private void NextTurn()
    {
        var next = CurrentTurnSeatIndex + 1;
        while (next < Seats.Count)
        {
            if (Seats[next].IsTaken && Seats[next].CurrentBet > 0 && Seats[next].Hand.Score() < 21)
            {
                CurrentTurnSeatIndex = next;
                SetTimer(30, $"It's {Seats[next].Player?.Name}'s Turn");
                return;
            }
            next++;
        }

        ClearTimer();
        var players = Seats.Where(s => s is {IsTaken: true, CurrentBet: > 0}).ToList();
        if (State == GameState.Playing && players.All(s => s.Hand.IsBust || s.Hand.IsBlackjack))
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

        var score = DealerHand.Score();
        if (Math.Abs(score) < 17)
        {
            DealerHand.AddCard(Deck.Draw());
            return true;
        }

        ResolvePayouts();
        return true;
    }

    
    private void ResolvePayouts()
    {
        State = GameState.GameOver;
        var dScore = DealerHand.AbsScore;

        foreach (var seat in Seats.Where(s => s is { IsTaken: true, CurrentBet: > 0, Player: not null} ))
        {
            if (seat.Player == null) continue;
            var pScore = seat.Hand.AbsScore;
            var xpEarned = 50;

            if (seat.Hand.IsBust)
            {
                xpEarned += 50; 
            }
            else
            {
                if (seat.Hand.IsBlackjack)
                {
                    seat.Player.Balance += seat.CurrentBet * 2.5;
                    xpEarned += 250;
                }
                else if (DealerHand.IsBust || pScore > dScore)
                {
                    seat.Player.Balance += seat.CurrentBet * 2;
                    xpEarned += 200;
                }
                else if (pScore == dScore)
                {
                    seat.Player.Balance += seat.CurrentBet;
                    xpEarned += 100;
                }
                else
                {
                    xpEarned += 50;
                }
            }

            seat.Player.GlobalXp += xpEarned;
        }
        
        SetTimer(3, "Next round starts in");
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