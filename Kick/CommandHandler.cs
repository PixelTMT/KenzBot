using KenzBot.Kick;
using KickLib;
using KickLib.Api.Interfaces;
using KickLib.Client.Models.Args;

public static class CommandHandler
{
    public static readonly Dictionary<string, Func<ChatMessageEventArgs, string[], Task>> Commands
        = new(StringComparer.OrdinalIgnoreCase);

    public static void RegisterCommand(string name, Func<ChatMessageEventArgs, string[], Task> action)
    {
        if (!Commands.ContainsKey(name))
        {
            Commands.Add(name, action);
            Console.WriteLine($"✅ Registered command: !{name}");
        }
        else
        {
            Console.WriteLine($"⚠️ Command '{name}' already exists.");
        }
    }

    public static async Task HandleCommand(ChatMessageEventArgs e, IChat chatClient)
    {
        string message = e.Data.Content.Trim();

        if (!message.StartsWith("!")) return; // Change prefix if you want

        string[] parts = message.Substring(1).Split(' ', StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length == 0) return;

        string commandName = parts[0];
        string[] args = parts.Skip(1).ToArray();

        if (Commands.TryGetValue(commandName, out var action))
        {
            try
            {
                await action(e, args);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"🔥 Error in command '{commandName}': {ex.Message}");
                await chatClient.SendMessageAsBotAsync($"Error: {ex.Message}");
            }
        }
        else
        {
            //Console.WriteLine($"❌ Unknown command: {commandName}");
        }
    }
    public static async Task HandleSO(ChatMessageEventArgs e, IKickApi chatAPI, JsonSO shoutOutJson)
    {
        string message = e.Data.Content.Trim();
        //bool isVerified = e.Data.Sender.Identity.Badges.Select((x) => x.Text).ToArray().Contains("Verified channel");
        shoutOutJson.Check();
        if (shoutOutJson.alreadyCheck.Contains(e.Data.Sender.Username)) return;

        shoutOutJson.alreadyCheck.Add(e.Data.Sender.Username);
        try
        {
            var channel = await chatAPI.Channels.GetChannelAsync(e.Data.Sender.Slug);
            if (channel.IsSuccess)
            {
                bool isLiveStreamer = channel.Value.Stream.IsLive;
                if ((isLiveStreamer))
                {
                    string shoutoutMSG = $"Go Follow @{e.Data.Sender.Username} Channel {(isLiveStreamer ? "(Currently Live)" : "")} -> https://kick.com/{channel.Value.Slug}";
                    //Console.WriteLine(shoutoutMSG);
                    var history = new JsonSO.History_SO()
                    {
                        name = e.Data.Sender.Username,
                        url = $"https://kick.com/{channel.Value.Slug}"
                    };
                    if (!shoutOutJson.history.Contains(history))
                    {
                        shoutOutJson.history.Add(history);
                    }
                    var soMSG = await chatAPI.Chat.SendMessageAsBotAsync(shoutoutMSG);
                    if (soMSG.IsSuccess)
                    {
                        await shoutOutJson.SaveToFile();
                    }
                    else
                    {
                        Console.WriteLine(string.Join("\n", soMSG.Errors));
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.Write($"HandleSO Error: {ex.Message}");
            await KenzKickMain.RefreshToken();
        }
    }
    public static async Task HandleBanUsernameContain(ChatMessageEventArgs e, int broadcasterID, IKickApi chatAPI, JsonBanned banContain)
    {
        try
        {
            string message = e.Data.Content.Trim();

            bool isUserTalking = banContain.userContain.Exists(name => e.Data.Sender.Username.ToLower().Contains(name.ToLower()));

            if (isUserTalking)
            {
                Console.WriteLine($"Ban {e.Data.Sender.Username}: {banContain.userContain.Where(name => e.Data.Sender.Username.ToLower().Contains(name.ToLower())).First()}");
                await chatAPI.Moderation.BanUserAsync(broadcasterID, e.Data.Sender.Id);
            }
        }
        catch (Exception ex)
        {
            Console.Write($"Error ban user: {ex.Message}");
            await KenzKickMain.RefreshToken();
        }
    }
    public static async Task HandleBanWordContain(ChatMessageEventArgs e, int broadcasterID, IKickApi chatAPI, JsonBanned banContain)
    {
        try
        {
            string message = e.Data.Content.Trim();

            bool isBanWord = banContain.msgContain.Exists(msg => message.ToLower().Contains(msg.ToLower()));

            if (isBanWord)
            {
                Console.WriteLine($"Ban {e.Data.Sender.Username} Containing: {banContain.msgContain.Where(msg => message.ToLower().Contains(msg.ToLower())).First()}");
                await chatAPI.Moderation.BanUserAsync(broadcasterID, e.Data.Sender.Id);
            }
        }
        catch (Exception ex)
        {
            Console.Write($"Error ban word: {ex.Message}");
            await KenzKickMain.RefreshToken();
        }
    }
}
