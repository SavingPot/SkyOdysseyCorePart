using Cysharp.Threading.Tasks;
using GameCore.High;
using GameCore.Network;
using GameCore.UI;
using Mirror;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using Random = System.Random;
using Sirenix.OdinInspector;
using SP.Tools.Unity;
using SP.Tools;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System;
using UnityEngine.Events;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine;

namespace GameCore
{
    /// <summary>
    /// GM, 即 Game Manager, 游戏管理器
    /// </summary>
    public class GM : SingletonClass<GM>
    {
        public BloodParticlePool bloodParticlePool = new();
        public DamageTextPool damageTextPool = new();

        public ParticleSystem weatherParticle { get; protected set; }
        public ParticleSystem.MainModule weatherParticleMain;
        public ParticleSystem.EmissionModule weatherParticleEmission;
        public Volume globalVolume { get; protected set; }
        public Light2D globalLight { get; protected set; }
        public static Action AfterPreparation = () => { };
        public static Action OnUpdate = () => { };



        #region 时间系统
        #region 变量

        public bool isMorning
        {
            get => GTime.isMorning;
            [Button("设置是否是上午")]
            set => GTime.isMorning = value;
        }

        public float timeOneDay //1440(s) = 24*60(s), 即 24min
        {
            get => GTime.timeOneDay;
            [Button("设置一天的总时间")]
            set => GTime.timeOneDay = value;
        }

        public float time
        {
            get => GTime.time;
            [Button("设置时间")]
            set => GTime.time = value;
        }

        public float timeSpeed
        {
            get => GTime.timeSpeed;
            [Button("设置时间流速")]
            set => GTime.timeSpeed = value;
        }
        #endregion

        #endregion



        #region 地形生成
        [LabelText("地图")] private static Map map => Map.instance;

        public readonly List<Vector2Int> generatingNewRegions = new();
        public readonly List<Region> generatedExistingRegions = new();
        public bool generatingNewRegion => generatingNewRegions.Count != 0;
        public bool generatingExistingRegion { get; private set; }

        public List<Vector2Int> recoveringRegions = new();
        #endregion



        #region 随机更新
#if UNITY_EDITOR
        [Button("设置随机更新几率")] private void EditorSetRandomUpdateProbability(byte u = byte.MaxValue) => RandomUpdater.randomUpdateProbability = u;
        [Button("随机更新")]
        private void EditorRandomUpdate()
        {
            Debug.Log(RandomUpdater.updates.Count);
            RandomUpdater.RandomUpdate();
        }
        [Button("随机更改天气")] private void ChangeWeatherRandomly() => RandomUpdater.ChangeWeatherRandomly();
#endif

        //随机更新只在服务器进行, 无需同步至客户端
        public byte randomUpdateProbability => RandomUpdater.randomUpdateProbability;
        #endregion



        #region 天气系统

        public List<WeatherData> weathers = new();
        public WeatherData weather { get; internal set; }

        #endregion









        public static int GetRegionUnlockingCost(Vector2Int index)
        {
            return Math.Abs(index.x) * 100 + Math.Abs(index.y) * 300;
        }









        private void FixedUpdate()
        {
            //执行随机更新
            if (Server.isServer && !generatingNewRegion && !generatingExistingRegion)
            {
                if (Tools.Prob100(RandomUpdater.randomUpdateProbability))
                {
                    RandomUpdater.RandomUpdate();
                }
            }
        }

        private void Update()
        {
            OnUpdate();

            /* ---------------------------------- 计算时间 ---------------------------------- */
            if (Server.isServer)
                GTime.Compute();

            if (Client.isClient)
            {
                /* -------------------------------- 设置全局光照亮度 -------------------------------- */
                globalLight.intensity = (time * 2 / timeOneDay).Range(0.1f, 0.85f);

                /* --------------------------------- 设置天空颜色 --------------------------------- */
                byte delta = (byte)Mathf.Min(GTime.darknessLevel * 12, 180);   //* xx 是为了扩大时间的影响, 要限制在 <= 180, 否则天会变绿
                Tools.instance.mainCamera.backgroundColor = new Color32(0, (byte)(180 - delta), (byte)(byte.MaxValue - delta), byte.MaxValue);  //设置摄像机背景颜色
            }
        }

        protected override void Awake()
        {
            base.Awake();

            /* -------------------------------------------------------------------------- */
            /*                                     初始化组件                                    */
            /* -------------------------------------------------------------------------- */
            GameObject glGo = GameObject.Find("GlobalLight");
            GameObject gv = GameObject.Find("Global Volume");
            GameObject glP = GameObject.Find("Weather Particle System");

            if (glGo)
            {
                globalLight = glGo.GetComponent<Light2D>();
            }
            if (gv)
            {
                globalVolume = gv.GetComponent<Volume>();
            }
            if (glP)
            {
                weatherParticle = glP.GetComponent<ParticleSystem>();
                weatherParticleMain = weatherParticle.main;
                weatherParticleEmission = weatherParticle.emission;

                //编辑粒子动画
                weatherParticle.textureSheetAnimation.Clear();
                weatherParticle.textureSheetAnimation.AddSprite(ModFactory.CompareTexture("ori:rain_particle_0").sprite);
                weatherParticle.textureSheetAnimation.AddSprite(ModFactory.CompareTexture("ori:rain_particle_1").sprite);
                weatherParticle.textureSheetAnimation.AddSprite(ModFactory.CompareTexture("ori:rain_particle_2").sprite);
                weatherParticle.textureSheetAnimation.AddSprite(ModFactory.CompareTexture("ori:rain_particle_3").sprite);
                weatherParticle.textureSheetAnimation.AddSprite(ModFactory.CompareTexture("ori:rain_particle_4").sprite);
                weatherParticle.textureSheetAnimation.AddSprite(ModFactory.CompareTexture("ori:rain_particle_5").sprite);
            }

            /* -------------------------------------------------------------------------- */
            /*                                    调用委托                                    */
            /* -------------------------------------------------------------------------- */
            AfterPreparation();

            /* -------------------------------------------------------------------------- */
            /*                                    设置天气                                    */
            /* -------------------------------------------------------------------------- */
            SetGlobalVolumeBloomToSunny();
            SetGlobalVolumeColorAdjustmentsToSunny();


            //晴朗
            AddWeather("ori:sunny", null, null);

            //酸雨
            AddWeather("ori:acid_rain", () =>
            {
                GAudio.Play(AudioID.Rain, true);

                //开始发射
                weatherParticleMain.startColor = Color.green;
                weatherParticleEmission.enabled = true;

                //设置模糊效果
                SetGlobalVolumeBloomToRain();
                SetGlobalVolumeColorAdjustmentsToAcidRain();
            }, () =>
            {
                weatherParticleMain.startColor = Color.white;

                //禁用发射
                weatherParticleEmission.enabled = false;

                //停止所有音效
                GAudio.Stop(AudioID.Rain);

                //设置模糊效果
                SetGlobalVolumeBloomToSunny();
                SetGlobalVolumeColorAdjustmentsToSunny();
            });

            //雨天
            AddWeather("ori:rain", () =>
            {
                GAudio.Play(AudioID.Rain, true);

                //开始发射
                weatherParticleEmission.enabled = true;

                //设置模糊效果
                SetGlobalVolumeBloomToRain();
                SetGlobalVolumeColorAdjustmentsToRain();
            },
            () =>
            {
                //禁用发射
                weatherParticleEmission.enabled = false;

                //停止所有音效
                GAudio.Stop(AudioID.Rain);

                //设置模糊效果
                SetGlobalVolumeBloomToSunny();
                SetGlobalVolumeColorAdjustmentsToSunny();
            });

            SetWeather("ori:sunny");
        }



