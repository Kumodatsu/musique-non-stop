using System.Threading.Tasks;
using Discord.Commands;

namespace MusiqueNonStop {

    public class Commands : ModuleBase<SocketCommandContext> {

        [Command("ping")]
        public async Task Ping() => await ReplyAsync("pong");

    }

}
