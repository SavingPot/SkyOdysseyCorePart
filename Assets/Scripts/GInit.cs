using GameCore.High;
using GameCore.Network;
using GameCore.UI;
using Sirenix.OdinInspector;
using SP.Tools;
using SP.Tools.Unity;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Rendering.Universal;
using Debug = UnityEngine.Debug;

namespace GameCore
{
    public class GInit : SingletonClass<GInit>
    {
        [Header("音频")]
        [LabelText("全局混音器"), Required(Tools.requiredErrorMessage)] public AudioMixerGroup globalAudioMixer;
        [LabelText("音乐混音器"), Required(Tools.requiredErrorMessage)] public AudioMixerGroup musicAudioMixer;
        [LabelText("默认混音器"), Required(Tools.requiredErrorMessage)] public AudioMixerGroup defaultAudioMixer;
        [LabelText("UI混音器"), Required(Tools.requiredErrorMessage)] public AudioMixerGroup uiAudioMixer;
        [LabelText("环境混音器"), Required(Tools.requiredErrorMessage)] public AudioMixerGroup ambientAudioMixer;

        [Header("UI")]
        [AssetsOnly, LabelText("画布预制体"), Required(Tools.requiredErrorMessage)] public Canvas canvasPrefab;
        [AssetsOnly, LabelText("按钮预制体"), Required(Tools.requiredErrorMessage)] public ButtonIdentity buttonPrefab;
        [AssetsOnly, LabelText("图文预制体"), Required(Tools.requiredErrorMessage)] public TextImageIdentity textImagePrefab;
        [AssetsOnly, LabelText("面板预制体"), Required(Tools.requiredErrorMessage)] public PanelIdentity panelPrefab;
        [AssetsOnly, LabelText("输入框预制体"), Required(Tools.requiredErrorMessage)] public InputFieldIdentity inputFieldPrefab;
        [AssetsOnly, LabelText("图片预制体"), Required(Tools.requiredErrorMessage)] public ImageIdentity imagePrefab;
        [AssetsOnly, LabelText("原始图片预制体"), Required(Tools.requiredErrorMessage)] public RawImageIdentity rawImagePrefab;
        [AssetsOnly, LabelText("文本预制体"), Required(Tools.requiredErrorMessage)] public TextIdentity textPrefab;
        [AssetsOnly, LabelText("开关预制体"), Required(Tools.requiredErrorMessage)] public ToggleIdentity togglePrefab;
        [AssetsOnly, LabelText("滚动视图预制体"), Required(Tools.requiredErrorMessage)] public ScrollViewIdentity scrollViewPrefab;
        [AssetsOnly, LabelText("图文按钮预制体"), Required(Tools.requiredErrorMessage)] public ImageTextButtonIdentity imageTextButtonPrefab;
        [AssetsOnly, LabelText("滑动条预制体"), Required(Tools.requiredErrorMessage)] public SliderIdentity sliderPrefab;
        [AssetsOnly, LabelText("输入按钮预制体"), Required(Tools.requiredErrorMessage)] public InputButtonIdentity inputButtonPrefab;



        [SerializeField, Required(Tools.requiredErrorMessage), AssetsOnly, LabelText("群系守护者粒子")] public ParticleSystem BiomeGuardParticleSystemPrefab;
        [SerializeField, Required(Tools.requiredErrorMessage), AssetsOnly, LabelText("血粒子")] public ParticleSystem BloodParticleSystemPrefab;
        [SerializeField, Required(Tools.requiredErrorMessage), AssetsOnly, LabelText("伤害数字")] public TMP_Text DamageTextPrefab;



        [Header("管理器")]
        [SerializeField, Required(Tools.requiredErrorMessage), AssetsOnly, LabelText("音频管理器")] public GameObject managerAudioPrefab;
        [SerializeField, Required(Tools.requiredErrorMessage), AssetsOnly, LabelText("网络管理器")] public GameObject managerNetworkPrefab;
        [SerializeField, Required(Tools.requiredErrorMessage), AssetsOnly, LabelText("工具")] public GameObject toolsPrefab;



