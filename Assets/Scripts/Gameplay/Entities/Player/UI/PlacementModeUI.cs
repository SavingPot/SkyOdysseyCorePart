using GameCore.UI;
using SP.Tools.Unity;
using SP.Tools;
using System.Collections.Generic;
using UnityEngine;
using System;
using DG.Tweening;
using GameCore.High;
using Unity.Burst.Intrinsics;
using UnityEngine.InputSystem;
using System.Collections;

namespace GameCore
{
    public class PlacementModeUI : PlayerUIPart
    {
        /* -------------------------------------------------------------------------- */
        /*                                  放置模式核心代码                                  */
        /* -------------------------------------------------------------------------- */
        public const float minCameraScale = 0.04f;
        public const float maxCameraScale = 2;
        public PanelIdentity placementModePanel;
        public List<PlacementEntry> placementEntries = new();
        public PlacementEntry currentEntry = null;
        public string currentEntryId = string.Empty;
        public Func<bool> NotInLaborCenter;

        public class PlacementEntry : IRectTransform
        {
            public string id;
            public PanelIdentity panel;
            public ButtonIdentity switchButton;
            public Action Refresh;
            public Action Activate;
            public Action Deactivate;
            public Action Update;
            public Action OnPlacementOff;

            public PlacementEntry(string id, PanelIdentity panel, ButtonIdentity switchButton, Action Refresh, Action Activate, Action Deactivate, Action Update, Action OnPlacementOff)
            {
                this.id = id;
                this.panel = panel;
                this.switchButton = switchButton;
                this.Refresh = Refresh;
                this.Activate = Activate;
                this.Deactivate = Deactivate;
                this.Update = Update;
                this.OnPlacementOff = OnPlacementOff;
            }

            public RectTransform rectTransform => panel.rectTransform;
        }

        public PlacementEntry GenerateEntry(
            string id,
            string switchButtonTexture,
            Action Refresh = null,
            Action OnActivate = null,
            Action OnDeactivate = null,
            Action Update = null,
            Action OnPlacementOff = null)
        {
            (var modId, var panelName) = Tools.SplitModIdAndName(id);


            /* -------------------------------------------------------------------------- */
            /*                                    生成面板                                    */
            /* -------------------------------------------------------------------------- */
            var panel = GameUI.AddPanel($"{modId}:panel.{panelName}", placementModePanel);
            panel.panelImage.color = Color.clear;
            panel.gameObject.SetActive(false);
            panel.panelImage.raycastTarget = false;



            /* -------------------------------------------------------------------------- */
            /*                                    生成按钮                                    */
            /* -------------------------------------------------------------------------- */

            /* ---------------------------------- 按钮显示 ---------------------------------- */
            var switchButton = GameUI.AddButton(UIA.UpperLeft, $"{modId}:button.{panelName}_switch", placementModePanel, switchButtonTexture);
            switchButton.sd = new(80, 80);
            switchButton.SetAPos(switchButton.sd.x / 2, -switchButton.sd.y * (placementEntries.Count + 0.5f));
            switchButton.button.ClearColorEffects();
            switchButton.SetOnClickBind(() =>
            {
                GAudio.Play(AudioID.SidebarSwitchButton, null);
            });
            GameObject.Destroy(switchButton.buttonText.gameObject);

            /* ---------------------------------- 按钮功能 ---------------------------------- */
            switchButton.OnClickBind(() => SetPlacementEntry(id));






            /* ----------------------------------------------------------------------- */
            var ActualActivate = new Action(() =>
            {
                panel.gameObject.SetActive(true);
                OnActivate?.Invoke();
            });
            var ActualDeactivate = new Action(() =>
            {
                panel.gameObject.SetActive(false);
                OnDeactivate?.Invoke();
            });

            PlacementEntry result = new(id, panel, switchButton, Refresh, ActualActivate, ActualDeactivate, Update, OnPlacementOff);
            placementEntries.Add(result);
            return result;
        }

