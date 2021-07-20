using System;
using System.Diagnostics;

namespace ThMEPEngineCore.Diagnostics
{
    public class ThStopWatchService
    {
        private static Stopwatch Watch;

        static ThStopWatchService()
        {
            Watch = new Stopwatch();
        }
        /// <summary>
        /// 开始或继续测量某个时间间隔的运行时间。
        /// </summary>
        public static void Start()
        {
            Watch.Reset();
            Watch.Start();
        }
        /// <summary>
        /// 停止测量某个时间间隔的运行时间
        /// </summary>
        public static void Stop()
        {
            Watch.Stop();
        }
        /// <summary>
        /// 停止时间间隔测量，将运行时间重置为零，然后开始测量运行时间。
        /// </summary>
        public static void ReStart()
        {
            Watch.Restart();
        }
        /// <summary>
        /// 获取以整秒数和秒的小数部分表示的当前 System.TimeSpan 结构的值
        /// </summary>
        /// <returns>此实例表示的总秒数</returns>
        public static double TimeSpan()
        {
            return Watch.Elapsed.TotalSeconds;
        }
        public static void Print(string prefix)
        {
            AcHelper.Active.Editor.WriteMessage("\n" + prefix + TimeSpan() + "秒");
            AcHelper.Active.Editor.WriteMessage("\n");
        }
    }
}
