using GameCore.High;
using SP.Tools;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Linq;
using System.IO;
using SP.Tools.Unity;
using System.Text;
using GameCore.Converters;
using TMPro;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using System.Runtime.Serialization;
using static GameCore.Network.SerializationSurrogates;
using GameCore.Network;

namespace GameCore.UI
{
    public class InternalUIAdder : SingletonClass<InternalUIAdder>
    {
        public class Mod_Info_WithPath
        {
            public string path;
            public Mod_Info info;

            public Mod_Info_WithPath(string path, Mod_Info info)
            {
                this.path = path;
                this.info = info;
            }
        }

        public List<WorldBasicData> worldFiles = new();
        public List<Mod_Info_WithPath> modDirs = new();

        public ScrollViewIdentity modScrollView;
        public PanelIdentity modConfiguringPanel;
        public Mod_Info_WithPath configuringModDir;

        public static Action AfterRefreshWorldList = () => { };
        public static Action AfterRefreshModView = () => { };

        public PanelIdentity worldConfigPanel;
        public static bool hasShowedModLoadingInterface;

        public List<ParallaxBackground> parallaxBackgrounds;



        /* -------------------------------------------------------------------------- */
        /*                                     状态文本                                     */
        /* -------------------------------------------------------------------------- */
        public static float statusTextFadeOutTime = 5;
        public float statusTextFadeOutWaitedTime;
        public bool preparingToFadeOutStatusText;
        public TextIdentity statusText;




        public void SetStatusText(string text)
        {
            statusText.AfterRefreshing += t => t.text.text = text;
            statusText.RefreshUI();

            //杀死淡出动画
            Tools.KillTweensOf(statusText.text);

            //播放淡入动画
            if (statusText.text.color.a == 1)
                statusText.text.SetAlpha(0);
            GameUI.FadeIn(statusText.text);

            //准备播放淡出动画
            statusTextFadeOutWaitedTime = 0;

            if (!preparingToFadeOutStatusText)
            {
                StartCoroutine(PrepareToFadeOutStatusText());
            }
        }

        IEnumerator PrepareToFadeOutStatusText()
        {
            preparingToFadeOutStatusText = true;

            //等待淡出间隔
            while (statusTextFadeOutWaitedTime < statusTextFadeOutTime)
            {
                statusTextFadeOutWaitedTime += Performance.frameTime;

                yield return null;
            }

            //杀死淡入动画
            Tools.KillTweensOf(statusText.text);

            statusText.text.SetAlpha(1);
            GameUI.FadeOut(statusText.text);
            preparingToFadeOutStatusText = false;
        }

        public void RefreshWorldFiles()
        {
            worldFiles.Clear();

            string[] vs = IOTools.GetFoldersInFolder(GInit.worldPath, true);
            vs = vs.Where(p => File.Exists(World.GetBasicDataPath(p))).ToArray();

            foreach (string folder in vs)
            {
                var basicData = JsonUtils.LoadTypeFromJsonPath<WorldBasicData>(World.GetBasicDataPath(folder));

                worldFiles.Add(basicData);
            }
        }

        public void RefreshMods()
        {
            modDirs.Clear();

            //只搜索 modsPath, 以防止 original 被关闭
            string[] folders = IOTools.GetFoldersInFolder(GInit.modsPath, true);

            List<string> info = new();

            foreach (string folder in folders)
            {
                string infoPath = ModFactory.GetInfoPath(folder);

                if (File.Exists(infoPath))
                    info.Add(infoPath);
            }

            foreach (var path in info)
            {
                modDirs.Add(new(path, ModLoading.LoadInfo(path, ModFactory.GetIconPath(IOTools.GetParentPath(path)))));
            }
        }

