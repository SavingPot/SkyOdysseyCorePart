using GameCore.UI;
using SP.Tools.Unity;
using SP.Tools;
using System.Collections.Generic;
using UnityEngine;

namespace GameCore
{
    public class PlacementModeUI : PlayerUIPart
    {
        /* -------------------------------------------------------------------------- */
        /*                                    放置模式                                    */
        /* -------------------------------------------------------------------------- */
        public PanelIdentity placementModePanel;
        public ButtonIdentity buildingCenterButton;
        public PanelIdentity housingRentalPanel;
        public ButtonIdentity marketCenterButton;
        public ButtonIdentity regionUnlockingButton;



        /* ---------------------------------- 建造中心 ---------------------------------- */
        public ButtonIdentity switchToHousingRentalPanelButton;
        public ScrollViewIdentity housingRentalScrollView;
        public PanelIdentity housingInfoPanel;
        public ButtonIdentity housingInfoToRentalButton;
        public ButtonIdentity housingInfoCollectRentButton;
        public TextIdentity housingInfoNameText;
        public ScrollViewIdentity housingRentalNPCScrollView;
        HousingRentalScrollViewPool housingRentalScrollViewPool;
        HousingRentalNPCScrollViewPool housingRentalNPCScrollViewPool;

        public void ShowHouseInfo(Doorplate doorplate, string name, string tenantName)
        {
            //TODO: 需要确保NPC和房屋没有在查看时因为其它玩家失效
            //设置房屋名
            housingInfoNameText.SetText(name);
            housingInfoCollectRentButton.SetText(tenantName.IsNullOrWhiteSpace() ? "收租：无租客" : $"向 {GameUI.CompareText(tenantName)} 收租：{Doorplate.GetRentAmount(tenantName)}");

            //回收 NPC 列表
            foreach (var item in housingRentalNPCScrollView.content.GetComponentsInChildren<ButtonIdentity>())
                housingRentalNPCScrollViewPool.Recover(item);

            //生成 NPC 列表
            foreach (var npc in NPCCenter.all)
            {
                var button = housingRentalNPCScrollViewPool.Get(this);
                button.SetText($"{GameUI.CompareText(npc.data.id)}");
                button.SetOnClickBind(() =>
                {
                    doorplate.SetTenantName(npc.data.id);

                    //刷新房屋信息
                    ShowHouseInfo(doorplate, name, doorplate.GetTenantName());
                });
                button.BindButtonAudio();
            }
        }

