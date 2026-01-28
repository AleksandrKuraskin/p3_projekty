using MiniCanteen.Models.Areas.DiningArea;

namespace MiniCanteen.Models;

public class Student(string name, SemaphoreSlim leftFork, SemaphoreSlim rightFork)
{
    public string Name { get; } = name;
    public string State { get; private set; } = "Thinking";
    public string Icon { get; private set; } = "🤔";
    
    private readonly SemaphoreSlim _leftFork = leftFork;
    private readonly SemaphoreSlim _rightFork = rightFork;

    public async Task StartCycleAsync(CancellationToken token)
    {
        var rnd = new Random();
        try
        {
            while (!token.IsCancellationRequested)
            {
                SetState("Thinking", "🤔");
                await Task.Delay(rnd.Next(3000, 10000), token);
                
                SetState("Waiting", "🤤");
                
                var gotForks = false;
                
                if (await _leftFork.WaitAsync(0, token))
                {
                    try
                    {
                        if (await _rightFork.WaitAsync(0, token))
                        {
                            gotForks = true;
                        }
                    }
                    finally
                    {
                        if (!gotForks)
                        {
                            _leftFork.Release();
                        }
                    }
                }

                if (gotForks)
                {
                    try 
                    {
                        SetState("Eating", "🍝");
                        await Task.Delay(rnd.Next(3000, 6000), token);
                    }
                    finally
                    {
                        _rightFork.Release();
                        _leftFork.Release();
                    }
                }
                else
                {
                    await Task.Delay(500, token);
                }
            }
        }
        catch(OperationCanceledException) {}
    }

    private void SetState(string status, string icon)
    {
        State = status;
        Icon = icon;
    }
}