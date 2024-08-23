using System;
using System.Collections.Generic;
using GameCore.Network;
using UnityEngine;

namespace GameCore
{
    public static class GWeather
    {
        public static List<WeatherData> weathers = new();
        public static WeatherData weather { get; internal set; }
        [Sync(nameof(TempWeather))] public static string _weatherId;
        public static string weatherId => _weatherId;

        internal static void TempWeather(byte[] _)
        {
            if (!GM.HasInstance())
                return;

            foreach (var value in weathers)
            {
                if (value.id == weatherId)
                {
                    Debug.Log($"天气切换为 {weatherId}");
                    weather?.OnExit?.Invoke();
                    weather = value;
                    weather.OnEnter?.Invoke();
                    return;
                }
            }

            Debug.LogError($"天气数据 {weatherId} 不存在");
        }








        public static void AddWeather(string weatherId, Action OnEnter, Action OnExit)
        {
            weathers.Add(new(weatherId, OnEnter, OnExit));
        }

        public static void SetWeather(string id)
        {
            if (!Server.isServer)
            {
                Debug.LogError($"只有服务器可以设置天气");
                return;
            }

            _weatherId = id;
        }






        public static void InitWeatherSystem()
        {
            var gm = GM.instance;
            weathers.Clear();
            weather = null;

            gm.SetGlobalVolumeBloomToSunny();
            gm.SetGlobalVolumeColorAdjustmentsToSunny();


            //晴朗
            AddWeather("ori:sunny", null, null);

            //酸雨
            AddWeather("ori:acid_rain", () =>
            {
                GAudio.Play(AudioID.Rain, null, true);

                //开始发射
                gm.weatherParticleMain.startColor = Color.green;
                gm.weatherParticleEmission.enabled = true;

                //设置模糊效果
                gm.SetGlobalVolumeBloomToRain();
                gm.SetGlobalVolumeColorAdjustmentsToAcidRain();
            }, () =>
            {
                gm.weatherParticleMain.startColor = Color.white;

                //禁用发射
                gm.weatherParticleEmission.enabled = false;

                //停止所有音效
                GAudio.Stop(AudioID.Rain);

                //设置模糊效果
                gm.SetGlobalVolumeBloomToSunny();
                gm.SetGlobalVolumeColorAdjustmentsToSunny();
            });

            //雨天
            AddWeather("ori:rain", () =>
            {
                GAudio.Play(AudioID.Rain, null, true);

                //开始发射
                gm.weatherParticleEmission.enabled = true;

                //设置模糊效果
                gm.SetGlobalVolumeBloomToRain();
                gm.SetGlobalVolumeColorAdjustmentsToRain();
            },
            () =>
            {
                //禁用发射
                gm.weatherParticleEmission.enabled = false;

                //停止所有音效
                GAudio.Stop(AudioID.Rain);

                //设置模糊效果
                gm.SetGlobalVolumeBloomToSunny();
                gm.SetGlobalVolumeColorAdjustmentsToSunny();
            });
        }
    }

    public class WeatherData
    {
        public string id;
        public Action OnEnter;
        public Action OnExit;

        public WeatherData(string id, Action OnEnter, Action OnExit)
        {
            this.id = id;
            this.OnEnter = OnEnter;
            this.OnExit = OnExit;
        }
    }
}