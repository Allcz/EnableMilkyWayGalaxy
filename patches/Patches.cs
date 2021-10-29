using HarmonyLib;

namespace EnableMilkyWayGalaxy.patches
{
    [HarmonyPatch]
    public class Patches
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(GameAbnormalityCheck_Obsolete), "isGameNormal")]
        public static bool IsGameNormalPatch() => true;

        [HarmonyPrefix]
        [HarmonyPatch(typeof(PARTNER), "UploadClusterGenerationToGalaxyServer")]
        public static void UploadClusterGenerationToGalaxyServer(GameData gameData)
        {
            PartnerPatches.UploadClusterGenerationToGalaxyServer(gameData);
        }
    }
}