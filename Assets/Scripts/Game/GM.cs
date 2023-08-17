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

        public readonly List<Vector2Int> generatingNewSandboxes = new();
        public readonly List<Sandbox> generatedExistingSandboxes = new();
        public bool generatingNewSandbox => generatingNewSandboxes.Count != 0;
        public bool generatingExistingSandbox { get; private set; }

        public List<Vector2Int> recoveringSandboxes = new();
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
            if (Server.isServer && !generatingNewSandbox && !generatingExistingSandbox)
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
                globalLight.intensity = (time * 2 / timeOneDay).Range(0.15f, 0.85f);

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
                if (n.newSceneName == SceneNames.gameScene)
                {
                    ManagerNetwork.instance.AddPlayer(conn);
                }
            };
        }

        public static async void StartGameHost(string worldDirPath, Action callback)
        {
            ushort port = Tools.GetUnoccupiedPort();

            ManagerNetwork.instance.StartHostForAddress(port);

            bool panelFadeOk = false;
            var kvp = SandboxGenerationMask((_, _) => panelFadeOk = true, null);
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
            /*                                   等待加载沙盒                                   */
            /* -------------------------------------------------------------------------- */
            kvp = SandboxGenerationMask((_, _) => panelFadeOk = true, null);
            panel = kvp.Key;
            text = kvp.Value;
            panel.panelImage.SetAlpha(1);

            GameCallbacks.AfterGeneratingExistingSandbox += InternalAfterGeneratingExistingSandbox;

            ////ManagerNetwork.instance.SummonAllPlayers();

            async void InternalAfterGeneratingExistingSandbox(Sandbox sandbox)
            {
                await UniTask.WaitUntil(() => Player.local && Player.local.correctedSyncVars);

                if (sandbox.index == Player.local.sandboxIndex)
                {
                    if (panel)
                        panel.CustomMethod("fade_out", null);

                    GameCallbacks.AfterGeneratingExistingSandbox -= InternalAfterGeneratingExistingSandbox;
                    callback.Invoke();
                }
            }
        }

        public static KeyValuePair<PanelIdentity, TextIdentity> SandboxGenerationMask(UnityAction<PanelIdentity, TextIdentity> afterFadingIn, UnityAction<PanelIdentity, TextIdentity> afterFadingOut)
        {
            return GameUI.GenerateMask("ori:panel.wait_generating_the_sandbox", "ori:text.wait_generating_the_sandbox", afterFadingIn, afterFadingOut);
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
                kvp = SandboxGenerationMask(null, null);
                panel = kvp.Key;
                text = kvp.Value;

                string nextSceneName = GScene.nextSceneName;
                GScene.NextAsync();
                await UniTask.WaitUntil(() => GScene.name == nextSceneName);
                #endregion

                kvp = SandboxGenerationMask(null, null);
                panel = kvp.Key;
                text = kvp.Value;

                panel.OnUpdate += pg =>
                {
                    pg.transform.SetSiblingIndex(pg.transform.childCount - 1);
                };

                GameCallbacks.AfterGeneratingExistingSandbox += InternalAfterGeneratingExistingSandbox;

                async void InternalAfterGeneratingExistingSandbox(Sandbox sandbox)
                {
                    await UniTask.WaitUntil(() => Player.local && Player.local.correctedSyncVars);

                    if (sandbox.index == Player.local.sandboxIndex)
                    {
                        if (panel)
                            panel.CustomMethod("fade_out", null);

                        GameCallbacks.AfterGeneratingExistingSandbox -= InternalAfterGeneratingExistingSandbox;
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
            if (GScene.name != SceneNames.gameScene)
                return;

            //如果服务器上存在该方块
            if (map.TryGetBlock(n.pos, n.layer, out Block block))
            {
                if (block.data == null)
                {
                    Debug.LogWarning($"{MethodGetter.GetCurrentMethodPath()}: 获取位置 {n.pos}({n.layer}) 的方块数据为空");
                    return;
                }

                //使方块生成掉落物
                block.OutputDrops(n.pos.To3());

                //摧毁方块
                GameCallbacks.CallOnBlockDestroyed(n.pos, n.layer, block.data);
            }
            //如果不存在
            else
            {
                Debug.LogWarning($"{MethodGetter.GetCurrentMethodPath()}: 获取位置 {n.pos}({n.layer}) 的方块失败");
            }

            //将消息转发给客户端
            Server.Send(n);
        }

        //当服务器收到 NMPos 消息时的回调
        static void OnServerGetNMSetBlockMessage(NetworkConnection conn, NMSetBlock n)
        {
            if (GScene.name != SceneNames.gameScene)
                return;

            //将消息转发给客户端
            Server.Send(n);
        }

        //当服务器收到 NMPos 消息时的回调
        static void OnServerGetNMSetBlockCustomData(NetworkConnection conn, NMSetBlockCustomData n)
        {
            if (GScene.name != SceneNames.gameScene)
                return;

            //将消息转发给客户端
            Server.Send(n);
        }

        //当客户端收到 NMPos 消息时的回调
        static void OnClientGetNMDestroyBlockMessage(NMDestroyBlock n) => MethodAgent.TryRun(() =>
        {
            if (GScene.name != SceneNames.gameScene)
                return;

            //把指定位置的方块摧毁
            map.DestroyBlock(n.pos, n.layer, true);

            GAudio.Play(AudioID.DestroyBlock);
        }, true);

        //当服务器收到 NMPos 消息时的回调
        static void OnClientGetNMSetBlockMessage(NMSetBlock n)
        {
            if (GScene.name != SceneNames.gameScene)
                return;

            map.SetBlock(n.pos, n.layer, n.block == null ? null : ModFactory.CompareBlockDatum(n.block), n.customData, true);
        }

        //当服务器收到 NMPos 消息时的回调
        static void OnClientGetNMSetBlockCustomData(NMSetBlockCustomData n)
        {
            if (GScene.name != SceneNames.gameScene)
                return;

            if (map.TryGetBlock(n.pos, n.layer, out Block block))
            {
                Debug.Log($"n: {n.pos}, {n.layer}");
                Debug.Log($"b: {block.pos}, {block.layer}");
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

        public void SummonEntity(Vector3 pos, string id, string saveId = null, bool intoSandbox = true, float? health = null, string customData = null)
        {
            saveId ??= Tools.randomGUID;

            //TODO: NMSummon 改为 ServerRun
            //发送生成消息给服务器
            Client.Send<NMSummon>(new(pos, id, saveId, intoSandbox, health, customData));
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

        public void RecoverSandbox(Vector2Int index)
        {
            //如果已经在回收就取消
            foreach (var item in recoveringSandboxes)
                if (item == index)
                    return;

            recoveringSandboxes.Add(index);





            for (int i = map.chunks.Count - 1; i >= 0; i--)
            {
                Chunk chunk = map.chunks[i];

                if (chunk.sandboxIndex == index)
                    map.chunkPool.Recover(chunk);
            }

            bool did = false;
            for (int i = generatedExistingSandboxes.Count - 1; i >= 0; i--)
            {
                Sandbox sb = generatedExistingSandboxes[i];
                if (sb.index == index)
                {
                    did = true;

                    //删除实体
                    List<Entity> entities = EntityCenter.all;

                    for (int e = entities.Count - 1; e >= 0; e--)
                    {
                        Entity entity = entities[e];

                        if (entity.sandboxIndex == sb.index)
                        {
                            entity.Recover();
                        }
                    }

                    //从已生成列表去除
                    generatedExistingSandboxes.RemoveAt(i);
                    break;
                    ////Debug.Log($"回收了沙盒 {index}");
                }
            }

            recoveringSandboxes.Remove(index);

            if (!did)
                Debug.LogWarning($"沙盒 {index} 不存在");
        }

        public void GenerateExistingSandbox(Sandbox sandbox, Action<Sandbox> afterGenerating = null, Action<Sandbox> ifGenerated = null, ushort waitScale = 80)
        {
            StartCoroutine(IEGenerateExistingSandbox(sandbox, afterGenerating, ifGenerated, waitScale));
        }

        private IEnumerator IEGenerateExistingSandbox(Sandbox sandbox, Action<Sandbox> afterGenerating, Action<Sandbox> ifGenerated, ushort waitScale)
        {
            /* -------------------------------------------------------------------------- */
            /*                                   检查生成状况                                   */
            /* -------------------------------------------------------------------------- */
            if (generatedExistingSandboxes.Any(p => p.index == sandbox.index && p.generatedAlready))
            {
                Debug.LogWarning($"沙盒 {sandbox.index} 已生成, 请勿频繁生成");
                ifGenerated?.Invoke(sandbox);
                yield break;
            }

            Debug.Log($"开始生成已有沙盒 {sandbox.index}");

            generatingExistingSandbox = true;

            GameCallbacks.CallBeforeGeneratingExistingSandbox(sandbox);





            /* -------------------------------------------------------------------------- */
            /*                                    正式生成                                    */
            /* -------------------------------------------------------------------------- */
            int xDelta = Sandbox.GetMiddleX(sandbox.index);
            int yDelta = Sandbox.GetMiddleY(sandbox.index);

            foreach (var save in sandbox.saves)
            {
                BlockData block = ModFactory.CompareBlockDatum(save.blockId);

                for (int i = 0; i < save.locations.Count; i++)
                {
                    BlockSave_Location location = save.locations[i];

                    Map.instance.SetBlock(new(location.pos.x + xDelta, location.pos.y + yDelta), location.layer, block, location.customData, false);

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
                for (int i = 0; i < sandbox.entities.Count; i++)
                {
                    SummonEntity(sandbox.entities[i]);
                }
            }





            /* -------------------------------------------------------------------------- */
            /*                                    完成后事项                                   */
            /* -------------------------------------------------------------------------- */
            generatingExistingSandbox = false;
            afterGenerating?.Invoke(sandbox);
            generatedExistingSandboxes.Add(sandbox);
            ////Performance.CollectMemory();
            GameCallbacks.CallAfterGeneratingExistingSandbox(sandbox);
        }

        public Sandbox GenerateNewSandbox(Vector2Int index, string biomeId = null)
        {
            /* -------------------------------------------------------------------------- */
            /*                                    生成前检查                                   */
            /* -------------------------------------------------------------------------- */
            lock (generatingNewSandboxes)
            {
                if (generatingNewSandboxes.Any(p => p == index))
                {
                    Debug.LogWarning($"新沙盒 {index} 正在生成, 请勿频繁生成!");
                    return null;
                }

                foreach (var temp in GFiles.world.sandboxData)
                {
                    if (temp.index == index && temp.generatedAlready)
                    {
                        Debug.LogWarning($"要生成的新沙盒 {index} 已存在于世界, 请勿频繁生成!");
                        return temp;
                    }
                }

                generatingNewSandboxes.Add(index);
            }



            /* -------------------------------------------------------------------------- */
            /*                                    初始化数据                                   */
            /* -------------------------------------------------------------------------- */
            MapGeneration generation = new(GFiles.world.basicData.seed, index, biomeId);
            GameCallbacks.CallBeforeGeneratingNewSandbox(generation);



            /* -------------------------------------------------------------------------- */
            /*                                    生成方块                                    */
            /* -------------------------------------------------------------------------- */
            // 遍历每个点
            for (int x = generation.minPoint.x; x < generation.maxPoint.x; x++)
                for (int y = generation.minPoint.y; y < generation.maxPoint.y; y++)
                {
                    //当前遍历到的点
                    Vector2Int pos = new(x, y);

                    Parallel.ForEach(generation.biome.blocks, g =>
                    {
                        if (g == null || !g.initialized)
                            return;

                        if (Tools.Prob100(g.rules.probability, generation.random))
                        {
                            if (g.attached.blockId.IsNullOrWhiteSpace() || generation.sandbox.GetBlock(pos + g.attached.offset, g.attached.layer)?.block.blockId == g.attached.blockId)
                            {
                                foreach (var range in g.ranges)
                                {
                                    if (range.min.IsNullOrWhiteSpace() || range.max.IsNullOrWhiteSpace() || y.IInRange(range.min.ComputeNum(generation.rules), range.max.ComputeNum(generation.rules)))
                                    {
                                        generation.sandbox.AddPos(g.id, pos.ToInt3(BlockLayerHelp.Parse(g.layer)), g.areas, true);
                                    }
                                }
                            }
                        }
                    });


                    /* -------------------------------------------------------------------------- */
                    /*                                    生成战利品                                   */
                    /* -------------------------------------------------------------------------- */
                    MapGeneration.LootGeneration(generation, pos);
                };




            /* -------------------------------------------------------------------------- */
            /*                                    生成结构                                    */
            /* -------------------------------------------------------------------------- */
            if (generation.biome.structures?.Length > 0)
            {
                //全部生成完后再生成结构
                for (int x = generation.minPoint.x; x < generation.maxPoint.x; x++)
                    for (int y = generation.minPoint.y; y < generation.maxPoint.y; y++)
                    {
                        //当前遍历到的点
                        Vector2Int pos = new(x, y);

                        Parallel.ForEach(generation.biome.structures, l =>
                        {
                            if (Tools.Prob100(l.structure.probability, generation.random))
                            {
                                //检查空间是否足够
                                if (l.structure.mustEnough)
                                {
                                    foreach (var fixedBlock in l.structure.fixedBlocks)
                                    {
                                        if (generation.sandbox.GetBlock(pos + fixedBlock.offset, fixedBlock.layer) != null)
                                        {
                                            goto stopGeneration;
                                        }
                                    }
                                }

                                //检查是否满足所有需求
                                foreach (var require in l.structure.require)
                                {
                                    if (!generation.sandbox.GetBlockOut(pos + require.offset, require.layer, out BlockSave_Location temp) || temp.block.blockId != require.blockId)
                                    {
                                        goto stopGeneration;
                                    }
                                }

                                //如果可以就继续
                                Parallel.ForEach(l.structure.fixedBlocks, attached =>
                                {
                                    var tempPos = pos + attached.offset;

                                    generation.sandbox.AddPos(attached.blockId, tempPos.ToInt3(BlockLayerHelp.Parse(attached.layer)), true);
                                });

                            stopGeneration:
                                return;
                            }
                        });
                    };
            }





            /* -------------------------------------------------------------------------- */
            /*                                    生成传送点                                   */
            /* -------------------------------------------------------------------------- */
            Vector3Int portalMiddle = new(generation.maxPoint.x / 2, generation.maxPoint.y * 3 / 4, BlockLayerHelp.Parse(BlockLayer.Wall));
            generation.sandbox.AddPos(BlockID.Portal, portalMiddle, true);
            generation.sandbox.AddPos(BlockID.PortalBase, portalMiddle + new Vector3Int(0, -1), true);
            generation.sandbox.AddPos(BlockID.PortalBase, portalMiddle + new Vector3Int(-1, -1), true);
            generation.sandbox.AddPos(BlockID.PortalBase, portalMiddle + new Vector3Int(1, -1), true);





            if (index == Vector2Int.zero)
            {
                //如是初始沙盒, 生成 Nick
                var nick = ModFactory.CompareEntity(EntityID.Nick);
                EntitySave nickSave = new()
                {
                    id = nick.id,
                    pos = generation.sandbox.spawnPoint + new Vector2Int(10, 0)
                };
                generation.sandbox.entities.Add(nickSave);
            }

            MethodAgent.RunOnMainThread(() =>
            {
                generation.Finish();
                GFiles.SaveAllDataToFiles();
                //Performance.CollectMemory();
                GameCallbacks.CallAfterGeneratingNewSandbox(generation.sandbox);
            });

            lock (generatingNewSandboxes)
            {
                generatingNewSandboxes.Remove(index);
            }
            return generation.sandbox;
        }
    }

    public class MapGeneration
    {
        public static Action<MapGeneration, Vector2Int> LootGeneration = (generation, pos) =>
        {
            if (pos.y == generation.surfaceExtra1)
            {
                /* ----------------------------------- 木桶 ----------------------------------- */
                if (Tools.Prob100(6, generation.random))
                {
                    JToken[] group = new JToken[28];

                    Parallel.For(0, group.Length, i =>
                    {
                        //每个格子都有 >=40% 的概率为空
                        if (Tools.Prob100(40, generation.random))
                            return;

                        Mod mod = null;
                        while (mod == null)
                        {
                            mod = ModFactory.mods.Extract();
                            if (mod.items.Count == 0)
                                mod = null;
                        }
                        ItemData item = null;
                        for (var inner = 0; inner < mod.items.Count / 5 + 1; inner++)  //最多尝试抽取 1/5 的物品
                        {
                            item = mod.items.Extract();

                            if (item == null)
                                continue;

                            //如果是木桶的战利品就通过
                            if (item.GetTag("ori:loot.barrel").hasTag)
                                break;
                            else
                                item = null;
                        }
                        if (item != null)  //如果获取失败了, 这个格子也会为空
                            group[i] = JToken.FromObject(item.ToExtended());
                    });

                    JObject jo = new();

                    jo.AddObject("ori:container");
                    jo["ori:container"].AddObject("items");
                    jo["ori:container"]["items"].AddArray("array", group);

                    generation.sandbox.AddPos(BlockID.Barrel, pos.ToInt3(BlockLayerHelp.Parse(BlockLayer.Background)), true, jo.ToString(Formatting.None));
                }

                /* ----------------------------------- 木箱 ----------------------------------- */
                if (Tools.Prob100(2, generation.random))
                {
                    JToken[] group = new JToken[21];

                    Parallel.For(0, group.Length, i =>
                    {
                        //每个格子都有 >=65% 的概率为空
                        if (Tools.Prob100(65, generation.random))
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
                            var extendedItem = item.ToExtended();

                            if (item.id == ItemID.ManaStone)
                            {
                                var jo = new JObject();
                                jo.AddObject("original:mana_container");
                                jo["original:mana_container"].AddProperty("totalMana", generation.random.Next(0, 100));
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

                    generation.sandbox.AddPos(BlockID.WoodenChest, pos.ToInt3(BlockLayerHelp.Parse(BlockLayer.Background)), true, jo.ToString(Formatting.None));
                }
            }
        };





        public Random random;
        public int originalSeed;
        public int actualSeed;
        public Vector2Int index;
        public Vector2Int size;
        public BiomeData biome;
        public Vector2Int maxPoint;
        public Vector2Int minPoint;
        public Sandbox sandbox;
        public ComputationRules rules;
        public int yOffset;
        public int surface;
        public int surfaceExtra1;





        public void Finish()
        {
            sandbox.generatedAlready = true;
            GFiles.world.AddSandbox(sandbox);
        }





        public MapGeneration(int seed, Vector2Int index, string biomeId = null)
        {
            this.index = index;

            //乘数是为了增加 index 差, 避免比较靠近的沙盒生成一致
            actualSeed = seed + index.x * 2 + index.y * 4;

            //改变随机数种子, 以确保同一种子的地形一致, 不同沙盒地形不一致
            random = new(actualSeed);



            if (biomeId.IsNullOrEmpty())
            {
                List<BiomeData> biomeList = new();
                int difficulty = Mathf.Max(Mathf.Abs(index.x), Mathf.Abs(index.y));

                // //添加候选群系, 只包含 Rich群系
                // if (forceCenter)
                //     foreach (Mod mod in ModFactory.mods)
                //         foreach (BiomeData b in mod.biomes)
                //             if (b.Rich().hasTag)
                //                 biomeList.Add(b);

                // //如果不强制生成 Rich 群系 或 没有 Rich群系, 就随便一个群系
                if (biomeList.Count == 0)
                    foreach (Mod mod in ModFactory.mods)
                        foreach (BiomeData b in mod.biomes)
                            biomeList.Add(b);

                biomeList = biomeList.Shuffle();


                //确定沙盒的群系
                int closest = int.MaxValue;
                foreach (var currentBiome in biomeList)
                {
                    int delta = Mathf.Abs(currentBiome.difficulty - difficulty);
                    if (delta < closest)
                    {
                        closest = delta;
                        biome = currentBiome;
                    }
                }
            }




            //确定大小并排除奇数
            size = new(random.Next(biome.minSize.x, biome.maxSize.x).IRange(10, Sandbox.place.x),
                                random.Next(biome.minSize.y, biome.maxSize.y).IRange(10, Sandbox.place.y));
            if (size.x % 2 != 0) size.x++;
            if (size.y % 2 != 0) size.y++;



            //边际 (左下右上)
            maxPoint = new(size.x / 2, size.y / 2);
            minPoint = -maxPoint;



            //使空岛有 y轴 偏移
            yOffset = random.Next(15, 35);
            surface = maxPoint.y - yOffset;
            surfaceExtra1 = surface + 1;

            sandbox = new()
            {
                biome = biome.id,
                size = size,
                index = index,
                spawnPoint = new(Sandbox.GetMiddleX(index), Sandbox.GetMiddleY(index) + surface + 3)
            };



            rules = BiomeData_Block_Range.GetRules(minPoint.y, surface, maxPoint.y);
        }
    }
}
