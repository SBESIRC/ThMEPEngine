using System;
using System.Collections.Generic;

namespace ThMEPEngineCore.Algorithm.AStarRoutingEngine.PublicMethod
{
    /// <summary>
    /// 最小堆
    /// </summary>
    public class MinHeap<T>
    {
        //默认容量为6
        private const int DEFAULT_CAPACITY = 100;
        private T[] mItems;
        private Comparer<T> mComparer;
        // 堆中数据量
        public int Count { get; private set; }

        public MinHeap() : this(DEFAULT_CAPACITY) { }

        public MinHeap(int capacity)
        {
            if (capacity < 0)
            {
                throw new IndexOutOfRangeException();
            }
            mItems = new T[capacity];
            mComparer = Comparer<T>.Default;
        }

        /// <summary>
        /// 增加元素到堆，并从后往前依次对各结点为根的子树进行筛选，使之成为堆，直到根结点
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool Enqueue(T value)
        {
            if (Count >= mItems.Length)
            {
                ResizeItemStore(mItems.Length * 2);
            }

            mItems[Count++] = value;
            int position = BubbleUp(Count - 1);

            return (position == 0);
        }

        /// <summary>
        /// 取出堆的最小值
        /// </summary>
        /// <returns></returns>
        public T Dequeue()
        {
            return Dequeue(true);
        }

        private T Dequeue(bool shrink)
        {
            if (Count == 0)
            {
                throw new InvalidOperationException();
            }
            T result = mItems[0];
            if (Count == 1)
            {
                Count = 0;
                mItems[0] = default(T);
            }
            else
            {
                --Count;
                //取序列最后的元素放在堆顶
                mItems[0] = mItems[Count];
                mItems[Count] = default(T);
                // 维护堆的结构
                BubbleDown();
            }
            if (shrink)
            {
                ShrinkStore();
            }
            return result;
        }

        /// <summary>
        /// 删除目标元素
        /// </summary>
        /// <param name="node"></param>
        public void Remove(T node)
        {
            Remove(node, true);
        }

        private void Remove(T node, bool shrink)
        {
            var index = FindIndex(node);
            if (index < 0)
            {
                return;
            }

            if (Count == 1)
            {
                Count = 0;
                mItems[0] = default(T);
            }
            else
            {
                --Count;
                //取序列最后的元素放在堆顶
                mItems[index] = mItems[Count];
                mItems[Count] = default(T);
                // 维护堆的结构
                BubbleDown();
            }
            if (shrink)
            {
                ShrinkStore();
            }
        } 

        private void ShrinkStore()
        {
            // 如果容量不足一半以上，默认容量会下降。
            if (mItems.Length > DEFAULT_CAPACITY && Count < mItems.Length / 3)
            {
                int newSize = Math.Max(
                    DEFAULT_CAPACITY, (((Count / DEFAULT_CAPACITY) + 1) * DEFAULT_CAPACITY));

                ResizeItemStore(newSize);
            }
        }

        private void ResizeItemStore(int newSize)
        {
            T[] temp = new T[newSize];
            Array.Copy(mItems, 0, temp, 0, Count);
            mItems = temp;
        }

        public void Clear()
        {
            Count = 0;
            mItems = new T[DEFAULT_CAPACITY];
        }

        /// <summary>
        /// 从前往后依次对各结点为根的子树进行筛选，使之成为堆，直到序列最后的节点
        /// </summary>
        private void BubbleDown()
        {
            int parent = 0;
            int leftChild = (parent * 2) + 1;
            while (leftChild < Count)
            {
                // 找到子节点中较小的那个
                int rightChild = leftChild + 1;
                int bestChild = (rightChild < Count && mComparer.Compare(mItems[rightChild], mItems[leftChild]) < 0) ?
                    rightChild : leftChild;
                if (mComparer.Compare(mItems[bestChild], mItems[parent]) < 0)
                {
                    // 如果子节点小于父节点, 交换子节点和父节点
                    T temp = mItems[parent];
                    mItems[parent] = mItems[bestChild];
                    mItems[bestChild] = temp;
                    parent = bestChild;
                    leftChild = (parent * 2) + 1;
                }
                else
                {
                    break;
                }
            }
        }

        /// <summary>
        /// 从后往前依次对各结点为根的子树进行筛选，使之成为堆，直到根结点
        /// </summary>
        /// <param name="startIndex"></param>
        /// <returns></returns>
        private int BubbleUp(int startIndex)
        {
            while (startIndex > 0)
            {
                int parent = (startIndex - 1) / 2;
                //如果子节点小于父节点，交换子节点和父节点
                if (mComparer.Compare(mItems[startIndex], mItems[parent]) < 0)
                {
                    T temp = mItems[startIndex];
                    mItems[startIndex] = mItems[parent];
                    mItems[parent] = temp;
                }
                else
                {
                    break;
                }
                startIndex = parent;
            }
            return startIndex;
        }

        /// <summary>
        /// 从后往前依次对各结点为根的子树进行筛选，使之成为堆，直到根结点
        /// </summary>
        /// <param name="startIndex"></param>
        /// <returns></returns>
        public bool Contains(T node)
        {
            for (int i = 0; i < Count; i++)
            {
                if (mComparer.Compare(mItems[i], node) == 0)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 找到目标节点
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        public T Find(T node)
        {
            var index = FindIndex(node);
            if (index >= 0)
            {
                return mItems[index];
            }

            return default(T);
        }

        private int FindIndex(T node)
        {
            for (int i = 0; i < Count; i++)
            {
                if (mComparer.Compare(mItems[i], node) == 0)
                {
                    return i;
                }
            }

            return -1;
        }
    }
}
