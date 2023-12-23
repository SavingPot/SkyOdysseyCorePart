using Mirror;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System.Text;
using Sirenix.OdinInspector;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using UnityEngine.SceneManagement;
using SP.Tools;
using UnityEngine.Events;
using SP.Tools.Unity;
using GameCore.UI;
using Random = System.Random;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using GameCore.High;

namespace GameCore
{
    /// <summary>
    /// GM, 即 Game Manager, 游戏管理器
    /// </summary>
    public class GM : SingletonToolsClass<GM>
    {
        public BloodParticlePool bloodParticlePool = new();
        public DamageTextPool damageTextPool = new();

        public ParticleSystem weatherParticle { get; protected set; }
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
        [Button("设置随机更新几率")] private void EditorSetRandomUpdateProbability(byte u) => RandomUpdater.randomUpdateProbability = u;
#endif

        //随机更新只在服务器进行, 无需同步至客户端
        public byte randomUpdateProbability => RandomUpdater.randomUpdateProbability;
        #endregion



        #region 天气系统

        public List<WeatherData> weathers = new();
        public WeatherData weather { get; internal set; }

        #endregion





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
                weatherParticleEmission = weatherParticle.emission;
            }

            /* -------------------------------------------------------------------------- */
            /*                                    调用委托                                    */
            /* -------------------------------------------------------------------------- */
            AfterPreparation();

            /* -------------------------------------------------------------------------- */
            /*                                    设置天气                                    */
            /* -------------------------------------------------------------------------- */
            weatherParticle.textureSheetAnimation.Clear();

            AddWeather("ori:sunny", null, null);

            AddWeather("ori:rain", () =>
            {
                //编辑粒子动画
                weatherParticle.textureSheetAnimation.Clear();
                weatherParticle.textureSheetAnimation.AddSprite(ModFactory.CompareTexture("ori:rain_particle_0").sprite);
                weatherParticle.textureSheetAnimation.AddSprite(ModFactory.CompareTexture("ori:rain_particle_1").sprite);
                weatherParticle.textureSheetAnimation.AddSprite(ModFactory.CompareTexture("ori:rain_particle_2").sprite);
                weatherParticle.textureSheetAnimation.AddSprite(ModFactory.CompareTexture("ori:rain_particle_3").sprite);
                weatherParticle.textureSheetAnimation.AddSprite(ModFactory.CompareTexture("ori:rain_particle_4").sprite);
                weatherParticle.textureSheetAnimation.AddSprite(ModFactory.CompareTexture("ori:rain_particle_5").sprite);

                GAudio.Play(AudioID.Rain, true);

                //开始发射
                weatherParticleEmission.enabled = true;

                if (globalVolume.profile.TryGet<Bloom>(out Bloom bloom))
                    bloom.active = true;
            },
            () =>
            {
                //禁用发射
                weatherParticleEmission.enabled = false;

                //停止所有音效
                GAudio.Stop(AudioID.Rain);

                if (globalVolume.profile.TryGet<Bloom>(out Bloom bloom))
                    bloom.active = false;
            });

