﻿using System;
using System.Collections.Generic;

namespace NFox.Collections
{
    /// <summary>
    /// 环链表节点
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class LoopListNode<T>
    {
        /// <summary>
        /// 取值
        /// </summary>
        public T Value { get; set; }

        /// <summary>
        /// 上一个节点
        /// </summary>
        public LoopListNode<T> Previous
        { internal set; get; }

        /// <summary>
        /// 下一个节点
        /// </summary>
        public LoopListNode<T> Next
        { internal set; get; }

        /// <summary>
        ///环链表序列
        /// </summary>
        public LoopList<T> List
        { internal set; get; }
        /// <summary>
        /// 环链表节点构造函数
        /// </summary>
        /// <param name="value">节点值</param>
        public LoopListNode(T value)
        {
            Value = value;
        }
        /// <summary>
        /// 获取当前节点的临近节点
        /// </summary>
        /// <param name="forward">搜索方向标志，<see langword="true"/>为向前搜索，<see langword="false"/>为向后搜索</param>
        /// <returns></returns>
        public LoopListNode<T> GetNext(bool forward)
        {
            return forward ? Next : Previous;
        }
    }

    /// <summary>
    /// 环链表
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class LoopList<T> :
        IEnumerable<T>, IFormattable
    {
        /// <summary>
        /// 默认构造函数
        /// </summary>
        public LoopList()
        { }
        /// <summary>
        /// 环链表构造函数
        /// </summary>
        /// <param name="values">节点迭代器</param>
        public LoopList(IEnumerable<T> values)
        {
            foreach (T value in values)
                Add(value);
        }

        /// <summary>
        /// 节点数
        /// </summary>
        public int Count
        { get; private set; }

        /// <summary>
        /// 首节点
        /// </summary>
        public LoopListNode<T> First
        { get; private set; }

        /// <summary>
        /// 尾节点
        /// </summary>
        public LoopListNode<T> Last
        {
            get { return First?.Previous; }
        }
        /// <summary>
        /// 设置首节点
        /// </summary>
        /// <param name="node">节点</param>
        /// <returns></returns>
        public bool SetFirst(LoopListNode<T> node)
        {
            if (Contains(node))
            {
                First = node;
                return true;
            }
            return false;
        }

        /// <summary>
        /// 交换两个节点的值
        /// </summary>
        /// <param name="node1">第一个节点</param>
        /// <param name="node2">第二个节点</param>
        public void Swap(LoopListNode<T> node1, LoopListNode<T> node2)
        {
            T value = node1.Value;
            node1.Value = node2.Value;
            node2.Value = value;
        }

        #region Contains
        /// <summary>
        /// 是否包含节点
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        public bool Contains(LoopListNode<T> node)
        {
            return node != null && node.List == this;
        }
        /// <summary>
        /// 是否包含值
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool Contains(T value)
        {
            LoopListNode<T> node = First;
            if (node == null)
                return false;

            for (int i = 0; i < Count; i++)
            {
                if (node.Value.Equals(value))
                    return true;
                node = node.Next;
            }

            return false;
        }
        /// <summary>
        /// 获取节点
        /// </summary>
        /// <param name="func"></param>
        /// <returns></returns>
        public LoopListNode<T> GetNode(Func<T, bool> func)
        {
            LoopListNode<T> node = First;
            if (node == null)
                return null;

            for (int i = 0; i < Count; i++)
            {
                if (func(node.Value))
                {
                    return node;
                }
                node = node.Next;
            }
            return null;
        }

        #endregion Contains

        #region Add

        /// <summary>
        /// 在首节点之前插入节点,并设置新节点为首节点
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public LoopListNode<T> AddFirst(T value)
        {
            LoopListNode<T> node = new LoopListNode<T>(value);
            node.List = this;
            if (Count == 0)
            {
                First = node;
                First.Previous = First.Next = node;
            }
            else
            {
                LoopListNode<T> last = Last;
                First.Previous = last.Next = node;
                node.Next = First;
                node.Previous = last;
                First = node;
            }
            Count++;
            return First;
        }

        /// <summary>
        ///  在尾节点之后插入节点,并设置新节点为尾节点
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public LoopListNode<T> Add(T value)
        {
            LoopListNode<T> node = new LoopListNode<T>(value);
            node.List = this;
            if (Count == 0)
            {
                First = node;
                First.Previous = First.Next = node;
            }
            else
            {
                LoopListNode<T> last = First.Previous;
                First.Previous = last.Next = node;
                node.Next = First;
                node.Previous = last;
            }
            Count++;
            return Last;
        }
        /// <summary>
        /// 前面增加节点
        /// </summary>
        /// <param name="node"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public LoopListNode<T> AddBefore(LoopListNode<T> node, T value)
        {
            if (node == First)
            {
                return AddFirst(value);
            }
            else
            {
                LoopListNode<T> tnode = new LoopListNode<T>(value);
                node.Previous.Next = tnode;
                tnode.Previous = node.Previous;
                node.Previous = tnode;
                tnode.Next = node;
                Count++;
                return tnode;
            }
        }
        /// <summary>
        /// 后面增加节点
        /// </summary>
        /// <param name="node"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public LoopListNode<T> AddAfter(LoopListNode<T> node, T value)
        {
            LoopListNode<T> tnode = new LoopListNode<T>(value);
            node.Next.Previous = tnode;
            tnode.Next = node.Next;
            node.Next = tnode;
            tnode.Previous = node;
            Count++;
            return tnode;
        }

        #endregion Add

        #region Remove

        /// <summary>
        /// 删除首节点
        /// </summary>
        /// <returns></returns>
        public bool RemoveFirst()
        {
            switch (Count)
            {
                case 0:
                    return false;

                case 1:
                    First = null;
                    break;

                default:
                    LoopListNode<T> last = Last;
                    First = First.Next;
                    First.Previous = last;
                    last.Next = First;
                    break;
            }
            Count--;
            return true;
        }

        /// <summary>
        /// 删除尾节点
        /// </summary>
        /// <returns></returns>
        public bool RemoveLast()
        {
            switch (Count)
            {
                case 0:
                    return false;

                case 1:
                    First = null;
                    break;

                default:
                    LoopListNode<T> last = Last.Previous;
                    last.Next = First;
                    First.Previous = last;
                    break;
            }
            Count--;
            return true;
        }

        /// <summary>
        /// 删除节点
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        public bool Remove(LoopListNode<T> node)
        {
            if (Contains(node))
            {
                if (Count == 1)
                {
                    First = null;
                }
                else
                {
                    if (node == First)
                    {
                        RemoveFirst();
                    }
                    else
                    {
                        node.Next.Previous = node.Previous;
                        node.Previous.Next = node.Next;
                    }
                }
                Count--;
                return true;
            }
            return false;
        }

        #endregion Remove

        #region LinkTo

        /// <summary>
        /// 链接两节点,并去除这两个节点间的所有节点
        /// </summary>
        /// <param name="from"></param>
        /// <param name="to"></param>
        public void LinkTo(LoopListNode<T> from, LoopListNode<T> to)
        {
            if (from != to && Contains(from) && Contains(to))
            {
                LoopListNode<T> node = from.Next;
                bool isFirstChanged = false;
                int number = 0;

                while (node != to)
                {
                    if (node == First)
                        isFirstChanged = true;

                    node = node.Next;
                    number++;
                }

                from.Next = to;
                to.Previous = from;

                if (number > 0 && isFirstChanged)
                    First = to;

                Count -= number;
            }
        }

        /// <summary>
        /// 链接两节点,并去除这两个节点间的所有节点
        /// </summary>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <param name="number"></param>
        public void LinkTo(LoopListNode<T> from, LoopListNode<T> to, int number)
        {
            if (from != to && Contains(from) && Contains(to))
            {
                from.Next = to;
                to.Previous = from;
                First = to;
                Count -= number;
            }
        }

        /// <summary>
        /// 链接两节点,并去除这两个节点间的所有节点
        /// </summary>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <param name="number"></param>
        /// <param name="isFirstChanged"></param>
        public void LinkTo(LoopListNode<T> from, LoopListNode<T> to, int number, bool isFirstChanged)
        {
            if (from != to && Contains(from) && Contains(to))
            {
                from.Next = to;
                to.Previous = from;
                if (isFirstChanged)
                    First = to;
                Count -= number;
            }
        }

        #endregion LinkTo

        #region IEnumerable<T> 成员

        /// <summary>
        /// 获取节点的查询器
        /// </summary>
        /// <param name="from"></param>
        /// <returns></returns>
        public IEnumerable<LoopListNode<T>> GetNodes(LoopListNode<T> from)
        {
            LoopListNode<T> node = from;
            for (int i = 0; i < Count; i++)
            {
                yield return node;
                node = node.Next;
            }
        }

        /// <summary>
        /// 获取节点的查询器
        /// </summary>
        /// <returns></returns>
        public IEnumerable<LoopListNode<T>> GetNodes()
        {
            LoopListNode<T> node = First;
            for (int i = 0; i < Count; i++)
            {
                yield return node;
                node = node.Next;
            }
        }

        /// <summary>
        /// 获取节点值的查询器
        /// </summary>
        /// <returns></returns>
        public IEnumerator<T> GetEnumerator()
        {
            LoopListNode<T> node = First;
            for (int i = 0; i < Count; i++)
            {
                yield return node.Value;
                node = node.Next;
            }
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return GetEnumerator();
        }

        #region IEnumerable 成员

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion IEnumerable 成员

        #endregion IEnumerable<T> 成员

        #region IFormattable 成员
        /// <summary>
        /// 转换为字符串
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            string s = "( ";
            foreach (T value in this)
            {
                s += value.ToString() + " ";
            }
            return s + ")";
        }

        string IFormattable.ToString(string format, IFormatProvider formatProvider)
        {
            return ToString();
        }

        #endregion IFormattable 成员
    }
}