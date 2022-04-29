namespace TianHua.Electrical.PDS.Project.GlobalConfiguration
{
    public class FolderSetting
    {
        /// <summary>
        /// 默认路径
        /// </summary>
        public static string DefaultPath { get; set; } = @"D:\Documents\Elecsandbox";

        /// <summary>
        /// 时间间隔
        /// </summary>
        public static int TimeInterval { get; set; } = 10;

        public static bool Individual { get; set; } = false;

        /// <summary>
        /// 绘图比例
        /// </summary>
        public static string DWGRatio { get; set; } = "1:100";

        public static string[] DwgRatioItemsSouce { get; } = new string[] { "1:100", "1:150" };
    }
}
