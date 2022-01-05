using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Victoria;
using Victoria.Enums;
using Victoria.EventArgs;
using Victoria.Responses.Search;

namespace Kumodatsu.MusiqueNonStop;

internal class MusicPlayer {

    private IGuild         guild;
    private IThreadChannel request_thread;
    private IUserMessage   player_message;
    private LavaNode       lava;
    private LavaPlayer     player;

    private MusicPlayer(
        IGuild         guild,
        IThreadChannel request_thread, 
        IUserMessage   player_message,
        LavaNode       lava
    ) {
        this.guild          = guild;
        this.request_thread = request_thread;
        this.player_message = player_message;
        this.lava           = lava;
        this.player         = lava.GetPlayer(guild);
    }

    public static async Task<MusicPlayer?> CreatePlayerAsync(
        IVoiceChannel       voice,
        ITextChannel        text,
        LavaNode            lava,
        DiscordSocketClient client
    ) {
        var guild = voice.Guild;
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
            new MusicPlayer(guild, request_thread, player_message, lava);
        lava.OnTrackEnded      += player.OnTrackEndedAsync;
        client.ButtonExecuted  += async (interaction) => {
            if (interaction.Message.Id == player_message.Id)
                await player.OnButtonExecutedAsync(interaction);
        };
        client.MessageReceived += async (msg) => {
            if (msg.Channel.Id == request_thread.Id && !msg.Author.IsBot)
                await player.OnRequestReceived(msg.Content);
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

    private async Task OnRequestReceived(string request)
        => await RequestAsync(request);
    #endregion

    #region Public Interface
    public bool IsIdle
        => player.Track is null
        || player.PlayerState is PlayerState.None or PlayerState.Stopped;

    public async Task RequestAsync(string request) {
        var track = await Search(request).FirstOrDefaultAsync();
        if (track is null)
            return;
        player.Queue.Enqueue(track);
        if (IsIdle)
            await TryPlayNextAsync();
        else
            await UpdateSongMenu();
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
        await StopAsync();
        await lava.LeaveAsync(player.VoiceChannel);
    }
    #endregion

    #region Private Methods
    private async IAsyncEnumerable<LavaTrack> Search(string query) {
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
