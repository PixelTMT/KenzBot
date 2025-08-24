using KickLib;
using KickLib.Api.Interfaces;
using KickLib.Auth;
using KickLib.Client;
using KickLib.Client.Interfaces;
using KickLib.Client.Models.Events.Chatroom;
using KickLib.Core;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using System.Diagnostics;

namespace KenzBot.Kick
{
    internal class KenzKickMain
    {
        static JsonObject jsonAuth;
        static JsonSO shoutOutJson;
        static JsonBanned banContain;

        static string tokenFile = "tokens.json";
        static string refreshTokenFile = "refreshTokens.json";

        static IChat chatbot;
        static IKickApi chatApi;
        static ApiSettings settings;
        static int BroadcasterID;
        static bool EnableBanWord;
        public static async Task MainProgram(string[] args)
        {
            string authStr = File.ReadAllText("Auth");
            jsonAuth = JsonConvert.DeserializeObject<JsonObject>(authStr);
            shoutOutJson = new JsonSO().LoadFile();
            banContain = new JsonBanned().LoadFile();
            if (!File.Exists(tokenFile) || !File.Exists(refreshTokenFile))
            {
                await GetNewToken(args);
                return;
            }
            else
                await RunBotAsync();
        }
        static async Task GetMessage()
        {
            IKickClient client = new KickClient();
            //var channel = await chatApi.Channels.GetChannelAsync(jsonAuth.chatroomName);
            //if (channel.IsSuccess)
            {
                int chatroomId = int.Parse(jsonAuth.chatroomName);

                client.OnMessage += async (sender, e) =>
                {
                    //Console.WriteLine($"📨 {string.Join(" ", e.Data.Sender.Identity.Badges.Select((x) => x.Text).ToArray())} {e.Data.Sender.Username} ({e.Data.Sender.Id}): {e.Data.Content}");
                    List<Task> onMSGTask = new();
                    onMSGTask.Add(CommandHandler.HandleCommand(e, chatbot));
                    onMSGTask.Add(CommandHandler.HandleBanUsernameContain(e, BroadcasterID, chatApi, banContain));
                    if (EnableBanWord)
                        onMSGTask.Add(CommandHandler.HandleBanWordContain(e, BroadcasterID, chatApi, banContain));
                    if (!IsBroadcaster(e.Data.Sender))
                        onMSGTask.Add(CommandHandler.HandleSO(e, chatApi, shoutOutJson));
                    await Task.WhenAll(onMSGTask);
                    onMSGTask.Clear();
                };
                Console.WriteLine($"Listening Channel {chatroomId}");
                await client.ListenToChatRoomAsync(chatroomId);
                await client.ConnectAsync();
            }
        }

