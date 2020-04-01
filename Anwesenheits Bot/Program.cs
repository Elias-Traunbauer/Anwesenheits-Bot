using System;

namespace Anwesenheits_Bot
{
    class Program
    {
        static string token = "Njg5MDgzNjY1MjQxOTk3MzU4.XoMCXQ.IEoJa7GVxpYHD1w98f2gQyd_uC8";

        static void Main(string[] args) => new Client(token).InitializeAsync().GetAwaiter().GetResult();
    }
}
