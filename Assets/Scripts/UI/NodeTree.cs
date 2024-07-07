using System;
using System.Collections.Generic;
using SP.Tools.Unity;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GameCore.UI
{
    public class NodeTree<TTreeNode, TTreeNodeData>
        where TTreeNode : TreeNode<TTreeNodeData>
        where TTreeNodeData : TreeNodeData
    {
        public TTreeNode rootNode;
        public ScrollViewIdentity nodeTreeView;

        public string nodeTreeViewName;
        public List<TTreeNodeData> nodeDataList = new();
        public Func<TTreeNode, Color> GetNodeColor;
        public Action<TTreeNode> OnPointerStayAction;
        public Action<PointerEventData> OnPointerExitAction;
        public Action<TTreeNode> OnClick;

        public TTreeNode FindTreeNode(string id)
        {
            //从根节点开始迭代
            Queue<TTreeNode> queue = new();
            queue.Enqueue(rootNode);

            while (queue.Count > 0)
            {
                TTreeNode currentNode = queue.Dequeue();

                //如果本身就是目标节点，则返回
                if (currentNode.data.id == id)
                {
                    return currentNode;
                }

                //把子节点加入迭代队列
                foreach (var childNode in currentNode.nodes)
                {
                    queue.Enqueue((TTreeNode)childNode);
                }
            }

            return null;
        }

        public NodeTree(
            string nodeTreeViewName,
            List<TTreeNodeData> nodeDataList,
            Transform nodeTreeViewParent,
            Func<TTreeNode, Color> GetNodeColor,
            Action<TTreeNode> OnPointerStayAction,
            Action<PointerEventData> OnPointerExitAction,
            Action<TTreeNode> OnClick)
        {
            this.nodeTreeViewName = nodeTreeViewName;
            this.nodeDataList = nodeDataList;
            this.GetNodeColor = GetNodeColor;
            this.OnPointerStayAction = OnPointerStayAction;
            this.OnPointerExitAction = OnPointerExitAction;
            this.OnClick = OnClick;




            nodeTreeView = GameUI.AddScrollView(UIA.StretchDouble, $"ori:view.{nodeTreeViewName}", nodeTreeViewParent);
            nodeTreeView.scrollViewImage.color = new(0.2f, 0.2f, 0.2f, 0.6f);
            nodeTreeView.rt.sizeDelta = Vector2.zero;
            nodeTreeView.content.anchoredPosition = new(GameUI.canvasScaler.referenceResolution.x / 2, GameUI.canvasScaler.referenceResolution.y / 2);  //将节点居中
            nodeTreeView.scrollRect.horizontal = true;   //允许水平拖拽
            nodeTreeView.scrollRect.movementType = ScrollRect.MovementType.Unrestricted;   //不限制拖拽
            nodeTreeView.scrollRect.scrollSensitivity = 0;   //不允许滚轮控制
            nodeTreeView.gameObject.AddComponent<RectMask2D>();   //添加新的遮罩
            UnityEngine.Object.Destroy(nodeTreeView.viewportMask);   //删除自带的遮罩
            UnityEngine.Object.Destroy(nodeTreeView.gridLayoutGroup);   //删除自动排序器
            UnityEngine.Object.Destroy(nodeTreeView.scrollRect.horizontalScrollbar.gameObject);   //删除水平滚动条
            UnityEngine.Object.Destroy(nodeTreeView.scrollRect.verticalScrollbar.gameObject);   //删除水平滚动条




            //找到节点树的根节点并创建
            foreach (var item in nodeDataList)
            {
                if (string.IsNullOrWhiteSpace(item.parent))
                {
                    rootNode = CreateChildrenNodesFor(item);
                    break;
                }
            }

            TTreeNode CreateChildrenNodesFor(TTreeNodeData data)
            {
                //创建节点
                TTreeNode temp = (TTreeNode)Activator.CreateInstance(typeof(TTreeNode), new object[] { data });

                //找到所有子节点并创建
                foreach (var nodeData in nodeDataList)
                {
                    //如果是 current 的子节点
                    if (nodeData.parent == data.id)
                    {
                        temp.nodes.Add(CreateChildrenNodesFor(nodeData));
                    }
                }

                return temp;
            }

            /* --------------------------------- 显示节点树 --------------------------------- */
            RefreshNodes(true);
        }










        /// <summary>
        /// 以根节点为起点，刷新节点树的显示
        /// </summary>
        /// <param name="completelyInit"></param>
        public void RefreshNodes(bool completelyInit)
        {
            if (completelyInit)
                nodeTreeView.Clear();

            RefreshChildrenNodesOf(rootNode, null, completelyInit);
        }

        /// <summary>
        /// 刷新指定节点的子节点（会一直递归到最底层）
        /// </summary>
        /// <param name="current"></param>
        /// <param name="parentNode"></param>
        /// <param name="completelyInit"></param>
        private void RefreshChildrenNodesOf(TTreeNode current, TTreeNode parentNode, bool completelyInit)
        {
            //初始化按钮
            if (completelyInit)
                InitNodeButton(current, parentNode);


            //设置图标
            current.icon.SetID($"ori:image.{nodeTreeViewName}.node.{current.data.id}");
            current.icon.image.sprite = ModFactory.CompareTexture(current.data.icon).sprite;


            //设置按钮和图标的颜色
            current.icon.image.color = current.button.image.color = GetNodeColor?.Invoke(current) ?? Color.white;


            //如果有连线还要设置线的颜色
            if (current.line) current.line.image.color = current.icon.image.color;


            /* ---------------------------------- 初始化子节点 --------------------------------- */
            foreach (var node in current.nodes)
            {
                RefreshChildrenNodesOf((TTreeNode)node, current, completelyInit);
            }
        }

        /// <summary>
        /// 初始化节点的按钮和图标
        /// </summary>
        /// <param name="node"></param>
        /// <param name="parentNode"></param>
        private void InitNodeButton(TTreeNode node, TTreeNode parentNode)
        {
            /* ---------------------------------- 初始化按钮 --------------------------------- */
            int space = 40;
            node.button = GameUI.AddButton(UIA.Middle, $"ori:button.{nodeTreeViewName}.node.{node.data.id}", GameUI.canvas.transform, "ori:square_button");
            node.parent = parentNode;
            node.button.SetSizeDelta(space, space);   //设置按钮大小
            node.button.buttonText.RefreshUI();

            /* ---------------------------------- 绑定按钮 ---------------------------------- */
            node.button.button.OnPointerStayAction = () => OnPointerStayAction(node);
            node.button.button.OnPointerExitAction = pointerEventData => OnPointerExitAction(pointerEventData);
            node.button.OnClickBind(() => OnClick(node));

            /* ---------------------------------- 设置父物体 --------------------------------- */
            if (parentNode == null)
                nodeTreeView.AddChild(node.button);
            else
                node.button.SetParentForUI(parentNode.button);

            /* -------------------------------- 根据父节点更改位置 ------------------------------- */
            Vector2 tempVec = Vector2.zero;
            if (parentNode != null) { tempVec.y -= node.button.sd.y + space; }

            //统计子节点数量
            int childrenCountOfCurrentNode = GetChildrenOfNode(node.data).Count;

            /* ------------------------------- 根据同级节点更改位置 ------------------------------- */
            var level = GetChildrenOfNode(node.data.parent);
            var indexInNodeLevel = GetIndexInNodeLevel(node.data);
            float countOfChildrenNodesThatCauseCoincidence = 0;
            bool isLeftNode = indexInNodeLevel < level.Count / 2;

            if (level.Count == 1)
            {
                
            }
            else
            {
                //中间的节点
                if (level.Count % 2 == 1 && indexInNodeLevel == (level.Count - 1) / 2)
                {
                    Debug.Log($"{node.data.id} 是中间节点！");
                }
                //左侧的节点
                if (isLeftNode)
                {
                    //注：这会遍历到自己
                    for (int i = indexInNodeLevel; i < (level.Count + 1) / 2; i++)
                    {
                        countOfChildrenNodesThatCauseCoincidence += GetMaxNodeCountLayerOfNodeLine(level[i]);
                    }

                    float deltaPos = space * 1.5f * countOfChildrenNodesThatCauseCoincidence;
                    tempVec.x -= deltaPos;
                }
                //右侧的节点
                else
                {
                    //注：这会遍历到自己
                    for (int i = level.Count / 2; i < indexInNodeLevel + 1; i++)
                    {
                        countOfChildrenNodesThatCauseCoincidence += GetMaxNodeCountLayerOfNodeLine(level[i]);
                    }

                    float deltaPos = space * 1.5f * countOfChildrenNodesThatCauseCoincidence;
                    tempVec.x += deltaPos;
                }
            }



            /* -------------------------------- 设置按钮和文本位置 ------------------------------- */
            node.button.ap = tempVec;
            node.button.buttonText.AddAPosY(-node.button.sd.y / 2 - node.button.buttonText.sd.y / 2 - 5);

            /* ---------------------------------- 设置图标 ---------------------------------- */
            node.icon = GameUI.AddImage(UIA.Middle, $"ori:image.{nodeTreeViewName}.node.{node.data.id}", null, node.button);
            node.icon.sd = node.button.sd;

            /* --------------------------------- 初始化连接线 --------------------------------- */
            ConnectNodeLine(node);
        }

        /// <summary>
        /// 初始化节点之间的连接线
        /// </summary>
        /// <param name="node"></param>
        private void ConnectNodeLine(TTreeNode node)
        {
            if (node.parent == null)
                return;

            //如果没有线就创建
            if (!node.line)
                node.line = GameUI.AddImage(UIA.Middle, $"ori:button.{nodeTreeViewName}.node.{node.data.id}.line", null, node.button);

            /* --------------------------------- 计算对应顶点 --------------------------------- */
            Vector2 buttonPoint = new(node.button.ap.x, node.button.ap.y + node.button.sd.y / 2);   //本身按钮上方
            Vector2 parentPoint = new(0, -node.button.sd.y / 2);   //父节点按钮下方

            /* ---------------------------------- 设置大小 ---------------------------------- */
            node.line.sd = new(Vector2.Distance(buttonPoint, parentPoint), 2);   //x轴为长度, y轴为宽度

            /* ---------------------------------- 设置旋转角 --------------------------------- */
            node.line.rt.localEulerAngles = new(0, 0, AngleTools.GetAngleFloat(buttonPoint, parentPoint) - 90);   //获取角度并旋转 -90 度 (我也不知道为啥)
            if (node.button.ap.x < 0) node.line.rt.localEulerAngles = new(0, 180, node.line.rt.localEulerAngles.z);   //如果按钮在父节点左侧就水平翻转

            /* ---------------------------------- 设置位置 ---------------------------------- */
            Vector2 temp = Vector2.zero;
            temp.x += 0.5f * (parentPoint.x - buttonPoint.x);   //使得线贴在按钮中间
            temp.y += node.button.sd.y;   //使得线贴在按钮上方
            node.line.ap = temp;
        }















        /// <summary>
        /// 这个方法会扫描该节点的所有子节点，并找到节点数最多的那一层的节点数
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        private int GetMaxNodeCountLayerOfNodeLine(TTreeNodeData data)
        {
            int result = GetChildrenOfNode(data).Count;


            var list = GetChildrenOfNode(data);
            foreach (var item in list)
            {
                var temp = GetMaxNodeCountLayerOfNodeLine(item);
                if (temp > result)
                    result = temp;
            }

            return Mathf.Max(1, result); //最低也是一个节点，因为 data 参数自己也算一个节点
        }

        private int GetTotalChildrenCountOfNodeLine(TTreeNodeData data)
        {
            int result = 0;

            foreach (var current in nodeDataList)
            {
                if (current.parent == data.id)
                {
                    result++;
                    result += GetTotalChildrenCountOfNodeLine(current);
                }
            }

            return result;
        }

        private int GetIndexInNodeLevel(TTreeNodeData data)
        {
            List<string> list = new();

            foreach (var current in nodeDataList)
                if (current.parent == data.parent)
                    list.Add(current.id);

            return list.IndexOf(data.id);
        }

        private List<TTreeNodeData> GetSiblingsOfNode(TTreeNodeData data)
        {
            List<TTreeNodeData> result = new();

            foreach (var current in nodeDataList)
                if (current.parent == data.parent && current.id != data.id)
                    result.Add(current);

            return result;
        }

        private List<TTreeNodeData> GetChildrenOfNode(TTreeNodeData data) => GetChildrenOfNode(data.id);
        private List<TTreeNodeData> GetChildrenOfNode(string nodeId)
        {
            List<TTreeNodeData> result = new();

            foreach (var current in nodeDataList)
                if (current.parent == nodeId)
                    result.Add(current);

            return result;
        }
    }

    public class TreeNodeData
    {
        public string id;
        public string parent;
        public string icon;

        public TreeNodeData(string id, string icon, string parent)
        {
            this.id = id;
            this.icon = icon;
            this.parent = parent;
        }
    }

    public class TreeNode<TTreeNodeData> where TTreeNodeData : TreeNodeData
    {
        public ButtonIdentity button;
        public ImageIdentity icon;
        public TTreeNodeData data;
        public TreeNode<TTreeNodeData> parent;
        public ImageIdentity line;
        public List<TreeNode<TTreeNodeData>> nodes = new();

        public TreeNode(TTreeNodeData data)
        {
            this.data = data;
        }
    }
}