        static async Task RunBotAsync()
        {
            // Load tokens from file
            var tokenData = File.ReadAllText(tokenFile);
            var refreshTokenData = File.ReadAllText(refreshTokenFile);
            settings = new ApiSettings
            {
                AccessToken = tokenData,
                RefreshToken = tokenData,
                ClientId = jsonAuth.client_id,
                ClientSecret = jsonAuth.client_secret,
            };

            chatApi = KickApi.Create(settings);
            chatbot = chatApi.Chat;

            settings.RefreshTokenChanged += (sender, args) =>
            {
                try
                {
                    var tokenData = args.NewToken;
                    File.WriteAllText(tokenFile, tokenData);
                    Console.WriteLine("Refresh Tokens updated and saved.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine("New Refresh Token Error: " + ex.Message);
                }
            };

            settings.AccessTokenChanged += (sender, args) =>
            {
                try
                {
                    var refreshTokenData = args.NewToken;
                    File.WriteAllText(refreshTokenFile, refreshTokenData);
                    Console.WriteLine("Access Tokens updated and saved.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine("New Token Error: " + ex.Message);
                }
            };

            while (true)
            {
                try
                {
                    var userResult = await chatApi.Users.GetMeAsync();
                    if (userResult.IsSuccess)
                    {
                        Console.WriteLine($"👤 Logged in as: {userResult.Value.Name} {userResult.Value.UserId}");
                        BroadcasterID = userResult.Value.UserId;
                        break;
                    }
                    else
                    {
                        Console.WriteLine("❌ Failed to get user info");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("❌ Failed to get user info " + ex.Message);
                    //await RefreshToken();
                }
                await Task.Delay(1000);
            }

            CommandHandler.RegisterCommand("hello", async (e, args) =>
            {
                await chatbot.ReplyToMessageAsBotAsync($"Hello @{e.Data.Sender.Username}! 👋", e.Data.Id);
            });
            CommandHandler.RegisterCommand("listBanUser", async (e, args) =>
            {
                if (!IsBroadcaster(e.Data.Sender)) return;
                await chatbot.ReplyToMessageAsBotAsync($"Username Ban List Containing: {string.Join(", ", banContain.userContain)}", e.Data.Id);
            });
            CommandHandler.RegisterCommand("addBanUser", async (e, args) =>
            {
                if (!IsBroadcaster(e.Data.Sender)) return;
                if (!banContain.userContain.Contains(args[0]))
                {
                    banContain.userContain.Add(args[0]);
                    await banContain.SaveToFile();
                    await chatbot.ReplyToMessageAsBotAsync($"Add Username Ban List Containing {args[0]}", e.Data.Id);
                }
                else
                {
                    await chatbot.ReplyToMessageAsBotAsync($"Username Containing {args[0]} is already Listed", e.Data.Id);
                }
            });
            CommandHandler.RegisterCommand("removeBanUser", async (e, args) =>
            {
                if (!IsBroadcaster(e.Data.Sender)) return;
                if (banContain.userContain.Contains(args[0]))
                {
                    banContain.userContain.Remove(args[0]);
                    await banContain.SaveToFile();
                    await chatbot.ReplyToMessageAsBotAsync($"Remove Username Ban List Containing {args[0]}", e.Data.Id);
                }
                else
                {
                    await chatbot.ReplyToMessageAsBotAsync($"Username Containing {args[0]} is not Listed", e.Data.Id);
                }
            });
            CommandHandler.RegisterCommand("EnableBanWord", async (e, args) =>
            {
                if (!IsBroadcaster(e.Data.Sender)) return;
                await chatbot.ReplyToMessageAsBotAsync($"Ban Word is Enable", e.Data.Id);
                EnableBanWord = true;
            });
            CommandHandler.RegisterCommand("DisableBanWord", async (e, args) =>
            {
                if (!IsBroadcaster(e.Data.Sender)) return;
                await chatbot.ReplyToMessageAsBotAsync($"Ban Word is Disable", e.Data.Id);
                EnableBanWord = false;
            });
            CommandHandler.RegisterCommand("listBanWord", async (e, args) =>
            {
                if (!IsBroadcaster(e.Data.Sender)) return;
                await chatbot.ReplyToMessageAsBotAsync($"Word Ban List Containing: {string.Join(", ", banContain.msgContain)}", e.Data.Id);
            });
            CommandHandler.RegisterCommand("addBanWord", async (e, args) =>
            {
                if (!IsBroadcaster(e.Data.Sender)) return;
                string fullArgs = string.Join(" ", args);
                if (!banContain.msgContain.Contains(fullArgs))
                {
                    banContain.msgContain.Add(fullArgs);
                    await banContain.SaveToFile();
                    await chatbot.ReplyToMessageAsBotAsync($"Add Word Ban List Containing {fullArgs}", e.Data.Id);
                }
                else
                {
                    await chatbot.ReplyToMessageAsBotAsync($"Word Containing {fullArgs} is already Listed", e.Data.Id);
                }
            });
            CommandHandler.RegisterCommand("removeBanWord", async (e, args) =>
            {
                if (!IsBroadcaster(e.Data.Sender)) return;
                if (banContain.msgContain.Contains(args[0]))
                {
                    banContain.msgContain.Remove(args[0]);
                    await banContain.SaveToFile();
                    await chatbot.ReplyToMessageAsBotAsync($"Remove Word Ban List Containing {args[0]}", e.Data.Id);
                }
                else
                {
                    await chatbot.ReplyToMessageAsBotAsync($"Word Containing {args[0]} is not Listed", e.Data.Id);
                }
            });
            CommandHandler.RegisterCommand("dissapear", async (e, args) =>
            {
                var timeout = await chatApi.Moderation.TimeoutUserAsync(BroadcasterID, e.Data.Sender.Id, 1, "!!!");
                if (timeout.IsSuccess)
                    await chatbot.SendMessageAsBotAsync($"@{e.Data.Sender.Username} is gone");
                else
                    Console.WriteLine(string.Join("\n", timeout.Reasons));
            });

            CommandHandler.RegisterCommand("uptime", async (e, args) =>
            {
                var uptime = DateTime.Now - Process.GetCurrentProcess().StartTime;
                await chatbot.SendMessageAsBotAsync($"⏱ Bot Uptime: {uptime.Hours}h {uptime.Minutes}m {uptime.Seconds}s");
            });
            await GetMessage();
            await Task.Delay(-1);
        }
        public static async Task RefreshToken()
        {
            var authGen = new KickOAuthGenerator();
            var tokenData = File.ReadAllText(tokenFile);
            var refreshTokenData = File.ReadAllText(refreshTokenFile);
            var exchangeResults = await authGen.RefreshAccessTokenAsync(
                refreshTokenData,
                jsonAuth.client_id,
                jsonAuth.client_secret);
            if (exchangeResults.IsSuccess)
            {
                Console.WriteLine($"Access Token: {exchangeResults.Value.AccessToken}");
                Console.WriteLine($"Refresh Token: {exchangeResults.Value.RefreshToken}");
                tokenData = exchangeResults.Value.AccessToken;
                refreshTokenData = exchangeResults.Value.RefreshToken;
                await File.WriteAllTextAsync(tokenFile, tokenData);
                await File.WriteAllTextAsync(refreshTokenFile, refreshTokenData);

                if (settings != null)
                {
                    settings.AccessTokenChanged -= OnRefreshTokenChanged;
                    settings.AccessTokenChanged -= OnAccessTokenChanged;
                }

                settings = new ApiSettings
                {
                    AccessToken = tokenData,
                    RefreshToken = refreshTokenData,
                    ClientId = jsonAuth.client_id,
                    ClientSecret = jsonAuth.client_secret,
                };

                chatApi = KickApi.Create(settings);

                settings.RefreshTokenChanged += OnRefreshTokenChanged;

                settings.AccessTokenChanged += OnAccessTokenChanged;
                chatbot = chatApi.Chat;
            }
            else
            {
                Console.WriteLine($"Get Refresh Token Error: {string.Join("\n", exchangeResults.Errors)}");

            }
        }
        private static void OnRefreshTokenChanged(object? sender, TokenChangedEventArgs args)
        {
            try
            {
                File.WriteAllText(tokenFile, args.NewToken);
                Console.WriteLine("Refresh Tokens updated and saved.");
            }
            catch (Exception ex)
            {
                Console.WriteLine("New Refresh Token Error: " + ex.Message);
            }
        }

        private static void OnAccessTokenChanged(object? sender, TokenChangedEventArgs args)
        {
            try
            {
                File.WriteAllText(refreshTokenFile, args.NewToken);
                Console.WriteLine("Access Tokens updated and saved.");
            }
            catch (Exception ex)
            {
                Console.WriteLine("New Token Error: " + ex.Message);
            }
        }
        static bool IsBroadcaster(MessageSender sender)
        {
            var badge = sender.Identity.Badges.Select(x => x.Text);
            bool isMod = badge.Contains("Broadcaster");
            return isMod;
        }


        static async Task GetNewToken(string[] args)
        {
            string REDIRECT_URI = "http://localhost:5000/auth/callback";

            var authGen = new KickOAuthGenerator();
            var authUrl = authGen.GetAuthorizationUri(
                REDIRECT_URI,
                jsonAuth.client_id,
                new List<string> { KickScopes.UserRead, KickScopes.ChatWrite, KickScopes.EventsSubscribe, KickScopes.ChannelRead, KickScopes.ModerationBan },
                out string codeVerifier
            );

            Console.WriteLine("🔗 Open the following URL in your browser to log in:");
            Console.WriteLine(authUrl);

            // 🚀 Auto-open default browser
            try
            {
                //var psi = new ProcessStartInfo
                //{
                //    FileName = authUrl.ToString(),
                //    UseShellExecute = true
                //};
                //Process.Start(psi);
            }
            catch
            {
                Console.WriteLine("⚠️ Failed to auto-open browser. Please open the URL manually.");
            }

            // 🌐 Start local web server
            var builder = WebApplication.CreateBuilder(args);
            var app = builder.Build();

            app.MapGet("/auth/callback", async (string code, string state) =>
            {
                try
                {
                    Console.WriteLine("📥 Received code and state from Kick. Exchanging for tokens...");

                    var tokenResult = await authGen.ExchangeCodeForTokenAsync(
                        code, jsonAuth.client_id, jsonAuth.client_secret, REDIRECT_URI, state);

                    if (!tokenResult.IsSuccess)
                    {
                        Console.WriteLine($"❌ Token exchange failed:");
                        return Results.BadRequest("❌ Token exchange failed:");
                    }

                    string accessToken = tokenResult.Value.AccessToken;
                    string refreshToken = tokenResult.Value.RefreshToken;

                    Console.WriteLine("✅ AccessToken: " + accessToken);
                    Console.WriteLine("✅ RefreshToken: " + refreshToken);

                    // 💾 Save tokens to file
                    await File.WriteAllTextAsync(tokenFile, accessToken);
                    await File.WriteAllTextAsync(refreshTokenFile, refreshToken);
                    Console.WriteLine("💾 Tokens saved to tokens.json");
                    RunBotAsync();
                    return Results.Ok("🎉 Authentication successful! You can close this browser window.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"🔥 Exception: {ex.Message}");
                    return Results.Problem("Internal server error");
                }
            });

            Console.WriteLine($"🌐 Listening on {REDIRECT_URI}");
            await app.RunAsync();
        }
    }
}