        public void SetPlacementEntry(string id)
        {
            if (id == currentEntryId) return;

            //先关闭当前面板
            foreach (var panel in placementEntries)
            {
                if (panel.id == currentEntryId)
                {
                    //改变按钮大小
                    panel.switchButton.sd /= 1.1f;
                    panel.switchButton.sd /= 1.1f;

                    //取消激活
                    panel.Deactivate();
                    currentEntry = null;
                }
            }

            //然后打开指定面板
            foreach (var panel in placementEntries)
            {
                if (panel.id == id)
                {
                    //改变按钮大小
                    panel.switchButton.sd *= 1.1f;
                    panel.switchButton.sd *= 1.1f;

                    panel.Refresh?.Invoke();
                    panel.Activate();
                    currentEntryId = id;
                    currentEntry = panel;
                    return;
                }
            }

            Debug.LogError($"未找到背包界面 {id}, 设置背包界面失败");
        }







        /* ---------------------------------- 劳工中心 ---------------------------------- */
        public PlacementEntry laborCenterEntry;
        public TextIdentity laborCenterTitleText;
        public TextIdentity laborCenterInfoText;
        public ButtonIdentity laborEmployButton;
        public ButtonIdentity laborHousingCheckButton;
        public ButtonIdentity laborExcavationButton;
        public ButtonIdentity laborBuildingButton;
        public ButtonIdentity laborStrategyBuildingButton;
        readonly LaborHousingFlagPool laborHousingFlagPool = new();
        readonly List<SpriteRenderer> usingLaborHousingFlags = new();






        /* ---------------------------------- 市场中心 ---------------------------------- */
        public PlacementEntry marketCenterEntry;
        public TextIdentity marketCenterTitleText;
        public TradeUI tradeUI;
        public ButtonIdentity sellItemButton;






        /* ---------------------------------- 区域解锁 ---------------------------------- */
        public PlacementEntry regionUnlockingEntry;
        public TextIdentity regionUnlockingTitleText;
        public (SpriteRenderer sr, TextImageIdentity ti)[] unlockedRegionColorRenderers { get; private set; }
        internal void SkillRegionUnlockingRendererTweens(SpriteRenderer sr, TextImageIdentity ti)
        {
            //用于防止动画冲突
            Tools.KillTweensOf(sr);
            Tools.KillTweensOf(ti);
            Tools.KillTweensOf(ti.text.text);
        }
        internal void FadeInRegionUnlockingRenderer(SpriteRenderer sr, TextImageIdentity ti)
        {
            var endAlpha = 1;
            var duration = 0.5f;
            SkillRegionUnlockingRendererTweens(sr, ti);

            //初始状态
            sr.gameObject.SetActive(true);
            ti.image.gameObject.SetActive(true);
            sr.color = new(1, 1, 1, 0);
            ti.image.color = new(1, 1, 1, 0);
            ti.text.text.color = new(1, 1, 1, 0);

            //淡入
            sr.DOFade(endAlpha * 0.8f, duration);
            ti.image.DOFade(endAlpha, duration);
            ti.text.text.DOFade(endAlpha, duration);
        }
        internal void FadeOutRegionUnlockingRenderer(SpriteRenderer sr, TextImageIdentity ti)
        {
            var endAlpha = 0;
            var duration = 0.5f;
            SkillRegionUnlockingRendererTweens(sr, ti);

            //淡出
            sr.DOFade(endAlpha, duration).OnComplete(() => sr.gameObject.SetActive(false));
            ti.image.DOFade(endAlpha, duration).OnComplete(() => ti.image.gameObject.SetActive(false));
            ti.text.text.DOFade(endAlpha, duration);
        }
        internal void FadeOutAllRegionUnlockingRenderers()
        {
            foreach (var (sr, ti) in unlockedRegionColorRenderers) FadeOutRegionUnlockingRenderer(sr, ti);
        }


        /// <returns> 对应区域是否未解锁且纵深层存在 </returns>
        public static bool IsRegionUnlocked(Vector2Int regionIndex)
        {
            return !GFiles.world.TryGetRegion(regionIndex, out _) &&
                   !GM.instance.generatingNewRegions.Contains(regionIndex) &&
                    RegionGeneration.IslandGenerationTable.ContainsKey(regionIndex.y) &&
                    regionIndex.y != ChallengeRoomGeneration.challengeRoomIndexY;
        }
        internal void RefreshRegionUnlockingRenderers()
        {
            //渲染器
            Render(0, Vector2Int.up);
            Render(1, Vector2Int.down);
            Render(2, Vector2Int.left);
            Render(3, Vector2Int.right);

            void Render(int index, Vector2Int regionIndexDelta)
            {
                var targetIndex = player.regionIndex + regionIndexDelta;
                var (sr, ti) = unlockedRegionColorRenderers[index];

                //TODO: 客户端的世界为空，这里需要进行一些处理
                if (IsRegionUnlocked(targetIndex))
                {
                    //更改渲染器位置并设置价格文本
                    sr.transform.position = Region.GetMiddle(targetIndex);
                    ti.SetText(GM.GetRegionUnlockingCost(targetIndex));

                    //淡入
                    FadeInRegionUnlockingRenderer(sr, ti);
                }
                else
                {
                    //淡出
                    FadeOutRegionUnlockingRenderer(sr, ti);
                }
            }
        }

