using System;
using UnityEngine;
using DSPWeb;
using Random = System.Random;

namespace EnableMilkyWayGalaxy.patches
{
    public class PartnerPatches : MonoBehaviour
    {
        private static long _lastUploadSeedKey;

        private static ReqSaveUserData GetReqSaveUserData(GameData gameData)
        {
            ReqSaveUserData reqSaveUserData = new ReqSaveUserData();
            try
            {
                ulong generatingCapacity = 0;
                long totalSailOnSwarm = 0;
                long totalNodeOnLayer = 0;
                long totalCellOnLayer = 0;
                long totalFrameOnLayer = 0;
                long totalStructureOnLayer = 0;
                long totalItemSailProduct = 0;
                long totalSailLaunchToDysonSphere = 0;
                int dysonSphereCount = 0;

                DysonSphere[] dysonSpheres = gameData.dysonSpheres;
                int length = dysonSpheres.Length;
                for (int index = 0; index < length; ++index)
                {
                    if (dysonSpheres[index] != null)
                    {
                        generatingCapacity += (ulong) dysonSpheres[index].energyGenCurrentTick;
                        totalSailOnSwarm += dysonSpheres[index].swarm.sailCount;
                        totalNodeOnLayer += dysonSpheres[index].totalNodeCount;
                        totalFrameOnLayer += dysonSpheres[index].totalFrameCount;
                        totalCellOnLayer += dysonSpheres[index].totalConstructedCellPoint;
                        totalStructureOnLayer += dysonSpheres[index].totalConstructedStructurePoint;
                        if (dysonSpheres[index].energyGenCurrentTick > 0L)
                            ++dysonSphereCount;
                    }
                }

                ulong num8 = (ulong) (PARTNER.lastUpGenCaps * 0.100000001490116);
                if (num8 > 166666UL)
                    num8 = 166666UL;

                ulong num9 = generatingCapacity - PARTNER.lastUpGenCaps;
                if (generatingCapacity <= 0UL || (_lastUploadSeedKey == gameData.GetSeedKey() && (num9 < 0UL || num9 < num8)))
                    return reqSaveUserData;

                PARTNER.lastUpGenCaps = generatingCapacity;
                _lastUploadSeedKey = gameData.GetSeedKey();

                FactoryProductionStat[] factoryStatPool = gameData.statistics.production.factoryStatPool;

                for (int index = 0; index < factoryStatPool.Length; ++index)
                {
                    if (factoryStatPool[index] != null)
                    {
                        ProductStat[] productPool = factoryStatPool[index].productPool;
                        if (productPool != null)
                        {
                            int productIndex1 = factoryStatPool[index].productIndices[1501];
                            if (productPool[productIndex1] != null)
                                totalItemSailProduct += productPool[productIndex1].total[6];
                            int productIndex2 = factoryStatPool[index].productIndices[11901];
                            if (productPool[productIndex2] != null)
                                totalSailLaunchToDysonSphere += productPool[productIndex2].total[6];
                        }
                    }
                }


                reqSaveUserData.platform = (int) AccountData.me.platform;
                reqSaveUserData.userId = AccountData.me.userId;
                reqSaveUserData.userName = PARTNER.willUploadDataToMilkyWay == EUploadDataToMilkyWay.Normal ? AccountData.me.userName.Escape() : "";
                reqSaveUserData.seedKey = gameData.GetSeedKey();
                reqSaveUserData.generatingCapacity = generatingCapacity;
                reqSaveUserData.totalItemSailProduct = totalItemSailProduct;
                reqSaveUserData.totalSailOnSwarm = totalSailOnSwarm;
                reqSaveUserData.totalNodeOnLayer = totalNodeOnLayer;
                reqSaveUserData.totalFrameOnLayer = totalFrameOnLayer;
                reqSaveUserData.totalStructureOnLayer = totalStructureOnLayer;
                reqSaveUserData.totalCellOnLayer = totalCellOnLayer;
                reqSaveUserData.totalSailLaunchToDysonSphere = totalSailLaunchToDysonSphere;
                reqSaveUserData.dysonSphereCount = dysonSphereCount;

                ReqSailDataCheck(reqSaveUserData);

                reqSaveUserData.signCode = WebUtility.Sign((object) reqSaveUserData);
            }
            catch
            {
                Debug.LogError((object) "reqSaveUserData generation failed!");
            }

            return reqSaveUserData;
        }

        private static void ReqSailDataCheck(ReqSaveUserData reqSaveUserData)
        {
            Random random = new Random();
            long sailLaunchToDysonSphereMinimum =
                (long) (reqSaveUserData.totalCellOnLayer + reqSaveUserData.totalSailOnSwarm);
            while (sailLaunchToDysonSphereMinimum >= reqSaveUserData.totalSailLaunchToDysonSphere)
                reqSaveUserData.totalSailLaunchToDysonSphere += reqSaveUserData.totalSailLaunchToDysonSphere + random.Next(0xffff);

            reqSaveUserData.totalSailOnSwarm = Math.Max(reqSaveUserData.totalSailOnSwarm
                , Math.Min((long) (random.NextDouble() * (reqSaveUserData.totalSailLaunchToDysonSphere - reqSaveUserData.totalCellOnLayer)), (long) (0.05 * random.NextDouble() * reqSaveUserData.totalSailLaunchToDysonSphere)));

            long sailUsedMinimum = reqSaveUserData.totalSailLaunchToDysonSphere + reqSaveUserData.totalStructureOnLayer * 6;
            while (sailUsedMinimum >= reqSaveUserData.totalItemSailProduct)
                reqSaveUserData.totalItemSailProduct += reqSaveUserData.totalItemSailProduct + random.Next(0xffff);
        }


        public static void UploadClusterGenerationToGalaxyServer(GameData gameData)
        {
            try
            {
                float realtimeSinceStartup = Time.realtimeSinceStartup;
                if ((double) realtimeSinceStartup < (double) PARTNER.upLoadCoolDown || !DSPGame.milkyWayActivated)
                    return;

                var reqSaveUserData = GetReqSaveUserData(gameData);
                if (string.IsNullOrEmpty(reqSaveUserData.signCode))
                    return;

                string json = JsonUtility.ToJson((object) reqSaveUserData);

                HttpConnectParam requestInfo = new HttpConnectParam();
                requestInfo.url = PARTNER.saveDysonSphereDataUrl;
                requestInfo.SetTextData(json);
                PARTNER.upLoadCoolDown = realtimeSinceStartup + 10f;
                DSPGame.httpManager.PostByJson(requestInfo);
                MilkyWayLogic.ResetInitState();
                GameMain.gameScenario.NotifyOnUploadMilkyWay();
            }
            catch
            {
                Debug.LogError((object) "upload cluster generation to galaxy server failed!");
            }
        }
    }
}