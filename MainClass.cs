using System;
using BepInEx;
using HarmonyLib;

namespace EnableMilkyWayGalaxy
{
    [BepInPlugin(ModGuid, ModName, ModVersion)]
    [BepInProcess(GameProcess)]
    public class MainClass : BaseUnityPlugin
    {
        private const string ModGuid = "Allz.EnableMilkyWayGalaxy";
        private const string ModName = "EnableMilkyWayGalaxy";
        private const string ModVersion = "1.0.5";
        private const string GameProcess = "DSPGAME.exe";
        private readonly Harmony _harmony = new Harmony(ModGuid);

        [Obsolete]
        public void Start() => this._harmony.PatchAll();
    }
}