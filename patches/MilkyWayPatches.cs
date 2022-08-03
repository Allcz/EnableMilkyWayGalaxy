using DSPWeb;
using UnityEngine;
using UnityEngine.Networking;
using System;
using System.Collections.Generic;
using Random = System.Random;

namespace EnableMilkyWayGalaxy.patches
{
    public class MilkyWayPatches : MonoBehaviour
    {
        /**
         * 戴森球上传数据日志
         */
        private static Log _milkyWayLog = new Log(Log.MILKY_WAY_LOG);

        /**
         * debug日志
         */
        private static Log _debugLog = new Log(Log.DEBUG_LOG);

        private static MilkyWayWebClient milkyWayWebClient = DSPGame.milkyWayWebClient;

        private static Random random = new Random();

        /**
         * 记录游戏模式，是否是沙盒模式
         */
        private static bool _isSandboxMode = false;

        private static double time => (DateTime.UtcNow.ToOADate() - 44217.0) * 86400.0;

        private static double reportInterval => 300.0;

        /**
         * 通过反射获取、赋值 MilkyWayWebClient 属性 lastUploadTime
         */
        private static double lastUploadTime
        {
            get { return ReflectionUtil.GetPrivateField<double>(milkyWayWebClient, "lastUploadTime"); }
            set { ReflectionUtil.SetPrivateField(milkyWayWebClient, "lastUploadTime", value); }
        }

        private static double lastReportTime
        {
            get { return ReflectionUtil.GetPrivateField<double>(milkyWayWebClient, "lastReportTime"); }
            set { ReflectionUtil.SetPrivateField(milkyWayWebClient, "lastReportTime", value); }
        }

        private static bool canReportUx
        {
            get { return ReflectionUtil.GetPrivateField<bool>(milkyWayWebClient, "canReportUx"); }
            set { ReflectionUtil.SetPrivateField(milkyWayWebClient, "canReportUx", value); }
        }

        /**
         * 发送戴森球数据到银河系服务器
         */
        public static void SendUploadRecordRequest(GameData gameData)
        {
            var data = new SaveUserData();
            if (gameData == null)
            {
                return;
            }

            SendRequestPrefix();
            SetGameDataAccountToMe();
            foreach (DysonSphere dysonSphere in gameData.dysonSpheres)
            {
                if (null == dysonSphere) continue;
                data.generatingCapacity += (ulong) dysonSphere.energyGenCurrentTick;
                data.totalSailOnSwarm += (long) dysonSphere.swarm.sailCount;
                data.totalNodeOnLayer += dysonSphere.totalConstructedNodeCount;
                data.totalFrameOnLayer += dysonSphere.totalConstructedFrameCount;
                data.totalCellOnLayer += dysonSphere.totalConstructedCellPoint;
                data.totalStructureOnLayer += dysonSphere.totalConstructedStructurePoint;
                if (dysonSphere.energyGenCurrentTick > 0L)
                    ++data.dysonSphereCount;
            }

            if (data.generatingCapacity <= 0UL)
                return;
            lastUploadTime = time;
            FactoryProductionStat[] factoryStatPool = gameData.statistics.production.factoryStatPool;
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
                11901 // 生产的太阳帆总数
            };
            // 20001 // 采矿消耗率 x1000000
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
            var randomRate = 1.01 + 0.02 * random.NextDouble();
            long level = Math.Min(5 + (int) (data.generatingCapacity / 10000000000), 1000);
            float miningCostRate = Math.Min(gameData.history.miningCostRate, (float) Math.Pow(0.94, level));
            long upgradeCost = Math.Abs((2000 * level * (level - 10) + 50000) * 20 + 0xffff + random.Next(0xfff));
            tempProductTotalDict[6006] = ProductRate(randomRate, upgradeCost + tempProductTotalDict[6006]);
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

            productTotalDict = new Dictionary<int, long>(tempProductTotalDict);

