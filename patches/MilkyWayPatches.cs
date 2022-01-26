using DSPWeb;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Random = System.Random;

namespace EnableMilkyWayGalaxy.patches
{
    public class MilkyWayPatches : MonoBehaviour
    {
        private static MilkyWayWebClient milkyWayWebClient = DSPGame.milkyWayWebClient;
        private static double time => (DateTime.UtcNow.ToOADate() - 44217.0) * 86400.0;

        private static double lastUploadTime
        {
            get { return ReflectionUtil.GetPrivateField<double>(milkyWayWebClient, "lastUploadTime"); }
            set { ReflectionUtil.SetPrivateField(milkyWayWebClient, "lastUploadTime", value); }
        }

        public static void SendUploadRecordRequest(GameData gameData)
        {
            var data = new SaveUserData();
            if (gameData == null)
            {
                return;
            }

            Pre(gameData);
            DysonSphere[] dysonSpheres = gameData.dysonSpheres;
            int length = dysonSpheres.Length;
            for (int index = 0; index < length; ++index)
            {
                if (dysonSpheres[index] != null)
                {
                    data.generatingCapacity += (ulong) dysonSpheres[index].energyGenCurrentTick;
                    data.totalSailOnSwarm += (long) dysonSpheres[index].swarm.sailCount;
                    data.totalNodeOnLayer += dysonSpheres[index].totalConstructedNodeCount;
                    data.totalFrameOnLayer += dysonSpheres[index].totalConstructedFrameCount;
                    data.totalCellOnLayer += dysonSpheres[index].totalConstructedCellPoint;
                    data.totalStructureOnLayer += dysonSpheres[index].totalConstructedStructurePoint;
                    if (dysonSpheres[index].energyGenCurrentTick > 0L)
                        ++data.dysonSphereCount;
                }
            }

            if (data.generatingCapacity <= 0UL)
                return;
            lastUploadTime = time;
            FactoryProductionStat[] factoryStatPool = gameData.statistics.production.factoryStatPool;
            float miningCostRate = Math.Min(gameData.history.miningCostRate, (float) Math.Pow(0.94, 5));

            int[] productItemId =
            {
                6001, // 蓝           
                6002, // 红          
                6003, // 黄          
                6004, // 紫          
                6005, // 绿          {(1304, 1)}
                6006, // 白          {(6001, 1), (6002, 1), (6003, 1), (6004, 1), (6005, 1)}   
                1304, // 位面过滤器    {(1126, 1)}
                1404, // 光子合并器      
                1125, // 框架材料       
                1126, // 卡西米尔晶体     
                1501, // 太阳帆         {(1404, 0.5)}  
                1503, // 小型运载火箭    {(1501, 6), (1304, 4), (1125, 6)}
                11901 // 
            };
            var productTotalDict = new Dictionary<int, long>();
            foreach (var id in productItemId)
            {
                productTotalDict.Add(id, 0);
            }

            for (int index = 0; index < factoryStatPool.Length; ++index)
            {
                if (factoryStatPool[index] == null || factoryStatPool[index].productPool == null)
                    continue;
                ProductStat[] productPool = factoryStatPool[index].productPool;
                foreach (int itemId in productItemId)
                {
                    int productIndex = factoryStatPool[index].productIndices[itemId];
                    if (productPool[productIndex] == null)
                        continue;
                    productTotalDict[itemId] += productPool[productIndex].total[6];
                }
            }


            Dictionary<int, long> tempProductTotalDict = new Dictionary<int, long>(productTotalDict);

            Random random = new Random();
            var randomRate = 1.01 + 0.02 * random.NextDouble();
            if (miningCostRate < Math.Pow(0.94, 5))
            {
                long upgradeCost = 0;
                int level = (int) (Math.Log(0.94, miningCostRate) + 0.5);
                if (level > 5)
                {
                    upgradeCost += 2000 * level * (level - 10) - 2000 * 5 * (5 - 10);
                    upgradeCost *= 10;
                    upgradeCost += random.Next(0xfff);
                }

                upgradeCost = Math.Max(upgradeCost,
                    (long) (data.generatingCapacity / (500.0d + 500 * random.NextDouble())));

                if (upgradeCost > tempProductTotalDict[6006])
                {
                    tempProductTotalDict[6006] = ProductRate(randomRate, upgradeCost);
                }
            }


            for (int i = 6001; i < 6006; i++)
            {
                var randomTotal = (long) ((1 + 0.5d * random.NextDouble()) * 0xfffff) + tempProductTotalDict[6006];
                if (tempProductTotalDict[i] < randomTotal)
                {
                    tempProductTotalDict[i] = ProductRate(randomRate, randomTotal);
                    if (i == 6005)
                    {
                        tempProductTotalDict[i] = ProductRate(randomRate, tempProductTotalDict[i]);
                    }
                }
            }


            long min1503 = data.totalStructureOnLayer;
            if (tempProductTotalDict[1503] < min1503)
            {
                tempProductTotalDict[1503] = ProductRate(randomRate, min1503);
            }

            long min11901 = data.totalCellOnLayer + data.totalSailOnSwarm;
            if (tempProductTotalDict[11901] < min11901)
            {
                tempProductTotalDict[11901] = ProductRate(randomRate, min11901);
            }

            long min1501 = tempProductTotalDict[11901] + tempProductTotalDict[1503] * 6;
            if (tempProductTotalDict[1501] < min1501)
            {
                tempProductTotalDict[1501] = ProductRate(randomRate, min1501);
            }

            long min1404 = tempProductTotalDict[1501] / 2;
            if (tempProductTotalDict[1404] < min1404)
            {
                tempProductTotalDict[1404] = ProductRate(randomRate, min1404);
            }

            long min1304 = tempProductTotalDict[1503] * 4 + tempProductTotalDict[6005];
            if (tempProductTotalDict[1304] < min1304)
            {
                tempProductTotalDict[1304] = ProductRate(randomRate, min1304);
            }

            long min1126 = tempProductTotalDict[1304];
            if (tempProductTotalDict[1126] < min1126)
            {
                tempProductTotalDict[1126] = ProductRate(randomRate, min1126);
            }

            long min1125 = tempProductTotalDict[1503] * 6;
            if (tempProductTotalDict[1125] < min1125)
            {
                tempProductTotalDict[1125] = ProductRate(randomRate, min1125);
            }


            /*
             List<RecipeProto> recipeProtos = new List<RecipeProto>((IEnumerable<RecipeProto>) LDB.recipes.dataArray);
             foreach (var kv in tempProductTotalDict)
            {
                if (productTotalDict[kv.Key] < kv.Value)
                {
                    int itemId = kv.Key;
                    long total = kv.Value;
                    ProductStat[] productPool = factoryStatPool[0].productPool;
                    int productIndex = factoryStatPool[0].productIndices[itemId];
                    if (productPool[productIndex] == null)
                    {
                        Log.SaveToFile($"productPool[{productIndex}] == null");
                        productPool[productIndex] = new ProductStat();
                        productPool[productIndex].Init(itemId);
                    }

                    var difVal = total - productTotalDict[itemId];
                    productPool[productIndex].total[6] += difVal;
                }
            }
            if (!OriginalDataCheck(data, productTotalDict))
            {
                Log.SaveToFile("OriginalDataCheck() -> Return False, Upload Fake Data To Milky Way Server.");
                
            }*/

            productTotalDict = new Dictionary<int, long>(tempProductTotalDict);
            
            long seedKey = gameData.GetSeedKey();
            long userId = (long) gameData.account.userId;
            byte platform = (byte) gameData.account.platform;
            string userName = gameData.account.userName;
            string str1 = string.Format(
                "0x{15}{16}n6001x{0}n6002x{1}n6003x{2}n6004x{3}n6005x{4}n6006x{5}n1304x{6}n1404x{7}n1125x{8}n1126x{9}n1501x{10}n1503x{11}n11901x{12}n20001x{13:0}n99999x{14}",
                (object) productTotalDict[6001], (object) productTotalDict[6002], (object) productTotalDict[6003],
                (object) productTotalDict[6004], (object) productTotalDict[6005], (object) productTotalDict[6006],
                (object) productTotalDict[1304], (object) productTotalDict[1404], (object) productTotalDict[1125],
                (object) productTotalDict[1126], (object) productTotalDict[1501], (object) productTotalDict[1503],
                (object) productTotalDict[11901], (object) (float) ((double) miningCostRate * 1000000.0), (object) 0,
                (object) GameConfig.gameVersion.Build, (object) 0);
            byte num29 = DSPGame.globalOption.dataUploadToMilkyWay == EUploadDataToMilkyWay.Anonymous
                ? (byte) 1
                : (byte) 0;
            string str2 = MD5F.Compute(string.Format("{0}+{1}+{2}+{3}+{4}+{5}+{6}+{7}+{8}+{9}+{10}+{11}",
                (object) seedKey, (object) userId, (object) platform, (object) (long) data.generatingCapacity,
                (object) data.dysonSphereCount, (object) data.totalNodeOnLayer,
                (object) data.totalFrameOnLayer, (object) data.totalSailOnSwarm, (object) data.totalStructureOnLayer,
                (object) data.totalCellOnLayer, (object) str1, (object) milkyWayWebClient.loginKey));
            string url = string.Format(
                "{0}{1}?seed={2}&user_id={3}&platform={4}&user_name={5}&cluster_generation={6}&dyson_sphere_count={7}&dyson_node_count={8}&dyson_frame_count={9}&total_sail={10}&total_sp={11}&total_cp={12}&evidence={13}&is_anonymous={14}&login_key={15}&signature={16}",
                (object) MilkyWayWebClient.galaxyServerAddress, (object) MilkyWayWebClient.uploadApi,
                (object) seedKey, (object) userId, (object) platform, (object) Uri.EscapeDataString(userName),
                (object) (long) data.generatingCapacity, (object) data.dysonSphereCount, (object) data.totalNodeOnLayer,
                (object) data.totalFrameOnLayer, (object) data.totalSailOnSwarm, (object) data.totalStructureOnLayer,
                (object) data.totalCellOnLayer, (object) str1, (object) num29, (object) milkyWayWebClient.loginKey,
                (object) str2);
            // Log.SaveToFile(url);
            
            lastUploadTime = time;
            
            milkyWayWebClient.uploadRequest = HttpManager.GetByUrl(new HttpConnectParam()
            {
                url = url,
                downloadHandler = (DownloadHandler) new DownloadHandlerBuffer(),
                successDelegate = new HttpRequestSuccessDelegate(milkyWayWebClient.OnUploadSucceed),
                errorDelegate = new HttpRequestErrorDelegate(milkyWayWebClient.OnUploadErrored),
                maxTimeoutTime = 30
            });
        }

