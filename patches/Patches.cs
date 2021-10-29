using HarmonyLib;

namespace EnableMilkyWayGalaxy.patches
{
    [HarmonyPatch]
    public class Patches
    {
        /*
         * [HarmonyPostfix] 在 GameSave.SaveCurrentGame()后执行
         * SaveCurrentGame()方法中会判断游戏数据合法性，只有当游戏数据异常未执行数据上传时，则该补丁会将数据合理化并上传
         */
        [HarmonyPostfix]
        [HarmonyPatch(typeof(GameSave), "SaveCurrentGame")]
        public static void SaveCurrentGamePostfix()
        {
            PartnerPatches.UploadClusterGenerationToGalaxyServer(GameMain.data);
        }
    }
}