            long clusterSeedKey = gameData.GetClusterSeedKey();
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
            // _debugLog.LOG(string.Format("evidence={0}", str1));
            byte num29 = DSPGame.globalOption.dataUploadToMilkyWay == EUploadDataToMilkyWay.Anonymous
                ? (byte) 1
                : (byte) 0;
            string str2 = MD5F.Compute(string.Format("{0}+{1}+{2}+{3}+{4}+{5}+{6}+{7}+{8}+{9}+{10}+{11}",
                (object) clusterSeedKey, (object) userId, (object) platform, (object) (long) data.generatingCapacity,
                (object) data.dysonSphereCount, (object) data.totalNodeOnLayer,
                (object) data.totalFrameOnLayer, (object) data.totalSailOnSwarm, (object) data.totalStructureOnLayer,
                (object) data.totalCellOnLayer, (object) str1, (object) milkyWayWebClient.loginKey));
            string url = string.Format(
                "{0}{1}?seed={2}&user_id={3}&platform={4}&user_name={5}&cluster_generation={6}&dyson_sphere_count={7}&dyson_node_count={8}&dyson_frame_count={9}&total_sail={10}&total_sp={11}&total_cp={12}&evidence={13}&is_anonymous={14}&login_key={15}&signature={16}",
                (object) MilkyWayWebClient.galaxyServerAddress, (object) MilkyWayWebClient.uploadApi,
                (object) clusterSeedKey, (object) userId, (object) platform, (object) Uri.EscapeDataString(userName),
                (object) (long) data.generatingCapacity, (object) data.dysonSphereCount, (object) data.totalNodeOnLayer,
                (object) data.totalFrameOnLayer, (object) data.totalSailOnSwarm, (object) data.totalStructureOnLayer,
                (object) data.totalCellOnLayer, (object) str1, (object) num29, (object) milkyWayWebClient.loginKey,
                (object) str2);
            SendRequestPostfix();
            milkyWayWebClient.uploadRequest = HttpManager.GetByUrl(new HttpConnectParam()
            {
                url = url,
                downloadHandler = (DownloadHandler) new DownloadHandlerBuffer(),
                successDelegate = new HttpRequestSuccessDelegate(milkyWayWebClient.OnUploadSucceed),
                errorDelegate = new HttpRequestErrorDelegate(milkyWayWebClient.OnUploadErrored),
                maxTimeoutTime = 30
            });
            String content = String.Format("send data to milky way: Get url={0}", url);
            _milkyWayLog.LOG(content);
        }

        /**
         * 存档账户信息与登录账户信息统一
         */
        public static void SetGameDataAccountToMe()
        {
            if (null != GameMain.data)
            {
                GameMain.data.account.userId = AccountData.me.userId;
                GameMain.data.account.platform = AccountData.me.platform;
                GameMain.data.account.detail = AccountData.me.detail;
            }
        }

        public static long ProductRate(double rate, long pro)
        {
            return (long) (rate * pro);
        }

