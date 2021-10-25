using System;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Victoria;

namespace Kumodatsu.MusiqueNonStop {

    internal sealed class Bot {

        public Bot(IServiceProvider services) {
            this.services = services;

            var client   = GetClient();
            client.Ready           += OnReadyAsync;
            client.MessageReceived += OnMessageReceivedAsync;
            client.Log             += OnLogAsync;
        }

        public async Task StartAsync() {
            var client = GetClient();
            var config = GetConfig();
            var commands = GetCommands();
            await commands.AddModuleAsync<Commands>(services);
            await client.LoginAsync(TokenType.Bot, config.Token);
            await client.StartAsync();
        }

        private IServiceProvider services;

        private DiscordSocketClient GetClient()
            => services.GetRequiredService<DiscordSocketClient>();
        private Config GetConfig()
            => services.GetRequiredService<Config>();
        private CommandService GetCommands()
            => services.GetRequiredService<CommandService>();
        private LavaNode GetLavaNode()
            => services.GetRequiredService<LavaNode>();

        private async Task OnReadyAsync() {
            var lava = GetLavaNode();
            if (lava is not null && !lava.IsConnected)
                await lava.ConnectAsync();
        }

        private async Task OnMessageReceivedAsync(SocketMessage sock_message) {
            if (sock_message is SocketUserMessage msg) {
                var client   = GetClient();
                var config   = GetConfig();
                var commands = GetCommands();

                var context = new SocketCommandContext(client, msg);
                if (msg.Author.IsBot)
                    return;
                int arg_pos = 0;
                if (msg.HasStringPrefix(config.CommandPrefix, ref arg_pos)) {
                    // The message is a command
                    var result = await commands
                        .ExecuteAsync(context, arg_pos, services);
                    if (!result.IsSuccess)
                        await LogAsync(
                            $"Failed to execute command: {result.ErrorReason}"
                        );
                }
            }
        }

        private async Task OnLogAsync(LogMessage message)
            => await LogAsync(message.Message);

        private async Task LogAsync(string message) {
            Console.WriteLine(message);
            await Task.CompletedTask;
        }
        
    }

}
