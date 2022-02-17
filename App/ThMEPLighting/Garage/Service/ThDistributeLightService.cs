using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using ThMEPEngineCore.CAD;
using ThMEPLighting.Garage.Model;

namespace ThMEPLighting.Garage.Service
{
    public static class ThDistributeLightService
    {
        public static List<Point3d> Distribute(this ThLineSplitParameter splitParameter)
        {
            var results = new List<Point3d>();
            if (!splitParameter.IsValid)
            {
                return results;
            }
            var D = splitParameter.Interval;
            int N = CalculateN(splitParameter.Length, splitParameter.Interval, splitParameter.Margin);
            var pts = ToCollection(splitParameter.Segment);
            var path = pts.CreatePolyline(false);
            var midPt = path.GetPolylinePt(path.Length/2.0);
            double step = 0.0;
            if (N % 2 == 1)
            {
                //奇数盏灯
                step = D;
                results.Add(midPt);
            }
            else
            {
                //偶数盏灯
                step = D / 2.0;
            }
            double half = path.Length / 2.0;
            for (int i = 1; i <= N / 2; i++)
            {
                var leftPt = path.GetPolylinePt(half - step);
                var rightPt = path.GetPolylinePt(half + step);
                results.Insert(0, leftPt);
                results.Add(rightPt);
                step += D;
            }
            return results;
        }

        private static Point3dCollection ToCollection(List<Point3d> pts)
        {
            var results = new Point3dCollection();
            pts.ForEach(o => results.Add(o));
            return results;
        }

        /// <summary>
        /// 只能对直线段分割
        /// </summary>
        /// <param name="splitParameter"></param>
        /// <returns></returns>
        public static List<Point3d> DistributeLinearSegment(this ThLineSplitParameter splitParameter)
        {
            // L->灯线长度
            // D->灯具间距
            // N盏灯所需的最小长度 :Lmin = D*(N-1)+1600
            // 取N（自然数）的最大值使之满足:Lmin<=L
            var results = new List<Point3d>();
            if (!splitParameter.IsValid && splitParameter.Segment.Count!=2)
            {
                return results;
            }
            var L = splitParameter.Length;
            var D = splitParameter.Interval;
            var N = CalculateN(L, D, splitParameter.Margin);

            var dir = splitParameter.Segment[0].GetVectorTo(splitParameter.Segment[1]).GetNormal();
            var midPt = splitParameter.Segment[0] + dir.MultiplyBy(L/2.0);
            double step = 0.0;
            if (N%2==1)
            {
                //奇数盏灯
                step = D;                
                results.Add(midPt);                
            }
            else
            {
                //偶数盏灯
                step = D / 2.0;
            }
            for (int i = 1; i <= N / 2; i++)
            {
                var leftPt = midPt + dir.Negate().MultiplyBy(step);
                var rightPt = midPt + dir.MultiplyBy(step);
                results.Insert(0, leftPt);
                results.Add(rightPt);
                step += D;
            }
            return results;
        }

        private static Point3d? GetDistributePoint(double length,List<Point3d> segment)
        {
            Point3d? result = null;
            if (length<0)
            {
                return result;
            }
            if(length<1e-6)
            {
                return segment[0];
            }
            var sum = 0.0;
            for(int i=0;i< segment.Count-1;i++)
            {
                var vec = segment[i].GetVectorTo(segment[i + 1]);
                if (length<= (sum+ vec.Length))
                {
                    return segment[i] + vec.GetNormal().MultiplyBy(length - sum);
                }
                else
                {
                    sum += vec.Length;
                }
            }
            return result;
        }
        /// <summary>
        /// 获取此段上可以布置多少盏灯
        /// </summary>
        /// <param name="l">线段长</param>
        /// <param name="d">灯间距</param>
        /// <param name="margin"></param>
        /// <returns></returns>
        private static int CalculateN(double l,double d,double margin)
        {            
            int n = 0;
            while(CalculateLMin(n,d,margin)<= l)
            {
                n++;
            }
            return n-1;
        }
        private static double CalculateD(double l, int n, double margin)
        {
            return (l - 2 * margin) / n;
        }
        /// <summary>
        /// 计算N盏灯使用的最小长度
        /// </summary>
        /// <param name="n"></param>
        /// <param name="d"></param>
        /// <param name="margin"></param>
        /// <returns></returns>
        private static double CalculateLMin(int n,double d,double margin)
        {
            return (n - 1) * d + 2 * margin;
        }
    }    
}
