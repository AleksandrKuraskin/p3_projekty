using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using BlackjackShared;

namespace BlackjackServer;

public static class UserManager
{
    private const string DbFile = "users.db";

    public static Profile? Login(string username, string password)
    {
        EnsureDbExists();
        var lines = File.ReadAllLines(DbFile);
        return lines
            .Select(line => line.Split('|'))
            .Where(parts => parts.Length >= 4
                            && parts[0].Equals(username)
                            && parts[1] == Hash(password))
            .Select(parts => new Profile(parts[0], double.Parse(parts[2]))
            {
                Xp = int.Parse(parts[3])
            }).FirstOrDefault();
    }

    public static bool Register(string username, string password)
    {
        EnsureDbExists();
        var lines = File.ReadAllLines(DbFile);
        if (lines.Any(l => l.StartsWith(username + "|")))
        {
            return false;
        }

        string entry = $"{username}|{Hash(password)}|1000|0";
        File.AppendAllLines(DbFile, new[] { entry });
        return true;
    }

    public static void SaveUser(Profile p)
    {
        EnsureDbExists();

        var tempLines = new List<string>();
        if (File.Exists(DbFile)) tempLines = File.ReadAllLines(DbFile).ToList();
        
        for(int i=0; i<tempLines.Count; i++)
        {
            var parts = tempLines[i].Split('|');
            if (parts[0].Equals(p.Name))
            {
                tempLines[i] = $"{p.Name}|{parts[1]}|{p.Balance}|{p.Xp}";
                File.WriteAllLines(DbFile, tempLines);
                return;
            }
        }
    }

    private static void EnsureDbExists()
    {
        if (!File.Exists(DbFile)) File.Create(DbFile).Close();
    }

    private static string Hash(string input)
    {
        using (var sha = SHA256.Create())
        {
            var bytes = Encoding.UTF8.GetBytes(input);
            var hash = sha.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }
    }
}