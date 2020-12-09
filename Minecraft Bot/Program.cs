using System;
using System.IO;

namespace Minecraft_Bot
{
    class Program
    {
        static string token = "";

        static void Main(string[] args)
        {
            if (File.Exists("/home/Host/GCCBot/token.txt"))
            {
                token = File.ReadAllText("/home/Host/GCCBot/token.txt");
            }
            else
            {
                return;
            }

            new Client(token).InitializeAsync().GetAwaiter().GetResult();
        }
    }
}
