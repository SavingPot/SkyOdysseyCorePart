using GameCore.UI;
using SP.Tools.Unity;
using SP.Tools;
using System.Collections.Generic;
using UnityEngine;
using System;
using DG.Tweening;
using GameCore.High;

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

        public class PlacementEntry : IRectTransform
        {
            public string id;
            public PanelIdentity panel;
            public ButtonIdentity switchButton;
            public Action Refresh;
            public Action Activate;
            public Action Deactivate;
            public Action Update;

            public PlacementEntry(string id, PanelIdentity panel, ButtonIdentity switchButton, Action Refresh, Action Activate, Action Deactivate, Action Update)
            {
                this.id = id;
                this.panel = panel;
                this.switchButton = switchButton;
                this.Refresh = Refresh;
                this.Activate = Activate;
                this.Deactivate = Deactivate;
                this.Update = Update;
            }

            public RectTransform rectTransform => panel.rectTransform;
        }

        public PlacementEntry GenerateEntry(
            string id,
            string switchButtonTexture,
            Action Refresh = null,
            Action OnActivate = null,
            Action OnDeactivate = null,
            Action Update = null)
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

            PlacementEntry result = new(id, panel, switchButton, Refresh, ActualActivate, ActualDeactivate, Update);
            placementEntries.Add(result);
            return result;
        }

        public void SetPlacementEntry(string id)
        {
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







        /* ---------------------------------- 建造中心 ---------------------------------- */
        public PlacementEntry buildingCenterEntry;
        HouseOccupiedSrPool houseOccupiedSrPool = new();





        /* ---------------------------------- 市场中心 ---------------------------------- */
        public PlacementEntry marketCenterEntry;
        public TradeUI tradeUI;
        public ButtonIdentity sellItemButton;






        /* ---------------------------------- 区域解锁 ---------------------------------- */
        public PlacementEntry regionUnlockingEntry;
        public (SpriteRenderer sr, TextImageIdentity ti)[] unlockedRegionColorRenderers { get; private set; }
        internal void FadeRegionUnlockingRenderer(SpriteRenderer sr)
        {
            sr.DOFade(0, 0.5f).OnComplete(() => sr.gameObject.SetActive(false));
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
                    sr.color = new(1, 1, 1, 0.8f);
                    sr.gameObject.SetActive(true);
                    sr.transform.position = Region.GetMiddle(targetIndex);
                    ti.SetText(GM.GetRegionUnlockingCost(targetIndex));
                }
                else
                {
                    FadeRegionUnlockingRenderer(sr);
                }
            }
        }







        internal PlacementModeUI(PlayerUI pui) : base(pui)
        {
            /* -------------------------------------------------------------------------- */
            /*                                    放置模式                                    */
            /* -------------------------------------------------------------------------- */
            placementModePanel = GameUI.AddPanel("ori:panel.placement_mode").DisableGameObject();

            /* ---------------------------------- 建造中心 ---------------------------------- */
            buildingCenterEntry = GenerateEntry("ori:building_center",
                "ori:building_center_button",
                null,
                null,
                null);

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
                () =>
                {
                    //渲染器
                    foreach (var (sr, _) in unlockedRegionColorRenderers) FadeRegionUnlockingRenderer(sr);
                });




            #region 建造中心

            #region 出租
            #endregion

            #endregion




            #region 市场中心

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

                //画布
                var canvas = GameUI.AddWorldSpaceCanvas(sr.transform);
                canvas.GetComponent<RectTransform>().sizeDelta = Vector3.zero;
                canvas.sortingOrder = 101;
                canvas.transform.localScale = new(1 / scale, 1 / scale, 0);

                var textImage = GameUI.AddTextImage(UIA.Middle, $"ori:text_image.unlocked_region_color.{Tools.randomGUID}", "ori:coin", canvas.transform);
                textImage.SetSizeDeltaBoth(50, 50);
                textImage.SetTextAttach(TextImageIdentity.TextAttach.Right);
                textImage.text.doRefresh = false;
                textImage.SetAPosX(-textImage.sd.x / 2);

                return (sr, textImage);
            }
            #endregion
        }






        public sealed class HouseOccupiedSrPool
        {
            public Stack<SpriteRenderer> stack = new();

            public SpriteRenderer Get(Vector2 pos)
            {
                var result = (stack.Count == 0) ? Generation() : stack.Pop();
                result.gameObject.SetActive(true);
                result.transform.position = pos;
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

                return result;
            }
        }
    }
}