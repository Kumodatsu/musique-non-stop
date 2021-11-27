using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Victoria;
using Victoria.Enums;
using Victoria.EventArgs;
using Victoria.Responses.Search;

namespace Kumodatsu.MusiqueNonStop;

internal sealed class Bot {

    public static Bot Instance => instance!;
    private static Bot? instance;

    public Bot(IServiceProvider services) {
        instance = this;

        this.services = services;

        var client = GetClient();
        var lava   = GetLavaNode();
        client.Ready                 += OnReadyAsync;
        client.MessageReceived       += OnMessageReceivedAsync;
        client.Log                   += OnLogAsync;
        client.UserVoiceStateUpdated += OnUserVoiceStateUpdatedAsync;
        lava.OnTrackEnded            += OnTrackEndedAsync;
        lava.OnTrackException        += OnTrackExceptionAsync;
    }

    private async Task OnTrackExceptionAsync(TrackExceptionEventArgs arg) {
        var channel = arg.Player.TextChannel;
        await channel.SendMessageAsync(
            $"Oopsie woopsie! Something went brokey wokey:\n{arg.ErrorMessage}"
        );
    }

    private async Task OnUserVoiceStateUpdatedAsync(
        SocketUser       user,
        SocketVoiceState old_state,
        SocketVoiceState new_state
    ) {
        var client = GetClient();
        var lava   = GetLavaNode();
        try {
            var voice_channel = old_state.VoiceChannel;
            if (voice_channel is null)
                return;
            var users = voice_channel.Users;
            // Make the bot leave the voice chat when alone
            if (users.Count == 1 && users.First().Id == client.CurrentUser.Id)
                await LeaveAsync(voice_channel.Guild);
        } catch (Exception exception) {
            await SendExceptionAsync(
                lava.GetPlayer(old_state.VoiceChannel.Guild).TextChannel,
                exception
            );
        }            
    }

    public async Task StartAsync() {
        var client   = GetClient();
        var config   = GetConfig();
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
        var client = GetClient();
        var lava   = GetLavaNode();
        if (!lava.IsConnected)
            await lava.ConnectAsync();
        await client.SetStatusAsync(UserStatus.Online);
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

    private async Task<IUserMessage> SendExceptionAsync(
        ITextChannel channel,
        Exception    exception
    ) => await channel.SendMessageAsync(
        $"```Exception: {exception.Message}"
        + $"\n{exception.StackTrace ?? string.Empty}"
        + "```"
    );

    public async Task JoinAsync(
        SocketGuildUser user,
        IGuild          guild,
        ITextChannel?   channel = null
    ) {
        async Task reply(string msg) {
            if (channel is not null)
                await channel.SendMessageAsync(msg);
        };

        var lava = GetLavaNode();

        if (lava.HasPlayer(guild)) {
            await reply("I'm already in a voice channel.");
            return;
        }

        if (user.VoiceChannel is not null) {
            try {
                await lava.JoinAsync(
                    user.VoiceChannel,
                    channel as ITextChannel
                );
                await reply($"Joined {user.VoiceChannel.Name}.");
            } catch (Exception exception) {
                if (channel is not null)
                    await SendExceptionAsync(channel, exception);
            }
        } else {
            await reply("You must be in a voice channel.");
            return;
        }
    }

    public async Task LeaveAsync(
        IGuild        guild,
        ITextChannel? channel = null
    ) {
        var lava   = GetLavaNode();
        var player = lava.GetPlayer(guild);
        try {
            if (player.PlayerState is PlayerState.Playing)
                await player.StopAsync();
            await lava.LeaveAsync(player.VoiceChannel);
        } catch (InvalidOperationException exception) {
            await LogAsync(exception.Message);
            await SendExceptionAsync(
                channel ?? player.TextChannel,
                exception
            );
        }
    }

    public async Task PlayAsync(
        SocketGuildUser user,
        IGuild guild,
        string query,
        ITextChannel? channel = null
    ) {
        async Task reply(string msg) {
            if (channel is not null)
                await channel.SendMessageAsync(msg);
        };

        if (user.VoiceChannel is null) {
            await reply("You are not in a voice channel!");
            return;
        }
        var lava = GetLavaNode();
        if (!lava.HasPlayer(guild))
            await JoinAsync(user, guild, channel);
        try {
            var player = lava.GetPlayer(guild);
            var search_results
                = Uri.IsWellFormedUriString(query, UriKind.Absolute)
                ? await lava.SearchAsync(SearchType.Direct, query)
                : await lava.SearchYouTubeAsync(query);
            if (search_results.Status is SearchStatus.NoMatches) {
                await reply("I couldn't find anything.");
                return;
            }
            var track = search_results.Tracks.FirstOrDefault()!;
            if (
                player.Track is not null &&
                player.PlayerState is PlayerState.Playing or PlayerState.Paused
            ) {
                player.Queue.Enqueue(track);
                await reply($"Added \"{track.Title}\" to the queue.");
            } else {
                await player.PlayAsync(track);
                await reply($"Playing \"{track.Title}\"");
            }
        } catch (Exception exception) {
            if (channel is not null)
                await SendExceptionAsync(channel, exception);
        }
    }

    public async Task StopPlayingAsync(
        SocketGuildUser user,
        IGuild          guild,
        ITextChannel?   channel = null
    ) {
        try {
            var lava = GetLavaNode();
            var player = lava.GetPlayer(guild);
            if (player.PlayerState is PlayerState.Playing)
                await player.StopAsync();
        } catch (InvalidOperationException exception) {
            await LogAsync(exception.Message);
            if (channel is not null)
                await SendExceptionAsync(channel, exception);
        }
    }

    public async Task SkipAsync(
        SocketGuildUser user,
        IGuild          guild,
        ITextChannel?   channel = null
    ) {
        await StopPlayingAsync(user, guild, channel);
        var lava   = GetLavaNode();
        var player = lava.GetPlayer(guild);
        await TryPlayNextAsync(player);
    }

    public async Task ShowQueueAsync(
        SocketGuildUser user,
        IGuild          guild
    ) {
        var lava    = GetLavaNode();
        var player  = lava.GetPlayer(guild);
        var builder = new StringBuilder();
        if (player.PlayerState is PlayerState.Playing)
            builder.AppendLine($"Now playing: {player.Track.Title}");
        if (player.Queue.Count == 0) {
            builder.AppendLine("The queue is empty.");
        } else {
            builder.AppendLine("In queue:");
            foreach (var track in player.Queue)
                builder.AppendLine($"- {track.Title}");
        }
        await player.TextChannel.SendMessageAsync(builder.ToString());
    }

    private async Task<bool> TryPlayNextAsync(LavaPlayer player) {
        if (player.Queue.TryDequeue(out var track)) {
            await player.TextChannel.SendMessageAsync(
                $"Next up: {track.Title}"
            );
            await player.PlayAsync(track);
            return true;
        }
        return false;
    }

    private async Task OnTrackEndedAsync(TrackEndedEventArgs args) {
        if (args.Reason is TrackEndReason.Finished)
            await TryPlayNextAsync(args.Player);
    }

}
