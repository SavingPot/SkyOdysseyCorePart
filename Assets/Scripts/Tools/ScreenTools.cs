using GameCore.Converters;
using SP.Tools.Unity;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace GameCore
{
    public static class ScreenTools
    {
        /// <summary>
        /// Vector2 是改变前的分辨率
        /// </summary>
        public static Action<Vector2> OnResolutionChanged = _ => { };

        public static void CaptureSquare(string path, Action callBack = null)
        {
            int w = Mathf.Min(Screen.width, Screen.height);
            int x, y;

            if (w == Screen.width && w == Screen.height)
            {

            }
            if (w == Screen.height)
            {
                x = w / 2;
                y = 0;
            }
            else
            {
                x = 0;
                y = w / 2;
            }

            Capture(new(x, y, w, w), path, callBack);
        }

        public static void Capture(Rect rect, string path, Action callback = null) => CoroutineStarter.Do(IECapture(rect, path, callback));

        /// <summary>
        /// 截取游戏屏幕内的像素
        /// </summary>
        /// <param name="rect">截取区域：屏幕左下角为0点</param>
        /// <param name="path">文件名</param>
        /// <param name="callback">截图完成回调</param>
        /// <returns></returns>
        public static IEnumerator IECapture(Rect rect, string path, Action callback = null)
        {
            yield return new WaitForEndOfFrame();//等到帧结束，不然会报错

            Texture2D tex = new((int)rect.width, (int)rect.height, TextureFormat.RGB24, false);//新建一个Texture2D对象
            tex.ReadPixels(rect, 0, 0);//读取像素，屏幕左下角为0点
            tex.Apply();//保存像素信息

            if (!File.Exists(path))
                File.Create(path).Dispose();
            File.WriteAllBytes(path, ByteConverter.ToBytes(tex)); //写入数据

            callback?.Invoke();
        }

        public static Vector2 middle => new(Screen.width / 2, Screen.height / 2);
    }
}
