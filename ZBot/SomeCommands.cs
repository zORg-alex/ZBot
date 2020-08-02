using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
namespace ZBot {
	public class SomeCommands : BaseCommandModule {
		[Command("ping")]
		[Description("Ping - pong")]
		public async Task Ping(CommandContext ctx) {
			await ctx.Channel.SendMessageAsync("Pong").ConfigureAwait(false);
		}

		[Command("add")]
		[Description("Add two numbers")]
		//[RequireRoles( RoleCheckMode.Any, "Owner")]
		public async Task Add(CommandContext ctx, [Description("Number one int")] int one, [Description("Number two int")] int two) {
			await ctx.Channel.SendMessageAsync((one + two).ToString()).ConfigureAwait(false);
		}

		[Command("remove-last")]
		[Description("Remove last messages if you have permission, or delete only your's from count messages")]
		public async Task RemoveLast(CommandContext ctx, int count) {
			//remove last count messages
			if (ctx.Member.PermissionsIn(ctx.Channel).HasFlag(Permissions.ManageMessages))
				await ctx.Channel.DeleteMessagesAsync(
				await ctx.Channel.GetMessagesBeforeAsync(
					ctx.Message.Id
					, count)
				.ConfigureAwait(false))
				.ConfigureAwait(false);
			//if doesn't have a permission, clean up your's only
			else
				foreach (var m in ctx.Channel.GetMessagesBeforeAsync(ctx.Message.Id, count).Result) {
					if (m.Author.Id == ctx.Member.Id)
						await m.DeleteAsync().ConfigureAwait(false);
				}
			//Cleanup
			await ctx.Message.DeleteAsync().ConfigureAwait(false);
		}
	}
}
