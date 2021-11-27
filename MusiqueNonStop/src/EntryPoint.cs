using System;
using System.IO;
using System.Threading.Tasks;
using Discord.Commands;
using Discord.WebSocket;
using Kumodatsu.MusiqueNonStop;
using Microsoft.Extensions.DependencyInjection;
using Victoria;

const string ConfigPath = "config.yml";

Config config;
try {
    config = Config.FromFile(ConfigPath);
} catch (FileNotFoundException) {
    Console.WriteLine("The config file could not be found.");
    return -1;
} catch (ParseException exception) {
    Console.WriteLine($"Something went wrong while reading the config file:");
    Console.WriteLine(exception.Message);
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
