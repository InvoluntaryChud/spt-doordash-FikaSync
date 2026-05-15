using EFT;
using HarmonyLib;
using SPT.Reflection.Patching;
using System;
using System.Reflection;
using Fika.Core.Main.Utils;
using UnityEngine;

namespace tarkin.doordash.Patches
{
    internal class Patch_GameWorld_OnGameStarted : ModulePatch
    {
        public static event Action<GameWorld> OnPostfix;

        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(GameWorld), nameof(GameWorld.OnGameStarted));
        }

        [PatchPostfix]
        private static void PatchPostfix(GameWorld __instance)
        {
            if (__instance is not null)
                OnPostfix?.Invoke(__instance);
        }
    }
}
