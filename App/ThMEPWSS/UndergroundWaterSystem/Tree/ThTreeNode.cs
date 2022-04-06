using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPWSS.UndergroundWaterSystem.Tree
{
    public class ThTreeNode<T>
    {
        /// <summary>
        /// 子节点
        /// </summary>
        public List<ThTreeNode<T>> Children { set; get; }
        /// <summary>
        /// 父节点
        /// </summary>
        public ThTreeNode<T> Parent { set; get; }
        /// <summary>
        /// 当前节点值
        /// </summary>
        public T Item { set; get; }
        public ThTreeNode(T item)
        {
            Children = new List<ThTreeNode<T>>();
            Parent = null;
            Item = item;
        }
        public void InsertChild(ThTreeNode<T> child)
        {
            child.Parent = this;
            Children.Add(child);
        }
        public ThTreeNode<T> GetChild(int index)
        {
            return Children[index];
        }
        public int ChildIndex(ThTreeNode<T> child)
        {
            for (int i = 0; i < Children.Count(); i++)
            {
                if (Children[i] == child)
                {
                    return i;
                }
            }
            return -1;
        }
    }
}
