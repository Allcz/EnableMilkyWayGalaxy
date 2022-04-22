using System;
using ABN;
using HarmonyLib;
using UnityEngine;

namespace EnableMilkyWayGalaxy.patches
{
    [HarmonyPatch]
    public class Patches
    {
        /*
         * 该补丁会处理戴森球发电数据使其合理化并上传至银河系服务器
         */
        [HarmonyPrefix]
        [HarmonyPatch(typeof(MilkyWayWebClient), "SendUploadLoginRequest")]
        public static bool GameSave_SaveCurrentGame_Prefix()
        {
            try
            {
                MilkyWayPatches.SendUploadLoginRequest();
            }
            catch (Exception e)
            {
                // Log.SaveToFile(e.ToString());
                Debug.Log(e);
                return true;
            }

            return true;
        }
        
        
        [HarmonyPrefix]
        // 判断游戏数据是否正常,返回true（正常）
        [HarmonyPatch(typeof(GameAbnormalityData_0925), "NothingAbnormal")]
        // 判断当前用户与存档用户是否一致，返回true（一致）
        [HarmonyPatch(typeof(AchievementLogic), "isSelfFormalGame", MethodType.Getter)]
        // 判断是否可以解锁成就，返回true（可以解锁） 原方法：public bool active => this.gameData.gameDesc.achievementEnable & this.gameData.gameAbnormality.IsGameNormal() && this.isSelfFormalGame;
        [HarmonyPatch(typeof(AchievementLogic), "active", MethodType.Getter)]
        public static bool IsGameLegitimate(ref bool __result)
        {
            __result = true;
            return false;
        }
    }
}