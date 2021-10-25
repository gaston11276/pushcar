using System;
using System.Threading.Tasks;
using JetBrains.Annotations;
using NFive.SDK.Client.Commands;
using NFive.SDK.Client.Communications;
using NFive.SDK.Client.Events;
using NFive.SDK.Client.Interface;
using NFive.SDK.Client.Services;
using NFive.SDK.Client.Input;
using NFive.SDK.Core.Diagnostics;
using NFive.SDK.Core.Models.Player;
using Gaston11276.Pushcar.Client.Overlays;
using Gaston11276.Pushcar.Shared;
using CitizenFX.Core;
using static CitizenFX.Core.Native.API;

namespace Gaston11276.Pushcar.Client
{
	

	[PublicAPI]
	public class PushcarService : Service
	{
		public const int ANIM_NORMAL = 1;//0b00000000000000000000000000000001;
		public const int ANIM_REPEAT = 1 << 1;//0b00000000000000000000000000000010;
		public const int ANIM_STOP_LAST_FRAME = 1 << 2;// = 0b00000000000000000000000000000100;
		public const int ANIM_UPPERBODY = 1 << 3;//0b00000000000000000000000000001000;
		public const int ANIM_ENABLE_PLAYER_CONTROL = 1 << 4;//0b00000000000000000000000000010000;
		public const int ANIM_CANCELABLE = 1 << 5;//0b00000000000000000000000000100000;
		//this.Logger.Debug($"{1 | 1 << 1 | 1 << 3 | 1 << 4}"); //1 + 2 + 8 + 16

		private Configuration config;
		private Hotkey activateKey;
		//private PushcarOverlay overlay;

		Vector3 m_vec_force;
		int m_pushed_vehicle;
		string animationDictionary;
		string animationName;

		public PushcarService(ILogger logger, ITickManager ticks, ICommunicationManager comms, ICommandManager commands, IOverlayManager overlay, User user) : base(logger, ticks, comms, commands, overlay, user) { }

		public override async Task Started()
		{
			// Request server configuration
			this.config = await this.Comms.Event(PushcarEvents.Configuration).ToServer().Request<Configuration>();
			this.activateKey = new Hotkey(this.config.Hotkey);

			RequestAnimDict("missfinale_c2ig_11");

			// Create overlay
			//this.overlay = new PushcarOverlay(this.OverlayManager);

			// Attach a tick handler
			this.Ticks.On(OnHotkey);
			this.Ticks.On(OnTick);
		}

		private async Task OnTick()
		{
			
			// Do something every frame

			await Delay(TimeSpan.FromSeconds(1));
		}

		public void OnHotkey()
		{
			if (this.activateKey.IsJustPressed())
			{
				m_pushed_vehicle = GetClosestVehicle(Game.PlayerPed.Position.X, Game.PlayerPed.Position.Y, Game.PlayerPed.Position.Z, 5f, 0, 1);
				if (m_pushed_vehicle != 0)
				{
					Model car_model = GetEntityModel(m_pushed_vehicle);
					Vector3 vec_min = new Vector3();
					Vector3 vec_max = new Vector3();

					GetModelDimensions((uint)car_model.GetHashCode(), ref vec_min, ref vec_max);

					Vector3 vehicle_position = GetEntityCoords(m_pushed_vehicle, false);
					Vector3 player_to_veh = vehicle_position - Game.PlayerPed.Position;

					if (GetDeg(player_to_veh, Game.PlayerPed.ForwardVector) < 45f && player_to_veh.Length() - vec_max.Y < 1.0f)
					{
						SetPedCanPlayAmbientAnims(Game.PlayerPed.Handle, true);
						TaskPlayAnim(Game.PlayerPed.Handle, "missfinale_c2ig_11", "pushcar_offcliff_m", 1f, 1f, -1, (ANIM_NORMAL | ANIM_REPEAT | ANIM_ENABLE_PLAYER_CONTROL | ANIM_CANCELABLE), 1f, false, false, false);
							
						m_vec_force = player_to_veh;
						m_vec_force.Normalize();
						m_vec_force *= GetVehicleHandlingFloat(m_pushed_vehicle, "CHandlingData", "fMass") * 15f;
					}
				}
			}

			if (this.activateKey.IsPressed())
			{
				if (m_pushed_vehicle != 0)
				{
					Model car_model = GetEntityModel(m_pushed_vehicle);
					Vector3 vec_min = new Vector3();
					Vector3 vec_max = new Vector3();

					GetModelDimensions((uint)car_model.GetHashCode(), ref vec_min, ref vec_max);

					Vector3 vehicle_position = GetEntityCoords(m_pushed_vehicle, false);
					Vector3 player_to_veh = vehicle_position - Game.PlayerPed.Position;

					if (GetDeg(player_to_veh, Game.PlayerPed.ForwardVector) < 45f && player_to_veh.Length() - vec_max.Y < 1.0f)
					{
						ApplyForceToEntity(m_pushed_vehicle, 0, m_vec_force.X, m_vec_force.Y, m_vec_force.Z, 0f, 0f, 0f, 0, false, true, false, false, true);
					}
					else
					{
						StopEntityAnim(Game.PlayerPed.Handle, "pushcar_offcliff_m", "missfinale_c2ig_11", 1.0f);
						m_pushed_vehicle = 0;
						m_vec_force.X = 0f; m_vec_force.Y = 0f; m_vec_force.Z = 0f;
					}
				}
			}

			if (this.activateKey.IsJustReleased())
			{
				StopEntityAnim(Game.PlayerPed.Handle, "pushcar_offcliff_m", "missfinale_c2ig_11", 1.0f);
				m_pushed_vehicle = 0;
				m_vec_force.X = 0f; m_vec_force.Y = 0f; m_vec_force.Z = 0f;
			}
		}

		private float GetDeg(Vector3 vec1, Vector3 vec2)
		{
			vec1.Normalize();
			vec2.Normalize();
			float scalar = vec1.X * vec2.X + vec1.Y * vec2.Y;
			float value_deg = (float)((Math.Acos((double)scalar)) / Math.PI) * 180f;
			return value_deg;
		}
	}
}
