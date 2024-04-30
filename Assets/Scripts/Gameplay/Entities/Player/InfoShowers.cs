using GameCore.UI;
using UnityEngine;
using SP.Tools.Unity;
using System.Text;
using System.Collections.Generic;

namespace GameCore
{
    public abstract class InfoShower<T> where T : InfoShower<T>, new()
    {
        private static T _instance;
        public static T instance
        {
            get
            {
                if (_instance == null || _instance.IsInvalid())
                    _instance = new();

                return _instance;
            }
        }




        public abstract bool IsInvalid();
    }















    //TODO: 把 InfoShower 打包为一个 abstract class，然后再派生出各种具体的实现类，这样可以更灵活快捷地使用
    //TODO: 注意要划分为两种 InfoShower：一种是名字文本+信息文本，另一种是只有一个文本

    public abstract class NameAndDetailInfoShower<T> : InfoShower<T> where T : NameAndDetailInfoShower<T>, new()
    {
        public ImageIdentity backgroundImage;
        public TextIdentity nameText;
        public TextIdentity detailText;



        public void Show(string nameTextContent, string detailTextContent)
        {
            Vector2 pos = GControls.cursorPosInMainCanvas;
            pos.x += backgroundImage.sd.x * 0.75f;
            pos.y -= backgroundImage.sd.y * 0.75f;

            backgroundImage.ap = pos;
            nameText.text.text = nameTextContent;
            detailText.text.text = detailTextContent;

            backgroundImage.gameObject.SetActive(true);
        }



        public void Hide()
        {
            backgroundImage.gameObject.SetActive(false);
        }


        public override bool IsInvalid() => backgroundImage == null || nameText == null || detailText == null;




        public NameAndDetailInfoShower()
        {
            int borderSize = 5;
            int detailTextFontSize = 15;

            backgroundImage = GameUI.AddImage(UPC.Middle, "ori:image.item_info_shower", "ori:item_info_shower");
            nameText = GameUI.AddText(UPC.UpperLeft, "ori:text.item_info_shower.name", backgroundImage);
            detailText = GameUI.AddText(UPC.UpperLeft, "ori:text.item_info_shower.detail", backgroundImage);

            backgroundImage.OnUpdate += x => GameUI.SetUILayerToTop(x);

            nameText.text.alignment = TMPro.TextAlignmentOptions.Left;
            detailText.text.alignment = TMPro.TextAlignmentOptions.TopLeft;
            detailText.text.paragraphSpacing = 15;

            backgroundImage.SetSizeDelta(200, 200);
            nameText.SetSizeDelta(backgroundImage.sd.x, 30);
            //damageIcon.SetSizeDelta(detailTextFontSize, detailTextFontSize);
            detailText.SetSizeDelta(nameText.sd.x, backgroundImage.sd.y - nameText.sd.y);

            nameText.SetAPos(nameText.sd.x / 2, -nameText.sd.y / 2 - borderSize);
            detailText.SetAPos(nameText.ap.x, nameText.ap.y - nameText.sd.y / 2 - detailText.sd.y / 2 - borderSize);
            //damageIcon.SetAPos(borderSize + damageIcon.sd.x / 2, nameText.ap.y - nameText.sd.y / 2 - damageIcon.sd.y - borderSize);

            nameText.text.SetFontSize(18);
            detailText.text.SetFontSize(detailTextFontSize);

            backgroundImage.image.raycastTarget = false;
            nameText.text.raycastTarget = false;
            detailText.text.raycastTarget = false;
        }
    }













    public class ItemInfoShower : NameAndDetailInfoShower<ItemInfoShower>
    {
        private readonly StringBuilder stringBuilder = new();



        public void Show(Item item) => Show(item.data);

        public void Show(ItemData item) => Show(GameUI.CompareText(item.id).text, $"<color=#E0E0E0>{GetDetailText(item, stringBuilder.Clear())}</color>");

        public StringBuilder GetDetailText(Item item) => GetDetailText(item.data, stringBuilder);

        public StringBuilder GetDetailText(ItemData item, StringBuilder sb)
        {
            sb.AppendLine(GameUI.CompareText("ori:item.damage").text.Replace("{value}", item.damage.ToString()));
            sb.AppendLine(GameUI.CompareText("ori:item.excavation_strength").text.Replace("{value}", item.excavationStrength.ToString()));
            sb.AppendLine(GameUI.CompareText("ori:item.use_cd").text.Replace("{value}", item.useCD.ToString()));
            sb.AppendLine(string.Empty);

            //如果成功匹配到了描述文本
            if (GameUI.TryCompareTextNullable(item.description, out var description))
                sb.Append(description.text);

            return sb;
        }
    }














    public class CraftingInfoShower
    {
        public Player player;
        public ImageIdentity background;
        public ScrollViewIdentity ingredientsView;
        public ImageIdentity arrow;
        public ScrollViewIdentity resultsView;
        public TextIdentity maximumCraftingTimesText;
        private readonly StringBuilder stringBuilder = new();