        [Header("资源")]
        [AssetsOnly, Required(Tools.requiredErrorMessage), LabelText("默认材质")] public Material defaultMat;
        [AssetsOnly, Required(Tools.requiredErrorMessage), LabelText("光照材质")] public Material spriteLitMat;
        public Material spriteDefaultMat => new(Shader.Find("Sprites/Default"));



        [SerializeField, Required(Tools.requiredErrorMessage), AssetsOnly, LabelText("未知贴图")] public Sprite spriteUnknown;

        public TextureData textureUnknown;
        public BlockData blockUnknown;






        [Header("实体")]
        [SerializeField]
        [Required(Tools.requiredErrorMessage)]
        [AssetsOnly]
        [LabelText("实体的预制体")]
        private EntityInit entityPrefab;

        [SerializeField]
        [Required(Tools.requiredErrorMessage)]
        [AssetsOnly]
        [LabelText("掉落物的预制体")]
        private EntityInit dropPrefab;

        [SerializeField]
        [Required(Tools.requiredErrorMessage)]
        [AssetsOnly]
        [LabelText("敌人的预制体")]
        private EntityInit enemyPrefab;

        [SerializeField]
        [Required(Tools.requiredErrorMessage)]
        [AssetsOnly]
        [LabelText("NPC 的预制体")]
        private EntityInit npcPrefab;

        [SerializeField]
        [Required(Tools.requiredErrorMessage)]
        [AssetsOnly]
        [LabelText("生物的预制体")]
        private EntityInit creaturePrefab;

        [SerializeField]
        [Required(Tools.requiredErrorMessage)]
        [AssetsOnly]
        [LabelText("方块的预制体")]
        private GameObject blockPrefab;

        [SerializeField]
        [Required(Tools.requiredErrorMessage)]
        [AssetsOnly]
        [LabelText("方块光照的预制体")]
        private Light2D blockLightPrefab;

        [SerializeField]
        [Required(Tools.requiredErrorMessage)]
        [AssetsOnly]
        [LabelText("光照的预制体")]
        private Light2D lightPrefab;

        public EntityInit GetEntityPrefab() => entityPrefab;
        public EntityInit GetDropPrefab() => dropPrefab;
        public EntityInit GetEnemyPrefab() => enemyPrefab;
        public EntityInit GetNPCPrefab() => npcPrefab;
        public EntityInit GetCreaturePrefab() => creaturePrefab;
        public GameObject GetBlockPrefab() => blockPrefab;
        public Light2D GetBlockLightPrefab() => blockLightPrefab;
        public Light2D GetLightPrefab() => lightPrefab;






        #region 缓存/目录
        public static string dataPath { get; private set; }
        public static string directoryLogsPath { get; private set; }
        public static string currentLogsFilePath { get; private set; }
        public static string soleAssetsPath { get; private set; }
        public static string modsPath { get; private set; }

        public static string playerSkinPath { get; private set; }

        public static string savesPath { get; private set; }
        public static string settingsPath { get; private set; }
        public static string worldPath { get; private set; }
        public static string cachePath { get; private set; }
        public static string gameVersion { get; private set; }
        public static string unityVersion { get; private set; }
        public static RuntimePlatform platform { get; private set; }
        public static string gameName;
        public static string coreMainDllPath;
        public static string[] coreDlls { get; private set; }
        #endregion



        public static readonly List<(string, LogType)> totalLogTexts = new();

        public static bool writeLogsToFile { get; set; } = true;







        public static Action BeforeQuitting = () =>
        {
            GFiles.SaveAllDataToFiles();
            Debug.Log("退出了游戏\n\n\n\n\n");
        };






        protected override void DestroyOrSave() => DontDestroyOnLoadSingleton();

