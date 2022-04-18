namespace ThMEPHVAC.EQPMFanModelEnums
{
    public class FanSelectionData
    {
        public string X { get; set; }

        public string Y { get; set; }

        public string Value { get; set; }

    }
    public class FanParameters
    {
        /// <summary>
        /// 后三位
        /// </summary>
        public string Suffix { get; set; }
        /// <summary>
        /// 机号
        /// </summary>
        public string No { get; set; }
        /// <summary>
        /// CCCF规格
        /// </summary>
        public string CCCF_Spec { get; set; }
        /// <summary>
        /// 转速r/min
        /// </summary>
        public string Rpm { get; set; }
        /// <summary>
        /// 风量m^3/h 
        /// </summary>
        public string AirVolume { get; set; }
        /// <summary>
        /// 全压(Pa)
        /// </summary>
        public string Pa { get; set; }
        /// <summary>
        /// 静压(Pa)
        /// </summary>
        public string StaticPressure { get; set; }
        /// <summary>
        /// 功率
        /// </summary>
        public string Power { get; set; }
        /// <summary>
        /// 重量
        /// </summary>
        public string Weight { get; set; }
        /// <summary>
        /// 噪声dB(A)
        /// </summary>
        public string Noise { get; set; }
        /// <summary>
        /// 高
        /// </summary>
        public string Height { get; set; }
        /// <summary>
        /// 长
        /// </summary>
        public string Length { get; set; }
        /// <summary>
        /// 宽
        /// </summary>
        public string Width { get; set; }
        /// <summary>
        /// 高_1
        /// </summary>
        public string Height1 { get; set; }
        /// <summary>
        /// 长_1
        /// </summary>
        public string Length1 { get; set; }
        /// <summary>
        /// 高_2
        /// </summary>
        public string Height2 { get; set; }
        /// <summary>
        /// 长_2
        /// </summary>
        public string Length2 { get; set; }
        /// <summary>
        /// 宽_1
        /// </summary>
        public string Width1 { get; set; }
        /// <summary>
        /// 出风口宽度
        /// </summary>
        public string OutletWidth { get; set; }
        /// <summary>
        /// 出风口高度
        /// </summary>
        public string AirOutletHeight { get; set; }
        /// <summary>
        /// 进风口宽度
        /// </summary>
        public string AirInletWidth { get; set; }
        /// <summary>
        /// 进风口高度
        /// </summary>
        public string AirInletHeight { get; set; }
        /// <summary>
        /// 出风风速
        /// </summary>
        public string TheWindSpeed { get; set; }
        /// <summary>
        /// 动压
        /// </summary>
        public string DynamicPressure { get; set; }
        /// <summary>
        /// 实际功率
        /// </summary>
        public string RealPower { get; set; }
        /// <summary>
        /// 风机效率
        /// </summary>
        public string FanEfficiency { get; set; }
        /// <summary>
        /// 电机功率
        /// </summary>
        public string MotorPower { get; set; }

        /// <summary>
        /// 档位
        /// </summary>
        public string Gears { get; set; }
    }

    public class AxialFanParameters
    {
        /// <summary>
        /// 机号
        /// </summary>
        public string No { get; set; }
        /// <summary>
        /// 型号
        /// </summary>
        public string ModelNum { get; set; }
        /// <summary>
        /// 转速r/min
        /// </summary>
        public string Rpm { get; set; }
        /// <summary>
        /// 风量m^3/h 
        /// </summary>
        public string AirVolume { get; set; }
        /// <summary>
        /// 全压(Pa)
        /// </summary>
        public string Pa { get; set; }
        /// <summary>
        /// 功率
        /// </summary>
        public string Power { get; set; }
        /// <summary>
        /// 噪声dB(A)
        /// </summary>
        public string Noise { get; set; }
        /// <summary>
        /// 重量
        /// </summary>
        public string Weight { get; set; }
        /// <summary>
        /// 直径
        /// </summary>
        public string Diameter { get; set; }
        /// <summary>
        /// 长度
        /// </summary>
        public string Length { get; set; }
        /// <summary>
        /// 档位
        /// </summary>
        public string Gears { get; set; }
    }

    public class AxialFanEfficiency
    {
        /// <summary>
        /// 最小机号
        /// </summary>
        public string No_Min { get; set; }
        /// <summary>
        /// 最大机号
        /// </summary>
        public string No_Max { get; set; }
        /// <summary>
        /// 风机效率等级
        /// </summary>
        public string FanEfficiencyLevel { get; set; }
        /// <summary>
        /// 风机内效率
        /// </summary>
        public int FanEfficiency { get; set; }
    }

    public class FanEfficiency
    {
        /// <summary>
        /// 最小转速
        /// </summary>
        public string Rpm_Min { get; set; }
        /// <summary>
        /// 最大转速
        /// </summary>
        public string Rpm_Max { get; set; }
        /// <summary>
        /// 最小机号
        /// </summary>
        public string No_Min { get; set; }
        /// <summary>
        /// 最大机号
        /// </summary>
        public string No_Max { get; set; }
        /// <summary>
        /// 风机效率等级
        /// </summary>
        public string FanEfficiencyLevel { get; set; }
        /// <summary>
        /// 风机内效率
        /// </summary>
        public int FanInternalEfficiency { get; set; }
    }

    public class MotorPower
    {
        /// <summary>
        /// 额定功率
        /// </summary>
        public string Power { get; set; }
        /// <summary>
        /// 电机能效等级
        /// </summary>
        public string MotorEfficiencyLevel { get; set; }
        /// <summary>
        /// 转速
        /// </summary>
        public string Rpm { get; set; }
        /// <summary>
        /// 电机效率
        /// </summary>
        public string MotorEfficiency { get; set; }
        /// <summary>
        /// 离心II高速
        /// </summary>
        public string Centrifuge2HighSpeed { get; set; }
        /// <summary>
        /// 离心II低速
        /// </summary>
        public string Centrifuge2LowSpeed { get; set; }
        /// <summary>
        /// 离心IV高速
        /// </summary>
        public string Centrifuge4HighSpeed { get; set; }
        /// <summary>
        /// 离心IV低速
        /// </summary>
        public string Centrifuge4LowSpeed { get; set; }
        /// <summary>
        /// 轴流II高速
        /// </summary>
        public string Axial2HighSpeed { get; set; }
        /// <summary>
        /// 轴流II低速
        /// </summary>
        public string Axial2LowSpeed { get; set; }
        /// <summary>
        /// 轴流IV高速
        /// </summary>
        public string Axial4HighSpeed { get; set; }
        /// <summary>
        /// 轴流IV低速
        /// </summary>
        public string Axial4LowSpeed { get; set; }
    }
}