        internal PlacementModeUI(PlayerUI pui) : base(pui)
        {
            /* -------------------------------------------------------------------------- */
            /*                                    放置模式                                    */
            /* -------------------------------------------------------------------------- */
            placementModePanel = GameUI.AddPanel("ori:panel.placement_mode").DisableGameObject();
            buildingCenterButton = GameUI.AddButton(UIA.UpperLeft, "ori:button.building_center", placementModePanel, "ori:building_center_button");
            buildingCenterButton.SetSizeDelta(80, 80);
            buildingCenterButton.SetAPos(buildingCenterButton.sd.x / 2, -buildingCenterButton.sd.y / 2);
            buildingCenterButton.buttonText.DisableAutoCompare().gameObject.SetActive(false);
            buildingCenterButton.OnClickBind(() =>
            {
                housingRentalPanel.RefreshUI();
                housingRentalPanel.gameObject.SetActive(true);
            });
            marketCenterButton = GameUI.AddButton(UIA.UpperLeft, "ori:button.market_center", placementModePanel, "ori:market_center_button");
            marketCenterButton.sd = buildingCenterButton.sd;
            marketCenterButton.SetAPosOnBySizeDown(buildingCenterButton, 0);
            marketCenterButton.buttonText.DisableAutoCompare().gameObject.SetActive(false);
            regionUnlockingButton = GameUI.AddButton(UIA.UpperLeft, "ori:button.region_unlocking", placementModePanel, "ori:region_unlocking_button");
            regionUnlockingButton.sd = buildingCenterButton.sd;
            regionUnlockingButton.SetAPosOnBySizeDown(marketCenterButton, 0);
            regionUnlockingButton.buttonText.DisableAutoCompare().gameObject.SetActive(false);

            /* ---------------------------------- 建造中心 ---------------------------------- */



            /* ----------------------------------- 出租 ----------------------------------- */
            housingRentalPanel = GameUI.AddPanel("ori:panel.building_center.housing_rental", placementModePanel).DisableGameObject();
            housingRentalPanel.panelImage.raycastTarget = false;
            housingRentalPanel.AfterRefreshing += _ =>
            {
                foreach (var item in housingRentalScrollView.content.GetComponentsInChildren<ButtonIdentity>())
                    housingRentalScrollViewPool.Recover(item);

                foreach (var chunk in Map.instance.chunks)
                {
                    //只会寻找当前区域内的房屋
                    if (chunk.regionIndex != pui.player.regionIndex)
                        continue;

                    foreach (var block in chunk.wallBlocks)
                    {
                        if (block is Doorplate doorplate)
                        {
                            var button = housingRentalScrollViewPool.Get(this);
                            var name = doorplate.GetRoomName();
                            var hasName = !name.IsNullOrWhiteSpace();
                            var tenantName = doorplate.GetTenantName();
                            var hasTenant = !tenantName.IsNullOrWhiteSpace();

                            //如果点击了就进入房屋设置
                            button.SetOnClickBind(() =>
                            {
                                ShowHouseInfo(doorplate, name, tenantName);
                                housingInfoPanel.gameObject.SetActive(true);
                            });
                            button.BindButtonAudio();

                            button.SetColor((hasName, hasTenant) switch
                            {
                                (true, false) => Color.white, //有房间名无租客
                                (true, true) => Color.gray,   //有房间名有租客
                                _ => Color.red                //无房间名
                            });

                            button.SetText($"房间：{(hasName ? name : "未命名房间")}");
                        }
                    }
                }
            };

            //侧边栏按钮
            (_, switchToHousingRentalPanelButton) = GameUI.GenerateSidebarSwitchButton(
                "ori:image.building_center.switch_to_housing_rental.background",
                "ori:button.building_center.switch_to_housing_rental",
                "ori:switch_to_housing_rental",
                placementModePanel,
                0);
            switchToHousingRentalPanelButton.OnClickBind(() =>
            {
                housingRentalPanel.RefreshUI();
                housingRentalPanel.gameObject.SetActive(true);
            });



            //房屋列表
            housingRentalScrollView = GameUI.AddScrollView(UIA.Middle, "ori:sv.building_center.housing_rental", housingRentalPanel);
            housingRentalScrollView.SetSizeDeltaX(250);
            housingRentalScrollView.SetGridLayoutGroupCellSizeToMax(45);
            housingRentalScrollViewPool = new();



            //房屋信息面板
            housingInfoPanel = GameUI.AddPanel("ori:panel.building_center.housing_info", housingRentalPanel, true);

            housingInfoToRentalButton = GameUI.AddButton(UIA.UpperLeft, "ori:button.building_center.housing_info_to_rental", housingInfoPanel);
            housingInfoToRentalButton.SetAPos(housingInfoToRentalButton.sd.x / 2 + 15, -housingInfoToRentalButton.sd.y / 2 - 15);
            housingInfoToRentalButton.OnClickBind(() =>
            {
                housingInfoPanel.gameObject.SetActive(false);
                housingRentalPanel.RefreshUI();
            });

            housingInfoCollectRentButton = GameUI.AddButton(UIA.Down, "ori:button.building_center.housing_info_collect_rent", housingInfoPanel);
            housingInfoCollectRentButton.SetAPos(0, housingInfoCollectRentButton.sd.y / 2 + 15);
            housingInfoCollectRentButton.buttonText.autoCompareText = false;

            housingInfoNameText = GameUI.AddText(UIA.Middle, "ori:text.building_center.housing_info_name", housingInfoPanel);
            housingInfoNameText.autoCompareText = false;
            housingInfoNameText.text.raycastTarget = false;
            housingInfoNameText.AddAPosY(150);



            //NPC列表
            housingRentalNPCScrollView = GameUI.AddScrollView(UIA.Middle, "ori:sv.building_center.housing_rental_npc", housingInfoPanel);
            housingRentalNPCScrollView.SetSizeDeltaX(250);
            housingRentalNPCScrollView.SetGridLayoutGroupCellSizeToMax(45);
            housingRentalNPCScrollViewPool = new();
        }






        public sealed class HousingRentalScrollViewPool
        {
            public Stack<ButtonIdentity> stack = new();

            public ButtonIdentity Get(PlacementModeUI ui)
            {
                var result = (stack.Count == 0) ? Generation(ui) : stack.Pop();
                result.gameObject.SetActive(true);
                return result;
            }

            public void Recover(ButtonIdentity obj)
            {
                obj.gameObject.SetActive(false);
                stack.Push(obj);
            }

            public ButtonIdentity Generation(PlacementModeUI ui)
            {
                var result = GameUI.AddButton(UIA.Middle, $"ori:button.building_center.housing_rental_item_{Tools.randomInt}", ui.housingRentalScrollView.content);

                result.buttonText.autoCompareText = false;

                return result;
            }
        }

        public sealed class HousingRentalNPCScrollViewPool
        {
            public Stack<ButtonIdentity> stack = new();

            public ButtonIdentity Get(PlacementModeUI ui)
            {
                var result = (stack.Count == 0) ? Generation(ui) : stack.Pop();
                result.gameObject.SetActive(true);
                return result;
            }

            public void Recover(ButtonIdentity obj)
            {
                obj.gameObject.SetActive(false);
                stack.Push(obj);
            }

            public ButtonIdentity Generation(PlacementModeUI ui)
            {
                var result = GameUI.AddButton(UIA.Middle, $"ori:button.building_center.housing_rental_npc_item_{Tools.randomInt}", ui.housingRentalNPCScrollView.content);

                result.buttonText.autoCompareText = false;

                return result;
            }
        }
    }
}