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

        public static readonly List<PerformanceSampler> updateSamplers = new();
        public static readonly List<PerformanceSampler> longtimeSamplers = new();

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

        [Conditional("ENABLE_PERFORMANCE_SAMPLER")]
        public static void BeginLongtimeSample(string name)
        {
            int instanceId = 0;

            //如果存在相同名称的性能采样器，那么实例ID+1
            foreach (var item in longtimeSamplers)
            {
                if (item.samplerName == name)
                {
                    instanceId = item.instanceId + 1;
                }
            }

            //推入
            longtimeSamplers.Add(new(name, instanceId));
        }

        [Conditional("ENABLE_PERFORMANCE_SAMPLER")]
        public static void EndLongtimeSample(string name)
        {
            //倒序遍历
            for (int i = longtimeSamplers.Count - 1; i >= 0; i--)
            {
                var sampler = longtimeSamplers[i];

                //如果线程一致
                if (sampler.thread == Thread.CurrentThread && sampler.samplerName == name)
                {
                    sampler.stopwatch.Stop();
                    Debug.Log($"性能测试结果: {sampler.samplerName}-{sampler.instanceId} 耗时 {sampler.stopwatch.Elapsed.TotalMilliseconds}ms");
                    return;
                }
            }

            throw new NotImplementedException();
        }

        [Conditional("ENABLE_PERFORMANCE_SAMPLER")]
        public static void BeginSampleUpdate(string name)
        {
            int instanceId = 0;

            //如果存在相同名称的性能采样器，那么实例ID+1
            foreach (var item in updateSamplers)
            {
                if (item.samplerName == name)
                {
                    instanceId = item.instanceId + 1;
                }
            }

            //推入
            updateSamplers.Add(new(name, instanceId));
        }

        [Conditional("ENABLE_PERFORMANCE_SAMPLER")]
        public static void EndSampleUpdate(string name)
        {
            //倒序遍历
            for (int i = updateSamplers.Count - 1; i >= 0; i--)
            {
                var item = updateSamplers[i];

                //如果线程一致
                if (item.samplerName == name && item.stopwatch.IsRunning)
                {
                    item.stopwatch.Stop();
                    return;
                }
            }

            throw new NotImplementedException();
        }

        //TODO: 在 Entity.Update 中调用 ( #if ENABLE_PERFORMANCE_SAMPLER )
        public readonly struct PerformanceSampler
        {
            internal readonly string samplerName;
            internal readonly Thread thread;
            internal readonly Stopwatch stopwatch;
            internal readonly int instanceId;

            public PerformanceSampler(string samplerName, int instanceId)
            {
                this.samplerName = samplerName;
                this.instanceId = instanceId;

                thread = Thread.CurrentThread;
                stopwatch = Stopwatch.StartNew();
            }
        }
    }
}