        public static void Pre(GameData gameData)
        {
            gameData.account.userId = AccountData.me.userId;
            gameData.account.platform = AccountData.me.platform;
            gameData.account.detail = AccountData.me.detail;
        }

        public static long ProductRate(double rate, long pro)
        {
            return (long) (rate * pro);
        }

        public static void OnUploadLoginSucceed(DownloadHandler handler)
        {
            if (!((UnityEngine.Object) milkyWayWebClient.loginRequest != (UnityEngine.Object) null))
                return;
            Debug.Log((object) ("Milky Way login (for upload): " + handler.text + " request time = " +
                                milkyWayWebClient.loginRequest.reqTime.ToString("0.000")));
            string[] strArray = handler.text.Split(new string[1] {","}, StringSplitOptions.RemoveEmptyEntries);
            if (strArray.Length >= 2)
            {
                int.TryParse(strArray[0], out milkyWayWebClient.loginKey);
                long.TryParse(strArray[1], out milkyWayWebClient.fullDataUrl);
            }

            if (milkyWayWebClient.loginKey <= 0 || milkyWayWebClient.fullDataUrl <= 0L)
                return;
            SendUploadRecordRequest(GameMain.data);
        }

        public static bool OriginalDataCheck(SaveUserData data, Dictionary<int, long> originalData)
        {
            var sailToDS = data.totalCellOnLayer + data.totalSailOnSwarm;
            long sailUsed = data.totalStructureOnLayer * 6 + sailToDS;
            if (originalData[11901] < sailToDS
                || originalData[1503] < data.totalStructureOnLayer
                || originalData[1501] < sailUsed
                || originalData[1404] < originalData[1501] / 2
                || originalData[1304] < originalData[6005] + originalData[1503] * 4
                || originalData[1126] < originalData[1304]
                || originalData[1125] < originalData[1503] * 6)
            {
                return false;
            }

            for (int i = 6001; i < 6006; i++)
            {
                if (originalData[i] < originalData[6006] || originalData[i] < 0x4ffff)
                {
                    return false;
                }
            }

            return true;
        }

        public static bool SendUploadLoginRequest()
        {
            if (GameMain.data == null || time - lastUploadTime <= 119.0)
                return false;
            if (AccountData.me.userId <= 0UL || AccountData.me.platform <= ESalePlatform.Standalone)
                return false;
            milkyWayWebClient.loginRequest = HttpManager.GetByUrl(new HttpConnectParam()
            {
                url = string.Format("{0}{1}?user_id={2}", (object) MilkyWayWebClient.galaxyServerAddress,
                    (object) MilkyWayWebClient.loginHeaderApi, (object) AccountData.me.userId),
                downloadHandler = (DownloadHandler) new DownloadHandlerBuffer(),
                successDelegate = new HttpRequestSuccessDelegate(OnUploadLoginSucceed),
                errorDelegate = new HttpRequestErrorDelegate(milkyWayWebClient.OnUploadLoginErrored),
                responseTimeoutTime = 30,
                maxTimeoutTime = 120
            });
            return true;
        }
    }
}