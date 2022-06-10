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

        /**
         * 获取seedKey
         */
        [HarmonyPrefix, HarmonyPatch(typeof(GameDesc), "seedKey64", MethodType.Getter)]
        public static bool GameDesc_SeedKey64_Prefix(ref long __result)
        {
            if (null != GameMain.data && null != GameMain.data.gameDesc)
            {
                var desc = GameMain.data.gameDesc;
                int galaxySeed = desc.galaxySeed;
                int num1 = desc.starCount;
                int num2 = (int) ((double) desc.resourceMultiplier * 10.0 + 0.5);
                if (num1 > 999)
                    num1 = 999;
                else if (num1 < 1)
                    num1 = 1;
                if (num2 > 99)
                    num2 = 99;
                else if (num2 < 1)
                    num2 = 1;
                int num3 = 0;
                if (desc.isSandboxMode)
                    num3 = 0;
                else if (!desc.isPeaceMode)
                    num3 = 0;
                __result = (long) galaxySeed * 100000000L + (long) num1 * 100000L + (long) num2 * 1000L + (long) num3;
            }

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
            ulong generatingCapacity = 0UL;
            if (null != GameMain.data && null != GameMain.data.dysonSpheres)
            {
                foreach (DysonSphere dysonSphere in GameMain.data.dysonSpheres)
                {
                    if (null == dysonSphere) continue;
                    generatingCapacity += (ulong) dysonSphere.energyGenCurrentTick;
                }
            }

            __state = GameMain.gameTick;
            ulong rate = 0L;
            // 计算一个相对固定的rate
            if (null != GameMain.data && null != GameMain.data.account)
            {
                String tem = GameMain.data.GetClusterSeedKey() + GameMain.data.account.userName +
                             GameMain.data.account.userId;
                var hashCode = Math.Abs(tem.GetHashCode());
                while (hashCode > 0xf11)
                {
                    hashCode /= 2;
                }

                rate = (ulong) (0xf11 + hashCode);
            }

            rate = rate == 0L ? (ulong) (random.Next(0xf11) + 0xf11) : rate;
            // 根据发电量估算小时数，10-20h/TW
            var minGameTickByGC = generatingCapacity / rate;
            // gameTick 值过小时，上传的戴森球数据不被承认
            long minGameTick = Math.Max((long) ((1 + 5 * random.NextDouble()) * 0x1fffff), (long) minGameTickByGC);
            GameMain.gameTick = __state > minGameTick ? __state : minGameTick;
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