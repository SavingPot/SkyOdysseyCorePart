using SP.Tools;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using UnityEngine;
using UnityEngine.Rendering;
using Debug = UnityEngine.Debug;

namespace GameCore.High
{
    /// <summary>
    /// Performance 性能
    /// </summary>
    public static class Performance
    {
        #region 性能
        public static float frameTime;
        public static float smoothFrameTime;
        public static float fps;
        public static float cpuUsage;
        #endregion

        #region 设备
        public static string deviceModel => SystemInfo.deviceModel;
        public static string deviceName => SystemInfo.deviceName;
        public static DeviceType deviceType => SystemInfo.deviceType;
        public static string deviceId => SystemInfo.deviceUniqueIdentifier;
        #endregion

        #region 显卡
        public static int totalGPUMemory => SystemInfo.graphicsMemorySize;
        public static int totalGPUMemoryAsKiB => SystemInfo.graphicsMemorySize * 1024;
        public static float totalGPUMemoryAsGiB => (float)SystemInfo.graphicsMemorySize / 1024;
        public static int gpuId => SystemInfo.graphicsDeviceID;
        public static string gpuName => SystemInfo.graphicsDeviceName;
        public static GraphicsDeviceType gpuType => SystemInfo.graphicsDeviceType;
        public static string gpuVendor => SystemInfo.graphicsDeviceVendor;
        public static string gpuVersion => SystemInfo.graphicsDeviceVersion;
        #endregion

        #region 内存
        public static int totalMemoryAsKiB => SystemInfo.systemMemorySize * 1024;
        public static int totalMemory => SystemInfo.systemMemorySize;
        public static float totalMemoryAsGiB => (float)SystemInfo.systemMemorySize / 1024;
        #endregion

        #region CPU
        public static int cpuCoreCount => SystemInfo.processorCount;
        #endregion

        #region 操作系统
        public static string operatingSystem => SystemInfo.operatingSystem;
        #endregion

        #region 移动设备
        public static float batteryPower => SystemInfo.batteryLevel;
        public static int batteryPowerAsInt => Mathf.FloorToInt(SystemInfo.batteryLevel);
        public static BatteryStatus batteryStatus => SystemInfo.batteryStatus;
        #endregion

        #region 性能采样

        public static ConcurrentQueue<PerformanceSampler> updateSamplers = new();

        #endregion

        [ChineseName("输出配置信息")]
        public static void OutputComputerInfo()
        {
            StringBuilder sb = new("配置信息\n");

            sb.Append("显卡厂家: ").AppendLine(gpuVendor);
            sb.Append("显卡名称: ").AppendLine(gpuName);
            sb.Append("显卡ID: ").AppendLine(gpuId.ToString());
            sb.Append("显存: ").Append(totalGPUMemoryAsGiB).AppendLine("G");
            sb.Append("内存: ").Append(totalMemoryAsGiB).AppendLine("G");
            sb.Append("CPU核心: ").AppendLine(cpuCoreCount.ToString());
            sb.Append("设备ID: ").AppendLine(deviceId);
            sb.Append("设备模型: ").AppendLine(deviceModel);
            sb.Append("设备名: ").AppendLine(deviceName);
            sb.Append("设备类型: ").AppendLine(deviceType.ToString());
            sb.Append("操作系统: ").AppendLine(operatingSystem);

            Debug.Log(sb);
        }

        public static void CollectMemory()
        {
            Resources.UnloadUnusedAssets();
            GC.Collect();
        }

        public static double GetMemoryUsage()
        {
            return 0;
        }

        public static double GetMemoryRemain() => totalMemory - GetMemoryUsage();

        [Conditional("Debug")]
        public static void BeginSample(string name)
        {
            foreach (var item in updateSamplers)
            {
                if (item.samplerName == name)
                {
                    throw new NotImplementedException();
                }
            }

            updateSamplers.Enqueue(new(name));
        }

        [Conditional("Debug")]
        public static void EndSample(string name)
        {
            foreach (var item in updateSamplers)
            {
                if (item.thread == Thread.CurrentThread && item.samplerName == name)
                {
                    item.stopwatch.Stop();
                    Debug.Log($"性能测试结果: {item.samplerName} 耗时 {item.stopwatch.ElapsedMilliseconds}ms");
                    return;
                }
            }

            throw new NotImplementedException();
        }

        public class PerformanceSampler
        {
            internal readonly string samplerName;
            internal readonly Thread thread;
            internal readonly Stopwatch stopwatch;

            public PerformanceSampler(string samplerName)
            {
                this.samplerName = samplerName;

                thread = Thread.CurrentThread;
                stopwatch = Stopwatch.StartNew();
            }
        }
    }
}
