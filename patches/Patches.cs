using HarmonyLib;

namespace EnableMilkyWayGalaxy.patches
{
    [HarmonyPatch]
    public class Patches
    {
        /*
         * 该补丁会处理戴森球发电数据使其合理化并上传至银河系服务器
         */
        [HarmonyPrefix]
        [HarmonyPatch(typeof(GameSave), "SaveCurrentGame")]
        public static bool GameSave_SaveCurrentGame_Prefix()
        {
            PartnerPatches.UploadClusterGenerationToGalaxyServer(GameMain.data);
            return true;
        }
        
        /*
         * 该补丁为了一定可以执行解锁成就的方法的目的
         * 判断游戏数据是否正常
         * @param __result = true, 游戏数据正常
         */
        [HarmonyPrefix]
        [HarmonyPatch(typeof (GameAbnormalityCheck_Obsolete), "isGameNormal")]
        public static bool GameAbnormalityCheck_Obsolete_isGameNormal_Prefix(ref bool __result)
        {
            __result = true;
            return false;
        }
    }
}