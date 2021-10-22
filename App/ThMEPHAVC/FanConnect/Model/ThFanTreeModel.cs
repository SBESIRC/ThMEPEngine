using Autodesk.AutoCAD.DatabaseServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPHVAC.FanConnect.Model
{
    public class ThFanTreeNode<T>
    {
        /// <summary>
        /// 子节点
        /// </summary>
        public List<ThFanTreeNode<T>> Children { set; get; }
        /// <summary>
        /// 父节点
        /// </summary>
        public ThFanTreeNode<T> Parent { set; get; }
        /// <summary>
        /// 当前节点
        /// </summary>
        public T Item { set; get; }
        public ThFanTreeNode(T item)
        {
            Children = new List<ThFanTreeNode<T>>();
            Parent = null;
            Item = item;
        }

        public void InsertChild(ThFanTreeNode<T> child)
        {
            Children.Add(child);
        }
    }
    public class ThFanTreeModel
    {
        public ThFanTreeNode<Line> RootNode { set; get; }
        public ThFanTreeModel()
        {
            RootNode = new ThFanTreeNode<Line>(new Line());
        }
        //遍历树，找到对应的节点
        public ThFanTreeNode<Line> FandTreeNode(Line line)
        {
            return null;
        }
    }
}
