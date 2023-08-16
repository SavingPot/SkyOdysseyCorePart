using GameCore.High;
using System;
using Unity.Burst;
using UnityEngine;

namespace GameCore
{
    public static class GTime
    {
        public const string varPrefix = nameof(GameCore) + "." + nameof(GTime) + ".";

        #region 时间系统
        #region 变量

        [SyncGetter] static bool isMorning_get() => default; [SyncSetter] static void isMorning_set(bool value) { }
        [Sync] public static bool isMorning { get => isMorning_get(); set => isMorning_set(value); }

        [SyncGetter] static float timeOneDay_get() => default; [SyncSetter] static void timeOneDay_set(float value) { }
        [Sync, SyncDefaultValue(1440f)] public static float timeOneDay { get => timeOneDay_get(); set => timeOneDay_set(value); }

        [SyncGetter] static float time_get() => default; [SyncSetter] static void time_set(float value) { }
        [Sync(nameof(TimeModify)), SyncDefaultValue(420f)] public static float time { get => time_get(); set => time_set(value); }

        public static void TimeModify()
        {
            _time12Format = GetTime12ByStandard(time);
            _time24Format = GetTime24By12(time12Format);
        }

        public static float timeSpeed = 1;
        #endregion

        #region 转换
        public static float darknessLevel => 1 / time * timeOneDay;

        private static float _time12Format;
        private static float _time24Format;
        public static float time12Format { get => _time12Format; set => SetTimeStandardBy12(value); }
        public static float time24Format { get => _time24Format; set => SetTime12By24(value); }

        public static void SetTime12(float value)
        {
            time12Format = value;
            Debug.Log($"设置了时间为 {(isMorning ? "上午" : "下午")} {time12Format}");
        }

        public static void SetTime24(float value)
        {
            time24Format = value;
            Debug.Log($"设置了时间为 {time24Format}");
        }

        [BurstCompile]
        public static float GetTime12ByStandard(float standardTime)
        {
            float value = standardTime / (timeOneDay / 2) * 12f;

            if (isMorning)
                return value;

            value = 12 - value;

            if (value >= 0 && value < 1)
                value += 12;

            return value;
        }

        [BurstCompile]
        public static float GetTimeStandardBy12(float time12)
        {
            float value;

            //是白天则直接计算 12时间 * 一天的时间的24形式
            if (isMorning)
                value = time12 * (timeOneDay / 24);
            else
                value = (timeOneDay / 2) - (time12 * (timeOneDay / 24));

            return value;
        }

        [BurstCompile] public static void SetTimeStandardBy12(float time12) => time = GetTimeStandardBy12(time12);

        [BurstCompile]
        public static float GetTime24By12(float time12)
        {
            if (isMorning)
                return time12;

            //如果时间为 正午12 则不额外加上 12 例子 =>      12 -> 12, 1 -> 13, 6 -> 18
            return (((int)time12 == 12) ? 0 : 12) + time12;
        }

        [BurstCompile]
        public static void SetTime12By24(float time24)
        {
            //小于 12 为上午, 大于或等于则为 下午 
            isMorning = time24 < 12;

            SetTimeStandardBy12(isMorning ? time24 : (time24 - 12));
        }
        #endregion

        #region 运算
        public static Action Compute = () =>
        {
            // time = 10;
            // return;
            var temp = time;

            temp += Performance.frameTime * (isMorning ? 1 : -1) * timeSpeed;

            //如果是上午并且时间超过了正午, 就把时间系统调到下午
            if (isMorning && temp >= timeOneDay / 2)
            {
                temp = timeOneDay / 2;
                isMorning = false;
            }
            //如果是下午并且时间超过了午夜, 就把时间系统调到早上
            if (!isMorning && temp < 0)
            {
                temp = 0;
                isMorning = true;
            }

            time = temp;
        };

        public static bool IsInTime(float time, float earliest, float latest)
        {
            // 10 ~ 20
            if (latest > earliest)
                return time >= earliest && time <= latest;
            //3 ~ 13
            else
                return time >= earliest || time <= latest;
        }
        #endregion
        #endregion
    }
}