        protected override void Awake()
        {
            /* --------------------------------- 缓存应用信息 --------------------------------- */
            gameVersion = Application.version;
            unityVersion = Application.unityVersion;
            platform = Application.platform;
            gameName = Application.productName;



            /* --------------------------------- 调整基础设置 --------------------------------- */
            Application.runInBackground = true; //允许 Windows 平台在窗口不被选择时持续运行
            QualitySettings.vSyncCount = 0; //关闭垂直同步
            Physics2D.queriesStartInColliders = false; //使射线不返回发射器本身
            Loom.Initialize();

            if (platform == RuntimePlatform.Android)
                Application.targetFrameRate = (int)Math.Ceiling(Screen.currentResolution.refreshRateRatio.value);



            /* --------------------------------- 加载默认资源 --------------------------------- */
            textureUnknown = new("ori:unknown", instance.spriteUnknown);
            blockUnknown = new("ori:unknown_block", BlockData.defaultHardness, textureUnknown.id, true);



            /* ---------------------------------- 获取目录 ---------------------------------- */
            dataPath = Application.persistentDataPath;
            directoryLogsPath = Path.Combine(dataPath, "logs");
            currentLogsFilePath = Path.Combine(directoryLogsPath, $"{Tools.GetFileConvertedDate()}_{gameVersion}.md");
            cachePath = Application.temporaryCachePath;
            modsPath = Path.Combine(dataPath, "mods");
            IOTools.CreateDirectoryIfNone(modsPath);
            playerSkinPath = Path.Combine(dataPath, "player_skins");
            IOTools.CreateDirectoryIfNone(playerSkinPath);
            savesPath = Path.Combine(dataPath, "saves");
            settingsPath = Path.Combine(savesPath, "settings_data.json");
            worldPath = Path.Combine(savesPath, "worlds");



            /* ---------------------------------- 管理日志 ---------------------------------- */
#if DEBUG || ENABLE_LOG_FOR_RELEASE
            Debug.unityLogger.logEnabled = true;
#else
            Debug.unityLogger.logEnabled = false;
#endif

            IOTools.CreateDirectoryIfNone(directoryLogsPath); //创建日志目录

            //创建日志文件
            if (File.Exists(currentLogsFilePath))
                File.Delete(currentLogsFilePath); //删除原有日志
            File.Create(currentLogsFilePath);

            //托管日志输出
            Application.logMessageReceivedThreaded += OnHandleLog; //包括主线程与所有子进程



            /* -------------------------------- 初始化 Tools ------------------------------- */
            Instantiate(toolsPrefab);



            /* ---------------------------------- 调整缓存 ---------------------------------- */
            //删除已有的缓存文件
            if (Directory.Exists(cachePath))
                IOTools.DeleteDir(cachePath);

            //创建缓存目录
            Directory.CreateDirectory(cachePath);

            switch (platform)
            {
                case RuntimePlatform.Android:
                    //定义压缩包路径与解压路径
                    string zipFilePath = Path.Combine(cachePath, "game_apk.zip");
                    string unzipPath = Path.Combine(cachePath, "game_apk_unzipped");

                    //将APK文件复制到缓存路径并解压   (Application.dataPath 就是 APK 目录)
                    File.Copy(Application.dataPath, zipFilePath, true);
                    ZipTools.UnzipFile(zipFilePath, unzipPath);

                    //APK根/assets/sole_assets
                    soleAssetsPath = Path.Combine(unzipPath, "assets", "sole_assets");
                    break;

                case RuntimePlatform.WindowsPlayer:
                    // SkyOdyssey_Data/StreamingAssets/sole_assets
                    soleAssetsPath = Path.Combine(Application.dataPath, "StreamingAssets", "sole_assets");
                    break;

                case RuntimePlatform.WindowsEditor:
                    soleAssetsPath = "D:/MakeGames/GameProject/ori_copy_for_editor/sole_assets";
                    break;
            }



            /* --------------------------------- dlls 处理 --------------------------------- */
            coreMainDllPath = platform switch
            {
                //Android: assets/bin/Data/Managed
                RuntimePlatform.Android => Path.Combine(IOTools.GetParentPath(soleAssetsPath), "bin", "Data", "Managed"),

                //Windows Editor: 版本目录/最新版本/XXX_Data/Managed   Windows Runtime: XXX_Data/Managed
                _ => Application.isEditor ? Path.Combine(GameTools.GetHighestVersion(Path.Combine(IOTools.GetParentPath(Application.dataPath), "GameLauncher", "Windows")), $"{gameName}_Data", "Managed") : Path.Combine(Application.dataPath, "Managed")
            };
            string[] gameDlls = IOTools.GetFilesInFolder(coreMainDllPath, true, "dll");
            coreDlls = new string[gameDlls.Length + 1];

            for (int i = 0; i < gameDlls.Length; i++)
            {
                coreDlls[i] = gameDlls[i];
            }

            coreDlls[^1] = Path.Combine(soleAssetsPath, "mods", "ori", "scripts", "ori.dll");



            /* --------------------------------- 初始化管理器 --------------------------------- */

            //ManagerAudio 是很多管理器需要的, 因此需要先实例化, 很多管理器也依赖于别的管理器, 要注意先后
            Instantiate(instance.managerAudioPrefab);
            Instantiate(instance.managerNetworkPrefab);



            /* ---------------------------------- 安装模组 ---------------------------------- */
            for (int i = 1; i < MethodAgent.launchArgs.Length; i++)
            {
                if (File.Exists(MethodAgent.launchArgs[i]))
                {
                    string path = Path.Combine(modsPath, IOTools.GetFileName(MethodAgent.launchArgs[i], false));

                    try
                    {
                        ZipTools.UnzipFile(MethodAgent.launchArgs[i], path);
                    }
                    catch (Exception ex)
                    {
                        if (Directory.Exists(path))
                            IOTools.DeleteDir(path);

                        Debug.LogException(ex);
                    }
                }
            }



            /* --------------------------------- 加载设置和模组 -------------------------------- */
            GFiles.LoadGame();

            MethodAgent.RunThread(() => ModFactory.ReloadMods(() =>
            {
                Rpc.Init();
            }));

            base.Awake();
        }






