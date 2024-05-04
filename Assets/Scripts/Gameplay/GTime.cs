using GameCore.High;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameCore
{
    public static class GTime
    {
        public const string varPrefix = nameof(GameCore) + "." + nameof(GTime) + ".";
        public static List<EventForTime> EventsForTime = new();
        public static List<EventForStageSwitch> EventsForSwitchToAfternoon = new();
        public static List<EventForStageSwitch> EventsForSwitchToTomorrow = new()
        {
            new(true, () => {
                for (int i = 0; i < EventsForTime.Count; i++)
                {
                    var bind = EventsForTime[i];
                    bind.hasNotCalledToday = true;

                    EventsForTime[i] = bind;
                }
            })
        };

        #region 变量

        [Sync, SyncDefaultValue(true)] public static bool isMorning;
        [Sync, SyncDefaultValue(1440f)] public static float timeOneDay;
        [Sync(nameof(TimeModify)), SyncDefaultValue(420f)] public static float time;

        public static void TimeModify(byte[] _)
        {
            _time12Format = GetTime12ByStandard(time);
            _time24Format = GetTime24By12(time12Format);

            for (int i = EventsForTime.Count - 1; i >= 0; i--)
            {
                var bind = EventsForTime[i];

                if (bind.hasNotCalledToday && _time24Format >= bind.time)
                {
                    bind.action();
                    bind.hasNotCalledToday = false;

                    if (!bind.repeat)
                    {
                        EventsForTime.RemoveAt(i);
                    }
                    else
                    {
                        EventsForTime[i] = bind;
                    }
                }
            }
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

        public static void SetTimeStandardBy12(float time12) => time = GetTimeStandardBy12(time12);

        public static float GetTime24By12(float time12)
        {
            if (isMorning)
                return time12;

            //如果时间为 正午12 则不额外加上 12 例子 =>      12 -> 12, 1 -> 13, 6 -> 18
            return (((int)time12 == 12) ? 0 : 12) + time12;
        }

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

                for (int i = EventsForSwitchToAfternoon.Count - 1; i >= 0; i--)
                {
                    var bind = EventsForSwitchToAfternoon[i];

                    bind.action();

                    if (!bind.repeat)
                    {
                        EventsForSwitchToAfternoon.RemoveAt(i);
                    }
                }
            }
            //如果是下午并且时间超过了午夜, 就把时间系统调到早上
            if (!isMorning && temp < 0)
            {
                temp = 0;
                isMorning = true;

                for (int i = EventsForSwitchToTomorrow.Count - 1; i >= 0; i--)
                {
                    var bind = EventsForSwitchToTomorrow[i];

                    bind.action();

                    if (!bind.repeat)
                    {
                        EventsForSwitchToTomorrow.RemoveAt(i);
                    }
                }
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



        public static void BindTimeEvent(float time, bool repeat, Action action)
        {
            EventsForTime.Add(new(time, repeat, action));
        }

        public static void BindSwitchToAfternoonEvent(bool repeat, Action action)
        {
            EventsForSwitchToAfternoon.Add(new(repeat, action));
        }

        public static void BindSwitchToTomorrowEvent(bool repeat, Action action)
        {
            EventsForSwitchToTomorrow.Add(new(repeat, action));
        }



        public struct EventForTime
        {
            public float time;
            public bool repeat;
            public Action action;
            internal bool hasNotCalledToday;

            public EventForTime(float time, bool repeat, Action action)
            {
                this.time = time;
                this.repeat = repeat;
                this.action = action;
                hasNotCalledToday = true;
            }
        }

        public struct EventForStageSwitch
        {
            public bool repeat;
            public Action action;

            public EventForStageSwitch(bool repeat, Action action)
            {
                this.repeat = repeat;
                this.action = action;
            }
        }
    }
}
