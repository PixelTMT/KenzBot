using KenzBot.Kick;

namespace KenzBot
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            List<Task> Bots = new();
            Bots.Add(KenzKickMain.MainProgram(args));
            //Bots.Add(DiscordBot.RunMain(args));

            await Task.WhenAll(Bots);
        }
    }
}
