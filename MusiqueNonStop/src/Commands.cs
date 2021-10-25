using System;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Victoria;

namespace Kumodatsu.MusiqueNonStop {

    public class Commands : ModuleBase<SocketCommandContext> {

        private readonly LavaNode lava;

        public Commands(LavaNode lava) => this.lava = lava;

        [Command("ping")]
        public async Task PingAsync() => await ReplyAsync("pong");

        [Command("join")]
        public async Task JoinAsync() {
            if (lava.HasPlayer(Context.Guild)) {
                await ReplyAsync("I'm already in a voice channel.");
                return;
            }

            if (Context.User is IVoiceState { VoiceChannel: var channel }
                    && channel is not null) {
                try {
                    await lava.JoinAsync(
                        channel,
                        Context.Channel as ITextChannel
                    );
                    await ReplyAsync($"Joined {channel.Name}.");
                } catch (Exception exception) {
                    await ReplyAsync(
                        $"Exception: {exception.Message}\n{exception.StackTrace ?? ""}"
                    );
                }
            } else {
                await ReplyAsync("You must be in a voice channel.");
                return;
            }
        }

    }

}
