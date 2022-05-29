using BepInEx;
using HarmonyLib;
using System;

namespace EnableMilkyWayGalaxy
{
    [BepInPlugin(ModGuid, ModName, ModVersion)]
    [BepInProcess(GameProcess)]
    public class EnableMilkyWayGalaxy : BaseUnityPlugin
    {
        private const string ModGuid = "Allz.EnableMilkyWayGalaxy";
        private const string ModName = "EnableMilkyWayGalaxy";
        private const string ModVersion = "1.1.3";
        private const string GameProcess = "DSPGAME.exe";
        private readonly Harmony _harmony = new Harmony(ModGuid);

        [Obsolete]
        public void Start()
        {
            _harmony.PatchAll();
        }
    }
}