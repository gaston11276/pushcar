using NFive.SDK.Core.Controllers;
using NFive.SDK.Core.Input;

namespace Gaston11276.Pushcar.Shared
{
	public class Configuration : ControllerConfiguration
	{
		public InputControl Hotkey { get; set; } = InputControl.Context; // Default to E
	}
}
