using System;
using System.Threading.Tasks;
using Discord.Commands;
using Discord.WebSocket;
using Kumodatsu.MusiqueNonStop;
using Microsoft.Extensions.DependencyInjection;
using Victoria;

const string ConfigPath = "data/config.json";

var config = Config.FromFile(ConfigPath);

if (config is null) {
    Console.WriteLine("Could not read config file.");
    return -1;
}

IServiceProvider services = new ServiceCollection()
    .AddSingleton<DiscordSocketClient>()
    .AddSingleton<CommandService>()
    .AddSingleton(config)
    .AddLavaNode(config => config.SelfDeaf = false)
    .BuildServiceProvider();

Bot bot = new Bot(services);
await bot.StartAsync();
await Task.Delay(-1);

return 0;
