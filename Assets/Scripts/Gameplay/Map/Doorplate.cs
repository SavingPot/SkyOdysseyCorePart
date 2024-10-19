using GameCore.UI;
using SP.Tools;
using SP.Tools.Unity;
using UnityEngine;

namespace GameCore
{
    public class Doorplate : Block
    {
        static PanelIdentity settingsPanel;
        static ToggleIdentity respawnPointToggle;
        static InputButtonIdentity nameEnterInputButton;
        static Doorplate operatingDoorplate;
        static Player operatingPlayer;


        public override void DoStart()
        {
            base.DoStart();

            RefreshTexture();
        }



        public override bool PlayerInteraction(Player player)
        {
            var roomCheck = new MapUtils.RoomCheck(pos);
            if (roomCheck.IsValidConstruction())
            {
                if (GetRoomName().IsNullOrWhiteSpace())
                {
                    operatingDoorplate = this;
                    operatingPlayer = player;
                    GameUI.SetPage(settingsPanel);
                    return true;
                }
                else
                {
                    InternalUIAdder.instance.SetStatusText($"门牌信息:   名称:{GetRoomName()}   租户:{GetTenantName()}");
                }
            }
            else
            {
                InternalUIAdder.instance.SetStatusText("请把牌子放在一个有效的房间内");
            }

            return true;
        }

        
        public static int GetRentAmount(string tenantName)
        {
            //TODO: 记录上次收租时间，并根据它计算租金
            return 1;
        }


        void FixCustomData()
        {
            customData ??= new();

            if (!customData.TryGetJToken("ori:doorplate", out var doorplateJT))
            {
                customData.AddObject("ori:doorplate");
                doorplateJT = customData["ori:doorplate"];
            }

            if (!doorplateJT.TryGetJToken("room_name", out var roomNameJT))
            {
                doorplateJT.AddProperty("room_name", null);
                roomNameJT = doorplateJT["room_name"];
            }

            if (!doorplateJT.TryGetJToken("tenant_name", out var tenantNameJT))
            {
                doorplateJT.AddProperty("tenant_name", null);
                tenantNameJT = doorplateJT["tenant_name"];
            }
        }


        public string GetRoomName()
        {
            FixCustomData();

            return customData["ori:doorplate"]["room_name"].ToString();
        }


        //TODO: Execute only on the server, maybe we need Rpc requests for this
        void SetRoomName(string roomName)
        {
            FixCustomData();

            customData["ori:doorplate"]["room_name"] = roomName;
            PushCustomDataToServer();
        }


        public string GetTenantName()
        {
            FixCustomData();

            return customData["ori:doorplate"]["tenant_name"].ToString();
        }


        //TODO: Execute only on the server, maybe we need Rpc requests for this
        public void SetTenantName(string tenantName)
        {
            FixCustomData();

            customData["ori:doorplate"]["tenant_name"] = tenantName;
            PushCustomDataToServer();
        }


        void RefreshTexture()
        {
            FixCustomData();
        }


        public static void InitUI()
        {
            settingsPanel = GameUI.AddBackpackFormedPanel("ori:panel.doorplate_settings", GameUI.canvasRT, true);

            respawnPointToggle = GameUI.AddToggle(UIA.Middle, "ori:toggle.doorplate_set_respawn_point", settingsPanel);
            respawnPointToggle.SetAPosY(-50);

            nameEnterInputButton = GameUI.AddInputButton(UIA.Middle, "ori:input_button.doorplate_name_enter", settingsPanel);
            nameEnterInputButton.SetSize(new(300, 50));
            nameEnterInputButton.OnClickBind(() =>
            {
                if (operatingDoorplate == null)
                {
                    Debug.LogError("No operating doorplate");
                    return;
                }

                var newName = nameEnterInputButton.field.field.text;
                if (newName.IsNullOrWhiteSpace())
                {
                    //名称为空会关掉界面
                    InternalUIAdder.instance.SetStatusText("名称为空");
                }
                if (newName.Length > 20)
                {
                    //名称过长不会关掉界面
                    InternalUIAdder.instance.SetStatusText("名称过长");
                    return;
                }
                else
                {
                    operatingDoorplate.SetRoomName(newName);

                    //设置重生点
                    if (respawnPointToggle.toggle.isOn)
                    {
                        Vector2Int? respawnPoint = null;
                        MapUtils.RoomCheck roomCheck = new(operatingDoorplate.pos);
                        roomCheck.IsEnclosedConstruction();

                        //在房间里找空间
                        foreach (var (x, y) in roomCheck.spacesInConstruction)
                        {
                            //如果可以容下一个人
                            if ((!Map.instance.TryGetBlock(new(x, y), false, out var currentBlock) || currentBlock.data.collidible) &&
                                (!Map.instance.TryGetBlock(new(x, y + 1), false, out var upperBlock) || upperBlock.data.collidible))
                            {
                                respawnPoint = new(x, y);
                                break;
                            }
                        }

                        //实在找不到就在门牌里
                        if (!respawnPoint.HasValue)
                            respawnPoint = operatingDoorplate.pos;

                        //设置重生点
                        operatingPlayer.ServerSetRespawnPoint(respawnPoint.Value);
                        InternalUIAdder.instance.SetStatusText($"设置了重生点");
                    }
                }

                GameUI.SetPage(null);
            });
        }
    }
}