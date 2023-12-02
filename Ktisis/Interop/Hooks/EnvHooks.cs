﻿using Dalamud.Hooking;

using Ktisis.Env;
using Ktisis.Structs.Env;

namespace Ktisis.Interop.Hooks {
	public static class EnvHooks {
		// Hooks

		private unsafe delegate nint EnvUpdateDelegate(EnvManagerEx* env, nint a2);
		private delegate bool SkyTexDelegate(nint a1, uint a2, float a3, float a4);

		private static Hook<EnvUpdateDelegate> EnvUpdateHook = null!;
		private unsafe static nint EnvUpdateDetour(EnvManagerEx* env, nint a2) {
			if (Ktisis.IsInGPose && EnvService.TimeOverride != null)
				env->Time = EnvService.TimeOverride.Value;
			return EnvUpdateHook.Original(env, a2);
		}

		private static Hook<SkyTexDelegate> SkyTexHook = null!;
		private static bool SkyTexDetour(nint a1, uint a2, float a3, float a4) {
			if (Ktisis.IsInGPose && EnvService.SkyOverride != null)
				a2 = EnvService.SkyOverride.Value;
			return SkyTexHook.Original(a1, a2, a3, a4);
		}


		private delegate nint WaterRendererUpdateDelegate(nint a1);
		private static Hook<WaterRendererUpdateDelegate> WaterRendererUpdateHook = null!;
		private static nint WaterRendererUpdateDetour(nint a1) {
			if (Ktisis.IsInGPose && EnvService.FreezeWater == true) {
				return 0;
			}
			return WaterRendererUpdateHook.Original(a1);
		}
		
		
		// State

		private static bool Enabled;
		
		internal static void SetEnabled(bool enable) {
			if (Enabled == enable) return;
			if (enable)
				EnableHooks();
			else
				DisableHooks();
		}

		private static void EnableHooks() {
			Enabled = true;
			EnvUpdateHook.Enable();
			SkyTexHook.Enable();
			WaterRendererUpdateHook.Enable();
		}
		
		private static void DisableHooks() {
			Enabled = false;
			EnvUpdateHook.Disable();
			SkyTexHook.Disable();
			WaterRendererUpdateHook.Disable();
		}
		
		// Init & Dispose
		
		public unsafe static void Init() {
			var addr1 = Services.SigScanner.ScanText("E8 ?? ?? ?? ?? 49 8B 0E 48 8D 93 ?? ?? ?? ??");
            EnvUpdateHook = Services.Hooking.HookFromAddress<EnvUpdateDelegate>(addr1, EnvUpdateDetour);
            
			var addr2 = Services.SigScanner.ScanText("E8 ?? ?? ?? ?? 44 38 63 30 74 05 0F 28 DE");
            SkyTexHook = Services.Hooking.HookFromAddress<SkyTexDelegate>(addr2, SkyTexDetour);
			
			var addr3 = Services.SigScanner.ScanText("48 8B C4 48 89 58 18 57 48 81 EC ?? ?? ?? ?? 0F 29 70 E8 48 8B D9");
			WaterRendererUpdateHook = Services.Hooking.HookFromAddress<WaterRendererUpdateDelegate>(addr3, WaterRendererUpdateDetour);
        }

		public static void Dispose() {
			DisableHooks();
			
			EnvUpdateHook.Dispose();
			EnvUpdateHook.Dispose();
			
			SkyTexHook.Disable();
			SkyTexHook.Dispose();
			
			WaterRendererUpdateHook.Disable();
			WaterRendererUpdateHook.Dispose();
		}
	}
}
