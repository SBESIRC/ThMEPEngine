using Autodesk.AutoCAD.DatabaseServices;
using Dreambuild.AutoCAD;
using System.Collections.Generic;
using System.Linq;
using ThMEPArchitecture.ParkingStallArrangement.Model;

namespace ThMEPArchitecture.ParkingStallArrangement.Method
{
    public static class Intersection
    {
        public static Dictionary<int, List<int>> GetIntersection(Dictionary<int, Line> segLineDic)//求取分割线的交点
        {
            var intersectDic = new Dictionary<int, List<int>>();
            var index = 0;
            var cnt = segLineDic.Count;
            for (int i = 0; i < cnt - 1; i++)
            {
                for (int j = i + 1; j < cnt; j++)
                {
                    if (segLineDic[i].HasIntersection(segLineDic[j]))
                    {
                        intersectDic.Add(index, new List<int>() { i, j });
                        index++;
                    }
                }
            }
            return intersectDic;
        }

        public static Dictionary<LinePairs, int> GetLinePtDic(Dictionary<int, List<int>> ptDic)
        {
            var linePtDic = new Dictionary<LinePairs, int>();
            foreach (var pt in ptDic.Keys)
            {
                var vals = ptDic[pt];
                if (vals.Count < 2)
                {
                    continue;//跳过
                }
                var linePair = new LinePairs(vals[0], vals[1]);
                linePtDic.Add(linePair, pt);
            }
            return linePtDic;
        }

        public static bool HasIntersection(this Line line1, Line line2)
        {
            if(line1.GetDirection() == line2.GetDirection())
            {
                //同方向的线不考虑交点
                return false;
            }
            var pts = line1.Intersect(line2, 0);
            if (pts.Count == 0)//不存在交点
            {
                return false;
            }
            return true;
        }
    }
}
