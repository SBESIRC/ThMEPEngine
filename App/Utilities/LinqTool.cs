using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace TianHua.AutoCAD.Utility.ExtensionTools
{
    public static class LinqTool
    {
        //让IEnumerable<T>也可以使用foreach
        public static void ForEach<T>(this IEnumerable<T> source, Action<T> func)
        {
            foreach (var item in source)
                func(item);
        }


        public static ObservableCollection<T> ToObservableCollection<T>(this IEnumerable<T> from)
        {
            ObservableCollection<T> to = new ObservableCollection<T>();
            foreach (var f in from)
            {
                to.Add(f);
            }
            return to;
        }


        //自定义找某个属性为最大的那个元素
        public static TElement MaxElement<TElement, TData>(
          this IEnumerable<TElement> source,
          Func<TElement, TData> selector)
          where TData : IComparable<TData>
        {
            if (source == null)
                throw new ArgumentNullException("source");
            if (selector == null)
                throw new ArgumentNullException("selector");

            Boolean firstElement = true;
            TElement result = default(TElement);
            TData maxValue = default(TData);
            foreach (TElement element in source)
            {
                var candidate = selector(element);
                if (firstElement || (candidate.CompareTo(maxValue) > 0))
                {
                    firstElement = false;
                    maxValue = candidate;
                    result = element;
                }
            }
            return result;
        }

        //自定义找某个属性为最小的那个元素
        public static TElement MinElement<TElement, TData>(
          this IEnumerable<TElement> source,
          Func<TElement, TData> selector)
          where TData : IComparable<TData>
        {
            if (source == null)
                throw new ArgumentNullException("source");
            if (selector == null)
                throw new ArgumentNullException("selector");

            Boolean firstElement = true;
            TElement result = default(TElement);
            TData maxValue = default(TData);
            foreach (TElement element in source)
            {
                var candidate = selector(element);
                if (firstElement || (candidate.CompareTo(maxValue) < 0))
                {
                    firstElement = false;
                    maxValue = candidate;
                    result = element;
                }
            }
            return result;
        }



        /// <summary>
        /// 按照一定的规律按顺序拿取归为一组
        /// </summary>
        /// <typeparam name="TElement"></typeparam>
        /// <param name="source"></param>
        /// <param name="predicate"></param>
        /// <param name="func"></param>
        /// <returns></returns>
        public static IEnumerable<IEnumerable<TElement>> GroupTake<TElement>(this IEnumerable<TElement> source, Func<TElement, bool> predicate, Func<IEnumerable<TElement>, int> func)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            if (predicate == null)
                throw new ArgumentNullException("predicate");
            if (func == null)
                throw new ArgumentNullException("func");

            var results = new List<List<TElement>>();

            while (true)
            {
                //从303开始遍历
                source = source.SkipWhile(predicate);
                if (source.Any())
                {
                    //找303后的第一个的值，拿出这些数量的值
                    var result = new List<TElement>();
                    var number = func(source);
                    //如果取的值不正常，则全部都取
                    if (number<0)
                    {
                        number = source.Count();
                    }

                    result.AddRange(source.Take(number));
                    results.Add(result);

                    //修改数据源
                    source = source.Skip(number);
                }
                else
                {
                    break;
                }
            }

            return results;
        }

        /// <summary>
        /// 判断指定元素在序列中第几次出现的索引位置
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="ids"></param>
        /// <param name="sourceElement"></param>
        /// <returns></returns>
        public static int TakeNumber<T>(this IEnumerable<T> ids, T sourceElement, int times)
        {
            if (ids == null)
                throw new ArgumentNullException("ids");
            if (sourceElement == null)
                throw new ArgumentNullException("sourceElement");

            var realTimes = 0;//定义指定元素出现的次数
            var n = 0;//记录索引值
            foreach (var item in ids)
            {
                if (ids.ElementAt(n).Equals(sourceElement))
                {
                    realTimes++;
                    if (realTimes==times)
                    {
                        return n;
                    }
                }
                n++;
            }

            //如果遍历完成后，没有找到，则返回-1表示查找失败
            return -1;
        }

    }
}
