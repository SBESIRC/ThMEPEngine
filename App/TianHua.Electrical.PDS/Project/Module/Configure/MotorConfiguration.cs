using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TianHua.Electrical.PDS.Project.Module.Configure
{
    /// <summary>
    /// LV_AC_Motor_TypeOneCoordination_19DX101.xlsx
    /// LV_AC_Motor_TypeTwoCoordination_19DX101.xlsx
    /// </summary>
    public class MotorConfiguration
    {
        /// <summary>
        /// 非消防(一类)-电动机（分立元件）配置
        /// </summary>
        public static List<Motor_DiscreteComponentsInfo> NonFire_DiscreteComponentsInfos = new List<Motor_DiscreteComponentsInfo>();

        /// <summary>
        /// 消防(二类)-电动机（分立元件）配置
        /// </summary>
        public static List<Motor_DiscreteComponentsInfo> Fire_DiscreteComponentsInfos = new List<Motor_DiscreteComponentsInfo>();

        /// <summary>
        /// 非消防(一类)-电动机（分立元件星三角启动）配置
        /// </summary>
        public static List<Motor_DiscreteComponentsStarTriangleStartInfo> NonFire_DiscreteComponentsStarTriangleStartInfos = new List<Motor_DiscreteComponentsStarTriangleStartInfo>();

        /// <summary>
        /// 消防(二类)-电动机（分立元件星三角启动）配置
        /// </summary>
        public static List<Motor_DiscreteComponentsStarTriangleStartInfo> Fire_DiscreteComponentsStarTriangleStartInfos = new List<Motor_DiscreteComponentsStarTriangleStartInfo>();
    }

    /// <summary>
    /// 电动机-分立元件 配置
    /// </summary>
    public class Motor_DiscreteComponentsInfo
    {
        /// <summary>
        /// 电机功率
        /// </summary>
        public double InstalledCapacity { get; set; }

        /// <summary>
        /// 计算电流
        /// </summary>
        public double CalculateCurrent { get; set; }

        /// <summary>
        /// 启动电流
        /// </summary>
        public double StartingCurrent { get; set; }

        /// <summary>
        /// 断路器规格
        /// </summary>
        public string CB { get; set; }

        /// <summary>
        /// 接触器规格
        /// </summary>
        public string QAC { get; set; }

        /// <summary>
        /// 热继电器规格
        /// </summary>
        public string KH { get; set; }

        /// <summary>
        /// 导体根数x每根导体截面积
        /// </summary>
        public string Conductor { get; set; }
    }

    /// <summary>
    /// 电动机-分立元件星三角启动 配置
    /// </summary>
    public class Motor_DiscreteComponentsStarTriangleStartInfo
    {
        /// <summary>
        /// 电机功率
        /// </summary>
        public double InstalledCapacity { get; set; }

        /// <summary>
        /// 计算电流
        /// </summary>
        public double CalculateCurrent { get; set; }

        /// <summary>
        /// 启动电流
        /// </summary>
        public double StartingCurrent { get; set; }

        /// <summary>
        /// 断路器规格
        /// </summary>
        public string CB { get; set; }

        /// <summary>
        /// 接触器规格
        /// </summary>
        public string QAC1 { get; set; }

        /// <summary>
        /// 接触器规格
        /// </summary>
        public string QAC2 { get; set; }

        /// <summary>
        /// 接触器规格
        /// </summary>
        public string QAC3 { get; set; }

        /// <summary>
        /// 热继电器规格
        /// </summary>
        public string KH { get; set; }

        /// <summary>
        /// 导体根数x每根导体截面积
        /// </summary>
        public string Conductor1 { get; set; }

        /// <summary>
        /// 导体根数x每根导体截面积
        /// </summary>
        public string Conductor2 { get; set; }
    }
}
