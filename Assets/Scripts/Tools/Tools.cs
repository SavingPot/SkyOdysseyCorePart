using DG.Tweening;
using DG.Tweening.Core;
using GameCore.Converters;
using GameCore.High;
using Newtonsoft.Json.Linq;
using Sirenix.OdinInspector;
using SP.Tools;
using SP.Tools.Unity;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Debug = UnityEngine.Debug;
using Random = UnityEngine.Random;

namespace GameCore
{
    /// <summary>
    /// 工具类, 提供很多额外功能
    /// </summary>
    [DisallowMultipleComponent, ChineseName("超级工具")]
    public class Tools : SingletonClass<Tools>
    {
        /// <summary>
        /// 每次调用 Time.deltaTime 时 Unity 都会计算, 所以使用 Performance.frameTime 比 Time.deltaTime 性能更高 (他们的输出结果一般一样)
        /// </summary>
        public static float time;
        public static float deltaTime;
        public static float smoothDeltaTime;
        public static float fps;
        private Camera _mainCamera;
        private CameraController _mainCameraController;

        public Camera mainCamera { get { if (!_mainCamera) _mainCamera = Camera.main; return _mainCamera; } }
        public CameraController mainCameraController
        {
            get
            {
                if (!_mainCameraController)
                    _mainCameraController = Camera.main.gameObject.GetOrAddComponent<CameraController>(); return _mainCameraController;
            }
        }
        public float viewLeftSideWorldPos;
        public float viewRightSideWorldPos;
        public float viewUpSideWorldPos;
        public float viewDownSideWorldPos;
        private static StreamWriter logStreamWriter;
        public const ushort defaultPort = 24442;

        public static string logsPath { get; internal set; }
        public static bool writeLogsToFile { get; set; } = true;

        #region Default or Unknown
        public const string requiredErrorMessage = "此变量必须被赋值否则会出问题!~";
        #endregion


#if UNITY_EDITOR
        [SerializeField, LabelText("模组"), ListDrawerSettings(HideAddButton = true)]
        private List<Mod> modsToDebug;

        [SerializeField, LabelText("最终文本"), ListDrawerSettings(HideAddButton = true)]
        private List<FinalLang> finalTextDataToDebug;

        [SerializeField, LabelText("游戏设置")]
        private GameSettings gameSettingsDatumToDebug;

        [SerializeField, LabelText("当前世界")]
        private World currentWorldToDebug;

        [SerializeField, LabelText("世界")]
        private World worldToDebug;
#endif


        public static readonly Dictionary<string, LogType> totalLogTexts = new();
        public static System.Random staticRandom = new();


        int screenWidthLastFrame;
        int screenHeightLastFrame;

        public static readonly StringBuilderPool stringBuilderPool = new();



        public static void TimeTest(Action action)
        {
            Stopwatch watch = new();
            watch.Start();

            action();

            watch.Stop();
            long usedTime = watch.ElapsedMilliseconds;

            Debug.Log(usedTime);
        }

        public static void TimeTest(Action action, long testTime)
        {
            TimeTest(() =>
            {
                for (long i = 0; i < testTime; i++)
                {
                    action();
                }
            });
        }

        public static void TimeTest(params Action[] actions)
        {
            foreach (Action action in actions)
            {
                TimeTest(action);
            }
        }

        public static void TimeTest(long testTime, params Action[] actions)
        {
            foreach (Action action in actions)
            {
                TimeTest(action, testTime);
            }
        }

        [AwakeChineseName]
        protected override void Awake()
        {
            base.Awake();

            //注册回调
            SceneManager.activeSceneChanged += SceneChanged;
            GControls.OnDownFullScreen += FullScreen;

#if DEBUG
            Debug.unityLogger.logEnabled = true;
#else
            Debug.unityLogger.logEnabled = false;
#endif
        }

        public static void KillTweensOf(object obj)
        {
            List<Tween> tweens = DOTween.PlayingTweens();

            if (tweens == null)
                return;

            for (int i = tweens.Count - 1; i >= 0; i--)
            {
                var tween = tweens[i];

                if (tween.target == obj)
                {
                    tween.Kill();
                }
            }
        }