        public void Show(CraftingRecipe recipe, List<Dictionary<int, ushort>> ingredients)
        {
            stringBuilder.Clear();
            stringBuilder.AppendLine(GameUI.CompareText("可合成次数(TODO)").text.Replace("{value}", recipe.ingredients.Length.ToString()));


            Vector2 pos = GControls.cursorPosInMainCanvas;
            pos.x += background.sd.x * 0.75f;
            pos.y -= background.sd.y * 0.75f;


            background.ap = pos;
            maximumCraftingTimesText.text.text = $"<color=#E0E0E0>{stringBuilder}</color>";

            //显示原料
            ingredientsView.Clear();
            foreach (var ele in ingredients)
            {
                foreach (var ingredient in ele)
                {
                    Item itemGot = player.inventory.GetItem(ingredient.Key);

                    //图标
                    var ingredientsBackground = GameUI.AddImage(UPC.Middle, $"ori:image.crafting_info_shower.ingredients_background_{recipe.id}", "ori:item_tab");
                    var ingredientsIcon = GameUI.AddImage(UPC.Middle, $"ori:button.crafting_info_shower.ingredients_{ingredient.Key}", null, ingredientsBackground);
                    var ingredientsText = GameUI.AddText(UPC.Middle, $"ori:text.crafting_info_shower.ingredients_{recipe.id}", ingredientsBackground);

                    ingredientsIcon.SetSizeDelta(ingredientsView.gridLayoutGroup.cellSize);
                    ingredientsIcon.image.sprite = Item.Null(itemGot) ? null : itemGot.data.texture.sprite;

                    ingredientsText.autoCompareText = false;
                    ingredientsText.text.enableAutoSizing = true;
                    ingredientsText.text.fontSizeMin = 0;
                    ingredientsText.text.text = $"{GameUI.CompareText(itemGot.data.id)?.text}x{ingredient.Value}";
                    ingredientsText.text.margin = Vector4.zero;
                    ingredientsText.SetSizeDelta(ingredientsIcon.sd.x, 8);
                    ingredientsText.SetAPosY(ingredientsIcon.ap.y / 2 - ingredientsIcon.sd.y / 2 - ingredientsText.sd.y / 2);

                    ingredientsView.AddChild(ingredientsBackground);
                }
            }

            //显示结果
            resultsView.Clear();

            var iconBackground = GameUI.AddImage(UPC.Middle, $"ori:image.crafting_info_shower.result_background_{recipe.id}", "ori:item_tab");
            var icon = GameUI.AddImage(UPC.Middle, $"ori:image.crafting_info_shower.result_{recipe.id}", "ori:item_tab", iconBackground);
            var iconText = GameUI.AddText(UPC.Middle, $"ori:text.crafting_info_shower.result_{recipe.id}", iconBackground);

            icon.SetSizeDelta(resultsView.gridLayoutGroup.cellSize);
            icon.image.sprite = ModFactory.CompareItem(recipe.result.id).texture.sprite;

            iconText.autoCompareText = false;
            iconText.text.enableAutoSizing = true;
            iconText.text.fontSizeMin = 0;
            iconText.text.text = $"{GameUI.CompareText(recipe.result.id)?.text}x{recipe.result.count}";
            iconText.text.margin = Vector4.zero;
            iconText.SetSizeDelta(icon.sd.x, 8);
            iconText.SetAPosY(icon.ap.y / 2 - icon.sd.y / 2 - iconText.sd.y / 2);

            resultsView.AddChild(iconBackground);


            background.gameObject.SetActive(true);
        }


        public void Hide()
        {
            background.gameObject.SetActive(false);
        }






        public CraftingInfoShower(Player player, ImageIdentity background, ScrollViewIdentity ingredientsView, ImageIdentity arrow, ScrollViewIdentity resultsView, TextIdentity maximumCraftingTimesText)
        {
            this.player = player;
            this.background = background;
            this.ingredientsView = ingredientsView;
            this.arrow = arrow;
            this.resultsView = resultsView;
            this.maximumCraftingTimesText = maximumCraftingTimesText;
        }
    }














    public class TaskInfoShower : NameAndDetailInfoShower<TaskInfoShower>
    {
        public TaskInfoShower()
        {
            backgroundImage = GameUI.AddImage(UPC.Middle, "ori:image.task_info_shower", "ori:item_info_shower");
            nameText = GameUI.AddText(UPC.UpperLeft, "ori:text.task_info_shower.name", backgroundImage);
            detailText = GameUI.AddText(UPC.UpperLeft, "ori:text.task_info_shower.detail", backgroundImage);

            nameText.text.alignment = TMPro.TextAlignmentOptions.Left;
            detailText.text.alignment = TMPro.TextAlignmentOptions.TopLeft;

            backgroundImage.SetSizeDelta(200, 200);
            nameText.SetSizeDelta(backgroundImage.sd.x, 30);
            detailText.SetSizeDelta(nameText.sd.x, backgroundImage.sd.y - nameText.sd.y);

            nameText.SetAPos(nameText.sd.x / 2, -nameText.sd.y / 2 - 5);
            detailText.SetAPos(nameText.ap.x, nameText.ap.y - nameText.sd.y / 2 - detailText.sd.y / 2 - 5);

            nameText.text.SetFontSize(18);
            detailText.text.SetFontSize(13);

            backgroundImage.image.raycastTarget = false;
            nameText.text.raycastTarget = false;
            detailText.text.raycastTarget = false;
        }

        public void Show(TaskNode task) => Show(task.button.buttonText.text.text, GetText(task).ToString());

        public static StringBuilder GetText(TaskNode task)
        {
            StringBuilder sb = new();

            if (task.data.rewards != null)
            {
                foreach (var reward in task.data.rewards)
                {
                    if (string.IsNullOrWhiteSpace(reward))
                        continue;

                    if (Drop.ConvertStringItem(reward, out string id, out ushort count, out _, out _))
                    {
                        sb.AppendLine(GameUI.CompareText("ori:task.rewards").text.Replace("{id}", GameUI.CompareText(id).text).Replace("{count}", count.ToString()));
                    }
                }
            }

            return sb;
        }
    }
}