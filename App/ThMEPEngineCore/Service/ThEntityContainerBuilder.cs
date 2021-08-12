using NFox.Cad;
using System.Linq;
using ThCADCore.NTS;
using Dreambuild.AutoCAD;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.CAD;

namespace ThMEPEngineCore.Service
{
    public class ThEntityContainerBuilder
    {
        /// <summary>
        /// 用于包含的空间元素,一定是Polygon
        /// </summary>
        private Dictionary<Entity, string> SpaceDict { get; set; }
        /// <summary>
        /// 不能用于判断包含其它物体的元素
        /// </summary>
        private Dictionary<Entity, string> ElementDict { get; set; }
        private ThCADCoreNTSSpatialIndexEx SpaceSpatialIndex { get; set; }
        private ThCADCoreNTSSpatialIndexEx ElementSpatialIndex { get; set; }
        public ThEntityContainerBuilder(Dictionary<Entity, string> spaceDict,
            Dictionary<Entity, string> elementDict)
        {
            SpaceDict = spaceDict;
            ElementDict = elementDict;         
            SpaceSpatialIndex = new ThCADCoreNTSSpatialIndexEx(SpaceDict.Keys.ToCollection());
            ElementSpatialIndex = new ThCADCoreNTSSpatialIndexEx(ElementDict.Keys.ToCollection());
        }
        public Dictionary<Entity, string> Build()
        {
            var result = new Dictionary<Entity, string>();
            SpaceDict.Keys.ForEach(k => result.Add(k, ""));
            ElementDict.Keys.ForEach(k => result.Add(k, ""));
            var spaceContainer = BuildSpaceContainer();
            spaceContainer.ForEach(m =>
            {
                m.Value.Cast<Entity>().ForEach(n =>
                {
                    if (string.IsNullOrEmpty(result[n]))
                    {
                        result[n] = SpaceDict[m.Key];
                    }
                    else
                    {
                        result[n] += "," + SpaceDict[m.Key];
                    }
                });
            });
            return result;
        }
        private Dictionary<Entity, DBObjectCollection> BuildSpaceContainer()
        {
            var spaces = Orderby(); // 把空间元素从小到大排序
            var spaceContainer = new Dictionary<Entity, DBObjectCollection>();
            spaces.Cast<Entity>().ForEach(o => spaceContainer.Add(o, new DBObjectCollection()));
            foreach (Entity space in spaces)
            {
                var queryElements = ElementSpatialIndex.SelectWindowPolygon(space);
                var querySpaces = SpaceSpatialIndex.SelectWindowPolygon(space);
                Add(queryElements, querySpaces);
                foreach (Entity innerSpace in querySpaces)
                {
                    Subtract(queryElements, spaceContainer[innerSpace]);
                }
                spaceContainer[space] = queryElements;
            }
            return spaceContainer;
        }
        private List<Entity> Orderby()
        {
            var results = SpaceDict.Keys.ToList();
            QuickSort(results, 0, results.Count - 1);
            return results;
        }

        private void QuickSort(List<Entity> ents, int begin, int end)
        {
            if (begin >= end) return;   //两个指针重合就返回，结束调用
            int pivotIndex = QuickSort_Once(ents, begin, end);  //会得到一个基准值下标

            QuickSort(ents, begin, pivotIndex - 1);  //对基准的左端进行排序  递归
            QuickSort(ents, pivotIndex + 1, end);   //对基准的右端进行排序  递归
        }
        private int QuickSort_Once(List<Entity> ents, int begin, int end)
        {
            Entity pivot = ents[begin];   //将首元素作为基准
            int i = begin;
            int j = end;
            while (i < j)
            {
                //从右到左，寻找第一个小于基准pivot的元素
                while (ents[j].GetArea() >= pivot.GetArea() && i < j) j--; //指针向前移
                ents[i] = ents[j];  //执行到此，j已指向从右端起第一个小于基准pivot的元素，执行替换

                //从左到右，寻找首个大于基准pivot的元素
                while (ents[i].GetArea() <= pivot.GetArea() && i < j) i++; //指针向后移
                ents[j] = ents[i];  //执行到此,i已指向从左端起首个大于基准pivot的元素，执行替换
            }

            //退出while循环,执行至此，必定是 i= j的情况（最后两个指针会碰头）
            //i(或j)所指向的既是基准位置，定位该趟的基准并将该基准位置返回
            ents[i] = pivot;
            return i;
        }
        private void Subtract(
            DBObjectCollection first,
            DBObjectCollection second)
        {
            second.Cast<Entity>().ForEach(e =>
            {
                if (first.Contains(e))
                {
                    first.Remove(e);
                }
            });
        }
        private void Add(DBObjectCollection first,DBObjectCollection second)
        {
            second.Cast<Entity>().ForEach(e =>
            {
                if (!first.Contains(e))
                {
                    first.Add(e);
                }
            });
        }
    }
}
