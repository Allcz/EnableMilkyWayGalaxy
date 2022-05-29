using System;
using ABN;
using HarmonyLib;
using UnityEngine;
using Random = System.Random;

namespace EnableMilkyWayGalaxy.patches
{
    [HarmonyPatch]
    public class Patches
    {
        /*
         * 该补丁会处理戴森球发电数据使其合理化，并上传数据至银河系服务器
         */
        [HarmonyPrefix, HarmonyPatch(typeof(MilkyWayWebClient), "SendUploadLoginRequest")]
        public static bool MilkyWayWebClient_SendUploadLoginRequest_Prefix()
        {
            try
            {
                MilkyWayPatches.SendUploadLoginRequest();
            }
            catch (Exception e)
            {
                Debug.Log(e);
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
    
    [HarmonyPatch(typeof(MilkyWayWebClient), "SendReportRequest")]
    class MilkyWayWebClientSendReportRequestPatch
    {
        private static Random random = new Random();
        
        /**
         * 方法执行前执行，为了确保gameTick值符合要求
         * 先校验 GameMain.gameTick 的值是否符合最低要求，如果不符合，临时改变其值
         */
        public static bool Prefix(ref long __state)
        {
            __state = GameMain.gameTick;
            // gameTick 值过小时，上传的戴森球数据不被承认
            long minGameTicket = (long) ((1 + 5 * random.NextDouble()) * 0x1fffff);
            GameMain.gameTick = __state > minGameTicket ? __state : minGameTicket;
            return true;
        }
        
        /**
         * 将 GameMain.gameTick 值复原
         */
        public static void Postfix(ref long __state)
        {
            GameMain.gameTick = __state;
        }
    }
}