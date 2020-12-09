using Discord;
using Discord.WebSocket;
using GCCBot;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Minecraft_Bot
{
    class Client
    {
        public DiscordSocketClient _client;

        private string token;

        public Client(string token)
        {
            this.token = token;

            _client = new DiscordSocketClient();
        }

        public async Task InitializeAsync()
        {
            await _client.StartAsync();

            await _client.LoginAsync(TokenType.Bot, token);

            await SetupEventHandler();

            new Thread(OneSecLoop).Start();

            await Task.Delay(-1);
        }

        private void OneSecLoop()
        {
            do
            {
                Thread.Sleep(1000);

                Compiler.Interval();
            } while (true);
        }

        private async Task SetupEventHandler()
        {
            _client.MessageReceived += client_MessageReceived;

            _client.Log += client_Log;

            _client.UserVoiceStateUpdated += _client_UserVoiceStateUpdated;
        }

        private async Task _client_UserVoiceStateUpdated(SocketUser arg1, SocketVoiceState arg2, SocketVoiceState arg3)
        {
            await _client.SetStatusAsync(UserStatus.Invisible);

            await ((SocketGuildUser)arg1).ModifyAsync(x => x.Mute = false);
        }

        private async Task client_Log(LogMessage arg)
        {
            Console.WriteLine(arg.Message);
        }

        private bool IsPrivateMessage(SocketMessage msg)
        {
            return msg.Channel.GetType().Name == "SocketDMChannel";
        }

        private async Task client_MessageReceived(SocketMessage arg)
        {
            try
            {
                if (arg.Author.IsBot || !IsPrivateMessage(arg))
                {
                    return;
                }

                string msg = arg.Content;
                Console.WriteLine(msg);

                Compiler comp = Compiler.GetCompiler(arg.Author.Username + arg.Author.Discriminator);

                if (msg == "gcc")
                {
                    if (arg.Attachments.Count > 0)
                    {
                        foreach (var item in arg.Attachments)
                        {
                            CompileProgram(arg.Channel, item.Url, arg.Author, arg);
                        }
                    }
                    else
                    {
                        await arg.Channel.SendMessageAsync("Please attach at least one file!");
                    }
                }
                else if (msg == comp.AbortCommand)
                {
                    comp.AbortProgram();
                }
                else
                {
                    comp.ProgramInput(msg);
                }
            }
            catch (Exception ex)
            {
                arg.Channel.SendMessageAsync("Error: " + ex.Message);
            }
        }

        async Task CompileProgram(ISocketMessageChannel channel, string url, SocketUser user, SocketMessage msg)
        {
            try
            {
                Console.WriteLine(user.Discriminator);
                Compiler compiler = Compiler.GetCompiler(user.Username + user.Discriminator);

                if (!compiler.ProgramRunning)
                {
                    bool succeeded;
                    string output;

                    compiler.CompileProgram(out succeeded, out output, url, (SocketDMChannel)msg.Channel);

                    await channel.SendMessageAsync($"Compile succeeded: " + succeeded.ToString() + "\nOutput: \n```" + output + "```");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }
}
