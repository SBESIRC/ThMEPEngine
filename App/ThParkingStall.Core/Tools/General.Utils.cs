using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Accord.Math;

namespace ThParkingStall.Core.Tools
{
    //from https://stackoverflow.com/questions/273313/randomize-a-listt
    public static class ShuffleExtensions
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

        [ThreadStatic] private static int _Seed;

        public static int Seed
        {
            get
            {
#if (DEBUG)
                var path = Path.Combine(System.IO.Path.GetTempPath(), "RandomSeed.txt");
                if (File.Exists(path))// 读取
                {
                    _Seed = int.Parse(File.ReadLines(path).First());
                }
                else//写入
                {
                    _Seed = unchecked(Environment.TickCount * 31 + Thread.CurrentThread.ManagedThreadId);

                    using (var tw = new StreamWriter(path, false))
                    {
                        tw.WriteLine(_Seed.ToString());
                    }
                }
#else
                //Release 只执行写入
                _Seed = unchecked(Environment.TickCount * 31 + Thread.CurrentThread.ManagedThreadId);
                //using (var tw = new StreamWriter(path, false))
                //{
                //    tw.WriteLine(_Seed.ToString());
                //}
#endif
                return _Seed;
            }
        }

        public static int ReadSeed()
        {
            return _Seed;
        }
        public static Random ThisThreadsRandom
        {
            get { return Local ?? (Local = new Random(Seed)); }
        }

        public static void Set()// 设置Seed
        {
            Local = new Random(Seed);
        }
    }
    public class ThParkingStallCoreTools
    {
        public static void SetSeed()
        {
            ThreadSafeRandom.Set();
        }
        public static int GetSeed()
        {
            return ThreadSafeRandom.ReadSeed();
        }
        public static List<int> RandChoice(int UpperBound, int n = -1, int LowerBound = 0)
        {
            // random choose n integers from n to UpperBound without replacement
            // if n < 0,return a shuffled list from lower to upper bound
            List<int> index = Enumerable.Range(LowerBound, UpperBound).ToList();
            index.Shuffle();
            if (n > UpperBound || n < 0)
            {
                return index;
            }
            else
            {
                return index.Take(n).ToList();
            }
        }
        // 旧版本截断正态分布，在特殊情况下容易出现不能找到随机数的情况
        // box muller method
        // 很多情况不符合截断随机分布
        // 速度较慢
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
        public static double _truncnormal(double loc, double scale, double LowerBound, double UpperBound)
        {
            // min distance bwtween pl&pu
            double e1 = 1e-10;
            // min distance to boundary(0 or 1)
            double e2 = 1e-300;
            if (LowerBound == UpperBound)
            {
                return LowerBound;
            }
            var ub = Math.Max(LowerBound, UpperBound);
            var lb = Math.Min(LowerBound, UpperBound);
            // transfrom lowerbound and upperbound to use standard normal distribution
            var trans_l = (lb - loc) / scale;
            var trans_u = (ub - loc) / scale;
            var rand1 = RandDouble();
            double pl = Normal.Function(trans_l);
            double pu = Normal.Function(trans_u);
            // convert rand1 to rand2
            double rand2;
            double res;
            // small probobability difference
            if (pu - pl < e1)
            {
                // keep distance to boundary
                if (pl - 0 < e2)
                {
                    pl = e2;
                }
                else if (1 - pl < (e2 + e1))
                {
                    pl = 1 - (e2 + e1);
                }
                pu = pl + e1;
                rand2 = rand1 * (e1) + pl;
                var lower = Normal.Inverse(pl);
                var size = Normal.Inverse(pu) - lower;
                res = (Normal.Inverse(rand2) - lower) / size;
                return res * (ub - lb) + lb;
            }
            rand2 = (pu - pl) * rand1 + pl;
            res = Normal.Inverse(rand2);
            return res * scale + loc;
        }
        //RandNormalInRange 的增强版本
        // inverse CDF method
        // 对于任意情况的随机数都符合截断正态分布，且输出值永远在范围内
        // 速度比之前优化100倍以上
        public static double Truncnormal(double loc, double scale, double LowerBound, double UpperBound)
        {

            double res = _truncnormal(loc, scale, LowerBound, UpperBound);
            var ub = Math.Max(LowerBound, UpperBound);
            var lb = Math.Min(LowerBound, UpperBound);
            // 输出范围检查
            if (res < ub && res > lb) return res;
            // 此函数必定返回范围内的随机数，不在范围内则返回均匀随机数
            else return RandDouble() * (ub - lb) + lb;
        }

        public static int RandInt(int range)
        {
            return ThreadSafeRandom.ThisThreadsRandom.Next(0, range);
        }
        public static double RandDouble()
        {
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
