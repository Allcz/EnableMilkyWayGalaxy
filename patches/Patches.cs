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
        [HarmonyPatch(typeof(GameAbnormalityData), "IsGameNormal")]
        public static bool GameAbnormalityCheck_Obsolete_isGameNormal_Prefix(ref bool __result)
        {
            __result = true;
            return false;
        }

        /*
         * 当前用户与存档用户不一致时，结果强制返回一致
         * UIAchievementPanel._OnOpen()中使用该判断，以显示成就开启新存档提示
         */
        [HarmonyPrefix]
        [HarmonyPatch(typeof(AchievementLogic), "isSelfFormalGame", MethodType.Getter)]
        public static bool AchievementSystem_isSelfFormalGame_Prefix(ref bool __result)
        {
            __result = true;
            return false;
        }

        /**
         * 该补丁使游戏一定可以执行解锁成就的方法
         * public bool active => this.gameData.gameDesc.achievementEnable & this.gameData.gameAbnormality.IsGameNormal() && this.isSelfFormalGame;
         */
        [HarmonyPrefix]
        [HarmonyPatch(typeof(AchievementLogic), "active", MethodType.Getter)]
        public static bool AchievementLogic_active_Prefix(ref bool __result)
        {
            __result = true;
            return false;
        }
    }
}