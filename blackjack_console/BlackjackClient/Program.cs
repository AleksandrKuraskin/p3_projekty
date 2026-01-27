using System.Text;

namespace BlackjackClient;

internal static class Program
{
    private static void Main()
    { 
        ClientState.Init();
        
        var inputBuffer = new StringBuilder();
        var running = true;

        while (running)
        {
            if (Console.KeyAvailable)
            {
                var key = Console.ReadKey(true);

                switch (key.Key)
                {
                    case ConsoleKey.Enter:
                        var cmd = inputBuffer.ToString().Trim();
                        inputBuffer.Clear();
                        CommandProcessor.Process(cmd, ref running);
                        break;
                    case ConsoleKey.Backspace:
                        if (inputBuffer.Length > 0) inputBuffer.Remove(inputBuffer.Length - 1, 1);
                        break;
                    default:
                        if (key.KeyChar is >= ' ' and <= '~') inputBuffer.Append(key.KeyChar);
                        break;
                }

                ConsoleRenderer.Draw(true);
                Console.Write(" > " + inputBuffer);
            }
            
            if (ClientState.IsHandInProgress && DateTime.UtcNow > ClientState.TurnExpiresAt)
            {
                ConsoleRenderer.AddLog("[System] Time expired! Auto-standing.");
                CommandProcessor.Process("stand", ref running);
                ConsoleRenderer.Draw(true);
                Console.Write(" > " + inputBuffer);
            }

            if (ClientState.CurrentTable?.TimerEnd.HasValue == true)
            {
                ConsoleRenderer.Draw(true);
                Console.Write(" > " + inputBuffer);
            }
            
            Thread.Sleep(17);
        }
        
        ConsoleRenderer.DrawExit();
    }
}