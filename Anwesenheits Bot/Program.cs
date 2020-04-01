using System.IO;
using System;

namespace Anwesenheits_Bot
{
    class Program
    {
        static string token = "";

        static void Main(string[] args)
        {
            if (File.Exists(Environment.CurrentDirectory + @"\token.txt"))
            {
                token = File.ReadAllText(Environment.CurrentDirectory + @"\token.txt");
            }
            else
            {
                return;
            }

            new Client(token).InitializeAsync().GetAwaiter().GetResult();
        }
    }
}