            SetWeather("ori:sunny");
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
                Server.Callback<NMSetBlockCustomData>(OnServerGetNMSetBlockCustomData);
            };
            NetworkCallbacks.OnTimeToClientCallback += () =>
            {
                Client.Callback<NMDestroyBlock>(OnClientGetNMDestroyBlockMessage);
                Client.Callback<NMSetBlock>(OnClientGetNMSetBlockMessage);
                Client.Callback<NMSetBlockCustomData>(OnClientGetNMSetBlockCustomData);
            };

            /* -------------------------------------------------------------------------- */
            /*                                   添加场景回调                                   */
            /* -------------------------------------------------------------------------- */
            NetworkCallbacks.OnClientChangeScene += (conn, n) =>
            {
                if (n.newSceneName == SceneNames.GameScene)
                {
                    ManagerNetwork.instance.AddPlayer(conn);
                }
            };
        }

        //Unite StartGameHost and StartGameCLient
        public static async void StartGameHost(string worldDirPath, Action callback)
        {
            ushort port = Tools.GetUnoccupiedPort();

            ManagerNetwork.instance.StartHostForAddress(port);

            bool panelFadeOk = false;
            var kvp = RegionGenerationMask((_, _) => panelFadeOk = true, null);
            var panel = kvp.Key;
            var text = kvp.Value;
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
                    //TODO: 游戏内反馈
                    Debug.LogError("世界加载失败");
                    return;
                }

                GFiles.world = worldLoadingTask.Result;
            }
            /* -------------------------------------------------------------------------- */
            /*                                    检查世界                                    */
            /* -------------------------------------------------------------------------- */
            if (GFiles.world == null)
            {
                Debug.LogError($"在加入世界前检查 GFiles.world 是否为空, 否则传入 {nameof(worldDirPath)} 参数");
                return;
            }

            if (GameTools.CompareVersions(GFiles.world.basicData.gameVersion, "0.7.4", Operators.less))
            {
                //TODO: 游戏内反馈
                Debug.LogError("世界版本不正确, 拒绝进入");
                return;
            }

            /* -------------------------------------------------------------------------- */
            /*                                    处理世界                                    */
            /* -------------------------------------------------------------------------- */
            Debug.Log($"正在进入世界 {GFiles.world.basicData.worldName}");

            if (GFiles.world.basicData.gameVersion != GInit.gameVersion)
            {
                Debug.LogWarning("世界与游戏版本不对齐, 正在尝试转换");

                //TODO: 世界版本转换
                switch (GFiles.world.basicData.gameVersion)
                {
                    default:
                        Debug.LogError("世界版本转换失败, 拒绝加入");
                        return;
                }
            }

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
            kvp = RegionGenerationMask((_, _) => panelFadeOk = true, null);
            panel = kvp.Key;
            text = kvp.Value;
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

        public static KeyValuePair<PanelIdentity, TextIdentity> RegionGenerationMask(UnityAction<PanelIdentity, TextIdentity> afterFadingIn, UnityAction<PanelIdentity, TextIdentity> afterFadingOut)
        {
            return GameUI.GenerateMask("ori:panel.wait_generating_the_region", "ori:text.wait_generating_the_region", afterFadingIn, afterFadingOut);
        }

        public static void StartGameClient(string address, ushort port)
        {
            bool panelFadeOk = false;
            var kvp = GameUI.GenerateMask("ori:panel.wait_joining_the_server", "ori:text.wait_joining_the_server", (_, _) => panelFadeOk = true, null);
            var panel = kvp.Key;
            var text = kvp.Value;
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
                kvp = RegionGenerationMask(null, null);
                panel = kvp.Key;
                text = kvp.Value;

                string nextSceneName = GScene.nextSceneName;
                GScene.NextAsync();
                await UniTask.WaitUntil(() => GScene.name == nextSceneName);
                #endregion

                kvp = RegionGenerationMask(null, null);
                panel = kvp.Key;
                text = kvp.Value;

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
            if (GScene.name != SceneNames.GameScene)
                return;

            //将消息转发给客户端
            Server.Send(n);
        }

        //当服务器收到 NMPos 消息时的回调
        static void OnServerGetNMSetBlockCustomData(NetworkConnection conn, NMSetBlockCustomData n)
        {
            if (GScene.name != SceneNames.GameScene)
                return;

            //将消息转发给客户端
            Server.Send(n);
        }

        //当客户端收到 NMPos 消息时的回调
        static void OnClientGetNMDestroyBlockMessage(NMDestroyBlock n) => MethodAgent.TryRun(() =>
        {
            if (GScene.name != SceneNames.GameScene)
                return;

            //把指定位置的方块摧毁
            map.DestroyBlock(n.pos, n.isBackground, true);

            GAudio.Play(AudioID.DestroyBlock);
        }, true);

        //当服务器收到 NMPos 消息时的回调
        static void OnClientGetNMSetBlockMessage(NMSetBlock n)
        {
            if (GScene.name != SceneNames.GameScene)
                return;

            map.SetBlock(n.pos, n.isBackground, n.block == null ? null : ModFactory.CompareBlockDatum(n.block), n.customData, true);
        }

        //当服务器收到 NMPos 消息时的回调
        static void OnClientGetNMSetBlockCustomData(NMSetBlockCustomData n)
        {
            if (GScene.name != SceneNames.GameScene)
                return;

            //TODO: 完善这个
            if (map.TryGetBlock(n.pos, n.isBackground, out Block block))
            {
                Debug.Log($"n: {n.pos}, {n.isBackground}");
                Debug.Log($"b: {block.pos}, {block.isBackground}");
                block.customData = JsonTools.LoadJObjectByString(n.customData);
                block.OnServerSetCustomData();
            }
            else
            {
                Debug.LogError("未找到要求的方块, 自定义数据设置失败");
            }
        }

        #endregion

        /// <summary>
        /// 让服务器生成物品实体
        /// </summary>
        /// <param name="pos"></param>
        /// <param name="itemId"></param>
        [ChineseName("生成物品")]
        public void SummonItem(Vector3 pos, string itemId, ushort count = 1, string customData = null)
        {
            string guid = Tools.randomGUID;
            StringBuilder sb = Tools.stringBuilderPool.Get();
            var param = new JObject();
            param.AddProperty("ori:item_data", sb.Append(itemId).Append("/=/").Append(count).Append("/=/").Append(customData).ToString());
            Tools.stringBuilderPool.Recover(sb);

            SummonEntity(pos, EntityID.Drop, guid, true, null, param.ToString());
        }

        public void SummonEntity(Vector3 pos, string id, string saveId = null, bool intoRegion = true, float? health = null, string customData = null)
        {
            saveId ??= Tools.randomGUID;

            //TODO: NMSummon 改为 ServerRun
            //发送生成消息给服务器
            Client.Send<NMSummon>(new(pos, id, saveId, intoRegion, health, customData));
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

        public void RecoverRegion(Vector2Int index)
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
                    map.chunkPool.Recover(chunk);
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
                            entity.Recover();
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

        public void GenerateExistingRegion(Region region, Action afterGenerating = null, Action ifGenerated = null, ushort waitScale = 80)
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

            Debug.Log($"开始生成已有区域 {region.index}");

            generatingExistingRegion = true;

            GameCallbacks.CallBeforeGeneratingExistingRegion(region);





            /* -------------------------------------------------------------------------- */
            /*                                    正式生成                                    */
            /* -------------------------------------------------------------------------- */
            int xDelta = Region.GetMiddleX(region.index);
            int yDelta = Region.GetMiddleY(region.index);

            foreach (var save in region.saves)
            {
                BlockData block = ModFactory.CompareBlockDatum(save.blockId);

                for (int i = 0; i < save.locations.Count; i++)
                {
                    BlockSave_Location location = save.locations[i];

                    Map.instance.SetBlock(new(location.pos.x + xDelta, location.pos.y + yDelta), location.isBackground, block, location.customData, false);

                    //定时 Sleep 防止游戏卡死
                    if (i % waitScale == 0)
                        yield return null;
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

        public Region GenerateNewRegion(Vector2Int index, string[] specificBiomes = null)
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
            RegionGeneration generation = new(GFiles.world.basicData.seed, index, specificBiomes);
            GameCallbacks.CallBeforeGeneratingNewRegion(generation);



            /* -------------------------------------------------------------------------- */
            /*                                    生成方块                                    */
            /* -------------------------------------------------------------------------- */
            List<Vector2Int> islandCentersTemp = new()
            {
                Vector2Int.zero
            };


            Vector2Int[] islandCenters = islandCentersTemp.ToArray();

            //TODO: 多线程生成每个岛屿
            foreach (var island in islandCenters)
            {
                IslandGeneration islandGeneration = generation.NewIsland();
                BiomeData_Block[] directBlocks;
                BiomeData_Block[] perlinBlocks;
                BiomeData_Block[] postProcessBlocks;
                BiomeData_Block[] unexpectedBlocks;

                List<BiomeData_Block> directBlocksTemp = new();
                List<BiomeData_Block> perlinBlocksTemp = new();
                List<BiomeData_Block> postProcessBlocksTemp = new();
                List<BiomeData_Block> unexpectedBlocksTemp = new();

                List<(Vector2Int pos, bool isBackground)> blockAdded = new();

                foreach (var g in islandGeneration.biome.blocks)
                {
                    if (g == null || !g.initialized)
                        continue;

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


                void AddBlockInTheIsland(string blockId, Vector2Int pos, bool isBackground)
                {
                    blockAdded.Add((pos, isBackground));

                    islandGeneration.regionGeneration.region.AddPos(blockId, pos, isBackground, true);
                }

                void AddBlockInTheIslandForAreas(string blockId, Vector2Int pos, Vector3Int[] areas)
                {
                    foreach (var item in areas)
                    {
                        Vector2Int actualPos = new(pos.x + item.x, pos.y + item.y);
                        bool actualIsBackground = item.z < 0;

                        lock (blockAdded)
                        {
                            for (int c = 0; c < blockAdded.Count; c++)
                            {
                                var p = blockAdded[c];
                                if (p.pos == actualPos && p.isBackground == actualIsBackground)
                                    blockAdded.RemoveAt(c);
                            }
                            blockAdded.Add((actualPos, actualIsBackground));
                        }
                    }

                    islandGeneration.regionGeneration.region.AddPos(blockId, pos, areas, true);
                }


                /* ---------------------------------- 生成 Direct --------------------------------- */
                //遍历每个点
                for (int x = islandGeneration.minPoint.x; x < islandGeneration.maxPoint.x; x++)
                {
                    for (int y = islandGeneration.minPoint.y; y < islandGeneration.maxPoint.y; y++)
                    {
                        //当前遍历到的点
                        Vector2Int pos = new(x, y);

                        foreach (var g in directBlocks)
                        {
                            if (Tools.Prob100(g.rules.probability, islandGeneration.regionGeneration.random))
                            {
                                if (string.IsNullOrWhiteSpace(g.attached.blockId) ||
                                    islandGeneration.regionGeneration.region.GetBlock(pos + g.attached.offset, g.attached.isBackground)?.block.blockId == g.attached.blockId)
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
                                            AddBlockInTheIslandForAreas(g.id, pos, g.areas);
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
                                    var blockPos = new Vector2Int(noise.x, startY + i);

                                    lock (blockAdded)
                                    {
                                        for (int c = 0; c < blockAdded.Count; c++)
                                        {
                                            var p = blockAdded[c];
                                            if (p.pos == blockPos && p.isBackground == block.isBackground)
                                                blockAdded.RemoveAt(c);
                                        }
                                        blockAdded.Add((blockPos, block.isBackground));
                                    }

                                    islandGeneration.regionGeneration.region.AddPos(block.block, blockPos, block.isBackground, true);
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

                foreach (var oneAdded in blockAdded)
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

                if (island == Vector2Int.zero)
                {
                    var middleX = Region.GetMiddleX(generation.index);
                    generation.region.spawnPoint = new(middleX, wallHighestPointFunction[middleX] + 3);
                }

                //遍历每个点
                for (int x = islandGeneration.minPoint.x; x < islandGeneration.maxPoint.x; x++)
                {
                    Parallel.For(islandGeneration.minPoint.y, islandGeneration.maxPoint.y, y =>
                    {
                        //当前遍历到的点
                        Vector2Int pos = new(x, y);

                        foreach (var g in postProcessBlocks)
                        {
                            if (Tools.Prob100(g.rules.probability, islandGeneration.regionGeneration.random))
                            {
                                if (string.IsNullOrWhiteSpace(g.attached.blockId) ||
                                    islandGeneration.regionGeneration.region.GetBlock(pos + g.attached.offset, g.attached.isBackground)?.block.blockId == g.attached.blockId)
                                {
                                    foreach (var range in g.ranges)
                                    {
                                        if (!wallHighestPointFunction.TryGetValue(x, out int highestY))
                                            highestY = 0;

                                        var formulaAlgebra = new FormulaAlgebra()
                                        {
                                        {
                                            "@bottom",islandGeneration.minPoint.y
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

                                        if (string.IsNullOrWhiteSpace(range.minFormula) ||
                                            string.IsNullOrWhiteSpace(range.maxFormula) ||
                                            y.IInRange(
                                                range.minFormula.ComputeFormula(formulaAlgebra),
                                                range.maxFormula.ComputeFormula(formulaAlgebra)
                                            ))
                                        {
                                            AddBlockInTheIslandForAreas(g.id, pos, g.areas);
                                        }
                                    }
                                }
                            }
                        }
                    });
                }

                //生成战利品
                IslandGeneration.LootGeneration(islandGeneration, wallHighestPointFunction);





                /* -------------------------------------------------------------------------- */
                /*                                    生成结构                                    */
                /* -------------------------------------------------------------------------- */
                if (islandGeneration.biome.structures?.Length > 0)
                {
                    //全部生成完后再生成结构
                    for (int x = islandGeneration.minPoint.x; x < islandGeneration.maxPoint.x; x++)
                        for (int y = islandGeneration.minPoint.y; y < islandGeneration.maxPoint.y; y++)
                        {
                            //当前遍历到的点
                            Vector2Int pos = new(x, y);

                            Parallel.ForEach(islandGeneration.biome.structures, l =>
                            {
                                if (Tools.Prob100(l.structure.probability, islandGeneration.regionGeneration.random))
                                {
                                    //检查空间是否足够
                                    if (l.structure.mustEnough)
                                    {
                                        foreach (var fixedBlock in l.structure.fixedBlocks)
                                        {
                                            if (islandGeneration.regionGeneration.region.GetBlock(pos + fixedBlock.offset, fixedBlock.isBackground) != null)
                                            {
                                                goto stopGeneration;
                                            }
                                        }
                                    }

                                    //检查是否满足所有需求
                                    foreach (var require in l.structure.require)
                                    {
                                        if (!islandGeneration.regionGeneration.region.GetBlockOut(pos + require.offset, require.isBackground, out BlockSave_Location temp) || temp.block.blockId != require.blockId)
                                        {
                                            goto stopGeneration;
                                        }
                                    }

                                    //如果可以就继续
                                    Parallel.ForEach(l.structure.fixedBlocks, attached =>
                                    {
                                        var tempPos = pos + attached.offset;

                                        AddBlockInTheIsland(attached.blockId, tempPos, attached.isBackground);
                                    });

                                stopGeneration:
                                    return;
                                }
                            });
                        };
                }
            }





            /* -------------------------------------------------------------------------- */
            /*                                    生成传送点                                   */
            /* -------------------------------------------------------------------------- */
            Vector2Int portalMiddle = new(generation.maxPoint.x / 2, generation.maxPoint.y * 3 / 4);
            generation.region.AddPos(BlockID.Portal, portalMiddle, false, true);
            generation.region.AddPos(BlockID.PortalBase, portalMiddle + new Vector2Int(0, -1), false, true);
            generation.region.AddPos(BlockID.PortalBase, portalMiddle + new Vector2Int(-1, -1), false, true);
            generation.region.AddPos(BlockID.PortalBase, portalMiddle + new Vector2Int(1, -1), false, true);



            if (index == Vector2Int.zero)
            {
                //如是初始区域, 生成 Nick
                var nick = ModFactory.CompareEntity(EntityID.Nick);
                EntitySave nickSave = new()
                {
                    id = nick.id,
                    pos = generation.region.spawnPoint + new Vector2Int(10, 0),
                    saveId = Tools.randomGUID
                };
                generation.region.entities.Add(nickSave);
            }

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
        public BiomeData[] biomes;



        public IslandGeneration NewIsland()
        {
            var islandGeneration = new IslandGeneration(this);
            return islandGeneration;
        }


        public void Finish()
        {
            region.generatedAlready = true;

            lock (GFiles.world.regionData)
                GFiles.world.AddRegion(region);
        }





        public RegionGeneration(int seed, Vector2Int index, string[] specificBiomes = null)
        {
            this.index = index;

            //乘数是为了增加 index 差, 避免比较靠近的区域生成一致
            actualSeed = seed + index.x * 2 + index.y * 4;

            //改变随机数种子, 以确保同一种子的地形一致, 不同区域地形不一致
            random = new(actualSeed);

            //获取可用的群系
            List<BiomeData> biomeList = new();

            if (specificBiomes == null)
            {
                int difficulty = Mathf.Max(Mathf.Abs(index.x), Mathf.Abs(index.y));

                // //添加候选群系, 只包含 Rich群系
                // if (forceCenter)
                //     foreach (Mod mod in ModFactory.mods)
                //         foreach (BiomeData b in mod.biomes)
                //             if (b.Rich().hasTag)
                //                 biomeList.Add(b);

                // //如果不强制生成 Rich 群系 或 没有 Rich群系, 就随便一个群系
                List<BiomeData> allBiomes = new();

                foreach (Mod mod in ModFactory.mods)
                    foreach (BiomeData b in mod.biomes)
                        allBiomes.Add(b);

                allBiomes = allBiomes.Shuffle();

                //TODO: 变成在 region.json 中设置指定的群系
                //获取难度最近的群系
                BiomeData closestBiome = null;
                int closest = int.MaxValue;

                foreach (var currentBiome in allBiomes)
                {
                    int delta = Mathf.Abs(currentBiome.difficulty - difficulty);
                    if (delta < closest)
                    {
                        closest = delta;
                        closestBiome = currentBiome;
                    }
                }

                //获取难度第二近的群系
                BiomeData secondClosestBiome = null;
                int secondClosest = int.MaxValue;

                foreach (var currentBiome in allBiomes)
                {
                    int delta = Mathf.Abs(currentBiome.difficulty - difficulty);
                    if (delta < secondClosest)
                    {
                        secondClosest = delta;
                        secondClosestBiome = currentBiome;
                    }
                }
            }
            else
            {
                foreach (var id in specificBiomes)
                    foreach (Mod mod in ModFactory.mods)
                        foreach (BiomeData b in mod.biomes)
                            if (b.id == id)
                                biomeList.Add(b);
            }

            //如果没有群系就随便添加
            if (biomeList.Count == 0)
                foreach (Mod mod in ModFactory.mods)
                    foreach (BiomeData b in mod.biomes)
                        biomeList.Add(b);

            this.biomes = biomeList.ToArray();

            //确定大小
            size = new(Region.chunkCountX * Chunk.blockCountPerAxis, Region.chunkCountX * Chunk.blockCountPerAxis);

            //边际 (左下右上)
            maxPoint = new(size.x / 2, size.y / 2);
            minPoint = -maxPoint;



            region = new()
            {
                size = size,
                index = index,
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
        public FormulaAlgebra directBlockComputationAlgebra;






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

        public static Action<IslandGeneration, Dictionary<int, int>> LootGeneration = (generation, wallHighestPointFunction) =>
        {
            foreach (var highest in wallHighestPointFunction)
            {
                Vector2Int lootBlockPos = new(highest.Key, highest.Value + 1);

                /* ----------------------------------- 木桶 (只有中心空岛有) ----------------------------------- */
                if (generation.biome.id == BiomeID.Center)
                {
                    if (lootBlockPos.x == 15)
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

                                default:
                                    {
                                        //每个格子都有 60% 的概率为空
                                        if (Tools.Prob100(60, generation.regionGeneration.random))
                                            continue;

                                        //抽取模组
                                        Mod mod = null;
                                        while (mod == null)
                                        {
                                            mod = ModFactory.mods.Extract();
                                            if (mod.items.Count == 0)
                                                mod = null;
                                        }

                                        //从抽取的模组里抽取物品
                                        ItemData itemData = null;
                                        for (var inner = 0; inner < mod.items.Count / 5 + 1; inner++)  //最多尝试抽取 1/5 的物品
                                        {
                                            itemData = mod.items.Extract();

                                            if (itemData == null)
                                                continue;

                                            //如果是木桶的战利品就通过
                                            if (itemData.GetTag("ori:loot.barrel").hasTag)
                                                break;
                                            else
                                                itemData = null;
                                        }

                                        /* ---------------------------------- 填充物品 ---------------------------------- */
                                        if (itemData != null)  //如果获取失败了, 这个格子也会为空
                                        {
                                            //随机一定数量
                                            ushort maxCount = (ushort)(itemData.maxCount / 4);
                                            ushort count = (ushort)(generation.regionGeneration.random.NextDouble() * maxCount);
                                            if (count <= 0) count = 1;
                                            if (count > maxCount) count = maxCount;

                                            item = itemData.DataToItem();
                                            item.count = count;
                                        }

                                        break;
                                    }
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

                        generation.regionGeneration.region.AddPos(BlockID.Barrel, lootBlockPos, false, true, jo.ToString(Formatting.None));
                    }
                }

                /* ----------------------------------- 木箱 ----------------------------------- */
                else if (Tools.Prob100(2, generation.regionGeneration.random))
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
                            mod = ModFactory.mods.Extract();
                            if (mod.items.Count == 0)
                                mod = null;
                        }

                        static string ExtractSpell()
                        {
                            Spell spell = null;
                            Mod mod = null;

                            //获取模组
                            while (mod == null)
                            {
                                mod = ModFactory.mods.Extract();
                                if (mod.spells.Count == 0)
                                    mod = null;
                            }

                            //获取魔咒
                            for (var inner = 0; inner < mod.spells.Count / 5 + 1; inner++)  //最多尝试抽取 1/5 的魔咒
                            {
                                spell = mod.spells.Extract();

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
                            item = mod.items.Extract();

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
                                jo.AddObject("original:mana_container");
                                jo["original:mana_container"].AddProperty("totalMana", generation.regionGeneration.random.Next(0, 100));
                                jo.AddObject("original:spell_container");
                                jo["original:spell_container"].AddProperty("spell", ExtractSpell());
                                extendedItem.customData = jo;
                            }
                            else if (item.id == ItemID.SpellManuscript)
                            {
                                var jo = new JObject();
                                jo.AddObject("original:spell_container");
                                jo["original:spell_container"].AddProperty("spell", ExtractSpell());
                                extendedItem.customData = jo;
                            }

                            group[i] = JToken.FromObject(extendedItem);
                        }
                    });

                    JObject jo = new();

                    jo.AddObject("ori:container");
                    jo["ori:container"].AddObject("items");
                    jo["ori:container"]["items"].AddArray("array", group);

                    generation.regionGeneration.region.AddPos(BlockID.WoodenChest, lootBlockPos, false, true, jo.ToString(Formatting.None));
                }
            }
        };




        public IslandGeneration(RegionGeneration regionGeneration)
        {
            this.regionGeneration = regionGeneration;

            //决定群系
            biome = regionGeneration.biomes.Extract();

            //决定岛的大小
            size = new(regionGeneration.random.Next(biome.minSize.x, biome.maxSize.x).IRange(10, Region.place.x),
                                regionGeneration.random.Next(biome.minSize.y, biome.maxSize.y).IRange(10, Region.place.y));
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