        public void Refresh()
        {
            RefreshLaborHousingFlags();
        }

        public void RefreshLaborHousingFlags()
        {
            //归还使用中的住房标记
            foreach (var item in usingLaborHousingFlags)
                laborHousingFlagPool.Recover(item);
            usingLaborHousingFlags.Clear();

            //只在 放置模式-建筑中心 中显示旗帜
            if (pui.IsInInteractionMode() || currentEntryId != laborCenterEntry.id)
                return;

            if (!player.TryGetLordInWorld(out var lord))
            {
                Debug.LogError("找不到领主信息");
                return;
            }

            //TODO: 网络化
            //显示住房标记
            foreach (var housing in lord.laborData.registeredHousings)
            {
                //只显示玩家距离500格以内的住房标记
                var flagPos = housing.spaces[0];
                if (Mathf.Abs(flagPos.x - player.transform.position.x) < 500 && Mathf.Abs(flagPos.y - player.transform.position.y) < 500)
                {
                    var sr = laborHousingFlagPool.Get();
                    sr.transform.position = flagPos.To3();
                    sr.sprite = housing.isOccupied ? ModFactory.CompareTexture("ori:labor_housing_occupied_flag").sprite : ModFactory.CompareTexture("ori:labor_housing_unoccupied_flag").sprite;
                    usingLaborHousingFlags.Add(sr);
                }
            }
        }