        protected override void DestroyOrSave() => DontDestroyOnLoadSingleton();



        [UpdateChineseName]
        void Update()
        {
            /* ---------------------------------- 计算时间 ---------------------------------- */
            time = Time.time;
            deltaTime = Time.deltaTime;
            smoothDeltaTime = Time.smoothDeltaTime;
            fps = 1 / smoothDeltaTime;



            /* --------------------------- 把时间赋值到 Performance --------------------------- */
            Performance.frameTime = deltaTime;
            Performance.smoothFrameTime = smoothDeltaTime;
            Performance.fps = fps;



            /* --------------------------------- 更新活跃场景 --------------------------------- */
            GScene.active = SceneManager.GetActiveScene();
            GScene.name = GScene.active.name;



            /* ---------------------------------- 执行代理 ---------------------------------- */
            MethodAgent.updates();



            /* -------------------------------- 为调试做特殊处理 -------------------------------- */
#if UNITY_EDITOR
            //更新日志消息
            modsToDebug = ModFactory.mods;
            finalTextDataToDebug = ModFactory.finalTextData;
            gameSettingsDatumToDebug = GFiles.settings;
            currentWorldToDebug = GFiles.world;
            worldToDebug = GFiles.world;
#endif

            //Debug.ClearDeveloperConsole();



            /* ---------------------------------- 检测分辨率 --------------------------------- */
            if (screenWidthLastFrame != 0 && screenHeightLastFrame != 0)
            {
                if (screenWidthLastFrame != Screen.width || screenHeightLastFrame != Screen.height)
                {
                    ScreenTools.OnResolutionChanged(new(screenWidthLastFrame, screenHeightLastFrame));
                }
            }

            screenWidthLastFrame = Screen.width;
            screenHeightLastFrame = Screen.height;



            /* ------------------------------ 计算视窗点位置的世界位置 ------------------------------ */
            if (mainCamera)
            {
                viewLeftSideWorldPos = mainCamera.ViewportToWorldPoint(new(0f, 0.5f)).x;
                viewRightSideWorldPos = mainCamera.ViewportToWorldPoint(new(1f, 0.5f)).x;
                viewUpSideWorldPos = mainCamera.ViewportToWorldPoint(new(0.5f, 1f)).y;
                viewDownSideWorldPos = mainCamera.ViewportToWorldPoint(new(0.5f, 0f)).y;
            }
        }

        [ChineseName("当收到日志时")]
        internal void OnHandleLog(string logString, string stackTrace, LogType type)
        {
            if (!writeLogsToFile)
                return;

            switch (type)
            {
                case LogType.Warning:
                    if (stackTrace.IsNullOrWhiteSpace())
                        LogWarningToFile(logString + "\n" + Environment.StackTrace);
                    else
                        LogWarningToFile(logString + "\n" + stackTrace);
                    break;

                case LogType.Error:
                case LogType.Exception:
                    if (stackTrace.IsNullOrWhiteSpace())
                        LogErrorToFile(logString + "\n" + Environment.StackTrace);
                    else
                        LogErrorToFile(logString + "\n" + stackTrace);
                    break;

                default:
                    LogToFile(logString);
                    break;
            }
        }


        public Vector2 World2ToUI(Vector3 wpos, Component uiParent, Camera camera)
        {
            //初始化一个屏幕坐标
            Vector2 m_tempV2 = Vector2.zero;
            //使用场景相机将世界坐标转换为屏幕坐标
            Vector3 spos = camera.WorldToScreenPoint(wpos);
            m_tempV2.Set(spos.x, spos.y);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(uiParent.GetComponent<RectTransform>(),
                m_tempV2, camera, out Vector2 retPos);

            return retPos;
        }



        public static string GetFileConvertedDate() => DateTime.Today.ToShortDateString().Replace('/', '_');

        public static void LogTextToFile(object content, LogType type)
        {
            string str = content.ToString();

            if (!totalLogTexts.ContainsKey(str))
                totalLogTexts.Add(str, type);

            try
            {
                using StreamWriter writer = new(logsPath, true, Encoding.UTF8);
                writer.WriteLine(str);
            }
            catch
            {

            }
        }

