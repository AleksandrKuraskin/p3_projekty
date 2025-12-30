using System;
using System.Text;
using System.Threading;
using BlackjackShared;

namespace BlackjackClient;

static class Program
{
    static void Main(string[] args)
    { 
        ClientState.Init();
        
        StringBuilder inputBuffer = new StringBuilder();
        bool running = true;

        while (running)
        {
            if (Console.KeyAvailable)
            {
                ConsoleKeyInfo key = Console.ReadKey(true);

                if (key.Key == ConsoleKey.Enter)
                {
                    string cmd = inputBuffer.ToString().Trim();
                    inputBuffer.Clear();
                    CommandProcessor.Process(cmd, ref running);
                }
                else if (key.Key == ConsoleKey.Backspace)
                {
                    if (inputBuffer.Length > 0) inputBuffer.Remove(inputBuffer.Length - 1, 1);
                }
                else if (key.KeyChar >= ' ' && key.KeyChar <= '~')
                {
                    inputBuffer.Append(key.KeyChar);
                }
                
                ConsoleRenderer.Draw(true);
                Console.Write(" > " + inputBuffer.ToString());
            }
            
            if (ClientState.IsHandInProgress && DateTime.Now > ClientState.TurnExpiresAt)
            {
                ConsoleRenderer.AddLog("[System] Time expired! Auto-Standing.");
                CommandProcessor.Process("stand", ref running);
                ConsoleRenderer.Draw();
                Console.Write(" > " + inputBuffer.ToString());
            }
            
            Thread.Sleep(50);
        }
        
        ConsoleRenderer.DrawExit();
    }
}