        internal PlacementModeUI(PlayerUI pui) : base(pui)
        {
            NotInLaborCenter = () => pui.PlacementMode.currentEntry != pui.PlacementMode.laborCenterEntry || !pui.IsInPlacementMode();

            //寻找领主信息
            if (!player.TryGetLordInWorld(out var lord))
            {
                Debug.LogError("找不到领主信息");
                return;
            }

            /* -------------------------------------------------------------------------- */
            /*                                    放置模式                                    */
            /* -------------------------------------------------------------------------- */
            placementModePanel = GameUI.AddPanel("ori:panel.placement_mode").DisableGameObject();

            /* ---------------------------------- 建造中心 ---------------------------------- */
            laborCenterEntry = GenerateEntry("ori:labor_center",
                "ori:labor_center_button",
                () =>
                {
                    RefreshLaborHousingFlags();
                    laborCenterInfoText.SetText($"共有 {lord.laborData.laborCount} 个劳工（共可居住 {lord.laborData.registeredHousings.Count} 个劳工）\n每日房租 {lord.laborData.GetHousingRent()} 金币"); //TODO: Client Logics
                },
                null,
                () => RefreshLaborHousingFlags());

            marketCenterEntry = GenerateEntry("ori:market_center",
                "ori:market_center_button",
                () => tradeUI.RefreshItems(),
                null,
                null);

            regionUnlockingEntry = GenerateEntry("ori:region_unlocking",
                "ori:region_unlocking_button",
                null,
                () =>
                {
                    //刷新解锁区域的渲染器
                    RefreshRegionUnlockingRenderers();
                    player.playerCameraScale = minCameraScale;
                },
                () => FadeOutAllRegionUnlockingRenderers(),
                null,
                () => FadeOutAllRegionUnlockingRenderers());






            #region 劳工中心

            laborCenterTitleText = GameUI.AddText(UIA.UpperLeft, "ori:text.labor_center_title", laborCenterEntry.panel.transform);
            laborCenterTitleText.SetSizeDeltaY(48);
            laborCenterTitleText.SetAPos(laborCenterEntry.switchButton.sd.x + laborCenterTitleText.sd.x / 2 + 10, -laborCenterTitleText.sd.y / 2);
            laborCenterTitleText.text.alignment = TMPro.TextAlignmentOptions.Left;
            laborCenterTitleText.text.SetFontSize(30);
            laborCenterTitleText.text.raycastTarget = false;

            laborCenterInfoText = GameUI.AddText(UIA.UpperLeft, "ori:text.labor_center_title", laborCenterEntry.panel.transform);
            laborCenterInfoText.DisableAutoCompare().SetSizeDeltaY(40);
            laborCenterInfoText.SetAPosOnBySizeDown(laborCenterTitleText, -10);
            laborCenterInfoText.text.SetFontSize(16);
            laborCenterInfoText.text.alignment = TMPro.TextAlignmentOptions.Left;
            laborCenterInfoText.text.raycastTarget = false;

            #region 招募劳工

            #endregion
            laborEmployButton = GameUI.AddButton(UIA.UpperLeft, "ori:button.labor_center_employ", laborCenterEntry.panel.transform);
            laborEmployButton.SetAPosOnBySizeDown(laborCenterInfoText, 10);
            laborEmployButton.buttonText.DisableAutoCompare().SetText("招募劳工：10 金币"); //TODO：把 文字“金币” 改成 图标金币
            laborEmployButton.OnClickBind(() =>
            {
                //TODO:网络化
                Debug.Log(lord.coins);
                if (lord.coins < 10)
                {
                    InternalUIAdder.instance.SetStatusText("金币不足 10");
                }
                else
                {
                    lord.laborData.laborCount++;
                    lord.coins -= 10;
                    lord.ApplyLordDataBack();
                    laborCenterEntry.Refresh();
                }
            });


            #region 宿舍标记

            //TODO: 类似浮岛物语的按钮，即一个园方形按钮，中间有房子图案，下面写着“劳工宿舍”
            laborHousingCheckButton = GameUI.AddButton(UIA.UpperLeft, "ori:button.labor_center_housing_check", laborCenterEntry.panel.transform);
            laborHousingCheckButton.SetAPosOnBySizeDown(laborEmployButton, 10);
            laborHousingCheckButton.OnClickBind(() =>
            {
                //TODO: 让玩家知道自己在选择
                //如果在劳工中心，且点击左键而没点到UI
                Tools.instance.ChoosePoint(_ =>
                {
                    if (!Tools.IsPointerOverInteractableUI())
                    {
                        player.HandleHousingRegistration(Mouse.current.leftButton.wasPressedThisFrame);
                    }
                    else
                    {
                        Debug.Log("取消了");
                        //TODO: 提示玩家失败了
                    }
                }, NotInLaborCenter);
            });


            // if (pui.PlacementMode.currentEntry == pui.PlacementMode.laborCenterEntry && (Mouse.current.leftButton.wasPressedThisFrame || Mouse.current.rightButton.wasPressedThisFrame) && !Tools.IsPointerOverInteractableUI())
            // {
            //     HandleHousingRegistration(Mouse.current.leftButton.wasPressedThisFrame);
            // }

            #endregion

            #region 挖掘

            laborExcavationButton = GameUI.AddButton(UIA.UpperLeft, "ori:button.labor_center_excavation", laborCenterEntry.panel.transform);
            laborExcavationButton.SetAPosOnBySizeDown(laborHousingCheckButton, 10);
            laborExcavationButton.OnClickBind(() =>
            {
                //检查劳工数
                if (lord.laborData.laborCount <= 0)
                {
                    InternalUIAdder.instance.SetStatusText("需要至少一个劳工");
                    return;
                }

                //TODO：目前只有矩形模式
                //TODO:网络化
                //TODO: 花费
                //TODO:显示出红色半透明矩形框
                Tools.instance.ChoosePoint(pos1 => Tools.instance.ChoosePoint(pos2 =>
                {
                    //放置方块
                    //TODO: 花费（在模板里设置）
                    //TODO: 需要提供的材料（在模板里设置，可在市场购买）
                    //TODO: 分配劳工个数（LaborTask, 先直接用一个 Slider，先不加Panel）
                    var placeBlockSteps = new List<LaborPlaceBlockWorkStep>();
                    var work = new LaborWork(placeBlockSteps, lord.laborData.laborCount);
                    lord.laborData.executingWorks.Add(work);

                    var mp1 = PosConvert.WorldToMapPos(pos1);
                    var mp2 = PosConvert.WorldToMapPos(pos2);

                    var minX = Mathf.Min(mp1.x, mp2.x);
                    var maxX = Mathf.Max(mp1.x, mp2.x);
                    var minY = Mathf.Min(mp1.y, mp2.y);
                    var maxY = Mathf.Max(mp1.y, mp2.y);

                    //遍历每一个方块，创建放置任务
                    for (int x = minX; x <= maxX; x++)
                    {
                        for (int y = minY; y <= maxY; y++)
                        {
                            placeBlockSteps.Add(new(work, new(x, y), false, null, BlockStatus.Normal));
                        }
                    }

                    //开始放置任务
                    work.Begin();
                }, NotInLaborCenter), NotInLaborCenter, true);
            });

            #endregion

            #region 建造（根据模板） //TODO:加上玩家自定义模板的功能

            laborBuildingButton = GameUI.AddButton(UIA.UpperLeft, "ori:button.labor_center_building", laborCenterEntry.panel.transform);
            laborBuildingButton.SetAPosOnBySizeDown(laborExcavationButton, 10);
            laborBuildingButton.OnClickBind(() =>
            {
                //检查劳工数
                if (lord.laborData.laborCount <= 0)
                {
                    InternalUIAdder.instance.SetStatusText("需要至少一个劳工");
                    return;
                }

                //TODO:网络化
                //TODO：把Debugger的代码搬过来
                Tools.instance.ChoosePoint(pos =>
                {
                    var structure = ModFactory.CompareStructure(StructureID.UndergroundRelics);
                    var anchor = PosConvert.WorldToMapPos(pos);

                    //放置方块
                    //TODO: 花费（在模板里设置）
                    //TODO: 需要提供的材料（在模板里设置，可在市场购买）
                    //TODO: 分配劳工个数（LaborTask, 先直接用一个 Slider，先不加Panel）
                    var placeBlockSteps = new List<LaborPlaceBlockWorkStep>();
                    var work = new LaborWork(placeBlockSteps, lord.laborData.laborCount);
                    lord.laborData.executingWorks.Add(work);

                    //遍历每一个方块，创建放置任务
                    foreach (var structBlock in structure.fixedBlocks)
                    {
                        var blockPos = anchor + structBlock.offset;
                        var blockToDestroy = Map.instance.GetBlock(blockPos, structBlock.isBackground);
                        placeBlockSteps.Add(new(work, blockPos, structBlock.isBackground, structBlock.blockId, structBlock.status));
                    }

                    //开始放置任务
                    work.Begin();
                }, NotInLaborCenter);
            });

            #endregion

            #region 战略建造（瞬间生成，直接生成，无需使用 executingWorks）
            laborStrategyBuildingButton = GameUI.AddButton(UIA.UpperLeft, "ori:button.labor_center_strategy_building", laborCenterEntry.panel.transform);
            laborStrategyBuildingButton.SetAPosOnBySizeDown(laborBuildingButton, 10);
            laborStrategyBuildingButton.OnClickBind(() =>
            {
                //检查劳工数
                if (lord.laborData.laborCount <= 0)
                {
                    InternalUIAdder.instance.SetStatusText("需要至少一个劳工");
                    return;
                }


                //TODO:网络化
                //TODO: 花费（在模板里设置）
                Tools.instance.ChoosePoint(pos =>
                {
                    var structure = ModFactory.CompareStructure(StructureID.UndergroundRelics);
                    var anchor = PosConvert.WorldToMapPos(pos);
                    StructureUtils.GenerateStructure(structure, anchor);
                }, NotInLaborCenter);
            });
            #endregion

            #endregion




            #region 市场中心

            marketCenterTitleText = GameUI.AddText(UIA.UpperLeft, "ori:text.market_center_title", marketCenterEntry.panel.transform);
            marketCenterTitleText.SetSizeDeltaY(48);
            marketCenterTitleText.SetAPos(marketCenterEntry.switchButton.sd.x + marketCenterTitleText.sd.x / 2 + 10, -marketCenterTitleText.sd.y / 2);
            marketCenterTitleText.text.alignment = TMPro.TextAlignmentOptions.Left;
            marketCenterTitleText.text.SetFontSize(30);
            marketCenterTitleText.text.raycastTarget = false;

            tradeUI = TradeUI.GenerateItemView(pui, "ori:placement_trade", new TradeUI.ItemTrade[]
            {
                new(BlockID.Torch, 1, 1),
                new(BlockID.Dirt, 1, 1),
                new(BlockID.Sand, itemCount: 1, 2),
                new(BlockID.Gravel, 1, 3),
                new(BlockID.Flowerpot, 1, 10),
                new(BlockID.PotatoCrop, 1, 3),
                new(BlockID.OnionCrop, 1, 5),
                new(BlockID.CarrotCrop, 1, 7),
                new(BlockID.PumpkinCrop, 1, 10),
                new(BlockID.TomatoCrop, 1, 10),
                new(BlockID.WatermelonCrop, 1, 10),
                new(BlockID.OakSeed, 1, 15),
                new(BlockID.AcaciaSeed, 1, 22),
                new(BlockID.MangroveSeed, 1, 30),
            }, marketCenterEntry.panel.transform);

            //UI向内收缩
            tradeUI.itemView.SetSizeDelta(-300, -100);

            sellItemButton = GameUI.AddButton(UIA.Down, "ori:button.placement_trade_sell_item", marketCenterEntry.panel);
            sellItemButton.SetAPosY(80);
            sellItemButton.ClearOnClickBind().OnClickBind(() =>
            {
                //TODO：添加一个“售卖手中物品”按钮
                if (player.TryGetUsingItem(out var item) && item.data.economy.worth != 0)
                {
                    player.ServerReduceUsingItemCount(1);
                    player.ServerAddCoin(item.data.economy.worth);
                    GAudio.Play(AudioID.Trade, null);
                }
            });

            #endregion




            #region 区域解锁

            regionUnlockingTitleText = GameUI.AddText(UIA.UpperLeft, "ori:text.region_unlocking_title", regionUnlockingEntry.panel.transform);
            regionUnlockingTitleText.SetSizeDeltaY(48);
            regionUnlockingTitleText.SetAPos(regionUnlockingEntry.switchButton.sd.x + regionUnlockingTitleText.sd.x / 2 + 10, -regionUnlockingTitleText.sd.y / 2);
            regionUnlockingTitleText.text.alignment = TMPro.TextAlignmentOptions.Left;
            regionUnlockingTitleText.text.SetFontSize(30);
            regionUnlockingTitleText.text.raycastTarget = false;

            unlockedRegionColorRenderers = new (SpriteRenderer, TextImageIdentity)[] {
                GenerateUnlockedRegionColorRenderers(),
                GenerateUnlockedRegionColorRenderers(),
                GenerateUnlockedRegionColorRenderers(),
                GenerateUnlockedRegionColorRenderers(),
            };

            static (SpriteRenderer, TextImageIdentity) GenerateUnlockedRegionColorRenderers()
            {
                //渲染器
                var sr = new GameObject().AddComponent<SpriteRenderer>();
                var scale = Region.chunkCount * Chunk.blockCountPerAxis * 0.9f;
                sr.sprite = ModFactory.CompareTexture("ori:unlocked_region_color").sprite;
                sr.color = new(1, 1, 1, 0.8f);
                sr.sortingOrder = 100;
                sr.transform.localScale = new(scale, scale);
                sr.gameObject.SetActive(false);

                //创建画布
                var canvas = GameUI.AddWorldSpaceCanvas(sr.transform);
                canvas.GetComponent<RectTransform>().sizeDelta = Vector3.zero;
                canvas.sortingOrder = 101;
                canvas.transform.localScale = new(1 / scale, 1 / scale, 0);

                //添加金币显示
                var textImage = GameUI.GenerateCoinTextImage(50, canvas.transform);

                return (sr, textImage);
            }
            #endregion
        }






        public sealed class LaborHousingFlagPool
        {
            public Stack<SpriteRenderer> stack = new();

            public SpriteRenderer Get()
            {
                var result = (stack.Count == 0) ? Generation() : stack.Pop();
                result.gameObject.SetActive(true);
                return result;
            }

            public void Recover(SpriteRenderer obj)
            {
                obj.gameObject.SetActive(false);
                stack.Push(obj);
            }

            public SpriteRenderer Generation()
            {
                var result = ObjectTools.CreateSpriteObject("HouseOccupiedSr");
                result.sortingOrder = 100;
                result.material = GInit.instance.spriteDefaultMat;
                return result;
            }
        }
    }
}