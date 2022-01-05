using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Victoria;
using Victoria.Enums;
using Victoria.EventArgs;
using Victoria.Responses.Search;

namespace Kumodatsu.MusiqueNonStop;

internal class MusicPlayer {

    private IGuild         guild;
    private IThreadChannel request_thread;
    private IUserMessage   player_message;
    private LavaPlayer     player;

    private IServiceProvider services;

    private MusicPlayer(
        IGuild           guild,
        IThreadChannel   request_thread, 
        IUserMessage     player_message,
        IServiceProvider services
    ) {
        this.guild          = guild;
        this.request_thread = request_thread;
        this.player_message = player_message;
        this.player         = services.GetRequiredService<LavaNode>()
            .GetPlayer(guild);
        this.services       = services;
    }

    public static async Task<MusicPlayer?> CreatePlayerAsync(
        IVoiceChannel       voice,
        ITextChannel        text,
        IServiceProvider    services
    ) {
        var guild  = voice.Guild;
        var lava   = services.GetRequiredService<LavaNode>();
        var client = services.GetRequiredService<DiscordSocketClient>();
        if (lava.HasPlayer(guild))
            return null;
        try {
            await lava.JoinAsync(voice, text);
        } catch (Exception exception) {
            await Logger.LogAsync(
                $"Couldn't join voice channel \"{voice.Name}\".",
                severity:  LogSeverity.Error,
                exception: exception
            );
            return null;
        }
        var player_message = await text.SendMessageAsync(
            embed:      new EmbedBuilder()
                .WithTitle("Awaiting Requests")
                .Build(),
            components: new ComponentBuilder()
                .WithButton(
                    label:    "Stop",
                    emote:    new Emoji("⏹️"),
                    style:    ButtonStyle.Danger,
                    customId: "stop"
                )
                .WithButton(
                    label:    "Pause",
                    emote:    new Emoji("⏸️"),
                    style:    ButtonStyle.Primary,
                    customId: "pause"
                )
                .WithButton(
                    label:    "Skip",
                    emote:    new Emoji("⏩"),
                    style:    ButtonStyle.Primary,
                    customId: "skip"
                )
                .Build()
        );
        try {
            await text.CreateThreadAsync(
                name:    "Song requests",
                type:    ThreadType.PublicThread,
                message: player_message
            );
        } catch (Exception) {}
        var request_thread =
            await guild.GetThreadChannelAsync(player_message.Id);
        var player =
            new MusicPlayer(guild, request_thread, player_message, services);
        lava.OnTrackEnded      += player.OnTrackEndedAsync;
        client.ButtonExecuted  += async (interaction) => {
            if (interaction.Message.Id == player_message.Id)
                await player.OnButtonExecutedAsync(interaction);
        };
        client.MessageReceived += async (msg) => {
            if (
                msg is IUserMessage user_msg
                && user_msg.Channel.Id == request_thread.Id
                && !user_msg.Author.IsBot
            ) {
                await player.OnRequestReceived(user_msg);
            }
        };
        return player;
    }

    #region Event Handlers
    private async Task OnButtonExecutedAsync(
        SocketMessageComponent interaction
    ) {
        await interaction.DeferAsync(ephemeral: true);
        var user    = interaction.User as SocketGuildUser;
        var channel = interaction.Channel as ITextChannel;
        var guild   = channel!.Guild;
        switch (interaction.Data.CustomId) {
            case "stop":
                await StopAsync();
                break;
            case "skip":
                await SkipAsync();
                break;
            case "pause":
                await PauseAsync();
                break;
            default:
                await Logger.LogAsync(
                    "Unknown interaction ID.",
                    severity: LogSeverity.Warning
                );
                break;
        }
    }

    private async Task OnTrackEndedAsync(TrackEndedEventArgs args) {
        if (args.Reason is TrackEndReason.Finished)
            await TryPlayNextAsync();
    }

    private async Task OnRequestReceived(IUserMessage request) {
        var success = await RequestAsync(request.Content);
        if (success) {
            await request.AddReactionAsync(new Emoji("✅"));
        } else {
            await request.AddReactionAsync(new Emoji("❌"));
            await request.ReplyAsync("Sorry, something went wrong!");
        }
    }
    #endregion

    #region Public Interface
    public bool IsIdle
        => player.Track is null
        || player.PlayerState is PlayerState.None or PlayerState.Stopped;

    public async Task<bool> RequestAsync(string request) {
        var track = await Search(request).FirstOrDefaultAsync();
        if (track is null)
            return false;
        player.Queue.Enqueue(track);
        if (IsIdle)
            await TryPlayNextAsync();
        else
            await UpdateSongMenu();
        return true;
    }

    public async Task StopAsync() {
        if (!IsIdle)
            await player.StopAsync();
        player.Queue.Clear();
        await UpdateSongMenu();
    }

    public async Task SkipAsync() {
        if (!IsIdle)
            await player.StopAsync();
        await TryPlayNextAsync();
    }

    public async Task PauseAsync() {
        switch (player.PlayerState) {
            case PlayerState.Playing:
                await player.PauseAsync();
                break;
            case PlayerState.Paused:
                await player.ResumeAsync();
                break;
        }
    }

    public async Task Destroy() {
        var lava   = services.GetRequiredService<LavaNode>();
        var config = services.GetRequiredService<Config>();
        await StopAsync();
        await lava.LeaveAsync(player.VoiceChannel);
        if (config.Ephemeral) {
            await request_thread.DeleteAsync();
            await player_message.DeleteAsync();
        }
    }
    #endregion

    #region Private Methods
    private async IAsyncEnumerable<LavaTrack> Search(string query) {
        var lava = services.GetRequiredService<LavaNode>();
        var search_results
            = Uri.IsWellFormedUriString(query, UriKind.Absolute)
            ? await lava.SearchAsync(SearchType.Direct, query)
            : await lava.SearchYouTubeAsync(query);
        if (search_results.Status is SearchStatus.NoMatches)
            yield break;
        foreach (var track in search_results.Tracks)
            yield return track;
    }

    private async Task<bool> TryPlayNextAsync() {
        var success = false;
        if (player.Queue.TryDequeue(out var track)) {
            await player.PlayAsync(track);
            success = true;
        }
        await UpdateSongMenu();
        return success;
    }

    private async Task UpdateSongMenu() {
        var eb = new EmbedBuilder();
        eb.WithTitle("Awaiting Requests");
        if (!IsIdle) {
            eb.WithTitle("Setlist").AddField("Now Playing", player.Track.Title);
        }
        if (player.Queue.Count != 0) {
            var sb = new StringBuilder();
            foreach (var enqueued in player.Queue)
                sb.AppendLine(enqueued.Title);
            eb.WithTitle("Setlist").AddField("Later Tonight", sb.ToString());
        }
        await player_message.ModifyAsync(p => p.Embed = eb.Build());
    }
    #endregion

}
