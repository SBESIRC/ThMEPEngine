using AcHelper;
using Autodesk.AutoCAD.EditorInput;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using ThMEPArchitecture.PartitionLayout;

namespace ThMEPArchitecture.ParkingStallArrangement.General
{
    //from https://stackoverflow.com/questions/273313/randomize-a-listt
    static class ShuffleExtensions
    {
        public static void Shuffle<T>(this IList<T> list)
        {
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = ThreadSafeRandom.ThisThreadsRandom.Next(n + 1);
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }
    }
    public static class ThreadSafeRandom
    {
        [ThreadStatic] private static Random Local;

        public static Random ThisThreadsRandom
        {
            get { return Local ?? (Local = new Random(unchecked(Environment.TickCount * 31 + Thread.CurrentThread.ManagedThreadId))); }
        }
    }
    internal class Utils
    {
        public static bool SetLayoutMainDirection()
        {
            var options = new PromptKeywordOptions("\n优先方向：");

            options.Keywords.Add("纵向", "V", "纵向(V)");
            options.Keywords.Add("横向", "H", "横向(H)");
            options.Keywords.Add("长度", "L", "长度(L)");

            options.Keywords.Default = "纵向";
            var rstDirection = Active.Editor.GetKeywords(options);
            if (rstDirection.Status != PromptStatus.OK)
            {
                return false;
            }

            if (rstDirection.StringResult.Equals("纵向"))
            {
                ThMEPArchitecture.PartitionLayout.ParkingPartition.LayoutMode = ((int)LayoutDirection.VERTICAL);
            }
            else if (rstDirection.StringResult.Equals("横向"))
            {
                ThMEPArchitecture.PartitionLayout.ParkingPartition.LayoutMode = ((int)LayoutDirection.HORIZONTAL);
            }
            else
            {
                ThMEPArchitecture.PartitionLayout.ParkingPartition.LayoutMode = ((int)LayoutDirection.LENGTH);
            }

            return true;
        }

        public static List<int> RandChoice(int UpperBound, int n=-1,int LowerBound = 0)
        {
            // random choose n integers from n to UpperBound without replacement
            // if n < 0,return a shuffled list from lower to upper bound
            List<int> index = Enumerable.Range(LowerBound, UpperBound).ToList();
            index.Shuffle();
            if (n > UpperBound||n<0)
            {
                return index;
            }
            else
            {
                return index.Take(n).ToList();
            }
        }
        public static double RandNormalInRange(double loc, double scale, double LowerBound, double UpperBound, int MaxIter = 1000)
        {
            // 如果loc 在范围外调整loc为lower或者upper
            double RandNumber;
            for (int i = 0; i < MaxIter; ++i)
            {
                if (loc >= LowerBound && loc <= UpperBound)
                {
                    RandNumber = RandNormal(loc, scale);
                }
                else if (loc < LowerBound)
                {
                    RandNumber = RandNormal(LowerBound, scale);
                }
                else
                {
                    RandNumber = RandNormal(UpperBound, scale);
                }
                if (RandNumber >= LowerBound && RandNumber <= UpperBound)
                {
                    return RandNumber;
                }
            }
            //未找到返回loc
            return loc;
        }
        public static int RandInt(int range)
        {
            //return General.Utils.RandInt(range);
            return ThreadSafeRandom.ThisThreadsRandom.Next(0, range);
        }
        public static double RandDouble()
        {
            //return General.Utils.RandDouble();
            return ThreadSafeRandom.ThisThreadsRandom.NextDouble();
        }
        public static double RandNormal(double loc, double scale)
        {
            //loc: mean of normal distribution
            // scale: standard deviation of normal distribution
            // return a random number with mean of loc and standard deviation of scale
            double u1, u2, z, x;
            //Random ram = new RandNormal();
            var d = scale * scale;

            u1 = RandDouble();
            u2 = RandDouble();
            z = Math.Sqrt(-2 * Math.Log(u1)) * Math.Sin(2 * Math.PI * u2);
            x = loc + d * z;
            return x;
        }
    }
}
