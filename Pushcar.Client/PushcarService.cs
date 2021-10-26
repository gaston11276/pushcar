using System;
using System.Threading.Tasks;
using JetBrains.Annotations;
using NFive.SDK.Client.Commands;
using NFive.SDK.Client.Communications;
using NFive.SDK.Client.Events;
using NFive.SDK.Client.Interface;
using NFive.SDK.Client.Services;
using NFive.SDK.Client.Input;
using NFive.SDK.Core.Input;
using NFive.SDK.Core.Diagnostics;
using NFive.SDK.Core.Models.Player;
using CitizenFX.Core;
using static CitizenFX.Core.Native.API;

using NFive.SDK.Core.Controllers;
//using Gaston11276.Debug;

namespace Gaston11276.Pushcar.Client
{
	[PublicAPI]
	public class PushcarService : Service
	{
		public const int ANIM_NORMAL = 1;
		public const int ANIM_REPEAT = 1 << 1;
		public const int ANIM_STOP_LAST_FRAME = 1 << 2;
		public const int ANIM_UPPERBODY = 1 << 3;
		public const int ANIM_ENABLE_PLAYER_CONTROL = 1 << 4;
		public const int ANIM_CANCELABLE = 1 << 5;

		private Hotkey activateKey;

		Vector3 vecForce;
		float vehicleMass;
		Vector3 vecPlayerToVehicle;
		int m_pushed_vehicle;

		//Debug
		//private Hotkey DEBUG_keyClear;
		//private Hotkey DEBUG_keyStepUp;
		//private Hotkey DEBUG_keyStepDown;
		float forceFactor;
		float distToCollision;

		public PushcarService(ILogger logger, ITickManager ticks, ICommunicationManager comms, ICommandManager commands, IOverlayManager overlay, User user) : base(logger, ticks, comms, commands, overlay, user) { }

		public override async Task Started()
		{
			this.activateKey = new Hotkey(InputControl.Context);

			RequestAnimDict("missfinale_c2ig_11");

			// Create overlay
			//this.overlay = new PushcarOverlay(this.OverlayManager);

			// Attach a tick handler
			this.Ticks.On(OnHotkey);

			//Debug
			forceFactor = 80f;
			//this.DEBUG_keyClear = new Hotkey(InputControl.Aim);
			//this.DEBUG_keyStepUp = new Hotkey(InputControl.FrontendUp);
			//this.DEBUG_keyStepDown = new Hotkey(InputControl.FrontendDown);
			//debug.Set();
			//this.Ticks.On(OnDraw);

			await Delay(1);
		}

		/*
		//Debug
		void OnDraw()
		{
			debug.Draw();
			
		}
		*/

		public async Task OnHotkey()
		{
			// Debug
			//debug.Clear();

			//Debug
			/*
			if (this.DEBUG_keyClear.IsJustReleased())
			{
				debug.Clear();
			}
			if (this.DEBUG_keyStepUp.IsJustReleased())
			{
				forceFactor += 1f;
			}
			if (this.DEBUG_keyStepDown.IsJustReleased())
			{
				forceFactor -= 1f;
			}
			*/

			if (this.activateKey.IsJustPressed())
			{				
				m_pushed_vehicle = GetClosestVehicle(Game.PlayerPed.Position.X, Game.PlayerPed.Position.Y, Game.PlayerPed.Position.Z, 5f, 0, 1);

				if (m_pushed_vehicle != 0)
				{
					if (IsPlayerFacingVehicle())
					{
						StartPushing();
					}
				}
			}
			else if (this.activateKey.IsPressed())
			{				
				if (m_pushed_vehicle != 0)
				{
					UpdateForce();
					if (IsPlayerFacingVehicle())
					{
						await Delay(10); // This affects the amount of force being applied, keeping it frametime neutral
						
						ApplyForceToEntity(m_pushed_vehicle, 0, vecForce.X, vecForce.Y, vecForce.Z, 0f, 0f, 0f, 0, false, true, false, false, true);
					}
					else
					{
						StopPushing();
					}
				}
			}

			if (this.activateKey.IsJustReleased())
			{
				StopPushing();
			}
		}

		private bool IsPlayerFacingVehicle(bool pushing = false)
		{
			Model car_model = GetEntityModel(m_pushed_vehicle);
			int model_hash = car_model.GetHashCode();

			Vector3 vec_min = new Vector3();
			Vector3 vec_max = new Vector3();

			GetModelDimensions((uint)car_model.GetHashCode(), ref vec_min, ref vec_max);
			Vector3 vehicle_position = GetEntityCoords(m_pushed_vehicle, false);
			vecPlayerToVehicle = vehicle_position - Game.PlayerPed.Position;

			//debug.Out($"Dist: {vecPlayerToVehicle.Length() - vec_max.Y}");
			distToCollision = vecPlayerToVehicle.Length() - vec_max.Y;

			if (GetDeg(vecPlayerToVehicle, Game.PlayerPed.ForwardVector) < 45f && vecPlayerToVehicle.Length() - vec_max.Y < 1.0f)
			{
				return true;
			}
			return false;
		}

		private void SetForce()
		{
			vehicleMass = GetVehicleHandlingFloat(m_pushed_vehicle, "CHandlingData", "fMass");
		}

		private void UpdateForce()
		{
			vecForce = vecPlayerToVehicle;
			vecForce.Normalize();

			//debug.Out($"ForceFactor: ${forceFactor}");
			float distFactor = 1.0f - distToCollision;
			if (distFactor > 1f) distFactor = 1f;
			//debug.Out($"DistFactor: ${distFactor}");
			float force = forceFactor * 0.5f * (1.0f - distToCollision);
			//debug.Out($"Force: ${force}");

			vecForce *= (vehicleMass * force);
		}

		private void StartPushing()
		{
			SetForce();
			UpdateForce();
			SetPedCanPlayAmbientAnims(Game.PlayerPed.Handle, true);
			TaskPlayAnim(Game.PlayerPed.Handle, "missfinale_c2ig_11", "pushcar_offcliff_m", 1f, 1f, -1, (ANIM_NORMAL | ANIM_REPEAT | ANIM_ENABLE_PLAYER_CONTROL | ANIM_CANCELABLE), 1f, false, false, false);
		}

		private void Push()
		{ }

		private void StopPushing()
		{
			StopEntityAnim(Game.PlayerPed.Handle, "pushcar_offcliff_m", "missfinale_c2ig_11", 1.0f);
			m_pushed_vehicle = 0;
			vecForce.X = 0f; vecForce.Y = 0f; vecForce.Z = 0f;
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
