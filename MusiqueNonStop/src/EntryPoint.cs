using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommandLine;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Kumodatsu.MusiqueNonStop;
using Microsoft.Extensions.DependencyInjection;
using Victoria;

Console.Title = "Musique Non Stop";

await Parser.Default.ParseArguments<CommandLineArgs>(args)
    .MapResult(WithValidArgs, WithInvalidArgs);

async Task WithValidArgs(CommandLineArgs args) {
    Config config;

    if (File.Exists(args.ConfigPath)) {
        try {
            config = Config.FromFile(args.ConfigPath);
        } catch (IOException exception) {
            Console.Error.WriteLine(
                $"Something went wrong reading the config:\n{exception.Message}"
            );
            Exit(ExitCode.IOError);
            return;
        } catch (ParseException exception) {
            Console.Error.WriteLine(
                $"Something went wrong reading the config:\n{exception.Message}"
            );
            Exit(ExitCode.InvalidConfig);
            return;
        }
    } else {
        Config.ToFile(new () { Token = "your token here" }, args.ConfigPath);
        Console.Out.WriteLine(
            $"The config file \"{args.ConfigPath}\" did not exist and has "
            +"been created. Please add the bot's token to the config "
            +"file's 'token' field."
        );
        Exit(ExitCode.Success);
        return;
    }

    IServiceProvider services = new ServiceCollection()
        .AddSingleton<DiscordSocketClient>(new DiscordSocketClient(new () {
            GatewayIntents = GatewayIntents.AllUnprivileged
        }))
        .AddSingleton<CommandService>()
        .AddSingleton(config)
        .AddLavaNode(config => config.SelfDeaf = false)
        .BuildServiceProvider();

    Bot bot = new Bot(services);
    await bot.StartAsync();
    await Task.Delay(-1);
}

async Task WithInvalidArgs(IEnumerable<Error> errors) {
    // The command parsing library handles --help and --version as errors.
    // Only return a non-success exit code if there are any 'actual' errors.
    if (errors.Any(error => error.Tag
        is  not ErrorType.HelpRequestedError
        and not ErrorType.VersionRequestedError
    )) {
        Exit(ExitCode.InvalidCommandLineArgs);
    } else {
        Exit(ExitCode.Success);
    }
    await Task.CompletedTask;
}

void Exit(ExitCode exit_code) => Environment.Exit((int) exit_code);

enum ExitCode {
    Success                = 0,
    InvalidCommandLineArgs = 1,
    IOError                = 2,
    InvalidConfig          = 3
}