        public static void LogToFile(object obj) => LogTextToFile($"[{DateTime.Now}]: {obj}", LogType.Log);

        public static void LogWarningToFile(object obj) => LogTextToFile($"[{DateTime.Now}]: `警告: {obj}`", LogType.Warning);

        public static void LogErrorToFile(object obj) => LogTextToFile($"[{DateTime.Now}]: ```错误: {obj}```", LogType.Error);


        #region 其他

        /// <summary>
        /// 检查物体是否有指定的 Component, 如果有就添加组件 com (where T : MonoBehaviour)
        /// </summary>
        /// <remarks>Check whether the object has the specified Component. If yes, add the component com to it. (where T : MonoBehaviour)</remarks>
        /// <typeparam name="T"></typeparam>
        /// <param name="com"></param>
        public static void AddComponentToObjectWithComponent<T>(Type com) where T : MonoBehaviour
        {
            T[] ts = FindObjectsOfType<T>();

            foreach (T t in ts)
            {
                t.gameObject.AddComponent(com);
            }
        }

        public static Vector2 screenCenter => new(Screen.width / 2, Screen.height / 2);



        public static void LogException(Exception ex, string pre) => Debug.LogError($"\n{pre}\n{ex.GetType().Name}\n{ex.Message}\n{ex.StackTrace}");

        public static void LogException(Exception ex) => Debug.LogError($"{ex.GetType().Name}\n{ex.Message}\n{ex.StackTrace}");

        /// <summary>
        /// 过滤特殊字符，保留中文，英文字母，数字，和-
        /// </summary>
        /// <param name="inputValue">输入字符串</param>
        /// <remarks>Filter special characters and retain Chinese, English letters, numbers, and "-"</remarks>
        /// <returns></returns>
        public static string FilterChar(string inputValue)
        {
            if (Regex.IsMatch(inputValue, "[A-Za-z0-9\u4e00-\u9fa5-]+"))
                return Regex.Match(inputValue, "[A-Za-z0-9\u4e00-\u9fa5-]+").Value;

            return string.Empty;
        }



        public static string computerName => Environment.MachineName;



        /// <summary>
        /// 随机返回 true 或 false.
        /// Returns true or false randomly.
        /// </summary>
        public static bool randomBool => Random.value >= 0.5f;

        public static int randomInt => Random.Range(int.MinValue, int.MaxValue);

        public static string randomGUID => Guid.NewGuid().ToString();

        /// <summary>
        /// 返回随机的颜色.
        /// Returns a random color.
        /// </summary>
        public static Color randomColor => new(Random.value, Random.value, Random.value);

        /// <summary>
        /// 全屏.
        /// FullScreen.
        /// </summary>
        public static void FullScreen()
        {
            Resolution[] resolutions = Screen.resolutions;//获取设置当前屏幕分辩率

            if (Screen.fullScreen)
                Screen.SetResolution(resolutions[^1].width / 2, resolutions[^1].height / 2, false);
            else
                SetResolution(resolutions[^1], true);//设置当前分辨率
        }

        /// <summary>
        /// 设置分辨率.
        /// </summary>
        /// <remarks>
        /// Set resolution.
        /// </remarks>
        /// <param name="resolution"></param>
        /// <param name="fullScreen"></param>
        public static void SetResolution(Vector2Int resolution, bool fullScreen) => Screen.SetResolution(resolution.x, resolution.y, fullScreen);
        public static void SetResolution(Resolution resolution, bool fullScreen) => Screen.SetResolution(resolution.width, resolution.height, fullScreen);

        /// <summary>
        /// 获取屏幕 (窗口) 分辨率.
        /// </summary>
        /// <remarks>
        /// Get screen (window) resolution.
        /// </remarks>
        /// < 不缓存是为了 适配多显示器
        public static Vector2Int resolution => new(Screen.width, Screen.height);

        /// <summary>
        /// 获取鼠标的位置.
        /// Get the position of the mouse.
        /// </summary>
        /// <returns></returns>
        public Vector2 GetMouseWorldPos() => mainCamera.ScreenToWorldPoint(GControls.mousePos);

