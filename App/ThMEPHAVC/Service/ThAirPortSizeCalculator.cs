using System;
using System.Linq;
using System.Collections.Generic;

namespace ThMEPHVAC.Service
{
    public class ThAirPortSizeCalculator
    {
        protected static double MmToMeter(int size)
        {
            return size / 1000.0;
        }
        protected static double GetArea(int length, int width)
        {
            return MmToMeter(length) * MmToMeter(width); // 平方米
        }
    }
    public class ThRectangleAirPortSizeCalculator: ThAirPortSizeCalculator
    {
        /// <summary>
        /// 计算风口尺寸(不包括：方形散流器、圆形风口)
        /// </summary>
        /// <param name="singleAirPortAirVolume">单个风口风量</param>
        /// <param name="upperLimitedValue">风速上限值</param>
        /// <returns></returns>
        public static Tuple<int,int> CalculateAirPortSize(double singleAirPortAirVolume,double airSpeedUpperLimitedValue,double lwRatio)
        {
           if(singleAirPortAirVolume<=0.0)
            {
                return Tuple.Create(0, 0);
            }
            double aMin = singleAirPortAirVolume / (3600 * airSpeedUpperLimitedValue);
            // 单风口风量 单位：m3/h , 风速上限值<=2.2m/s
            var sizes = ThMEPHAVCDataManager.GetRectangleSizes().OrderBy(o=>o).ToList();
            var specs = GetSpecs(sizes);

            // 用"单个风口风量/(3600*截面面积)",筛选符合条件的风管规格
            var options = specs
                .Where(o => IsAirPortAreaValid(o.Item1, o.Item2, singleAirPortAirVolume, airSpeedUpperLimitedValue))
                .ToList();
            // 用[aMin,1.5*aMin],筛选符合条件的风管规格
            options = options.Where(o => GetArea(o.Item1, o.Item2) >= aMin && GetArea(o.Item1, o.Item2) <= 1.5 * aMin).ToList();

            // 用长宽比，筛选
            options = options
                .Where(o => LengthWidthRatio(o.Item1, o.Item2) <= lwRatio)
                .OrderByDescending(o=> LengthWidthRatio(o.Item1, o.Item2))
                .ToList();

            return options.Count>0? options.First():Tuple.Create(sizes.FirstOrDefault(), sizes.FirstOrDefault());
        }

        private static bool IsAirPortAreaValid(int length, int width,
            double singleAirPortAirVolume,double upperLimitedValue)
        {
            var area = GetArea(length, width); //m2
            return singleAirPortAirVolume / (3600 * area) <= upperLimitedValue;
        }


        private static double LengthWidthRatio(int length,int width)
        {
            return MmToMeter(length) / MmToMeter(width);
        }
        private static List<Tuple<int, int>> GetSpecs(List<int> sizes)
        {
            var results = new List<Tuple<int, int>>();
            for (int i = 0; i < sizes.Count; i++)
            {
                for (int j = i ; j >= 0; j--)
                {
                    results.Add(Tuple.Create(sizes[i], sizes[j]));
                }
            }
            return results;
        }
    }
    public class ThSquareDiffuserAirPortSizeCalculator:ThAirPortSizeCalculator
    {
        private const double AirSpeedDownLimitedValue = 2.4; // 单位 m/s
        private const double AirSpeedUpperLimitedValue = 3.0;// 单位 m/s

        public static Tuple<int, int> CalculateAirPortSize(double singleAirPortAirVolume)
        {
            if (singleAirPortAirVolume <= 0.0)
            {
                return Tuple.Create(0, 0);
            }
            // 单风口风量 单位：m3/h 
            var sizes = ThMEPHAVCDataManager.GetSquareSizes().OrderBy(o => o).ToList();
            var specs = GetSpecs(sizes);
            var minArea = CalculateMinArea(singleAirPortAirVolume);
            var maxArea = CalculateMaxArea(singleAirPortAirVolume);
            var options = specs
                .Where(o => GetArea(o.Item1, o.Item2) >= minArea && GetArea(o.Item1, o.Item2) <= maxArea)
                .OrderBy(o => GetArea(o.Item1, o.Item2))
                .ToList();
            return options.Count > 0 ? options.First() : Tuple.Create(sizes.FirstOrDefault(), sizes.FirstOrDefault());
        }

        private static List<Tuple<int, int>> GetSpecs(List<int> sizes)
        {
            var results = new List<Tuple<int, int>>();
            for (int i = 0; i < sizes.Count; i++)
            {
                results.Add(Tuple.Create(sizes[i], sizes[i]));
            }
            return results;
        }

        private static double CalculateArea(double singleAirPortAirVolume,double speed)
        {
            // 计算风口面积
            return singleAirPortAirVolume / (speed * 3600);
        }

        private static double CalculateMaxArea(double singleAirPortAirVolume)
        {
            return CalculateArea(singleAirPortAirVolume, AirSpeedDownLimitedValue);
        }
        private static double CalculateMinArea(double singleAirPortAirVolume)
        {
            return CalculateArea(singleAirPortAirVolume, AirSpeedUpperLimitedValue);
        }
    }
    public class ThCircleAirPortSizeCalculator : ThAirPortSizeCalculator
    {
        private const double AirSpeedDownLimitedValue = 2.4; // 单位 m/s
        private const double AirSpeedUpperLimitedValue = 3.0;// 单位 m/s

        public static Tuple<int, int> CalculateAirPortSize(double singleAirPortAirVolume)
        {
            if (singleAirPortAirVolume <= 0.0)
            {
                return Tuple.Create(0, 0);
            }
            // 单风口风量 单位：m3/h , 风速上限值<=2.2m/s            
            var minArea = CalculateMinArea(singleAirPortAirVolume);
            var maxArea = CalculateMaxArea(singleAirPortAirVolume);

            var sizes = ThMEPHAVCDataManager.GetCircleSizes().OrderBy(o => o).ToList();
            var options = sizes
                .Where(o => GetArea(o/2) >= minArea && GetArea(o/2) <= maxArea)
                .OrderBy(o => GetArea(o/2)).ToList();
            return options.Count > 0 ? Tuple.Create(options.First(), options.First()) : 
                Tuple.Create(sizes.FirstOrDefault(), sizes.FirstOrDefault());
        }

        private static List<Tuple<int, int>> GetSpecs(List<int> sizes)
        {
            var results = new List<Tuple<int, int>>();
            for (int i = 0; i < sizes.Count; i++)
            {
                results.Add(Tuple.Create(sizes[i], sizes[i]));
            }
            return results;
        }

        private static double CalculateArea(double singleAirPortAirVolume, double speed)
        {
            // 计算风口面积
            return singleAirPortAirVolume / (speed * 3600);
        }

        private static double CalculateMaxArea(double singleAirPortAirVolume)
        {
            return CalculateArea(singleAirPortAirVolume, AirSpeedDownLimitedValue);
        }
        private static double CalculateMinArea(double singleAirPortAirVolume)
        {
            return CalculateArea(singleAirPortAirVolume, AirSpeedUpperLimitedValue);
        }
        private static double GetArea(int radius)
        {
            return Math.PI * MmToMeter(radius) * MmToMeter(radius); // 平方米
        }
    }
}