        /**
         * 登录成功后执行该方法
         */
        public static void OnUploadLoginSucceed(DownloadHandler handler)
        {
            if (!((UnityEngine.Object) milkyWayWebClient.loginRequest != (UnityEngine.Object) null))
            {
                // _debugLog.LOG("loginRequest failed");
                return;
            }

            // _debugLog.LOG( ("Milky Way login (for upload): " + handler.text + " request time = " + milkyWayWebClient.loginRequest.reqTime.ToString("0.000")));
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

        /**
         * 上传银河系数据的入口方法
         */
        public static bool SendUploadLoginRequest()
        {
            if (GameMain.data == null || time - lastUploadTime <= 119.0)
                return false;
            lastUploadTime = time;
            if (AccountData.me.userId <= 0UL || AccountData.me.platform <= ESalePlatform.Standalone)
                return false;
            string url = string.Format("{0}{1}?user_id={2}", (object) MilkyWayWebClient.galaxyServerAddress,
                (object) MilkyWayWebClient.loginHeaderApi, (object) AccountData.me.userId);
            // _debugLog.LOG(string.Format("Send Milky Way login: Get {0}", url));
            milkyWayWebClient.loginRequest = HttpManager.GetByUrl(new HttpConnectParam()
            {
                url = url,
                downloadHandler = (DownloadHandler) new DownloadHandlerBuffer(),
                successDelegate = new HttpRequestSuccessDelegate(OnUploadLoginSucceed),
                errorDelegate = new HttpRequestErrorDelegate(milkyWayWebClient.OnUploadLoginErrored),
                responseTimeoutTime = 30,
                maxTimeoutTime = 120
            });
            return true;
        }

        public static void SendReportRequest()
        {
            lastReportTime = time;
            SendRequestPrefix();
            long num1 = (long) AccountData.me.userId;
            if (num1 == 0L)
                num1 = (long) GameMain.data.gameDesc.galaxySeed + 10000000000L;
            if (num1 == 0L)
                return;
            long userId = (long) AccountData.me.userId;
            int build = GameConfig.gameVersion.Build;
            long gameTick = 100 * 60 * 60 * 60 + GameMain.gameTick;
            double timeSinceStart = 11 * GlobalObject.timeSinceStart;
            int opCounter = 11 * GlobalObject.opCounter;
            double num2 = PerformanceMonitor.timeCostsShowing[1] * 1000.0;
            string str1 = "";
            int num3 = Math.Min(GameMain.multithreadSystem.usedThreadCnt, SystemInfo.processorCount);
            long dataLength1 = 11 * PerformanceMonitor.dataLengths[1];
            string str2 = "";
            int num4 = (int) (FPSController.averageFPS + 0.5);
            int num5 = (int) (FPSController.averageUPS + 0.5);
            for (int index = 2; index < 35; ++index)
            {
                double num6 = PerformanceMonitor.timeCostsShowing[index] * 1000.0;
                if (num6 > 0.0001)
                {
                    string str3 = ((ECpuWorkEntry) index).ToString() + "-" + num6.ToString("0.0000") + "|";
                    str1 += str3;
                }
            }

            long[] dataLengths = new long[32];
            Array.Copy(PerformanceMonitor.dataLengths, dataLengths, dataLengths.Length);

            for (int index = 2; index < 32; ++index)
            {
                long dataLength2 = 11 * dataLengths[index];
                if (dataLength2 > 0L)
                {
                    string str4 = ((ESaveDataEntry) index).ToString() + "-" + dataLength2.ToString("0") + "|";
                    str2 += str4;
                }
            }

            SendRequestPostfix();
            String url = string.Format(
                "{0}{1}?user_id={2}&owner_id={3}&version={4}&game_tick={5}&game_time={6:0.00}&game_exp={7}&cpu_time={8:0.0000}&cpu_detail={9}&thread_count={10}&data_len={11}&data_detail={12}&fps={13}&ups={14}&pwd=41917",
                (object) MilkyWayWebClient.galaxyServerAddress, (object) MilkyWayWebClient.uxReportApi,
                (object) num1, (object) userId, (object) build, (object) gameTick, (object) timeSinceStart,
                (object) opCounter, (object) num2, (object) str1, (object) num3, (object) dataLength1,
                (object) str2, (object) num4, (object) num5);
            _milkyWayLog.LOG(String.Format("send report to server: url={0}", url));
            HttpManager.GetByUrl(new HttpConnectParam()
            {
                url = url,
                downloadHandler = (DownloadHandler) new DownloadHandlerBuffer(),
                responseTimeoutTime = 60,
                maxTimeoutTime = 120
            });
        }

        public static void CheckReportPeriod()
        {
            // 暂时不执行上传游戏数据详情
            lastReportTime = time;
            if (time - lastReportTime <= reportInterval)
            {
                return;
            }

            try
            {
                if ((double) UIAutoSave.autoSaveTime < 110.0)
                {
                    SendReportRequest();
                }
                else
                {
                    float num1 = (float) (GameMain.gameTick - UIAutoSave.lastSaveTick) * 0.01666667f;
                    float num2 = UIAutoSave.autoSaveTime - num1;
                    if ((double) num1 <= 80.0 || (double) num2 <= 20.0)
                        return;
                    SendReportRequest();
                }
            }
            catch
            {
            }
        }


        /**
         * 拼接上传数据前，将游戏模式调整维正常模式,
         */
        public static void SendRequestPrefix()
        {
            if (null != GameMain.data && null != GameMain.data.gameDesc)
            {
                _isSandboxMode = GameMain.data.gameDesc.isSandboxMode;
                // 是否是正常模式
                GameMain.data.gameDesc.isPeaceMode = true;
                // 是否是沙盒模式
                GameMain.data.gameDesc.isSandboxMode = false;
                GameMain.sandboxToolsEnabled = false;
            }
        }

        /**
         * 拼接完上传的数据后，将游戏模式还原
         */
        public static void SendRequestPostfix()
        {
            if (null != GameMain.data && null != GameMain.data.gameDesc)
            {
                GameMain.data.gameDesc.isPeaceMode = !_isSandboxMode;
                GameMain.data.gameDesc.isSandboxMode = _isSandboxMode;
                GameMain.sandboxToolsEnabled = _isSandboxMode;
            }
        }
    }
}