        public void SetGlobalVolumeBloomToSunny() => SetGlobalVolumeBloom(0.95f, 0.5f);
        public void SetGlobalVolumeBloomToRain() => SetGlobalVolumeBloom(0.8f, 2.5f);
        public void SetGlobalVolumeBloom(float threshold, float intensity)
        {
            if (globalVolume.profile.TryGet(out Bloom bloom))
            {
                bloom.active = true;
                bloom.threshold.Override(threshold);
                bloom.intensity.Override(intensity);
            }
        }



        public void SetGlobalVolumeColorAdjustmentsToSunny() => SetGlobalVolumeColorAdjustments(new(0.75f, 0.66f, 0.66f), 1.52f, 8.5f);
        public void SetGlobalVolumeColorAdjustmentsToAcidRain() => SetGlobalVolumeColorAdjustments(new(0.8f, 1f, 0.92f), 1.05f, 6);
        public void SetGlobalVolumeColorAdjustmentsToRain() => SetGlobalVolumeColorAdjustments(new(0.75f, 0.66f, 0.66f), 1.45f, 5);
        public void SetGlobalVolumeColorAdjustments(Color colorFilter, float colorFilterIntensity, float saturation)
        {
            if (globalVolume.profile.TryGet(out ColorAdjustments colorAdjustments))
            {
                colorAdjustments.colorFilter.Override(colorFilter * colorFilterIntensity);
                colorAdjustments.saturation.Override(saturation);
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

        public void AddWeather(string weatherId, Action OnEnter, Action OnExit)
        {
            weathers.Add(new(weatherId, OnEnter, OnExit));
        }

        [Button]
        public void SetWeather(string weatherId)
        {
            foreach (var value in weathers)
            {
                if (value.id == weatherId)
                {
                    weather?.OnExit?.Invoke();

                    value.OnEnter?.Invoke();
                    weather = value;

                    return;
                }
            }

            Debug.LogWarning($"天气 {weatherId} 不存在");
        }



        [RuntimeInitializeOnLoadMethod]
        private static void BindMethods()
        {
            /* -------------------------------------------------------------------------- */
            /*                                   绑定网络回调                                   */
            /* -------------------------------------------------------------------------- */
            NetworkCallbacks.OnTimeToServerCallback += () =>
            {
                Server.Callback<NMDestroyBlock>(OnServerGetNMDestroyBlock);
                Server.Callback<NMSetBlock>(OnServerGetNMSetBlockMessage);
                Server.Callback<NMRemoveBlock>(OnServerGetNMRemoveBlockMessage);
                Server.Callback<NMSetBlockCustomData>(OnServerGetNMSetBlockCustomData);
            };
            NetworkCallbacks.OnTimeToClientCallback += () =>
            {
                Client.Callback<NMDestroyBlock>(OnClientGetNMDestroyBlockMessage);
                Client.Callback<NMSetBlock>(OnClientGetNMSetBlockMessage);
                Client.Callback<NMRemoveBlock>(OnClientGetNMRemoveBlockMessage);
                Client.Callback<NMSetBlockCustomData>(OnClientGetNMSetBlockCustomData);
            };
        }

        //TODO: Unite StartGameHost and StartGameCLient
        public static async void StartGameHost(string worldDirPath, Action callback)
        {
            ushort port = Tools.GetUnoccupiedPort();

            ManagerNetwork.instance.StartHostForAddress(port);

            bool panelFadeOk = false;
            var tuple = GameUI.RegionGenerationMask((_, _) => panelFadeOk = true, null);
            var panel = tuple.panel;
            var text = tuple.text;
            panel.CustomMethod("fade_in", null);

            /* -------------------------------------------------------------------------- */
            /*                                   加载世界文件                                   */
            /* -------------------------------------------------------------------------- */
            if (worldDirPath != null)
            {
                Task<World> worldLoadingTask = new(() =>
                {
                    return World.Load(worldDirPath);
                });
                worldLoadingTask.Start();
                await worldLoadingTask;

                if (worldLoadingTask.IsFaulted || worldLoadingTask.IsCanceled)
                {
                    InternalUIAdder.instance.SetStatusText("世界加载失败");
                    Debug.LogError("世界加载失败");
                    return;
                }

                GFiles.world = worldLoadingTask.Result;
            }
            /* -------------------------------------------------------------------------- */
            /*                                    检查并处理世界                                    */
            /* -------------------------------------------------------------------------- */
            //检查世界是否为空
            if (GFiles.world == null)
            {
                Debug.LogError($"在加入世界前检查 GFiles.world 是否为空, 否则传入 {nameof(worldDirPath)} 参数");
                return;
            }

            //检查世界版本是否被永久弃用
            if (GameTools.CompareVersions(GFiles.world.basicData.gameVersion, "0.7.8", Operators.less))
            {
                InternalUIAdder.instance.SetStatusText("世界版本被永久弃用了, 拒绝进入");
                Debug.LogError("世界版本被永久弃用了, 拒绝进入");
                return;
            }

            //检查世界的方块
            foreach (var region in GFiles.world.regionData)
            {
                foreach (var blockSave in region.blocks)
                {
                    if (ModFactory.CompareBlockData(blockSave.blockId) == null)
                    {
                        //TODO: 显示有哪些方块被移除了，并让玩家自己决定要不要删除那些方块
                    }
                }
            }

            if (GFiles.world.basicData.gameVersion != GInit.gameVersion)
            {
                Debug.LogWarning("世界与游戏版本不对齐, 正在尝试转换");

                //TODO: 世界版本转换
                switch (GFiles.world.basicData.gameVersion)
                {
                    default:
                        InternalUIAdder.instance.SetStatusText("世界版本转换失败, 拒绝进入");
                        Debug.LogError("世界版本转换失败, 拒绝进入");
                        return;
                }
            }








            Debug.Log($"正在进入世界 {GFiles.world.basicData.worldName}");

            /* -------------------------------------------------------------------------- */
            /*                                   加载下一个场景                                  */
            /* -------------------------------------------------------------------------- */
            var v = SceneManager.LoadSceneAsync(GScene.nextIndex);
            v.allowSceneActivation = false;
            await UniTask.WaitUntil(() => panelFadeOk);
            await UniTask.WaitUntil(() => v.progress == 0.9f);

            string nameShouldBe = GScene.nextSceneName;
            GScene.BeforeLoad(GScene.nextIndex);
            v.allowSceneActivation = true;
            await v;

            await UniTask.WaitUntil(() => GScene.name == nameShouldBe);

            /* -------------------------------------------------------------------------- */
            /*                                    公布服务器                                   */
            /* -------------------------------------------------------------------------- */
            ManagerNetwork.instance.discovery.AdvertiseServer();

            /* -------------------------------------------------------------------------- */
            /*                                   等待加载区域                                   */
            /* -------------------------------------------------------------------------- */
            //到下一个场景了，要重新创建一个遮罩
            tuple = GameUI.RegionGenerationMask((_, _) => panelFadeOk = true, null);
            panel = tuple.panel;
            text = tuple.text;
            panel.panelImage.SetAlpha(1);

            GameCallbacks.AfterGeneratingExistingRegion += InternalAfterGeneratingExistingRegion;

            ////ManagerNetwork.instance.SummonAllPlayers();

            panel.OnUpdate += pg =>
            {
                GameUI.SetUILayerToTop(pg);
            };

            async void InternalAfterGeneratingExistingRegion(Region region)
            {
                await UniTask.WaitUntil(() => Player.local);

                if (region.index == Player.local.regionIndex)
                {
                    if (panel)
                        panel.CustomMethod("fade_out", null);

                    GameCallbacks.AfterGeneratingExistingRegion -= InternalAfterGeneratingExistingRegion;
                    callback.Invoke();
                }
            }
        }



        public static void StartGameClient(string address, ushort port)
        {
            bool panelFadeOk = false;
            var tuple = GameUI.GenerateMask("ori:panel.wait_joining_the_server", "ori:text.wait_joining_the_server", (_, _) => panelFadeOk = true, null);
            var panel = tuple.panel;
            var text = tuple.text;
            panel.CustomMethod("fade_in", null);

            ManagerNetwork.instance.StartClientForAddress(address, port);

            GeneratingTheWorld();

            async void GeneratingTheWorld()
            {
                await UniTask.WaitUntil(() => panelFadeOk);
                await UniTask.WaitUntil(() => Client.isConnected || !Client.isConnecting);

                //连接失败
                if (!Client.isConnected)
                {
                    Debug.Log("服务器连接失败");
                    panel.CustomMethod("fade_out", null);
                    return;
                }

                #region 声明并等待场景加载
                tuple = GameUI.RegionGenerationMask(null, null);
                panel = tuple.panel;
                text = tuple.text;

                string nextSceneName = GScene.nextSceneName;
                GScene.NextAsync();
                await UniTask.WaitUntil(() => GScene.name == nextSceneName);
                #endregion

                //到下一个场景了，要重新创建一个遮罩
                tuple = GameUI.RegionGenerationMask(null, null);
                panel = tuple.panel;
                text = tuple.text;

                panel.OnUpdate += pg =>
                {
                    GameUI.SetUILayerToTop(pg);
                };

                GameCallbacks.AfterGeneratingExistingRegion += InternalAfterGeneratingExistingRegion;

                async void InternalAfterGeneratingExistingRegion(Region region)
                {
                    await UniTask.WaitUntil(() => Player.local);

                    if (region.index == Player.local.regionIndex)
                    {
                        if (panel)
                            panel.CustomMethod("fade_out", null);

                        GameCallbacks.AfterGeneratingExistingRegion -= InternalAfterGeneratingExistingRegion;
                    }
                }
            }
        }

        protected override void Start()
        {
            if (Server.isServer)
            {
                GTime.isMorning = GFiles.world.basicData.isAM;
                GTime.time = GFiles.world.basicData.time;
            }

            if (Server.isServer)
                InitializeWorld(GFiles.world);

            base.Start();
        }

        #region 绑定
        //当服务器收到 NMPos 消息时的回调
        static void OnServerGetNMDestroyBlock(NetworkConnection conn, NMDestroyBlock n)
        {
            if (GScene.name != SceneNames.GameScene)
                return;

            //如果服务器上存在该方块
            if (map.TryGetBlock(n.pos, n.isBackground, out Block block))
            {
                if (block.data == null)
                {
                    Debug.LogWarning($"{MethodGetter.GetCurrentMethodPath()}: 获取位置 {n.pos} [{n.isBackground}] 的方块数据为空");
                    return;
                }

                //使方块生成掉落物
                block.OutputDrops(n.pos.To3());

                //摧毁方块
                GameCallbacks.OnBlockDestroyed(n.pos, n.isBackground, block.data);
            }
            //如果不存在
            else
            {
                Debug.LogWarning($"{MethodGetter.GetCurrentMethodPath()}: 获取位置 {n.pos} [{n.isBackground}] 的方块失败");
            }

            //将消息转发给客户端
            Server.Send(n);
        }

        //当服务器收到 NMPos 消息时的回调
        static void OnServerGetNMSetBlockMessage(NetworkConnection conn, NMSetBlock n)
        {
            //将消息转发给客户端
            Server.Send(n);
        }

        //当服务器收到 NMPos 消息时的回调
        static void OnServerGetNMRemoveBlockMessage(NetworkConnection conn, NMRemoveBlock n)
        {
            //将消息转发给客户端
            Server.Send(n);
        }

        //当服务器收到 NMPos 消息时的回调
        static void OnServerGetNMSetBlockCustomData(NetworkConnection conn, NMSetBlockCustomData n)
        {
            //将消息转发给客户端
            Server.Send(n);
        }

        //当客户端收到 NMPos 消息时的回调
        static void OnClientGetNMDestroyBlockMessage(NMDestroyBlock n)
        {
            if (GScene.name != SceneNames.GameScene)
                return;

            //把指定位置的方块摧毁
            map.RemoveBlock(n.pos, n.isBackground, true, true);

            GAudio.Play(AudioID.DestroyBlock);
        }

        //当服务器收到 NMPos 消息时的回调
        static void OnClientGetNMSetBlockMessage(NMSetBlock n)
        {
            if (GScene.name != SceneNames.GameScene)
                return;

            map.SetBlock(n.pos, n.isBackground, n.block == null ? null : ModFactory.CompareBlockData(n.block), n.customData, true, true);
        }

        //当客户端收到 NMPos 消息时的回调
        static void OnClientGetNMRemoveBlockMessage(NMRemoveBlock n)
        {
            if (GScene.name != SceneNames.GameScene)
                return;

            //把指定位置的方块摧毁
            map.RemoveBlock(n.pos, n.isBackground, true, true);
        }

        //当服务器收到 NMPos 消息时的回调
        static void OnClientGetNMSetBlockCustomData(NMSetBlockCustomData n)
        {
            if (GScene.name != SceneNames.GameScene)
                return;

            if (map.TryGetBlock(n.pos, n.isBackground, out Block block))
            {
                Debug.Log($"n: {n.pos}, {n.isBackground}");
                Debug.Log($"b: {block.pos}, {block.isBackground}");
                block.customData = JsonUtils.LoadJObjectByString(n.customData);
                block.OnServerSetCustomData();
            }
            else
            {
                Debug.LogError("未找到要求的方块, 自定义数据设置失败");
            }
        }

        #endregion

        /// <summary>
        /// 让服务器生成金币实体
        /// </summary>
        /// <param name="pos"></param>
        /// <param name="itemId"></param>
        [ChineseName("生成金币实体")]
        public void SummonCoinEntity(Vector3 pos, int count)
        {
            var param = new JObject();
            param.AddObject("ori:coin_entity", new JProperty("count", count));

            SummonEntity(pos, EntityID.CoinEntity, null, true, null, param.ToString());
        }

        /// <summary>
        /// 让服务器生成掉落物
        /// </summary>
        /// <param name="pos"></param>
        /// <param name="itemId"></param>
        [ChineseName("生成掉落物")]
        public void SummonDrop(Vector3 pos, string itemId, ushort count = 1, string customData = null)
        {
            StringBuilder sb = Tools.stringBuilderPool.Get();
            JObject jo = customData == null ? new() : JsonUtils.LoadJObjectByString(customData);

            jo.AddProperty(
                "ori:item_data",
                sb.Append(itemId)
                        .Append("/=/")
                        .Append(count)
                        .Append("/=/")
                        .Append(customData)
                        .ToString()
            );

            Tools.stringBuilderPool.Recover(sb);

            SummonEntity(pos, EntityID.Drop, null, true, null, jo.ToString());
        }

        [Button]
        public void SummonEntity(Vector3 pos, string id, string saveId = null, bool saveIntoRegion = true, int? health = null, string customData = null)
        {
            saveId ??= Tools.randomGUID;

            //发送生成消息给服务器
            Client.Send<NMSummon>(new(pos, id, saveId, saveIntoRegion, health, customData));
        }

        public void SummonEntityAndCallbackWhenSummoned(Vector3 pos, string id, Action<Entity> callback, string saveId = null, bool saveIntoRegion = true, int? health = null, string customData = null)
        {
            saveId ??= Tools.randomGUID;

            //绑定回调
            EntityCenter.BindEventOnEntitySummoned(saveId, callback);

            //发送生成消息给服务器
            Client.Send<NMSummon>(new(pos, id, saveId, saveIntoRegion, health, customData));
        }

        public void SummonEntity(EntitySave save)
        {
            SummonEntity(save.pos, save.id, save.saveId, false, save.health, save.customData);
        }

        private void OnDestroy()
        {
            RandomUpdater.updates.Clear();
        }

        /// <summary>
        /// 初始化世界
        /// </summary>
        /// <param name="world"></param>
        [ChineseName("初始化世界")]
        public void InitializeWorld(World world)
        {
            if (world.basicData.gameVersion.IsNullOrWhiteSpace())
                world.basicData.gameVersion = GInit.gameVersion;
        }

        public void RecycleRegion(Vector2Int index)
        {
            //如果已经在回收就取消
            foreach (var item in recoveringRegions)
                if (item == index)
                    return;

            recoveringRegions.Add(index);





            for (int i = map.chunks.Count - 1; i >= 0; i--)
            {
                Chunk chunk = map.chunks[i];

                if (chunk.regionIndex == index)
                    map.chunkPool.Recycle(chunk);
            }

            bool did = false;
            for (int i = generatedExistingRegions.Count - 1; i >= 0; i--)
            {
                Region region = generatedExistingRegions[i];
                if (region.index == index)
                {
                    did = true;

                    //删除实体
                    List<Entity> entities = EntityCenter.all;

                    for (int e = entities.Count - 1; e >= 0; e--)
                    {
                        Entity entity = entities[e];

                        if (entity.regionIndex == region.index)
                        {
                            entity.Kill();
                        }
                    }

                    //从已生成列表去除
                    generatedExistingRegions.RemoveAt(i);
                    break;
                    ////Debug.Log($"回收了区域 {index}");
                }
            }

            recoveringRegions.Remove(index);

            if (!did)
                Debug.LogWarning($"区域 {index} 不存在");
        }

        public void GenerateExistingRegion(Region region, Action afterGenerating, Action ifGenerated, ushort waitScale)
        {
            StartCoroutine(IEGenerateExistingRegion(region, afterGenerating, ifGenerated, waitScale));
        }

        private IEnumerator IEGenerateExistingRegion(Region region, Action afterGenerating, Action ifGenerated, ushort waitScale)
        {
            /* -------------------------------------------------------------------------- */
            /*                                   检查生成状况                                   */
            /* -------------------------------------------------------------------------- */
            if (generatedExistingRegions.Any(p => p.index == region.index && p.generatedAlready))
            {
                Debug.LogWarning($"区域 {region.index} 已生成, 请勿频繁生成");
                ifGenerated?.Invoke();
                yield break;
            }

            generatingExistingRegion = true;

            GameCallbacks.CallBeforeGeneratingExistingRegion(region);





            /* -------------------------------------------------------------------------- */
            /*                                    正式生成                                    */
            /* -------------------------------------------------------------------------- */
            int xDelta = Region.GetMiddleX(region.index);
            int yDelta = Region.GetMiddleY(region.index);

            foreach (var save in region.blocks)
            {
                BlockData block = ModFactory.CompareBlockData(save.blockId);

                for (int i = 0; i < save.locations.Count; i++)
                {
                    BlockSave_Location location = save.locations[i];

                    Map.instance.SetBlock(new(location.x + xDelta, location.y + yDelta), save.isBg, block, location.cd, false, false);

                    //定时 Sleep 防止游戏卡死
                    if (i % waitScale == 0)
                        yield return null;
                }
            }



            //更新所有方块
            lock (Map.instance.chunks)
            {
                //要复制一份，防止在生成过程中迭代器变化
                var chunks = Map.instance.chunks.ToArray();

                foreach (var chunk in chunks)
                {
                    //要复制一份，防止在生成过程中迭代器变化
                    var blocks = chunk.blocks.ToArray();

                    foreach (var block in blocks)
                    {
                        block?.OnUpdate();
                    }
                }
            }





            /* -------------------------------------------------------------------------- */
            /*                                    生成实体                                    */
            /* -------------------------------------------------------------------------- */
            if (Server.isServer)
            {
                for (int i = 0; i < region.entities.Count; i++)
                {
                    SummonEntity(region.entities[i]);
                }
            }





            /* -------------------------------------------------------------------------- */
            /*                                    完成后事项                                   */
            /* -------------------------------------------------------------------------- */
            generatingExistingRegion = false;
            afterGenerating?.Invoke();
            generatedExistingRegions.Add(region);
            ////Performance.CollectMemory();
            GameCallbacks.CallAfterGeneratingExistingRegion(region);
        }

        public Region GenerateNewRegion(Vector2Int index, string specificBiome = null)
        {
            /* -------------------------------------------------------------------------- */
            /*                                    生成前检查                                   */
            /* -------------------------------------------------------------------------- */
            lock (generatingNewRegions)
            {
                if (generatingNewRegions.Any(p => p == index))
                {
                    Debug.LogWarning($"新区域 {index} 正在生成, 请勿频繁生成!");
                    return null;
                }

                foreach (var temp in GFiles.world.regionData)
                {
                    if (temp.index == index && temp.generatedAlready)
                    {
                        Debug.LogWarning($"要生成的新区域 {index} 已存在于世界, 请勿频繁生成!");
                        return temp;
                    }
                }

                generatingNewRegions.Add(index);
            }



            /* -------------------------------------------------------------------------- */
            /*                                    初始化数据                                   */
            /* -------------------------------------------------------------------------- */
            RegionGeneration generation = new(GFiles.world.basicData.seed, index, specificBiome);
            GameCallbacks.CallBeforeGeneratingNewRegion(generation);



            /* -------------------------------------------------------------------------- */
            /*                                    生成浮岛                                    */
            /* -------------------------------------------------------------------------- */
            List<Vector2Int> islandCentersTemp = new()
            {
                Vector2Int.zero,
                // new(60, 0),
                // new(-60, 0),
                // new(0, 60),
                // new(0, -60)
            };


            Vector2Int[] islandCenterPoints = islandCentersTemp.ToArray();

            foreach (var centerPoint in islandCenterPoints)
            {
                IslandGeneration islandGeneration = generation.NewIsland(centerPoint == Vector2Int.zero);
                BiomeData_Block[] directBlocks;
                BiomeData_Block[] perlinBlocks;
                BiomeData_Block[] postProcessBlocks;
                BiomeData_Block[] unexpectedBlocks;

                List<BiomeData_Block> directBlocksTemp = new();
                List<BiomeData_Block> perlinBlocksTemp = new();
                List<BiomeData_Block> postProcessBlocksTemp = new();
                List<BiomeData_Block> unexpectedBlocksTemp = new();

                foreach (var g in islandGeneration.biome.blocks)
                {
                    if (g == null)
                    {
                        Debug.Log($"生成岛屿时发现了一个空的方块生成规则");
                        continue;
                    }
                    if (!g.initialized)
                    {
                        Debug.Log($"生成岛屿时发现了一个未初始化的方块生成规则 {g.id}");
                        continue;
                    }

                    if (g.type == "direct")
                        directBlocksTemp.Add(g);
                    else if (g.type == "perlin")
                        perlinBlocksTemp.Add(g);
                    else if (g.type == "post_process")
                        postProcessBlocksTemp.Add(g);
                    else
                        unexpectedBlocksTemp.Add(g);
                }

                directBlocks = directBlocksTemp.ToArray();
                perlinBlocks = perlinBlocksTemp.ToArray();
                postProcessBlocks = postProcessBlocksTemp.ToArray();
                unexpectedBlocks = unexpectedBlocksTemp.ToArray();


                void AddBlockInTheIsland(string blockId, int x, int y, bool isBackground, string customData = null)
                {
                    //? 添加到区域生成，需要根据岛的中心位置做偏移，而在添加到岛屿生成则不需要
                    islandGeneration.regionGeneration.region.AddPos(blockId, x + centerPoint.x, y + centerPoint.y, isBackground, true, customData);

                    islandGeneration.blockAdded.Add((new(x, y), isBackground)); //* 如果已经存在过方块可能会出问题，但是目前来看无伤大雅
                }

                void AddBlockInTheIslandForAreas(string blockId, int x, int y, Vector3Int[] areas)
                {
                    islandGeneration.regionGeneration.region.AddPos(blockId, x + centerPoint.x, y + centerPoint.y, areas, true); //* 如果已经存在过方块可能会出问题，但是目前来看无伤大雅

                    foreach (var item in areas)
                    {
                        Vector2Int actualPos = new(x + item.x, y + item.y);
                        bool actualIsBackground = item.z < 0;

                        for (int c = 0; c < islandGeneration.blockAdded.Count; c++)
                        {
                            var (pos, isBackground) = islandGeneration.blockAdded[c];
                            if (pos == actualPos && isBackground == actualIsBackground)
                                islandGeneration.blockAdded.RemoveAt(c);
                        }
                        islandGeneration.blockAdded.Add((actualPos, actualIsBackground));
                    }
                }


                /* ---------------------------------- 生成 Direct --------------------------------- */
                //遍历每个点
                for (int x = islandGeneration.minPoint.x; x < islandGeneration.maxPoint.x; x++)
                {
                    for (int y = islandGeneration.minPoint.y; y < islandGeneration.maxPoint.y; y++)
                    {
                        foreach (var g in directBlocks)
                        {
                            if (Tools.Prob100(g.rules.probability, islandGeneration.regionGeneration.random))
                            {
                                if (string.IsNullOrWhiteSpace(g.attached.blockId) ||
                                   islandGeneration.regionGeneration.region.GetBlock(centerPoint.x + x + g.attached.offset.x, centerPoint.y + y + g.attached.offset.y, g.attached.isBackground).save?.blockId == g.attached.blockId
                                    )
                                {
                                    foreach (var range in g.ranges)
                                    {
                                        if (string.IsNullOrWhiteSpace(range.minFormula) ||
                                            string.IsNullOrWhiteSpace(range.maxFormula) ||
                                            y.IInRange(
                                                range.minFormula.ComputeFormula(islandGeneration.directBlockComputationAlgebra),
                                                range.maxFormula.ComputeFormula(islandGeneration.directBlockComputationAlgebra)
                                            ))
                                        {
                                            AddBlockInTheIslandForAreas(g.id, x, y, g.areas);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                /* ---------------------------------- 生成 Perlin --------------------------------- */
                foreach (var perlinBlock in perlinBlocks)
                {
                    var perlin = perlinBlock.perlin;

                    //Key是x轴, Value是对应的噪声值
                    List<Vector2Int> noises = new();
                    int lowestNoise = int.MaxValue;
                    int highestNoise = int.MaxValue;

                    for (int x = islandGeneration.minPoint.x; x < islandGeneration.maxPoint.x; x++)
                    {
                        var xSample = (x + (float)islandGeneration.regionGeneration.actualSeed / 1000f) / perlin.fluctuationFrequency;
                        var noise = (int)(Mathf.PerlinNoise1D(xSample) * perlin.fluctuationHeight);

                        noises.Add(new(x, noise));

                        //最低/最高值为 int.MaxValue 代表的是最低/最高值还未被赋值过     
                        if (lowestNoise == int.MaxValue || noise < lowestNoise)
                            lowestNoise = noise;
                        if (highestNoise == int.MaxValue || noise > highestNoise)
                            highestNoise = noise;
                    }

                    var startY = (int)perlin.startYFormula.ComputeFormula(islandGeneration.directBlockComputationAlgebra);

                    //遍历每一个噪声点
                    foreach (var noise in noises)
                    {
                        //如果 noise.y == 6, 那就要从 0 开始向上遍历, 把方块一个个放出来, 一共七个方块 (0123456)
                        for (int i = 0; i < noise.y + 1; i++)
                        {
                            /* -------------------------------- 接下来是添加方块 -------------------------------- */
                            foreach (var block in perlin.blocks)
                            {
                                var algebra = GetPerlinFormulaAlgebra(0, noise.y, islandGeneration.minPoint.y, islandGeneration.surface, islandGeneration.maxPoint.y);
                                var min = (int)block.minFormula.ComputeFormula(algebra);
                                var max = (int)block.maxFormula.ComputeFormula(algebra);

                                if (i >= min && i <= max)
                                {
                                    AddBlockInTheIsland(block.block, noise.x, startY + i, block.isBackground);
                                    break;
                                }
                            }
                        }
                    }
                }

                /* ---------------------------------- 生成 PostProcess --------------------------------- */

                //字典返回的是这个 x 对应的 y 值最高的点 (x 为 Key   y 为 Value)
                Dictionary<int, int> wallHighestPointFunction = new();
                Dictionary<int, int> backgroundHighestPointFunction = new();

                foreach (var oneAdded in islandGeneration.blockAdded)
                {
                    var pos = oneAdded.pos;
                    var isBackground = oneAdded.isBackground;

                    var dic = isBackground ? backgroundHighestPointFunction : wallHighestPointFunction;

                    //如果该 x 值没有被记录过, 那就记录该 x 值
                    if (!dic.ContainsKey(pos.x))
                        dic.Add(pos.x, pos.y);
                    else if (dic[pos.x] < pos.y)  //如果该 x 值已经被记录过, 但是记录的 y 值比当前 y 值要小, 那就更新记录的 y 值
                        dic[pos.x] = pos.y;
                }

                //遍历每个点
                foreach (var g in postProcessBlocks)
                {
                    for (int x = islandGeneration.minPoint.x; x < islandGeneration.maxPoint.x; x++)
                    {
                        for (int y = islandGeneration.minPoint.y; y < islandGeneration.maxPoint.y; y++)
                        {
                            if (Tools.Prob100(g.rules.probability, islandGeneration.regionGeneration.random))
                            {
                                if (string.IsNullOrWhiteSpace(g.attached.blockId) ||
                                    islandGeneration.regionGeneration.region.GetBlock(centerPoint.x + x + g.attached.offset.x, centerPoint.y + y + g.attached.offset.y, g.attached.isBackground).save?.blockId == g.attached.blockId
                                    )
                                {
                                    if (!wallHighestPointFunction.TryGetValue(x, out int highestY))
                                        highestY = 0;

                                    var formulaAlgebra = new FormulaAlgebra()
                                        {
                                            {
                                                "@bottom", islandGeneration.minPoint.y
                                            },
                                            {
                                                "@surface", islandGeneration.surface
                                            },
                                            {
                                                "@top", islandGeneration.maxPoint.y
                                            },
                                            {
                                                "@highest_point", highestY
                                            }
                                        };


                                    foreach (var range in g.ranges)
                                    {
                                        if (string.IsNullOrWhiteSpace(range.minFormula) ||
                                            string.IsNullOrWhiteSpace(range.maxFormula) ||
                                            y.IInRange(
                                                range.minFormula.ComputeFormula(formulaAlgebra),
                                                range.maxFormula.ComputeFormula(formulaAlgebra)
                                            ))
                                        {
                                            AddBlockInTheIslandForAreas(g.id, x, y, g.areas);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                if (centerPoint == Vector2Int.zero)
                {
                    var middleX = Region.GetMiddleX(generation.index);
                    var middleY = Region.GetMiddleY(generation.index);

                    try
                    {
                        generation.region.spawnPoint = new(middleX, middleY + wallHighestPointFunction[0] + 3);
                    }
                    catch (Exception ex)
                    {
                        Debug.Log($"选取出生点时失败, 堆栈如下: {ex}");
                        generation.region.spawnPoint = new(middleX, middleY);
                    }
                }

                //生成战利品
                IslandGeneration.LootGeneration(islandGeneration, wallHighestPointFunction, AddBlockInTheIsland);





                /* -------------------------------------------------------------------------- */
                /*                                    生成结构                                    */
                /* -------------------------------------------------------------------------- */
                if (islandGeneration.biome.structures?.Length > 0)
                {
                    //全部生成完后再生成结构
                    for (int x = islandGeneration.minPoint.x; x < islandGeneration.maxPoint.x; x++)
                    {
                        for (int y = islandGeneration.minPoint.y; y < islandGeneration.maxPoint.y; y++)
                        {
                            //当前遍历到的点
                            Vector2Int pos = new(x, y);

                            foreach (var l in islandGeneration.biome.structures)
                            {
                                if (Tools.Prob100(l.structure.probability, islandGeneration.regionGeneration.random))
                                {
                                    //检查空间是否足够
                                    if (l.structure.mustEnough)
                                    {
                                        foreach (var fixedBlock in l.structure.fixedBlocks)
                                        {
                                            int tempPosX = pos.x + fixedBlock.offset.x + centerPoint.x;
                                            int tempPosY = pos.y + fixedBlock.offset.y + centerPoint.y;

                                            if (islandGeneration.regionGeneration.region.GetBlock(tempPosX, tempPosY, fixedBlock.isBackground).location != null)
                                            {
                                                goto stopGeneration;
                                            }
                                        }
                                    }

                                    //检查是否满足所有需求
                                    foreach (var require in l.structure.require)
                                    {
                                        int tempPosX = pos.x + require.offset.x + centerPoint.x;
                                        int tempPosY = pos.y + require.offset.y + centerPoint.y;

                                        if (islandGeneration.regionGeneration.region.GetBlock(tempPosX, tempPosY, require.isBackground).save?.blockId != require.blockId)
                                        {
                                            goto stopGeneration;
                                        }
                                    }

                                    //如果可以就继续
                                    foreach (var fixedBlock in l.structure.fixedBlocks)
                                    {
                                        var tempPosX = pos.x + fixedBlock.offset.x;
                                        var tempPosY = pos.y + fixedBlock.offset.y;

                                        AddBlockInTheIsland(fixedBlock.blockId, tempPosX, tempPosY, fixedBlock.isBackground);
                                    }

                                stopGeneration:
                                    continue;
                                }
                            }
                        };
                    }
                }
            }


            //TODO: 检测矿石的位置，把单个矿石变成矿石团块
            IslandGeneration.OreClumpsGeneration(generation);

            /* -------------------------------------------------------------------------- */
            /*                                    生成传送点                                   */
            /* -------------------------------------------------------------------------- */
            int portalMiddleX = generation.region.spawnPoint.x;
            int portalMiddleY = generation.region.spawnPoint.y + 10;
            generation.region.AddPos(BlockID.Portal, portalMiddleX, portalMiddleY, false, true);
            generation.region.AddPos(BlockID.PortalBase, portalMiddleX, portalMiddleY - 1, false, true);
            generation.region.AddPos(BlockID.PortalBase, portalMiddleX - 2, portalMiddleY - 1, false, true);
            generation.region.AddPos(BlockID.PortalBase, portalMiddleX - 1, portalMiddleY - 1, false, true);
            generation.region.AddPos(BlockID.PortalBase, portalMiddleX + 1, portalMiddleY - 1, false, true);
            generation.region.AddPos(BlockID.PortalBase, portalMiddleX + 2, portalMiddleY - 1, false, true);





            /* -------------------------------------------------------------------------- */
            /*                                   加上区域边界                                   */
            /* -------------------------------------------------------------------------- */

            //? 为什么是 "<=" 最高点, 而不是 "<" 最高点呢?
            //? 我们假设 min=(-3,-3), max=(3,3)
            //? 那么我们会循环 -3, -2, -1, 0, 1, 2
            //? 发现了吗, 3 没有被遍历到, 以此要使用 "<="
            for (int x = generation.minPoint.x; x <= generation.maxPoint.x; x++)
            {
                for (int y = generation.minPoint.y; y <= generation.maxPoint.y; y++)
                {
                    //检测点位是否在边界上
                    if (x == generation.minPoint.x || y == generation.minPoint.y || x == generation.maxPoint.x || y == generation.maxPoint.y)
                    {
                        //删除边界上的任何方块
                        generation.region.RemovePos(x, y, false);
                        generation.region.RemovePos(x, y, true);

                        //添加边界方块
                        generation.region.AddPos(BlockID.Boundary, x, y, false, true);
                    }
                }
            }



            //生成实体（脚本的绑定）
            IslandGeneration.EntitiesGeneration(generation);



            MethodAgent.RunOnMainThread(() =>
            {
                generation.Finish();
                GFiles.SaveAllDataToFiles();
                //Performance.CollectMemory();
                GameCallbacks.CallAfterGeneratingNewRegion(generation.region);
            });

            lock (generatingNewRegions)
            {
                generatingNewRegions.Remove(index);
            }
            return generation.region;
        }

        public static FormulaAlgebra GetPerlinFormulaAlgebra(int lowestNoise, int highestNoise, int bottom, int surface, int top)
        {
            return new()
            {
                {
                    "@lowest_noise", lowestNoise
                },
                {
                    "@highest_noise", highestNoise
                },
                {
                    "@bottom", bottom
                },
                {
                    "@surface", surface
                },
                {
                    "@top", top
                }
            };
        }
    }

    public class RegionGeneration
    {
        public Random random;
        public int originalSeed;
        public int actualSeed;
        public Vector2Int index;
        public Vector2Int size;
        public Vector2Int maxPoint;
        public Vector2Int minPoint;
        public Region region;



        public IslandGeneration NewIsland(bool isCenterIsland)
        {
            var islandGeneration = new IslandGeneration(this, isCenterIsland);
            return islandGeneration;
        }


        public void Finish()
        {
            region.generatedAlready = true;

            lock (GFiles.world.regionData)
                GFiles.world.AddRegion(region);
        }





        public RegionGeneration(int seed, Vector2Int index, string specificBiome = null)
        {
            this.index = index;

            //乘数是为了增加 index 差, 避免比较靠近的区域生成一致
            actualSeed = seed + index.x * 2 + index.y * 4;

            //改变随机数种子, 以确保同一种子的地形一致, 不同区域地形不一致
            random = new(actualSeed);

            /* --------------------------------- 群系 -------------------------------- */
            BiomeData biome = null;
            if (specificBiome == null)
            {
                if (index.x == 0)
                {
                    biome = ModFactory.CompareBiome(BiomeID.Center);
                }
                else
                {
                    List<BiomeData> biomes = new();

                    foreach (var mod in ModFactory.mods)
                    {
                        foreach (var currentBiome in mod.biomes)
                        {
                            //找到正负相符的群系
                            if (Math.Sign(currentBiome.distribution) == Math.Sign(index.x))
                            {
                                biomes.Add(currentBiome);
                            }
                        }
                    }

                    if (biomes.Count != 0)
                    {
                        biomes = biomes.OrderBy(b => Math.Abs(b.distribution)).ToList();
                        biomes.ForEach(b => Debug.Log(b.id + " " + b.distribution));
                        biome = biomes[Math.Min(Math.Abs(index.x) - 1, biomes.Count - 1)];
                    }
                }
            }
            else
            {
                biome = ModFactory.CompareBiome(specificBiome);
            }
            if (biome == null)
            {
                Debug.LogError($"群系为空, 将生成 {BiomeID.Desert}");
                biome = ModFactory.CompareBiome(BiomeID.Desert);
            }

            /* ------------------------------------------------------------------------ */

            //确定大小
            size = new(Region.chunkCount * Chunk.blockCountPerAxis, Region.chunkCount * Chunk.blockCountPerAxis);

            //边际 (左下右上)
            maxPoint = new((int)Math.Floor(size.x / 2f), (int)Math.Floor(size.y / 2f));
            minPoint = -maxPoint;



            region = new()
            {
                size = size,
                index = index,
                biomeId = biome.id,
                maxPoint = maxPoint,
                minPoint = minPoint
            };
        }
    }

    public class IslandGeneration
    {
        public Vector2Int maxPoint;
        public Vector2Int minPoint;
        public Vector2Int size;
        public RegionGeneration regionGeneration;
        public BiomeData biome;
        public int yOffset;
        public int surface;
        public int surfaceExtra1;
        public bool isCenterIsland;
        public FormulaAlgebra directBlockComputationAlgebra;
        public List<(Vector2Int pos, bool isBackground)> blockAdded;






        public static FormulaAlgebra GetIslandGenerationFormulaAlgebra(int bottom, int surface, int top)
        {
            return new()
            {
                {
                    "@bottom", bottom
                },
                {
                    "@surface", surface
                },
                {
                    "@top", top
                }
            };
        }

        public static Action<RegionGeneration> OreClumpsGeneration = (generation) =>
        {
            foreach (var save in generation.region.blocks)
            {
                var blockData = ModFactory.CompareBlockData(save.blockId);

                //找到矿石
                if (blockData != null)
                {
                    var tag = blockData.GetValueTagToInt("ori:ore", 10);

                    if (tag.hasTag)
                    {
                        var locationsTemp = save.locations.ToArray();

                        foreach (var location in locationsTemp)
                        {
                            List<Vector3Int> locations = new() { Vector3Int.zero };

                            // tagValue 决定了团块有多大
                            for (int i = 0; i < generation.random.Next(tag.tagValue / 3, tag.tagValue + 1); i++)
                            {
                                var randomLocation = locations.Extract(generation.random);
                                var randomPos = new Vector3Int(
                                                    randomLocation.x + generation.random.Next(-1, 2),
                                                    randomLocation.y + generation.random.Next(-1, 2));
                                if (!locations.Contains(randomPos))
                                    locations.Add(randomPos);
                            }

                            locations.Remove(Vector3Int.zero);
                            foreach (var loc in locations)
                            {
                                int newX = location.x + loc.x;
                                int newY = location.y + loc.y;

                                //只有石头会被替换
                                if (generation.region.TryGetBlock(newX, newY, save.isBg, out var stone) && stone.save.blockId == BlockID.Stone)
                                {
                                    stone.save.RemoveLocation(newX, newY);
                                    save.AddLocation(newX, newY);
                                }
                            }
                        }
                    }
                }
            }
        };

        public static Action<IslandGeneration, Dictionary<int, int>, Action<string, int, int, bool, string>> LootGeneration = (generation, wallHighestPointFunction, AddBlockInIsland) =>
        {
            //遍历每个最高点
            foreach (var highest in wallHighestPointFunction)
            {
                Vector2Int lootBlockPos = new(highest.Key, highest.Value + 1);

                /* ----------------------------------- 木桶 (只有中心空岛有) ----------------------------------- */
                if (generation.biome.id == BiomeID.Center)
                {
                    if (lootBlockPos.x == -6)
                    {
                        AddBlockInIsland(BlockID.RemoteMarket, lootBlockPos.x, lootBlockPos.y, false, null);
                    }
                    else if (lootBlockPos.x == 15)
                    {
                        JToken[] group = new JToken[28];

                        //填充每个栏位
                        for (int i = 0; i < group.Length; i++)
                        {
                            /* --------------------------------- 抽取一个物品 --------------------------------- */
                            Item item = null;

                            switch (i)
                            {
                                case 0:
                                    item = ModFactory.CompareItem(BlockID.Dirt).DataToItem();
                                    item.count = 30;
                                    break;

                                case 1:
                                    item = ModFactory.CompareItem(ItemID.SportsVest).DataToItem();
                                    break;

                                case 2:
                                    item = ModFactory.CompareItem(ItemID.SportsShorts).DataToItem();
                                    break;

                                case 3:
                                    item = ModFactory.CompareItem(ItemID.Sneakers).DataToItem();
                                    break;

                                case 4:
                                    item = ModFactory.CompareItem(BlockID.OnionCrop).DataToItem();
                                    item.count = 3;
                                    break;
                            }

                            /* ---------------------------------- 填充物品 ---------------------------------- */
                            if (item != null)  //如果获取失败了, 这个格子也会为空
                            {
                                group[i] = JToken.FromObject(item);
                            }
                        }

                        JObject jo = new();

                        jo.AddObject("ori:container");
                        jo["ori:container"].AddObject("items");
                        jo["ori:container"]["items"].AddArray("array", group);

                        AddBlockInIsland(BlockID.Barrel, lootBlockPos.x, lootBlockPos.y, false, jo.ToString(Formatting.None));
                    }
                }

                //如果位置被占用就跳过
                if (generation.blockAdded.Any(added => added.pos == lootBlockPos && !added.isBackground))
                    continue;

                /* ----------------------------------- 木箱 ----------------------------------- */
                if (Tools.Prob100(1.5f, generation.regionGeneration.random))
                {
                    JToken[] group = new JToken[21];

                    Parallel.For(0, group.Length, i =>
                    {
                        //每个格子都有 >=65% 的概率为空
                        if (Tools.Prob100(65, generation.regionGeneration.random))
                            return;

                        Mod mod = null;
                        while (mod == null)
                        {
                            mod = ModFactory.mods.Extract(generation.regionGeneration.random);
                            if (mod.items.Count == 0)
                                mod = null;
                        }

                        //从模组中抽取一种魔咒
                        static string ExtractSpell(Random random)
                        {
                            Spell spell = null;
                            Mod mod = null;

                            //获取模组
                            while (mod == null)
                            {
                                mod = ModFactory.mods.Extract(random);
                                if (mod.spells.Count == 0)
                                    mod = null;
                            }

                            //从获得到的模组中抽取一种魔咒
                            for (var inner = 0; inner < mod.spells.Count / 5 + 1; inner++)  //最多尝试抽取 1/5 的魔咒
                            {
                                spell = mod.spells.Extract(random);

                                if (spell == null)
                                    continue;

                                //如果是木箱的战利品就通过
                                if (spell.GetTag("ori:loot.wooden_chest").hasTag)
                                    break;
                                else
                                    spell = null;
                            }

                            return spell?.id;
                        }

                        ItemData item = null;
                        for (var inner = 0; inner < mod.items.Count / 5 + 1; inner++)  //最多尝试抽取 1/5 的物品
                        {
                            item = mod.items.Extract(generation.regionGeneration.random);

                            if (item == null)
                                continue;

                            //如果是木箱的战利品就通过
                            if (item.GetTag("ori:loot.wooden_chest").hasTag)
                                break;
                            else
                                item = null;
                        }
                        if (item != null)  //如果获取失败了, 这个格子也会为空
                        {
                            var extendedItem = item.DataToItem();

                            if (item.id == ItemID.ManaStone)
                            {
                                var jo = new JObject();
                                jo.AddObject("ori:mana_container");
                                jo["ori:mana_container"].AddProperty("total_mana", generation.regionGeneration.random.Next(0, 100));
                                jo.AddObject("ori:spell_container");
                                jo["ori:spell_container"].AddProperty("spell", ExtractSpell(generation.regionGeneration.random));
                                extendedItem.customData = jo;
                            }
                            else if (item.id == ItemID.SpellManuscript)
                            {
                                var jo = new JObject();
                                jo.AddObject("ori:spell_container");
                                jo["ori:spell_container"].AddProperty("spell", ExtractSpell(generation.regionGeneration.random));
                                extendedItem.customData = jo;
                            }

                            group[i] = JToken.FromObject(extendedItem);
                        }
                    });

                    JObject jo = new();

                    jo.AddObject("ori:container");
                    jo["ori:container"].AddObject("items");
                    jo["ori:container"]["items"].AddArray("array", group);

                    AddBlockInIsland(BlockID.WoodenChest, lootBlockPos.x, lootBlockPos.y, false, jo.ToString(Formatting.None));
                }
            }
        };

        public static Action<RegionGeneration> EntitiesGeneration = (generation) =>
        {
            if (generation.index == Vector2Int.zero)
            {
                //如是初始区域, 生成 Nick
                var nick = ModFactory.CompareEntity(EntityID.Nick);
                EntitySave nickSave = new()
                {
                    id = nick.id,
                    pos = generation.region.spawnPoint + new Vector2Int(10, 0),
                    saveId = Tools.randomGUID
                };
                generation.region.AddEntity(nickSave);


                //如是初始区域, 生成商人
                var trader = ModFactory.CompareEntity(EntityID.Trader);
                EntitySave traderSave = new()
                {
                    id = trader.id,
                    pos = generation.region.spawnPoint + new Vector2Int(0, 0),
                    saveId = Tools.randomGUID
                };
                generation.region.AddEntity(traderSave);
            }

            if (generation.region.biomeId == BiomeID.GrasslandFighting)
            {
                //如是初始区域, 生成 Nick
                var entityData = ModFactory.CompareEntity(EntityID.GrasslandGuard);
                EntitySave entitySave = new()
                {
                    id = entityData.id,
                    pos = generation.region.spawnPoint + new Vector2Int(0, 8),
                    saveId = Tools.randomGUID
                };
                generation.region.AddEntity(entitySave);
            }
        };



        public IslandGeneration(RegionGeneration regionGeneration, bool isCenterIsland)
        {
            this.regionGeneration = regionGeneration;
            this.isCenterIsland = isCenterIsland;
            this.blockAdded = new();

            //决定群系
            biome = ModFactory.CompareBiome(regionGeneration.region.biomeId);

            //决定岛的大小
            size = new(regionGeneration.random.Next((int)(Region.placeVec.x * biome.minScale.x), (int)(Region.placeVec.x * biome.maxScale.x)).IRange(10, Region.place.x),
                                regionGeneration.random.Next((int)(Region.placeVec.y * biome.minScale.y), (int)(Region.placeVec.y * biome.maxScale.y)).IRange(10, Region.place.y));
            if (size.x % 2 != 0) size.x++;
            if (size.y % 2 != 0) size.y++;

            //边际 (左下右上)
            maxPoint = new(size.x / 2, size.y / 2);
            minPoint = -maxPoint;

            //使空岛有 y轴 偏移
            yOffset = regionGeneration.random.Next(15, 35);
            surface = maxPoint.y - yOffset;
            surfaceExtra1 = surface + 1;

            directBlockComputationAlgebra = GetIslandGenerationFormulaAlgebra(minPoint.y, surface, maxPoint.y);
        }
    }
}
