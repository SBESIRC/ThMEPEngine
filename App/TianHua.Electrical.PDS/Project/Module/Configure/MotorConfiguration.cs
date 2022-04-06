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
        /// 非消防(一类)-电动机（CPS）配置
        /// </summary>
        public static List<Motor_CPS> NonFire_CPSInfos = new List<Motor_CPS>();

        /// <summary>
        /// 消防(二类)-电动机（CPS）配置
        /// </summary>
        public static List<Motor_CPS> Fire_CPSInfos = new List<Motor_CPS>();

        /// <summary>
        /// 非消防(一类)-电动机（分立元件星三角启动）配置
        /// </summary>
        public static List<Motor_DiscreteComponentsStarTriangleStartInfo> NonFire_DiscreteComponentsStarTriangleStartInfos = new List<Motor_DiscreteComponentsStarTriangleStartInfo>();

        /// <summary>
        /// 消防(二类)-电动机（分立元件星三角启动）配置
        /// </summary>
        public static List<Motor_DiscreteComponentsStarTriangleStartInfo> Fire_DiscreteComponentsStarTriangleStartInfos = new List<Motor_DiscreteComponentsStarTriangleStartInfo>();

        /// <summary>
        /// 非消防(一类)-电动机（CPS星三角启动）配置
        /// </summary>
        public static List<Motor_CPSStarTriangleStartInfo> NonFire_CPSStarTriangleStartInfos = new List<Motor_CPSStarTriangleStartInfo>();

        /// <summary>
        /// 消防(二类)-电动机（CPS星三角启动）配置
        /// </summary>
        public static List<Motor_CPSStarTriangleStartInfo> Fire_CPSStarTriangleStartInfos = new List<Motor_CPSStarTriangleStartInfo>();

        /// <summary>
        /// 非消防(一类)-电动机（双速电动机（分立元件D-YY））配置
        /// </summary>
        public static List<TwoSpeedMotor_DiscreteComponentsDYYInfo> NonFire_TwoSpeedMotor_DiscreteComponentsDYYCircuitInfos = new List<TwoSpeedMotor_DiscreteComponentsDYYInfo>();

        /// <summary>
        /// 消防(二类)-电动机（双速电动机（分立元件D-YY））配置
        /// </summary>
        public static List<TwoSpeedMotor_DiscreteComponentsDYYInfo> Fire_TwoSpeedMotor_DiscreteComponentsDYYCircuitInfos = new List<TwoSpeedMotor_DiscreteComponentsDYYInfo>();

        /// <summary>
        /// 非消防(一类)-电动机（双速电动机（分立元件Y-Y））配置
        /// </summary>
        public static List<TwoSpeedMotor_DiscreteComponentsYYInfo> NonFire_TwoSpeedMotor_DiscreteComponentsYYCircuitInfos = new List<TwoSpeedMotor_DiscreteComponentsYYInfo>();

        /// <summary>
        /// 消防(二类)-电动机（双速电动机（分立元件Y-Y））配置
        /// </summary>
        public static List<TwoSpeedMotor_DiscreteComponentsYYInfo> Fire_TwoSpeedMotor_DiscreteComponentsYYCircuitInfos = new List<TwoSpeedMotor_DiscreteComponentsYYInfo>();

        /// <summary>
        /// 非消防(一类)-电动机（双速电动机（CPS））配置
        /// </summary>
        public static List<TwoSpeedMotor_CPSInfo> NonFire_TwoSpeedMotor_CPSInfos = new List<TwoSpeedMotor_CPSInfo>();

        /// <summary>
        /// 消防(二类)-电动机（双速电动机（CPS））配置
        /// </summary>
        public static List<TwoSpeedMotor_CPSInfo> Fire_TwoSpeedMotor_CPSInfos = new List<TwoSpeedMotor_CPSInfo>();
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

    /// <summary>
    /// 电动机-CPS 配置
    /// </summary>
    public class Motor_CPS
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
        /// CPS规格
        /// </summary>
        public string CPS { get; set; }

        /// <summary>
        /// 导体根数x每根导体截面积
        /// </summary>
        public string Conductor { get; set; }
    }

    /// <summary>
    /// 电动机-CPS星三角启动 配置
    /// </summary>
    public class Motor_CPSStarTriangleStartInfo
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
        /// CPS规格
        /// </summary>
        public string CPS { get; set; }

        /// <summary>
        /// 接触器规格
        /// </summary>
        public string QAC1 { get; set; }

        /// <summary>
        /// 接触器规格
        /// </summary>
        public string QAC2 { get; set; }

        /// <summary>
        /// 导体根数x每根导体截面积
        /// </summary>
        public string Conductor1 { get; set; }

        /// <summary>
        /// 导体根数x每根导体截面积
        /// </summary>
        public string Conductor2 { get; set; }
    }

    /// <summary>
    /// 双速电动机-分立元件D-YY 配置
    /// </summary>
    public class TwoSpeedMotor_DiscreteComponentsDYYInfo
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
        /// 接触器规格
        /// </summary>
        public string QAC3 { get; set; }

        /// <summary>
        /// 导体根数x每根导体截面积
        /// </summary>
        public string Conductor1 { get; set; }

        /// <summary>
        /// 导体根数x每根导体截面积
        /// </summary>
        public string Conductor2 { get; set; }
    }

    /// <summary>
    /// 双速电动机-分立元件Y-Y 配置
    /// </summary>
    public class TwoSpeedMotor_DiscreteComponentsYYInfo
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
        /// 导体根数x每根导体截面积
        /// </summary>
        public string Conductor1 { get; set; }

        /// <summary>
        /// 导体根数x每根导体截面积
        /// </summary>
        public string Conductor2 { get; set; }
    }

    /// <summary>
    /// 双速电动机-CPS 配置
    /// </summary>
    public class TwoSpeedMotor_CPSInfo
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
        /// CPS规格
        /// </summary>
        public string CPS { get; set; }

        /// <summary>
        /// 接触器规格
        /// </summary>
        public string QAC { get; set; }

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
