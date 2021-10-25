using JetBrains.Annotations;
using NFive.SDK.Core.Diagnostics;
using NFive.SDK.Server.Communications;
using NFive.SDK.Server.Controllers;
using Gaston11276.Pushcar.Shared;

namespace Gaston11276.Pushcar.Server
{
	[PublicAPI]
	public class PushcarController : ConfigurableController<Configuration>
	{
		public PushcarController(ILogger logger, Configuration configuration, ICommunicationManager comms) : base(logger, configuration)
		{
			// Send configuration when requested
			comms.Event(PushcarEvents.Configuration).FromClients().OnRequest(e => e.Reply(this.Configuration));
		}
	}
}
