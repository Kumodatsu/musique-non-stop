using System;
using Discord.Commands;
using Discord.WebSocket;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using Discord;
using Victoria;

const string CommandPrefix = "//";
const string ConfigPath    = "data/config.json";

var config = MusiqueNonStop.Config.FromFile(ConfigPath);

if (config is null) {
    Console.WriteLine("Could not read config file.");
    return -1;
}

DiscordSocketClient client          = new();
CommandService      command_service = new();
IServiceProvider    services        = new ServiceCollection()
    .AddSingleton(client)
    .AddSingleton(command_service)
    .AddLavaNode(config => config.SelfDeaf = false)
    .BuildServiceProvider();

client.Log   += LogAsync;
client.Ready += OnReadyAsync;
await RegisterCommandsAsync();
await client.LoginAsync(TokenType.Bot, config.Token);
await client.StartAsync();
await Task.Delay(-1);

return 0;

async Task RegisterCommandsAsync() {
    client.MessageReceived += HandleCommandAsync;
    await command_service
        .AddModulesAsync(Assembly.GetEntryAssembly(), services);
}

async Task HandleCommandAsync(SocketMessage socket_message) {
    if (socket_message is SocketUserMessage message) {
        var context = new SocketCommandContext(client, message);
        if (message.Author.IsBot)
            return;
        int arg_pos = 0;
        if (message.HasStringPrefix(CommandPrefix, ref arg_pos)) {
            var result = await command_service
                .ExecuteAsync(context, arg_pos, services);
            if (!result.IsSuccess)
                Console.WriteLine("AAAAAAAAAAH!");
        }
    }
}

async Task LogAsync(LogMessage message) {
    Console.WriteLine(message);
    await Task.CompletedTask;
}

async Task OnReadyAsync() {
    var lava = services.GetRequiredService<LavaNode>();
    System.Console.WriteLine($"Heya. {lava is not null}");
    if (lava is not null && !lava.IsConnected)
        await lava.ConnectAsync();
}