        public static bool Prob100(float probability)
        {
            if (probability >= 100)
                return true;

            return probability >= Random.value * 100;
        }

        public static bool Prob100(float probability, System.Random random)
        {
            if (probability >= 100)
                return true;

            if (probability <= 0)
                return false;

            return probability >= random.NextDouble() * 100;
        }

        public static int MultiProb100(float probability)
        {
            float temp = probability;
            int times = 0;

            //对于 >=100 直接 ++ 并将概率减去 100
            while (temp >= 100)
            {
                times++;
                temp -= 100;
            }

            //运算 <100 后的结果
            times += Prob100(temp) ? 1 : 0;

            return times;
        }

        public static int MultiProb100(float probability, System.Random random)
        {
            float temp = probability;
            int times = 0;

            //对于 >=100 直接 ++ 并将概率减去 100
            while (temp >= 100)
            {
                times++;
                temp -= 100;
            }

            //运算 <100 后的结果
            times += Prob100(temp, random) ? 1 : 0;

            return times;
        }

        /// <summary>
        /// 检测物体是否在屏幕内.
        /// Check if is the point object in the screen.
        /// </summary>
        /// <param name="pos"></param>
        /// <returns></returns>
        public bool IsInView(Vector3 pos, float errorValue = 0)
        {
            Vector2 viewPos = mainCamera.WorldToViewportPoint(pos);
            Vector3 dir = (pos - mainCamera.transform.position).normalized;
            float dot = Vector3.Dot(mainCamera.transform.forward, dir);     //判断物体是否在相机前面 (dot > 0)

            return dot > 0 && ((viewPos.x + errorValue) >= 0) && ((viewPos.x - errorValue) <= 1) && ((viewPos.y + errorValue) >= 0) && ((viewPos.y - errorValue) <= 1);
        }

        /// <summary>
        /// 检测物体是否在屏幕内.
        /// Check if is the point object in the screen.
        /// </summary>
        /// <param name="pos"></param>
        /// <returns></returns>
        public bool IsInView2D(Vector2 pos)
        {
            Vector2 viewPos = mainCamera.WorldToViewportPoint(pos);

            return viewPos.x >= 0 && viewPos.x <= 1 && viewPos.y >= 0 && viewPos.y <= 1;
        }

        /// <summary>
        /// 检测物体是否在屏幕内.
        /// Check if is the point object in the screen.
        /// </summary>
        /// <param name="pos"></param>
        /// <returns></returns>
        public bool IsInView2D(Vector2 pos, float errorValue)
        {
            Vector2 viewPos = mainCamera.WorldToViewportPoint(pos);

            return ((viewPos.x + errorValue) >= 0) && ((viewPos.x - errorValue) <= 1) && ((viewPos.y + errorValue) >= 0) && ((viewPos.y - errorValue) <= 1);
        }

        /// <summary>
        /// 通过颜色获取十六进制颜色码.
        /// Get hex color code from a color.
        /// </summary>
        /// <param name="hexColorCode"></param>
        /// <returns></returns>
        public static Color HexToColor(string hexColorCode) => ColorUtility.TryParseHtmlString(hexColorCode, out Color newColor) ? newColor : Color.white;

        /// <summary>
        /// 通过十六进制颜色码 (例如 #xxxxxx) 获取颜色.
        /// Get color from a hex color code (like #xxxxxx) .
        /// </summary>
        /// <param name="color"></param>
        /// <returns></returns>
        public static string ColorToHex(Color color, bool withSharp = true) => (withSharp ? "#" : string.Empty) + ColorUtility.ToHtmlStringRGB(color);



        //TODO: Angle Tools
        public static float IncludedAngleBetweenX(Vector2 vec) => Mathf.Atan2(vec.y, vec.x) * Mathf.Rad2Deg;


        /// <summary>
        /// 获取物体到另一个物体的角度 (float).
        /// Get the angle of an object to another object (float).
        /// </summary>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <returns></returns>
        public static float GetAngleFloat(Vector2 from, Vector2 to) => Vector2.Angle(GetAngleVector2(from, to), Vector2.up);

