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

        public WeatherParticleSystem weatherParticleSystem { get; protected set; }
        public ParticleSystem weatherParticle { get; protected set; }
        public ParticleSystem.MainModule weatherParticleMain;
        public ParticleSystem.EmissionModule weatherParticleEmission;
        public ParticleSystem.TriggerModule weatherParticleTrigger;
        public ParticleSystem parrySuccessParticle { get; protected set; }
        public Light2D globalLight { get; protected set; }
        public static Action AfterPreparation = () => { };
        public static Action OnUpdate = () => { };




#if UNITY_EDITOR
        [Button("设置是否是上午")] void Editor_SetIsMorning(bool value) => GTime.isMorning = value;
        [Button("设置时间")] void Editor_SetTime(float value) => GTime.time = value;
        [Button("设置时间流速")] void Editor_SetTimeSpeed(float value) => GTime.timeSpeed = value;
#endif



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
        [Button("设置天气")] private void EditorSetWeather(string id) => GWeather.SetWeather(id);
        [Button("随机更改天气")] private void EditorChangeWeatherRandomly() => RandomUpdater.ChangeWeatherRandomly();
#endif

        //随机更新只在服务器进行, 无需同步至客户端
        public byte randomUpdateProbability => RandomUpdater.randomUpdateProbability;
        #endregion














        public static int GetRegionUnlockingCost(Vector2Int index)
        {
            return Math.Abs(index.x) * 250 + Math.Abs(index.y) * 150;
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
                //设置全局光照亮度
                globalLight.intensity = Mathf.Clamp(GTime.time * 2 / GTime.timeOneDay, 0.1f, 0.85f);

                //设置天空颜色
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
            GameObject wpsGo = GameObject.Find("Weather Particle System");
            GameObject psGo = GameObject.Find("Parry Success Particle System");

            if (glGo)
            {
                globalLight = glGo.GetComponent<Light2D>();
            }
            if (wpsGo)
            {
                weatherParticleSystem = wpsGo.GetComponent<WeatherParticleSystem>();
                weatherParticle = weatherParticleSystem.system;
                weatherParticleMain = weatherParticle.main;
                weatherParticleEmission = weatherParticle.emission;
                weatherParticleTrigger = weatherParticle.trigger;

                //编辑粒子动画
                weatherParticle.textureSheetAnimation.Clear();
                weatherParticle.textureSheetAnimation.AddSprite(ModFactory.CompareTexture("ori:rain_particle_0").sprite);
                weatherParticle.textureSheetAnimation.AddSprite(ModFactory.CompareTexture("ori:rain_particle_1").sprite);
                weatherParticle.textureSheetAnimation.AddSprite(ModFactory.CompareTexture("ori:rain_particle_2").sprite);
                weatherParticle.textureSheetAnimation.AddSprite(ModFactory.CompareTexture("ori:rain_particle_3").sprite);
                weatherParticle.textureSheetAnimation.AddSprite(ModFactory.CompareTexture("ori:rain_particle_4").sprite);
                weatherParticle.textureSheetAnimation.AddSprite(ModFactory.CompareTexture("ori:rain_particle_5").sprite);
            }
            if (psGo)
            {
                parrySuccessParticle = psGo.GetComponent<ParticleSystem>();
            }
        }

        protected override void Start()
        {
            //初始化天气系统
            GWeather.InitWeatherSystem();

            if (Server.isServer)
            {
                InitializeWorld(GFiles.world);

                //加载世界的时间、天气数据
                GTime.isMorning = GFiles.world.basicData.isAM;
                GTime.time = GFiles.world.basicData.time;
                GTime.totalTime = GFiles.world.basicData.totalTime;
                GWeather.SetWeather(GFiles.world.basicData.weather);
            }
            else
            {
                GWeather.TempWeather(null);
            }

            //绑定随机更新
            RandomUpdater.Init();


            base.Start();
            AfterPreparation();
        }




        public void SetGlobalVolumeBloomToSunny() => Tools.instance.mainCameraController.SetGlobalVolumeBloom(0.95f, 0.5f);
        public void SetGlobalVolumeBloomToRain() => Tools.instance.mainCameraController.SetGlobalVolumeBloom(0.8f, 2.5f);



        public void SetGlobalVolumeColorAdjustmentsToSunny() => Tools.instance.mainCameraController.SetGlobalVolumeColorAdjustments(new(0.75f, 0.66f, 0.66f), 1.52f, 8.5f);
        public void SetGlobalVolumeColorAdjustmentsToAcidRain() => Tools.instance.mainCameraController.SetGlobalVolumeColorAdjustments(new(0.8f, 1f, 0.92f), 1.05f, 6);
        public void SetGlobalVolumeColorAdjustmentsToRain() => Tools.instance.mainCameraController.SetGlobalVolumeColorAdjustments(new(0.75f, 0.66f, 0.66f), 1.45f, 5);








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
                switch (GInit.gameVersion, GFiles.world.basicData.gameVersion)
                {
                    case ("0.7.9", "0.7.8"):
                        //TODO: 改变存档里的版本号
                        break;

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
            if (GScene.name != SceneNames.GameScene)
                return;

            if (map.TryGetBlock(n.pos, n.isBackground, out Block block))
            {
                //把自定义数据写入存档
                block.WriteCustomDataToSave(n.customData);
            }
            else
            {
                Debug.LogError("未找到要求的方块, 自定义数据设置失败");
            }

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
                Debug.Log($"设置了方块 {n.pos} [{n.isBackground}] 的自定义数据: {n.customData}");
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

        public void SummonEntity(EntitySave save)
        {
            SummonEntity(save.pos, save.id, save.saveId, false, save.health, save.customData);
        }

        public void SummonEntityCallback(Vector3 pos, string id, Action<Entity> callback, string saveId = null, bool saveIntoRegion = true, int? health = null, string customData = null)
        {
            saveId ??= Tools.randomGUID;

            //绑定回调
            EntityCenter.BindGenerationEvent(saveId, callback);

            //发送生成消息给服务器
            Client.Send<NMSummon>(new(pos, id, saveId, saveIntoRegion, health, customData));
        }

        public void SummonBullet(Vector3 pos, string id, Vector2 velocity, uint ownerId, string saveId = null, bool saveIntoRegion = true, string customData = null)
        {
            SummonEntityCallback(pos, id, entity =>
            {
                var bullet = (Bullet)entity;
                bullet.ServerSetVelocity(velocity);
                bullet.ownerId = ownerId;
            }, saveId, saveIntoRegion, null, customData);
        }

        public void SummonBulletCallback(Vector3 pos, string id, Vector2 velocity, uint ownerId, Action<Entity> callback, string saveId = null, bool saveIntoRegion = true, string customData = null)
        {
            SummonEntityCallback(pos, id, entity =>
            {
                var bullet = (Bullet)entity;
                bullet.ServerSetVelocity(velocity);
                bullet.ownerId = ownerId;
                callback(entity);
            }, saveId, saveIntoRegion, null, customData);
        }

        public void LeftGame()
        {
            //保存数据
            GFiles.SaveAllDataToFiles();

            //如果是服务器
            if (Server.isServer)
            {
                //截图
                GameUI.canvas.gameObject.SetActive(false);
                ScreenTools.CaptureSquare(GFiles.world.worldImagePath, () =>
                {
                    GameUI.canvas.gameObject.SetActive(true);

                    //关闭 Host
                    ShutDownHostWithMask();
                });
            }
            //如果是单纯的客户端
            else
            {
                //关闭客户端
                ShutDownClientWithMask();
            }
        }

        public void ShutDownHostWithMask()
        {
            //生成蒙版
            (var panel, _) = GameUI.LeavingGameMask(new((_, _) =>
            {
                //清除方块防止警告
                if (Map.HasInstance())
                {
                    Map.instance.RecycleChunks();
                }

                //关闭 Host
                ManagerNetwork.instance.StopHost();
            }), null);

            //显示蒙版
            panel.OnUpdate += x => GameUI.SetUILayerToTop(x);
            panel.CustomMethod("fade_in", null);
        }

        public void ShutDownClientWithMask()
        {
            //生成蒙版
            (var panel, _) = GameUI.LeavingGameMask(new((_, _) =>
            {
                Client.Disconnect();
            }), null);

            //显示蒙版
            panel.OnUpdate += x => GameUI.SetUILayerToTop(x);
            panel.CustomMethod("fade_in", null);
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


            //回收该区域的所有区块
            for (int i = map.chunks.Count - 1; i >= 0; i--)
            {
                Chunk chunk = map.chunks[i];

                if (chunk.regionIndex == index)
                    map.chunkPool.Recycle(chunk);
            }

            //回收该区域的所有实体
            bool did = false;
            for (int i = generatedExistingRegions.Count - 1; i >= 0; i--)
            {
                Region region = generatedExistingRegions[i];
                if (region.index == index)
                {
                    did = true;

                    //删除实体
                    Entity[] entities = EntityCenter.all.ToArray();

                    for (int e = entities.Length - 1; e >= 0; e--)
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
            Map.instance.UpdateAllBlocks();





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
                IslandGeneration islandGeneration = generation.NewIsland(centerPoint);


                //生成 Direct
                islandGeneration.GenerateDirectBlocks();

                //生成 Perlin
                islandGeneration.GeneratePerlinBlocks();

                //生成 PostProcess
                islandGeneration.GeneratePostProcessBlocks();

                //生成战利品
                IslandGeneration.LootGeneration(islandGeneration);

                //生成结构
                islandGeneration.GenerateStructures();

                //确定出生点
                if (centerPoint == Vector2Int.zero)
                {
                    var middleX = Region.GetMiddleX(generation.index);
                    var middleY = Region.GetMiddleY(generation.index);

                    try
                    {
                        //更新最高点函数
                        islandGeneration.UpdateHighestPointFunction();

                        //设置出生点
                        generation.region.spawnPoint = new(middleX, middleY + islandGeneration.wallHighestPointFunction[0] + 3);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"选取出生点时失败, 堆栈如下: {ex}");
                        generation.region.spawnPoint = new(middleX, middleY);
                    }
                }
            }

            //生成矿石团块
            IslandGeneration.OreClumpsGeneration(generation);

            //生成传送点
            generation.GeneratePortal();

            //生成区域边界
            generation.GenerateBoundaries();

            //生成实体（脚本的绑定）
            IslandGeneration.EntitiesGeneration(generation);



            /* ----------------------------------- 完成 ----------------------------------- */
            MethodAgent.RunOnMainThread(() =>
            {
                generation.Finish();
                GFiles.SaveAllDataToFiles();
                GameCallbacks.CallAfterGeneratingNewRegion(generation.region);
            });

            lock (generatingNewRegions)
            {
                generatingNewRegions.Remove(index);
            }
            return generation.region;
        }
    }
}
