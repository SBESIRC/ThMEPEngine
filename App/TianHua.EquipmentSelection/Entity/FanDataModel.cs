using System.Collections.Generic;
using TianHua.FanSelection.Model;

namespace TianHua.FanSelection
{
    public partial class FanDataModel
    {
        /// <summary>
        /// 构造函数
        /// </summary>
        public FanDataModel()
        {
            SelectionFactor = 1.1;
        }

        /// <summary>
        /// 应用场景
        /// </summary>
        public string Scenario { get; set; }

        /// <summary>
        /// 场景类型
        /// </summary>
        public int ScenarioType { get; set; }

        /// <summary>
        /// 唯一ID
        /// </summary>
        public string ID { get; set; }

        /// <summary>
        /// PID
        /// </summary>
        public string PID { get; set; }

        /// <summary>
        /// 名称
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 子项
        /// </summary>
        public string InstallSpace { get; set; }

        /// <summary>
        /// 风机安装楼层
        /// </summary>
        public string InstallFloor { get; set; }

        /// <summary>
        /// 数量
        /// </summary>
        public int VentQuan { get; set; }

        /// <summary>
        /// 数量
        /// </summary>
        public List<int> ListVentQuan { get; set; }

        /// <summary>
        /// 风机序号
        /// </summary>
        public string VentNum { get; set; }

        /// <summary>
        /// 风量
        /// </summary>
        public int AirVolume { get; set; }

        /// <summary>
        /// 工况类型
        /// </summary>
        public int SecType { get; set; }

        /// <summary>
        /// 风阻：正整数
        /// </summary>
        public int WindResis { get; set; }

        /// <summary>
        /// 风管长度：小数点后最多1位
        /// </summary>
        public double DuctLength { get; set; }

        /// <summary>
        /// 比摩阻：小数点后最多1位
        /// </summary>
        public double Friction { get; set; }

        /// <summary>
        /// 局部阻力倍数：小数点后最多1位
        /// </summary>
        public double LocRes { get; set; }

        /// <summary>
        /// 消音器阻力：正整数
        /// </summary>
        public int Damper { get; set; }

        /// <summary>
        /// 风管阻力
        /// </summary>
        public int DuctResistance { get; set; }

        /// <summary>
        /// 动压
        /// </summary>
        public int DynPress { get; set; }

        /// <summary>
        /// 备注
        /// </summary>
        public string Remark { get; set; }

        /// <summary>
        /// 风机形式
        /// </summary>
        public string VentStyle { get; set; }

        /// <summary>
        /// 连接方式
        /// </summary>
        public string VentConnect { get; set; }

        /// <summary>
        /// 风机能效
        /// </summary>
        public string VentLev { get; set; }

        /// <summary>
        /// 电机能效等级
        /// </summary>
        public string EleLev { get; set; }

        /// <summary>
        /// 电机的转速
        /// </summary>
        public int MotorTempo { get; set; }

        /// <summary>
        /// 风机型号表的ID
        /// </summary>
        public string FanModelID { get; set; }

        /// <summary>
        /// 风机型号表的名称
        /// </summary>
        public string FanModelName { get; set; }

        /// <summary>
        /// 型号
        /// </summary>
        public string FanModelNum { get; set; }

        /// <summary>
        /// CCCF规格
        /// </summary>
        public string FanModelCCCF { get; set; }

        /// <summary>
        /// 风量
        /// </summary>
        public string FanModelAirVolume { get; set; }

        /// <summary>
        /// 全压
        /// </summary>
        public string FanModelPa { get; set; }

        /// <summary>
        /// 电机功率
        /// </summary>
        public string FanModelMotorPower { get; set; }

        /// <summary>
        /// 电机功率 输入
        /// </summary>
        public string FanModelInputMotorPower { get; set; }

        /// <summary>
        /// 噪音
        /// </summary>
        public string FanModelNoise { get; set; }

        /// <summary>
        /// 风机转速
        /// </summary>
        public string FanModelFanSpeed { get; set; }

        /// <summary>
        /// 单位功耗
        /// </summary>
        public string FanModelPower { get; set; }

        /// <summary>
        /// 长
        /// </summary>
        public string FanModelLength { get; set; }

        /// <summary>
        /// 宽
        /// </summary>
        public string FanModelWidth { get; set; }

        /// <summary>
        /// 高
        /// </summary>
        public string FanModelHeight { get; set; }

        /// <summary>
        /// 重量
        /// </summary>
        public string FanModelWeight { get; set; }

        /// <summary>
        /// 直径
        /// </summary>
        public string FanModelDIA { get; set; }

        /// <summary>
        /// 风机的控制方式
        /// </summary>
        public string Control { get; set; }

        /// <summary>
        /// 是否为变频
        /// </summary>
        public bool IsFre { get; set; }

        /// <summary>
        /// 电源类型:平时、消防、事故
        /// </summary>
        public string PowerType { get; set; }

        /// <summary>
        /// 安装方式
        /// </summary>
        public string MountType { get; set; }

        /// <summary>
        /// 排序ID
        /// </summary>
        public int SortID { get; set; }

        /// <summary>
        /// 应用场景排序
        /// </summary>
        public int SortScenario { get; set; }

        /// <summary>
        /// 细分用途
        /// </summary>
        public string Use { get; set; }

        /// <summary>
        ///  进风形式
        /// </summary>
        public string IntakeForm { get; set; }

        /// <summary>
        /// 风量计算值
        /// </summary>
        public int AirCalcValue { get; set; }

        /// <summary>
        /// 风量计算系数
        /// </summary>
        public double AirCalcFactor { get; set; }

        /// <summary>
        /// 风机内效率
        /// </summary>
        public string FanInternalEfficiency { get; set; }

        /// <summary>
        /// 输入总阻力是否偏小
        /// </summary>
        public bool IsPointSafe { get; set; }

        /// <summary>
        /// 减震方式
        /// </summary>
        public string VibrationMode { get; set; }

        /// <summary>
        /// 选型系数
        /// </summary>
        public double SelectionFactor { get; set; }

        /// <summary>
        /// 计算总阻力
        /// </summary>
        public double CalcResistance { get; set; }

        /// <summary>
        /// 风量计算
        /// </summary>
        public ThFanVolumeModel FanVolumeModel { get; set; }

        /// </summary>        
        /// 排烟计算
        /// </summary>
        public ExhaustCalcModel ExhaustModel { get; set; }    
        
 
        /// <summary>
        /// 风机选择状态信息
        /// </summary>
        public FanSelectionStateInfo FanSelectionStateInfo { get; set; }

        /// <summary>
        /// 末端预留风压
        /// </summary>
        public int EndReservedAirPressure { get; set; }

        /// <summary>
        /// 风量 描述
        /// </summary>
        public string AirVolumeDescribe { get; set; }

        /// <summary>
        /// 风阻：正整数 描述
        /// </summary>
        public string WindResisDescribe { get; set; }

        /// <summary>
        /// 电机功率 描述
        /// </summary>
        public string FanModelPowerDescribe { get; set; }

        /// <summary>
        /// 是否重复
        /// </summary>
        public bool IsRepetitions { get; set; }

        /// <summary>
        /// 是否手动输入 电机功率
        /// </summary>
        public bool IsInputMotorPower { get; set; }

 
    }
}
