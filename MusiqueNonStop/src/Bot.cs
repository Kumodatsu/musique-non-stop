using System;
using System.Collections.Generic;
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

    public async Task StartAsync() {
        var client   = GetClient();
        var config   = GetConfig();
        var commands = GetCommands();
        await commands.AddModuleAsync<Commands>(services);
        await client.LoginAsync(TokenType.Bot, config.Token);
        await client.StartAsync();
    }

    #region Services
    private IServiceProvider services;

    private DiscordSocketClient GetClient()
        => services.GetRequiredService<DiscordSocketClient>();
    private Config GetConfig()
        => services.GetRequiredService<Config>();
    private CommandService GetCommands()
        => services.GetRequiredService<CommandService>();
    private LavaNode GetLavaNode()
        => services.GetRequiredService<LavaNode>();
    #endregion

    #region Event Handlers
    private async Task OnReadyAsync() {
        var client = GetClient();
        var lava   = GetLavaNode();
        if (!lava.IsConnected)
            await lava.ConnectAsync();
        await client.SetStatusAsync(UserStatus.Online);
    }

    private async Task OnMessageReceivedAsync(SocketMessage sock_message) {
        if (sock_message.Author.IsBot)
            return;
        if (sock_message is SocketUserMessage msg) {
            var client   = GetClient();
            var config   = GetConfig();
            var commands = GetCommands();

            var context = new SocketCommandContext(client, msg);
            int arg_pos = 0;
            if (msg.HasStringPrefix(config.CommandPrefix, ref arg_pos)) {
                // The message is a command
                var result = await commands
                    .ExecuteAsync(context, arg_pos, services);
                if (!result.IsSuccess)
                    await Logger.LogAsync(
                        $"Failed to execute command: {result.ErrorReason}"
                    );
            }
        }
    }

    private async Task OnLogAsync(LogMessage message)
        => await Logger.LogAsync(
            message.Message,
            source:    message.Source,
            severity:  message.Severity,
            exception: message.Exception
        );

    private async Task OnTrackExceptionAsync(TrackExceptionEventArgs arg) {
        var channel = arg.Player.TextChannel;
        await channel.SendMessageAsync(
            $"Something went went wrong:\n{arg.ErrorMessage}"
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
                await LeaveVoiceAsync(voice_channel.Guild);
        } catch (Exception exception) {
            await Logger.LogAsync(
                "Lavalink encountered an exception.",
                severity:  LogSeverity.Error,
                exception: exception
            );
        }            
    }
    #endregion

    public async Task JoinVoiceAsync(
        IVoiceChannel voice,
        ITextChannel? channel = null
    ) {
        var lava = GetLavaNode();
        if (lava.HasPlayer(voice.Guild))
            return;
        try {
            await lava.JoinAsync(voice, channel);
        } catch (Exception exception) {
            await Logger.LogAsync(
                $"Couldn't join voice channel \"{voice.Name}\".",
                severity:  LogSeverity.Error,
                exception: exception
            );
        }
    }

    public async Task LeaveVoiceAsync(IGuild guild) {
        var lava = GetLavaNode();
        if (lava.TryGetPlayer(guild, out var player)) {
            if (player.PlayerState is PlayerState.Playing)
                await player.StopAsync();
            await lava.LeaveAsync(player.VoiceChannel);
        }        
    }

    private async IAsyncEnumerable<LavaTrack> Search(string query) {
        var lava = GetLavaNode();
        var search_results
            = Uri.IsWellFormedUriString(query, UriKind.Absolute)
            ? await lava.SearchAsync(SearchType.Direct, query)
            : await lava.SearchYouTubeAsync(query);
        if (search_results.Status is SearchStatus.NoMatches)
            yield break;
        foreach (var track in search_results.Tracks)
            yield return track;
    }

    public async Task PlayAsync(IGuild guild, string query) {
        var lava = GetLavaNode();
        if (lava.TryGetPlayer(guild, out var player)) {
            try {
                var track = await Search(query).FirstOrDefaultAsync();
                if (track is null)
                    return;
                if (
                    player.Track is not null && player.PlayerState is
                        PlayerState.Playing or PlayerState.Paused
                ) {
                    player.Queue.Enqueue(track);
                } else {
                    if (player.TextChannel is not null)
                        await player.TextChannel
                            .SendMessageAsync($"Now playing: {track.Title}");
                    await player.PlayAsync(track);
                }
            } catch (Exception exception) {
                await Logger.LogAsync(
                    "Something went wrong trying to play a song.",
                    severity:  LogSeverity.Error,
                    exception: exception
                );
            }
        }        
    }

    public async Task StopPlayingAsync(
        SocketGuildUser user,
        IGuild          guild
    ) {
        try {
            var lava = GetLavaNode();
            var player = lava.GetPlayer(guild);
            if (player.PlayerState is PlayerState.Playing)
                await player.StopAsync();
        } catch (InvalidOperationException exception) {
            await Logger.LogAsync(
                "Something went wrong trying to stop playing.",
                severity:  LogSeverity.Error,
                exception: exception
            );
        }
    }

    public async Task SkipAsync(
        SocketGuildUser user,
        IGuild          guild
    ) {
        await StopPlayingAsync(user, guild);
        var lava   = GetLavaNode();
        var player = lava.GetPlayer(guild);
        await TryPlayNextAsync(player);
    }

    public async Task ShowQueueAsync(
        SocketGuildUser user,
        IGuild          guild,
        ITextChannel    output_channel
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
        await output_channel.SendMessageAsync(builder.ToString());
    }

    private async Task<bool> TryPlayNextAsync(LavaPlayer player) {
        if (player.Queue.TryDequeue(out var track)) {
            if (player.TextChannel is not null)
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
