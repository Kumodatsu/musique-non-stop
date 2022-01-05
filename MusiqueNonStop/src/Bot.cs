using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Victoria;

namespace Kumodatsu.MusiqueNonStop;

internal sealed class Bot {

    public static Bot Instance => instance!;
    private static Bot? instance;

    private ConcurrentDictionary<ulong, MusicPlayer> music_players = new();

    public Bot(IServiceProvider services) {
        instance = this;

        this.services = services;

        var client = GetClient();
        var lava   = GetLavaNode();
        client.Ready                 += OnReadyAsync;
        client.Log                   += OnLogAsync;
        client.UserVoiceStateUpdated += OnUserVoiceStateUpdatedAsync;
        client.GuildAvailable        += OnGuildAvailableAsync;
        client.SlashCommandExecuted  += OnSlashCommandExecutedAsync;
    }

    public async Task StartAsync() {
        var client   = GetClient();
        var config   = GetConfig();
        var commands = GetCommands();
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
    }

    private async Task OnGuildAvailableAsync(SocketGuild guild) {
        if (guild.Id != 278177188346462208)
            return;
        var cmd_join = new SlashCommandBuilder()
            .WithName("join")
            .WithDescription("Makes the bot join your voice channel")
            .Build();
        await guild.CreateApplicationCommandAsync(cmd_join);
        var cmd_leave = new SlashCommandBuilder()
            .WithName("leave")
            .WithDescription("Makes the bot leave its voice channel")
            .Build();
        await guild.CreateApplicationCommandAsync(cmd_leave);
    }

    private async Task OnSlashCommandExecutedAsync(SocketSlashCommand cmd) {
        // await cmd.DeferAsync(ephemeral: true);
        await cmd.RespondAsync("Alright!", ephemeral: true);
        switch (cmd.Data.Name) {
            case "join":
                var voice   = (cmd.User as IGuildUser)?.VoiceChannel;
                var channel = cmd.Channel as ITextChannel;
                if (voice is null || channel is null)
                    break;
                await JoinVoiceAsync(voice, channel);
                break;
            case "leave":
                var guild = (cmd.User as IGuildUser)?.Guild;
                if (guild is null)
                    break;
                await LeaveVoiceAsync(guild);
                break;
        }
    }

    private async Task OnLogAsync(LogMessage message)
        => await Logger.LogAsync(
            message.Message,
            source:    message.Source,
            severity:  message.Severity,
            exception: message.Exception
        );

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
        ITextChannel  text
    ) {
        var client = GetClient();
        var lava   = GetLavaNode();
        var player = await MusicPlayer.CreatePlayerAsync(voice, text, services);
        if (player is null) {
            await Logger.LogAsync(
                $"Could not create player for voice channel: {voice.Name}",
                severity: LogSeverity.Error
            );
            return;
        }
        var is_added = music_players.TryAdd(voice.GuildId, player);
        if (!is_added) {
            await Logger.LogAsync(
                $"Guild already has a music player.",
                severity: LogSeverity.Warning
            );
            return;
        }
    }

    public async Task LeaveVoiceAsync(IGuild guild) {
        if (music_players.TryRemove(guild.Id, out var player)) {
            await player.Destroy();
        } else {
            await Logger.LogAsync(
                $"Can't leave voice channel in guild {guild.Name}.",
                severity: LogSeverity.Error
            );
        }   
    }

}