        protected async override void Start()
        {
            base.Start();

            #region 状态文本
            {
                statusText = GameUI.AddText(UPC.Down, "ori:text.player_status");
                statusText.SetAPosY(80);
                statusText.SetSizeDeltaY(40);
                statusText.text.SetFontSize(18);
                statusText.text.raycastTarget = false;
                statusText.gameObject.SetActive(false);
                statusText.OnUpdate += t => GameUI.SetUILayerToFirst(t);
            }
            #endregion



            switch (GScene.name)
            {
                case SceneNames.MainMenu:
                    {
                        #region 游戏初始化界面
                        if (!hasShowedModLoadingInterface)
                        {
                            float earliestExitTime = Tools.time + 3;

                            var panel = GameUI.AddPanel("ori:panel.first_scene_panel");
                            panel.panelImage.color = Tools.HexToColor("#070714");

                            var (barBg, barFull, mascotImage, progressText) = GameUI.GenerateLoadingBar(
                                            UPC.StretchBottom,
                                            "ori:image.mod_loading_bar_bg",
                                            "ori:image.mod_loading_bar_full",
                                            "ori:image.mod_loading_bar_mascot",
                                            "ori:mod_loading_progress.mod_loading_progress_text",
                                            "ori:loading_bar_2",
                                            "ori:loading_bar_2",
                                            "ori:mod_loading_mascot",
                                            15, 0,
                                            new(0, 24), new(50, 50),
                                            () => (float)ModFactory.mods.Length / (float)ModFactory.modCountFound,
                                            () =>
                                            {
                                                if (ModFactory.mods.Length != ModFactory.modCountFound)
                                                    return $"Loading... {ModFactory.mods.Length}/{ModFactory.modCountFound}";
                                                else if (!SyncPacker.initialized)
                                                    return "正在等待网络系统初始化";
                                                else
                                                    return "Loading... 100%";
                                            }, //TODO; Make Loading into text comparison
                                            panel.transform);

                            //刷新吉祥物位置
                            mascotImage.OnUpdate += img =>
                            {
                                var progressFloat = (float)ModFactory.mods.Length / (float)ModFactory.modCountFound;

                                mascotImage.SetAPosX(progressFloat * GameUI.canvasRT.sizeDelta.x);
                            };

                            barBg.SetAPosY(barBg.sd.y / 2);
                            progressText.SetSizeDelta(400, 30);

                            var houseImage = GameUI.AddImage(UPC.Middle, "ori:image.loading_house", "ori:loading_house_0", panel);
                            houseImage.SetSizeDelta(300, 300);
                            houseImage.SetAPos(0, 75);

                            Sprite[] sprites = new[]
                            {
                                ModFactory.CompareTexture("ori:loading_house_0").sprite,
                                ModFactory.CompareTexture("ori:loading_house_1").sprite,
                            };

                            var _ = EasyAnim.PlaySprites(1.5f, sprites, sprite =>
                            {
                                if (houseImage)
                                {
                                    houseImage.image.sprite = sprite;
                                    return true;
                                }

                                return false;
                            });

                            //等待模组加载
                            while (ModFactory.mods.Length < ModFactory.modCountFound)
                                await UniTask.NextFrame();

                            //等待网络系统
                            while (!SyncPacker.initialized)
                                await UniTask.NextFrame();

#if !DEBUG
                            //等待动画播放完成
                            while (Tools.time < earliestExitTime)
                                await UniTask.NextFrame();
#endif

                            hasShowedModLoadingInterface = true;



                            //播放淡出动画
                            float fadeOutDuration = 2;
                            GameUI.FadeOutGroup(panel, true, fadeOutDuration, new(() => Destroy(panel.gameObject)));

#if !DEBUG
                            //等待动画结束后继续执行
                            await fadeOutDuration;
#endif
                        }
                        #endregion

                        #region 滚动背景
                        var c = UObjectTools.CreateComponent<ScrollingBackground>("ScrollingBackground");
                        c.defaultPoint = Vector2.zero;
                        c.scrollSpeed = -7;
                        c.bound = -64;
                        float appearDuration = 3;

                        c.AddRenderers("ori:scroll_background", 8, 0);
                        foreach (var item in c.renderers)
                        {
                            item.color = new(0, 0, 0, 0);
                            var _ = item.DOColor(Color.white, appearDuration);
                        }
#if !DEBUG
                        //等待动画播放完成
                        await appearDuration;
#endif
                        #endregion

                        #region 游戏信息
                        var gameInfoText = GameUI.AddText(UPC.LowerRight, "ori:text.game_info");
                        gameInfoText.SetSizeDelta(350, 200);
                        gameInfoText.SetAPos(-gameInfoText.sd.x / 2, gameInfoText.sd.y / 2);
                        gameInfoText.text.SetFontSize(16);
                        gameInfoText.text.alignment = TMPro.TextAlignmentOptions.BottomRight;
                        gameInfoText.AfterRefreshing += t =>
                        {
                            var sb = Tools.stringBuilderPool.Get();

                            sb.Append(GameUI.CompareText("ori:game_version").text).AppendLine(GInit.gameVersion);
                            sb.Append(GameUI.CompareText("ori:engine_version").text).AppendLine(GInit.unityVersion);
                            sb.Append(GameUI.CompareText("ori:mod_count").text).AppendLine(ModFactory.mods.Length.ToString());

                            t.text.text = sb.ToString();
                            Tools.stringBuilderPool.Recover(sb);
                        };
                        #endregion

                        var firstPanel = GameUI.AddPanel("ori:panel.first");
                        var joinGamePanel = GameUI.AddPanel("ori:panel.join_game", GameUI.canvasRT, true);
                        var settingsPanel = GameUI.AddPanel("ori:panel.settings", GameUI.canvasRT, true);
                        var setLanguagePanel = GameUI.AddPanel("ori:panel.settings.language", GameUI.canvasRT, true);
                        var setPlayerSkinNamePanel = GameUI.AddPanel("ori:panel.settings.playerSkinName", GameUI.canvasRT, true);
                        var chooseWorldPanel = GameUI.AddPanel("ori:panel.choose_world", GameUI.canvasRT, true);
                        var createWorldPanel = GameUI.AddPanel("ori:panel.create_world", GameUI.canvasRT, true);
                        var modViewPanel = GameUI.AddPanel("ori:panel.mod_view", GameUI.canvasRT, true);

                        GameUI.SetPage(firstPanel);

                        /* -------------------------------------------------------------------------- */
                        /*                                    First                                   */
                        /* -------------------------------------------------------------------------- */
                        GameUI.AddButton(UPC.Middle, "ori:button.choose_world_panel", firstPanel).OnClickBind(() => GameUI.SetPage(chooseWorldPanel)).rt.AddLocalPosY(10);
                        GameUI.AddButton(UPC.Middle, "ori:button.join_game_panel", firstPanel).OnClickBind(() => GameUI.SetPage(joinGamePanel)).rt.AddLocalPosY(-50);
                        GameUI.AddButton(UPC.Middle, "ori:button.settings_panel", firstPanel).OnClickBind(() => GameUI.SetPage(settingsPanel)).rt.AddLocalPosY(-110);
                        GameUI.AddButton(UPC.Down, "ori:button.quit_game", firstPanel).OnClickBind(() => { GInit.Quit(); }).AddAPosY(50);

                        /* -------------------------------------------------------------------------- */
                        /*                                  JoinGame                                  */
                        /* -------------------------------------------------------------------------- */
                        InputFieldIdentity joinIPField = GameUI.AddInputField(new(0.5f, 0.83f, 0.5f, 0.83f), "ori:inputfield.join_game_ip", joinGamePanel);
                        joinIPField.field.text = $"127.0.0.1:{Tools.defaultPort}";
                        joinIPField.field.onEndEdit.AddListener(_ =>
                        {
                            StringBuilder builder = Tools.stringBuilderPool.Get();

                            builder.Append(joinIPField.field.text);
                            builder.Replace("：", ":");
                            builder.Replace("；", ";");
                            builder.Replace("．", ".");
                            builder.Replace("。", ".");
                            builder.Replace("，", ",");
                            builder.Replace(";", ":");
                            builder.Replace(",", ".");
                            builder.Replace("{", "[");
                            builder.Replace("}", "]");
                            builder.Replace("\r\n", "");

                            joinIPField.field.text = builder.ToString();
                            Tools.stringBuilderPool.Recover(builder);
                        });

                        var lanServersShow = GameUI.AddScrollView(UPC.Middle, "ori:sv.LANServers_show", joinGamePanel);
                        GameUI.AddButton(new(0.5f, 0.43f, 0.5f, 0.43f), "ori:button.refresh_LANServers", joinGamePanel).OnClickBind(() =>
                        {
                            ManagerNetwork.instance.discovery.StopDiscovery();

                            lanServersShow.Clear();
                            Client.respones.Clear();

                            ManagerNetwork.instance.discovery.OnServerFound.AddListener(sr =>
                            {
                                if (Client.respones.ContainsKey(sr.serverId))
                                    return;

                                Client.respones.Add(sr.serverId, sr);
                                string targetIP = $"{sr.EndPoint.Address}:{Tools.defaultPort}";
                                string text = $"{sr.worldName}[{sr.version}]\n({targetIP})";

                                var lanServerButton = GameUI.AddButton(UPC.Middle, $"ori:button.LANServers_show.{sr.serverId}");
                                lanServerButton.OnClickBind(() => joinIPField.field.text = targetIP);
                                lanServerButton.buttonText.AfterRefreshing += t => t.text.text = text;
                                lanServerButton.buttonText.text.SetFontSize(16);

                                lanServersShow.AddChild(lanServerButton);
                            });

                            ManagerNetwork.instance.discovery.StartDiscovery();
                        }).SetAPosOnBySizeRight(lanServersShow, 10);

                        GameUI.AddButton(new(0.5f, 0.43f, 0.5f, 0.43f), "ori:button.join_game", joinGamePanel).OnClickBind(() =>
                        {
                            if (!joinIPField.field.text.Contains(':'))
                                return;

                            string[] splitted = joinIPField.field.text.Split(':');

                            if (splitted.Length < 2)
                                return;

                            GM.StartGameClient(new(new ArraySegment<char>(joinIPField.field.text.ToCharArray(), 0, joinIPField.field.text.LastIndexOf(':')).ToArray()), Convert.ToUInt16(splitted[^1]));
                        }).SetAPosOnBySizeDown(lanServersShow, 10);

                        GameUI.AddButton(new(0.5f, 0.1f, 0.5f, 0.1f), "ori:button.join_game_to_first", joinGamePanel).OnClickBind(() =>
                        {
                            GameUI.SetPage(firstPanel);
                            Client.Disconnect();
                        });

                        /* -------------------------------------------------------------------------- */
                        /*                                 ChooseWorld                                */
                        /* -------------------------------------------------------------------------- */
                        var chooseWorldScrollView = GameUI.AddScrollView(UPC.Middle, "ori:scrollview.choose_world", chooseWorldPanel);

                        RefreshWorldFiles();
                        RefreshWorldList(ref chooseWorldScrollView, chooseWorldPanel);

                        GameUI.AddButton(UPC.Middle, "ori:button.to_create_world", chooseWorldPanel).OnClickBind(() =>
                        {
                            GameUI.SetPage(createWorldPanel);
                        }).rt.AddLocalPosY(-(chooseWorldScrollView.rt.sizeDelta.y / 2) - 25);

                        /* ------------------------------- CreateWorld ------------------------------ */
                        var worldNameField = GameUI.AddInputField(UPC.Middle, "ori:inputfield.create_new_world_world_name", createWorldPanel);
                        var worldSeedField = GameUI.AddInputField(UPC.Middle, "ori:inputfield.create_new_world_world_seed", createWorldPanel);
                        worldSeedField.SetAPosOnBySizeDown(worldNameField, 15);
                        worldSeedField.lockType = InputFieldIdentity.LockType.IntNumber;
                        worldSeedField.field.text = Tools.randomInt.ToString();
                        //TODO: World view fix

                        ButtonIdentity create = GameUI.AddButton(UPC.Middle, "ori:button.create_new_world", createWorldPanel).OnClickBind(() =>
                        {
                            if (worldNameField.field.text.IsNullOrWhiteSpace())
                            {
                                var message = "世界名不能为空，创建失败";
                                SetStatusText(message);
                                Debug.LogError(message);
                                return;
                            }

                            foreach (var worldFile in worldFiles)
                            {
                                if (worldFile.worldName == worldNameField.field.text)
                                {
                                    var message = "存在一个同名世界，创建失败";
                                    SetStatusText(message);
                                    Debug.LogError(message);
                                    return;
                                }
                            }

                            var worldNameBuilder = Tools.stringBuilderPool.Get().Append(worldNameField.field.text);
                            StringTools.ModifySpecialPath(worldNameBuilder, "New World", "x");

                            GFiles.CreateWorld(worldSeedField.field.text.ToInt(), worldNameBuilder.ToString());
                            RefreshWorldFiles();
                            RefreshWorldList(ref chooseWorldScrollView, chooseWorldPanel);
                            GameUI.SetPage(chooseWorldPanel);
                            Tools.stringBuilderPool.Recover(worldNameBuilder);
                        });
                        create.SetAPosOnBySizeDown(worldSeedField, 50);

                        ButtonIdentity cancel = GameUI.AddButton(UPC.Middle, "ori:button.create_new_world_cancel", createWorldPanel).OnClickBind(() =>
                        {
                            GameUI.SetPage(chooseWorldPanel);
                        });
                        cancel.SetAPosOnBySizeDown(create, 15);

                        /* ------------------------------- ConfigPanel ------------------------------ */
                        worldConfigPanel = GameUI.AddPanel("ori:panel.delete_world", GameUI.canvasRT, true);
                        string pathToDelete = string.Empty;

                        ButtonIdentity worldConfigPanel_back = GameUI.AddButton(UPC.Down, "ori:button.worldConfigPanel_back", worldConfigPanel).OnClickBind(() =>
                        {
                            GameUI.SetPage(chooseWorldPanel);
                        });
                        worldConfigPanel_back.SetAPosY(worldConfigPanel_back.sd.y / 2 + 30);
                        ButtonIdentity deleteWorld = GameUI.AddButton(UPC.Middle, "ori:button.delete_world", worldConfigPanel).OnClickBind(() =>
                        {
                            //删除世界
                            IOTools.DeleteDir(pathToDelete);
                            GFiles.world = null;

                            //刷新文本
                            RefreshWorldFiles();
                            RefreshWorldList(ref chooseWorldScrollView, chooseWorldPanel);

                            //返回界面
                            GameUI.SetPage(chooseWorldPanel);
                        });
                        deleteWorld.buttonText.AfterRefreshing += t =>
                        {
                            try
                            {
                                t.text.text = GameUI.CompareText("ori:button.delete_world.text").text.Replace("{world_name}", IOTools.GetDirectoryName(pathToDelete));
                            }
                            catch
                            {

                            }
                        };
                        worldConfigPanel.CustomMethod += (type, param) =>
                        {
                            if (type == "ori:refresh")
                            {
                                pathToDelete = param;
                                deleteWorld.buttonText.RefreshUI();
                            }
                        };

                        GameUI.AddButton(new(0.5f, 0.1f, 0.5f, 0.1f), "ori:button.choose_world_to_first", chooseWorldPanel).OnClickBind(() => GameUI.SetPage(firstPanel));

                        /* -------------------------------------------------------------------------- */
                        /*                                  Settings                                 */
                        /* -------------------------------------------------------------------------- */
                        /* ----------------------------------- 返回 ----------------------------------- */
                        GameUI.AddButton(new(0.5f, 0.135f, 0.5f, 0.135f), "ori:button.settings_to_first", settingsPanel).OnClickBind(() => GameUI.SetPage(firstPanel));

                        /* -------------------------------- 玩家名称设置 -------------------------------- */
                        var playerNameField = GameUI.AddInputField(UPC.Middle, "ori:field.settings.playerName", settingsPanel);
                        playerNameField.field.text = GFiles.settings.playerName;
                        playerNameField.field.characterLimit = 20;
                        playerNameField.OnUpdate += x => x.field.placeholder.enabled = true;
                        playerNameField.field.placeholder.rectTransform.AddPosY(playerNameField.rt.sizeDelta.y / 2 + playerNameField.field.placeholder.rectTransform.sizeDelta.y / 2);
                        playerNameField.field.placeholder.color = Color.white;//TODO: 给世界名称也加上类似的机制
                        playerNameField.mask.enabled = false;
                        playerNameField.field.onValueChanged.AddListener(v =>
                        {
                            var placeholder = playerNameField.field.placeholder.GetComponent<TMP_Text>();

                            if (string.IsNullOrWhiteSpace(playerNameField.field.text))
                            {
                                placeholder.text = GameUI.CompareText($"{playerNameField.id}_empty").text;
                                return;
                            }

                            StringBuilder sb = Tools.stringBuilderPool.Get().Append(playerNameField.field.text);
                            StringTools.ModifySpecialPath(sb, "", "x");
                            playerNameField.field.text = sb.ToString();
                            Tools.stringBuilderPool.Recover(sb);

                            if (playerNameField.field.text.Length < 2)
                            {
                                placeholder.text = GameUI.CompareText($"{playerNameField.id}_short").text;
                                return;
                            }

                            placeholder.text = GameUI.CompareText($"{playerNameField.id}_success").text.Replace("{value}", playerNameField.field.text);
                            GFiles.settings.playerName = playerNameField.field.text;
                        });

                        /* ----------------------------------- 玩家皮肤 ----------------------------------- */
                        var playerSkinSetButton = GameUI.AddButton(UPC.Middle, "ori:button.settings.playerSkinName", settingsPanel).OnClickBind(() => GameUI.SetPage(setPlayerSkinNamePanel));
                        var svPlayerSkinNames = GameUI.AddScrollView(UPC.Middle, "ori:sv.playerSkinNames", setPlayerSkinNamePanel);
                        GameUI.AddButton(new(0.5f, 0.135f, 0.5f, 0.135f), "ori:button.settings.playerSkinName.back", setPlayerSkinNamePanel).OnClickBind(() => GameUI.SetPage(settingsPanel));
                        playerSkinSetButton.SetAPosOnBySizeUp(playerNameField, 35);
                        foreach (var path in IOTools.GetFoldersInFolder(GInit.playerSkinPath, true))
                        {
                            PlayerSkin data = new(path);
                            data.Modify();

                            var b = GameUI.AddButton(UPC.Middle, $"ori:button.playerSkinNames.{path}");
                            b.buttonText.autoCompareText = false;
                            b.buttonText.text.text = data.name;
                            b.OnClickBind(() =>
                            {
                                //设置设置中的语言ID
                                GFiles.settings.playerSkinName = data.name;

                                //将更改后的设置应用到文件
                                GFiles.SaveAllDataToFiles();

                                SetStatusText("保存了设置文件");
                            });

                            GameUI.GenerateSkinShow(data, b.transform);

                            svPlayerSkinNames.AddChild(b);
                        }

                        /* ----------------------------------- 语言 ----------------------------------- */
                        var languageSetButton = GameUI.AddButton(UPC.Middle, "ori:button.settings.language", settingsPanel).OnClickBind(() => GameUI.SetPage(setLanguagePanel));
                        var svLanguages = GameUI.AddScrollView(UPC.Middle, "ori:sv.languages", setLanguagePanel);
                        GameUI.AddButton(new(0.5f, 0.135f, 0.5f, 0.135f), "ori:button.settings.language.back", setLanguagePanel).OnClickBind(() => GameUI.SetPage(settingsPanel));
                        languageSetButton.SetAPosOnBySizeUp(playerSkinSetButton, 20);
                        foreach (var data in ModFactory.finalTextData)
                        {
                            var b = GameUI.AddButton(UPC.Middle, $"ori:button.languages.{data.id}");
                            b.buttonText.autoCompareText = false;
                            b.buttonText.text.text = data.textName.IsNullOrWhiteSpace() ? data.id : data.textName;
                            b.OnClickBind(() =>
                            {
                                //设置设置中的语言ID
                                GFiles.settings.langId = data.id;

                                //刷新文本
                                for (int i = 0; i < IdentityCenter.textIdentities.Count; i++)
                                {
                                    IdentityCenter.textIdentities[i].RefreshUI();
                                }
                                for (int i = 0; i < IdentityCenter.inputFieldIdentities.Count; i++)
                                {
                                    IdentityCenter.inputFieldIdentities[i].RefreshUI();
                                }

                                //将更改后的设置应用到文件
                                GFiles.SaveAllDataToFiles();

                                SetStatusText("保存了设置文件");
                            });
                            svLanguages.AddChild(b);
                        }

                        /* ---------------------------------- 控制设置 ---------------------------------- */
                        var controlsPanel = GameUI.AddPanel("ori:panel.settings.controls", GameUI.canvas.transform, true);
                        var controlsSetButton = GameUI.AddButton(UPC.Middle, "ori:button.settings.controls", settingsPanel).OnClickBind(() => GameUI.SetPage(controlsPanel));
                        GameUI.AddButton(new(0.5f, 0.135f, 0.5f, 0.135f), "ori:button.settings.controls.back", controlsPanel).OnClickBind(() => GameUI.SetPage(settingsPanel));
                        controlsSetButton.SetAPosOnBySizeDown(playerNameField, 20);

                        /* -------------------------------- 玩家光标速度设置 -------------------------------- */
                        ImageIdentity playerCursorSpeedBackground = GameUI.AddImage(UPC.Middle, "ori:image.settings.playerCursorSpeed_background", "ori:clear_button", controlsPanel);
                        playerCursorSpeedBackground.sd = new(languageSetButton.sd.x, 64);

                        var playerCursorSpeedSlider = GameUI.AddSlider(UPC.Middle, "ori:slider.settings.playerCursorSpeed", playerCursorSpeedBackground);
                        playerCursorSpeedSlider.SetAPosY(-10);
                        playerCursorSpeedSlider.slider.minValue = 1f;
                        playerCursorSpeedSlider.slider.maxValue = 50f;
                        playerCursorSpeedSlider.slider.wholeNumbers = true;
                        playerCursorSpeedSlider.slider.value = GFiles.settings.playerCursorSpeed;
                        playerCursorSpeedSlider.text.text.enableAutoSizing = true;
                        playerCursorSpeedSlider.text.autoCompareText = false;
                        playerCursorSpeedSlider.text.AfterRefreshing += x => x.text.text = $"{GameUI.CompareText("ori:player_cursor_speed").text}: {playerCursorSpeedSlider.slider.value}";
                        playerCursorSpeedSlider.slider.onValueChanged.AddListener(v =>
                        {
                            GFiles.settings.playerCursorSpeed = Convert.ToInt32(v);
                            playerCursorSpeedSlider.text.RefreshUI();
                            GFiles.ApplyVolumesToMixers();
                        });

                        /* ---------------------------------- 界面设置 ---------------------------------- */
                        //TODO: Anim speed
                        //TODO: Canvas Scaler Scale

                        /* ---------------------------------- 性能设置 ---------------------------------- */
                        //TODO: Performance Level

                        /* ----------------------------------- 音量 ----------------------------------- */
                        var soundPanel = GameUI.AddPanel("ori:panel.settings.sound", GameUI.canvas.transform, true);
                        var volumeSetButton = GameUI.AddButton(UPC.Middle, "ori:button.settings.sound", settingsPanel).OnClickBind(() => GameUI.SetPage(soundPanel));
                        GameUI.AddButton(new(0.5f, 0.135f, 0.5f, 0.135f), "ori:button.settings.sound.back", soundPanel).OnClickBind(() => GameUI.SetPage(settingsPanel));
                        volumeSetButton.SetAPosOnBySizeDown(controlsSetButton, 20);

                        List<ImageIdentity> sliderBgs = new();

                        SetVolumeSlider("ori:slider.settings.sound.volume.global", "ori:image.settings.sound.volume.global.background", v => { if (v != null) GFiles.settings.volume.globalVolume = (int)v; return GFiles.settings.volume.globalVolume; });
                        SetVolumeSlider("ori:slider.settings.sound.volume.music", "ori:image.settings.sound.volume.music.background", v => { if (v != null) GFiles.settings.volume.musicVolume = (int)v; return GFiles.settings.volume.musicVolume; });
                        SetVolumeSlider("ori:slider.settings.sound.volume.default", "ori:image.settings.sound.volume.default.background", v => { if (v != null) GFiles.settings.volume.defaultVolume = (int)v; return GFiles.settings.volume.defaultVolume; });
                        SetVolumeSlider("ori:slider.settings.sound.volume.ui", "ori:image.settings.sound.volume.ui.background", v => { if (v != null) GFiles.settings.volume.uiVolume = (int)v; return GFiles.settings.volume.uiVolume; });
                        SetVolumeSlider("ori:slider.settings.sound.volume.ambient", "ori:image.settings.sound.volume.ambient.background", v => { if (v != null) GFiles.settings.volume.ambientVolume = (int)v; return GFiles.settings.volume.ambientVolume; });

                        void SetVolumeSlider(string id, string bgId, Func<int?, float> setter)
                        {
                            ImageIdentity bg = GameUI.AddImage(UPC.Middle, bgId, "ori:clear_button", soundPanel);
                            bg.sd = new(languageSetButton.sd.x, 64);
                            bg.ap = new(0, 0.5f * bg.sd.y);

                            if (sliderBgs.Count != 0)
                            {
                                bg.SetAPosOnBySizeDown(sliderBgs[^1], 0);
                                sliderBgs[^1].SetAPosOnBySizeUp(bg, sliderBgs[^1].sd.y / 2);
                            }

                            var slider = GameUI.AddSlider(UPC.Middle, id, bg);
                            slider.text.text.enableAutoSizing = true;
                            slider.SetAPosY(-10);
                            slider.slider.minValue = -80f;
                            slider.slider.maxValue = 20f;
                            slider.slider.wholeNumbers = true;
                            slider.slider.value = setter(null);
                            slider.text.autoCompareText = false;
                            slider.text.AfterRefreshing += x => x.text.text = $"{GameUI.CompareText($"{id}.text").text}: {slider.slider.value}";
                            slider.slider.onValueChanged.AddListener(v =>
                            {
                                setter(Convert.ToInt32(v));
                                slider.text.RefreshUI();
                            });


                            sliderBgs.Add(bg);
                        }

                        sliderBgs[^1].AddAPosY(sliderBgs[^1].sd.y / 2);

                        /* -------------------------------------------------------------------------- */
                        /*                                  ModView                                  */
                        /* -------------------------------------------------------------------------- */
                        {
                            ButtonIdentity showButton = GameUI.AddButton(UPC.LowerLeft, "ori:button.mod_view_panel", firstPanel).OnClickBind(() => GameUI.SetPage(modViewPanel));
                            showButton.rt.anchoredPosition = new(showButton.image.sprite.texture.width + 5, showButton.image.sprite.texture.height + 10);

                            GameUI.AddButton(new(0.5f, 0.1f, 0.5f, 0.1f), "ori:button.mod_view_to_first", modViewPanel).OnClickBind(() => GameUI.SetPage(firstPanel));

                            modScrollView = GameUI.AddScrollView(UPC.Middle, "ori:scrollview.mod_view", modViewPanel);
                            modScrollView.gridLayoutGroup.spacing = new(0, 5);

                            var openSourceButton = GameUI.AddButton(UPC.Middle, "ori:button.mod_open_source", modViewPanel);
                            openSourceButton.SetAPosOnBySizeDown(modScrollView, 10);
                            openSourceButton.OnClickBind(() =>
                            {
                                switch (GInit.platform)
                                {
                                    case RuntimePlatform.WindowsEditor:
                                    case RuntimePlatform.WindowsPlayer:
                                    case RuntimePlatform.WindowsServer:
#if UNITY_STANDALONE_WIN
                                        WindowsTools.OpenPathInExplorer(GInit.modsPath.Replace("/", @"\"));
#endif
                                        break;

                                    case RuntimePlatform.Android:
                                        GUIUtility.systemCopyBuffer = GInit.modsPath.Replace("/", @"\");
#if UNITY_ANDROID
                                        AndroidTools.OpenFileManager();
#endif

                                        break;
                                }
                            });


                            modConfiguringPanel = GameUI.AddPanel("ori:panel.mod_view.mod_config", GameUI.canvasRT, true);
                            ButtonIdentity modConfiguringPanel_ActivityButton = GameUI.AddButton(UPC.Middle, "ori:button.mod_view.mod_config.activity", modConfiguringPanel).OnClickBind(() =>
                            {
                                configuringModDir.info.jo["ori:mod_info"]["enabled"] = !configuringModDir.info.enabled;
                                File.WriteAllText(configuringModDir.path, configuringModDir.info.jo.ToString());

                                RefreshMods();
                                RefreshModView();
                            });

                            var backButton = GameUI.AddButton(new(0.5f, 0.1f, 0.5f, 0.1f), "ori:button.mod_view.mod_config.back", modConfiguringPanel).OnClickBind(() =>
                            {
                                GameUI.SetPage(modViewPanel);
                            });

                            modConfiguringPanel.CustomMethod += (id, _) =>
                            {
                                if (id == "ori:refresh")
                                {
                                    if (configuringModDir.info.enabled)
                                        modConfiguringPanel_ActivityButton.buttonText.text.text = GameUI.currentLang.CompareOrCreateText("ori:disable").text;
                                    else
                                        modConfiguringPanel_ActivityButton.buttonText.text.text = GameUI.currentLang.CompareOrCreateText("ori:enable").text;
                                    return;
                                }
                            };

                            RefreshMods();
                            RefreshModView();
                        }

                        break;
                    }

                case SceneNames.GameScene:
                    {
                        parallaxBackgrounds = new();

                        var c0 = UObjectTools.CreateComponent<ParallaxBackground>("ParallaxBackground0");
                        var c1 = UObjectTools.CreateComponent<ParallaxBackground>("ParallaxBackground1");
                        var c2 = UObjectTools.CreateComponent<ParallaxBackground>("ParallaxBackground2");
                        var c3 = UObjectTools.CreateComponent<ParallaxBackground>("ParallaxBackground3");

                        parallaxBackgrounds.Add(c0);
                        parallaxBackgrounds.Add(c1);
                        parallaxBackgrounds.Add(c2);
                        parallaxBackgrounds.Add(c3);

                        c0.parallaxFactor = 0.5f;
                        c1.parallaxFactor = 0.65f;
                        c2.parallaxFactor = 0.8f;
                        c3.parallaxFactor = 0.95f;

                        c0.positionDelta = new(0, 10);
                        c1.positionDelta = new(0, 10);
                        c2.positionDelta = new(0, 10);
                        c3.positionDelta = new(0, 10);

                        c0.AddRenderers("ori:world_background_0", 8, 0);
                        c1.AddRenderers("ori:world_background_1", 8, -1);
                        c2.AddRenderers("ori:world_background_2", 8, -2);
                        c3.AddRenderers("ori:world_background_3", 8, -3);
                        break;
                    }
            }
        }


        private void Update()
        {
            if (modConfiguringPanel && configuringModDir != null)
                modConfiguringPanel.CustomMethod("ori:refresh", null);

            if (parallaxBackgrounds != null)
            {
                Color color = new(
                        GM.instance.globalLight.color.r * GM.instance.globalLight.intensity,
                        GM.instance.globalLight.color.g * GM.instance.globalLight.intensity,
                        GM.instance.globalLight.color.b * GM.instance.globalLight.intensity);

                foreach (var item in parallaxBackgrounds)
                {
                    foreach (var sr in item.renderers)
                    {
                        sr.color = color;
                    }
                }
            }
        }

        private void RefreshWorldList(ref ScrollViewIdentity chooseWorldScrollView, PanelIdentity panel)
        {
            chooseWorldScrollView.Clear();

            for (int index = 0; index < worldFiles.Count; index++)
            {
                int i = index;
                var file = worldFiles[i];
                string worldPath = file.worldPath;
                string worldName = file.worldName;
                Vector2Int delBtnSize = new(40, 40);

                Transform t = chooseWorldScrollView.transform.parent;
                ImageTextButtonIdentity lb = GameUI.AddImageTextButton(UPC.Middle, "ori:itb.choose_world_" + worldName + "_" + i).OnClickBind(() => MethodAgent.DebugRun(() =>
                {
                    GameUI.Disappear(t.gameObject);

                    //开始并设置游戏时间
                    GM.StartGameHost(worldPath, () => GTime.time = GFiles.world.basicData.time);
                }));

                var configButton = GameUI.AddButton(UPC.LowerRight, "ori:button.config_world_" + worldPath, lb, "ori:square_button");
                configButton.rt.SetParent(lb.rt);
                string str = GameUI.CompareText("ori:button.config_world.text").text;
                configButton.buttonText.AfterRefreshing += tc => tc.text.text = str;
                configButton.sd = delBtnSize;
                configButton.SetAPos(-configButton.sd.x / 2, configButton.sd.y / 2);
                configButton.buttonText.sd = configButton.sd;
                configButton.buttonText.text.SetFontSize(10);
                configButton.OnClickBind(() =>
                {
                    worldConfigPanel.CustomMethod("ori:refresh", worldPath);
                    GameUI.SetPage(worldConfigPanel);
                });
                configButton.RefreshUI();

                lb.image.AfterRefreshing += _ =>
                {
                    string imagePath = World.GetImagePath(worldPath);

                    if (File.Exists(imagePath))
                        MethodAgent.TryRun(() => lb.image.image.sprite = Tools.LoadSpriteByPath(imagePath));
                    else
                        lb.image.image.sprite = GInit.instance.textureUnknown.sprite;
                };
                lb.buttonTextUp.AfterRefreshing += o => o.text.text = worldName;
                lb.buttonTextDown.AfterRefreshing += o => o.text.text = $"{file.seed} [{file.gameVersion}] ({file.time})";

                chooseWorldScrollView.AddChild(lb.rt);
                lb.ResetStatusInScrollView(chooseWorldScrollView);
            }

            AfterRefreshWorldList();
        }

        public void RefreshModView()
        {
            modScrollView.Clear();

            for (int index = 0; index < modDirs.Count; index++)
            {
                int i = index;
                var dir = modDirs[i];

                ImageTextButtonIdentity lb = GameUI.AddImageTextButton(UPC.Middle, "ori:button.edit_mod_" + dir.info.id).OnClickBind(() => MethodAgent.DebugRun(() =>
                {
                    configuringModDir = dir;
                    GameUI.SetPage(modConfiguringPanel);
                }));

                string newName = dir.info.name == null ? dir.info.id : GameUI.currentLang.CompareOrCreateText(dir.info.name).text;
                string newID = dir.info.id;
                string newDescription = dir.info.description == null ? string.Empty : GameUI.currentLang.CompareOrCreateText(dir.info.description).text;
                string newVersion = dir.info.version;

                lb.buttonTextUp.text.SetFontSize(15);
                lb.buttonTextDown.text.SetFontSize(9);

                lb.buttonTextUp.AfterRefreshing += o => o.text.text = $"{newName} ({newID})";
                lb.buttonTextDown.AfterRefreshing += o => o.text.text = newDescription;

                lb.image.image.sprite = dir.info.icon;

                modScrollView.AddChild(lb);
                lb.ResetStatusInScrollView(modScrollView);


                TextIdentity tRT = lb.CreateText("TextTopRight", TMPro.TextAlignmentOptions.TopRight);
                tRT.AfterRefreshing += o => o.text.text = newVersion;
            }

            AfterRefreshModView();
        }

        [RuntimeInitializeOnLoadMethod]
        private static void BindMethod()
        {
            GScene.AfterChanged += scene =>
            {
                //如果加载过模组了就创建 UI 对象, 否则让 ModFactory 来创建
                if (hasShowedModLoadingInterface)
                {
                    MethodAgent.QueueOnMainThread(_ => Tools.NewObjectToComponent(typeof(InternalUIAdder)));
                }
            };
        }
    }
}