        /// <summary>
        /// 获取物体到另一个物体的角度 (欧拉).
        /// Get the angle of an object to another object (euler).
        /// </summary>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <returns></returns>
        public static Vector3 GetAngleEuler(Vector2 from, Vector2 to) => new(0, 0, GetAngleFloat(from, to));

        /// <summary>
        /// 获取物体到另一个物体的角度 [Vector (可用于方向即 Direction)].
        /// Get the angle of an object to another object [Vector (direction)].
        /// </summary>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <returns></returns>
        public static Vector2 GetAngleVector2(Vector2 from, Vector2 to) => to - from;

        /// <summary>
        /// 返回一个 (从开始端口到结束端口中) 未被占用端口.
        /// return an unoccupiedPort (from beginPort to endPort).
        /// </summary>
        /// <param name="beginPort"></param>
        /// <param name="endPort"></param>
        /// <returns></returns>
        public static ushort GetUnoccupiedPort(ushort beginPort = defaultPort, ushort endPort = ushort.MaxValue)
        {
            if (GInit.platform == RuntimePlatform.Android)
            {
                return beginPort;
            }
            else
            {
                NetworkTools.GetUnoccupiedPort(beginPort, out ushort port, endPort);

                return port;
            }
        }

        /// <summary>
        /// 如果指定端口被占用, 返回 true, 否则返回 false.
        /// If the point port is occupied, return true, or return false.
        /// </summary>
        /// <param name="port"></param>
        /// <returns></returns>
        public static bool IsPortOccupied(ushort port)
        {
            if (GInit.platform == RuntimePlatform.Android)
            {
                return false;
            }
            else
            {
                return NetworkTools.IsPortOccupied(port);
            }
        }
        #endregion



        #region 文件-IO数据流
        public static Type[] GetTypesFromDll(string dllPath) => Assembly.LoadFrom(dllPath.ToCSharpPath()).GetExportedTypes();


        /// <summary>
        /// 使用 IO 流加载图片，并将图片转换成 Sprite 类型返回
        /// </summary>
        /// <param name="path">图片目录</param>
        /// <returns></returns>
        public static Sprite LoadSpriteByPath(string path, FilterMode filterMode = FilterMode.Point, int spritePerUnit = 0) => ByteConverter.ToSprite(IOTools.LoadBytes(path), filterMode, spritePerUnit);
        #endregion



        #region 游戏对象-组件
        public static Component GetComponentByFullName(Component c, string fullName)
        {
            Component[] coms = c.GetComponents<Component>();

            for (int i = 0; i < coms.Length; i++)
                if (coms[i].GetType().FullName == fullName)
                    return coms[i];

            return null;
        }

        public static Component[] GetComponentsByFullName(Component c, string fullName)
        {
            Component[] coms = c.GetComponents<Component>();
            List<Component> ts = new();

            for (int i = 0; i < coms.Length; i++)
                if (coms[i].GetType().FullName == fullName)
                    ts.Add(coms[i]);

            return ts.ToArray();
        }
        #endregion



        #region 场景
        //当场景加载出来调用触发的事件
        void SceneChanged(Scene oldScene, Scene newScene)
        {
            GScene.AfterChanged(newScene);
        }

        public static T NewObjectToComponent<T>() where T : Component => NewObjectToComponent(typeof(T)) as T;

        public static T NewObjectToComponent<T>(string debugMessage) where T : Component => NewObjectToComponent(typeof(T), debugMessage) as T;

        public static Component NewObjectToComponent(Type type, string debugMessage = null)
        {
            GameObject go = new(type.Name);
            Component com = go.AddComponent(type);

            if (!string.IsNullOrEmpty(debugMessage))
                Debug.Log(debugMessage);
            //Debug.Log($"已为 {type.Name} ({type.Assembly.Location}) 新建物体");


            return com;
        }

        public static TweenerCore<float, float, DG.Tweening.Plugins.Options.FloatOptions> InvokeAfter(float time, UnityAction action)
        {
            float timed = 0;
            return DOTween.To(() => timed, a => timed = a, 1, time).OnComplete(() => action());
        }

        public static TweenerCore<float, float, DG.Tweening.Plugins.Options.FloatOptions> InvokeCycle(float time, UnityAction action)
        {
            float timed = 0;
            return DOTween.To(() => timed, a => timed = a, 1, time).OnStepComplete(() => action()).SetLoops(-1);
        }