        internal void OnHandleLog(string logString, string _, LogType type)
        {
            if (!writeLogsToFile)
                return;

            switch (type)
            {
                case LogType.Warning:
                    LogWarningToFile(logString);
                    break;

                case LogType.Error:
                case LogType.Exception:
                    LogErrorToFile(logString + "\n" + Tools.HighlightedStackTraceForMarkdown());
                    break;

                default:
                    LogToFile(logString);
                    break;
            }
        }




        public static void LogTextToFile(object content, LogType type)
        {
            string str = content.ToString();

            totalLogTexts.Add(new(str, type));

            try
            {
                using StreamWriter writer = new(currentLogsFilePath, true, Encoding.UTF8);
                writer.WriteLine($"***\n{str}");
            }
            catch
            {

            }
        }

        public static void LogToFile(object obj) => LogTextToFile($"{Tools.TimeInDayWithSquareBrackets()}: {obj}", LogType.Log);

        public static void LogWarningToFile(object obj) => LogTextToFile($"{Tools.TimeInDayWithSquareBrackets()}: 警告: {obj}", LogType.Warning);

        public static void LogErrorToFile(object obj) => LogTextToFile($"{Tools.TimeInDayWithSquareBrackets()}: 错误: {obj}", LogType.Error);



        public static void Quit()
        {
            BeforeQuitting();

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
			Application.Quit();
#endif
        }

        public static void RestartDirectly()
        {
            Debug.Log("尝试重启游戏");

            if (platform == RuntimePlatform.Android)
            {
                Quit();
            }
            else
            {
#if UNITY_EDITOR
                Quit();
#else
				BeforeQuitting();

				//开启新的实例
				Process.Start(Process.GetCurrentProcess().MainModule.FileName);

				//关闭当前实例
				Process.GetCurrentProcess().Kill();
#endif
            }
        }
    }
}
