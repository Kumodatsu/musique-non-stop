using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Victoria;
using Victoria.Enums;
using Victoria.EventArgs;
using Victoria.Responses.Search;

namespace Kumodatsu.MusiqueNonStop {

    public class Commands : ModuleBase<SocketCommandContext> {

        public Commands(IServiceProvider services)
            => this.services = services;

        [Command("ping")]
        public async Task PingAsync() => await ReplyAsync("Pong!");

        [Command("join"), Summary("Makes the bot join your voice channel.")]
        public async Task Join() => await Bot.Instance.JoinAsync(
            (Context.User as SocketGuildUser)!,
            Context.Guild,
            Context.Channel as ITextChannel
        );

        [Command("leave"), Summary("Makes the bot leave its voice channel.")]
        public async Task Leave() => await Bot.Instance.LeaveAsync(
            Context.Guild,
            Context.Channel as ITextChannel
        );

        [Command("play"), Summary("Plays a song from a given query.")]
        public async Task Play([Remainder] string query)
            => await Bot.Instance.PlayAsync(
                (Context.User as SocketGuildUser)!,
                Context.Guild,
                query,
                Context.Channel as ITextChannel
            );

        [Command("stop"), Summary("Stops the currently playing song.")]
        public async Task Stop()
            => await Bot.Instance.StopPlayingAsync(
                (Context.User as SocketGuildUser)!,
                Context.Guild,
                Context.Channel as ITextChannel
            );

        [Command("skip"), Summary("Skips the currently playing song.")]
        public async Task Skip()
            => await Bot.Instance.SkipAsync(
                (Context.User as SocketGuildUser)!,
                Context.Guild,
                Context.Channel as ITextChannel
            );

        [Command("queue"), Summary("Shows the current song queue.")]
        public async Task ShowQueue()
            => await Bot.Instance.ShowQueueAsync(
                (Context.User as SocketGuildUser)!,
                Context.Guild
            );

        [Command("help"), Summary("Displays the list of commands.")]
        public async Task ShowCommandList() {
            var command_service = GetCommandService();

            var commands = command_service.Commands;
            var builder  = new EmbedBuilder();
            foreach (var command in commands)
                builder.AddField(
                    command.Name,
                    command.Summary ?? "(no description)"
                );
            await ReplyAsync(embed: builder.Build());
        }

        private readonly IServiceProvider services;

        private LavaNode GetLavaNode()
            => services.GetRequiredService<LavaNode>();
        private CommandService GetCommandService()
            => services.GetRequiredService<CommandService>();

    }

}