        static readonly Regex HighlightedStackTraceRegex = new(@"at\s(.*)\s(\(.*\))\s\[\w+\]\sin\s(.*:\d+)");

        public static string HighlightedStackTrace()
        {
            return HighlightedStackTrace(Environment.StackTrace);
        }

        public static string HighlightedStackTrace(Exception exception)
        {
            return HighlightedStackTrace(exception.ToString());
        }

        public static string HighlightedStackTrace(string stackTrace)
        {
            string replacement = @"at <color=#e5c072>$1</color>$2 <color=#4988ff>[$3]</color>";

            return HighlightedStackTraceRegex.Replace(stackTrace, replacement);
        }





        /// <summary>
        /// 显示鼠标.
        /// </summary>
        /// <remarks>
        /// Show the cursor.
        /// </remarks>
        public static void ShowCursor()
        {
            Cursor.visible = true;

            //不限制鼠标指针
            Cursor.lockState = CursorLockMode.None;
        }

        /// <summary>
        /// 隐藏鼠标.
        /// </summary>
        /// <remarks>
        /// Hide the cursor.
        /// </remarks>
        public static void HideCursor()
        {
            Cursor.visible = false;

            //将鼠标指针限制在游戏窗口中
            Cursor.lockState = CursorLockMode.Confined;
        }

        #region LayerMask

        public static int LayerMaskExcept(int num) => ~(1 << num);

        public static int LayerMaskExcept(int num1, int num2) => ~(1 << num1 | 1 << num2);

        public static int LayerMaskOnly(int num) => 1 << num;

        public static int LayerMaskOnly(int num1, int num2) => 1 << num1 | 1 << num2;

        #endregion


        /// <summary>
        /// 获取类实现的接口
        /// </summary>
        /// <remarks>
        /// Get the interface implemented by the class
        /// </remarks>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static Type[] GetInterfaces<T>() => GetInterfaces(typeof(T));

        /// <summary>
        /// 获取类实现的接口
        /// </summary>
        /// <remarks>
        /// Get the interface implemented by the class
        /// </remarks>
        /// <returns></returns>
        public static Type[] GetInterfaces(Type type) => type.GetInterfaces();

        /// <summary>
        /// 游戏主体的程序集
        /// </summary>
        public static Assembly coreAssembly = Assembly.GetExecutingAssembly();

        /// <summary>
        /// 当前方法的程序集
        /// </summary>
        public static Assembly executingAssembly => Assembly.GetExecutingAssembly();
        #endregion
    }

    /// <summary>
    /// 预制的场景名
    /// </summary>
    /// <remarks>
    /// Prefabricated scene names
    /// </remarks>
    public struct SceneNames
    {
        public const string PreloadScene = "PreloadScene";
        public const string MainMenu = "MainMenu";
        public const string GameScene = "GameScene";
    }





    public static class UICamSize
    {
        private static readonly Vector2Int size1 = new(720, 480);
        private static readonly Vector2Int size2 = new(1024, 576);
        private static readonly Vector2Int size3 = new(1080, 720);
        private static readonly Vector2Int size4 = new(1920, 1080);
        private static readonly Vector2Int size5 = new(2560, 1440);

        public static Vector2Int large => size1;
        public static Vector2Int big => size2;
        public static Vector2Int medium => size3;
        public static Vector2Int small => size4;
        public static Vector2Int mini => size5;
    }

    public struct JobRandom
    {
        public static Unity.Mathematics.Random instance = Unity.Mathematics.Random.CreateFromIndex(1);

        public static bool Prob100(float probability)
        {
            if (probability >= 100)
                return true;

            if (probability <= 0)
                return false;

            return probability >= instance.NextFloat(0, 100);
        }

        public static int MultiProb100(float probability)
        {
            float temp = probability;
            int times = 0;

            //对于 >=100 直接 ++ 并将概率减去 100
            while (temp >= 100)
            {
                times++;
                temp -= 100;
            }

            //运算 <100 后的结果
            times += Prob100(temp) ? 1 : 0;

            return times;
        }
    }
}
