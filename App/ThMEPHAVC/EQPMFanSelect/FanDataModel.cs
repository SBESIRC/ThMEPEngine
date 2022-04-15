using System.Collections.Generic;
using System.Linq;
using ThMEPHVAC.EQPMFanModelEnums;

namespace ThMEPHVAC.EQPMFanSelect
{
    public class FanDataModel
    {
        /// <summary>
        /// 构造函数
        /// </summary>
        public FanDataModel(EnumScenario scenario)
        {
            ID = System.Guid.NewGuid().ToString();
            Scenario = scenario;
            Name = EQPMFanCommon.FanNameAttr(scenario);
            ListVentQuan = new List<int>();
            DragModel = new DragCalcModel();
            var tempVal = EQPMFanCommon.ListSceneResistaCalc.Where(c => c.Scene == scenario).FirstOrDefault();
            if (null != tempVal) 
            {
                DragModel.Friction = tempVal.Friction;
                DragModel.LocRes = tempVal.LocRes;
                DragModel.Damper = tempVal.Damper;
                DragModel.DynPress = tempVal.DynPress;
            }
            if (scenario == EnumScenario.KitchenFumeExhaust)
            {
                DragModel.EndReservedAirPressure = 100;
            }
            else
            {
                DragModel.EndReservedAirPressure = 0;
            }

            VolumeCalcModel = new FanVolumeCalcModel();
            VolumeCalcModel.AirCalcFactor = scenario == EnumScenario.NormalAirSupply ? 1.1 : 1.2;

            DragModel.Friction = 2;
            DragModel.LocRes = 1.5;
            DragModel.SelectionFactor = 1.1;
            DragModel.Damper = 80;
            DragModel.EndReservedAirPressure = 100;

            FanPowerType = EQPMFanCommon.GetFanPowerType(scenario);

            FanModelTypeCalcModel = new CalcFanModel();
            FanModelTypeCalcModel.MotorTempo = 1450;
            FanModelTypeCalcModel.ValueSource = EnumValueSource.IsCalcValue;
        }

        /// <summary>
        /// 应用场景
        /// </summary>
        public EnumScenario Scenario { get; }
        /// <summary>
        /// 风机电源类型
        /// </summary>
        public EnumFanPowerType FanPowerType { get; }
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
        public List<int> ListVentQuan { get; set; }
        /// <summary>
        /// 风机序号
        /// </summary>
        public string VentNum { get; set; }
        /// <summary>
        /// 服务区域
        /// </summary>
        public string ServiceArea { get; set; }
        /// <summary>
        /// 风量
        /// </summary>
        public int AirVolume { get; set; }
        public FanVolumeCalcModel VolumeCalcModel { get; set; }
        /// <summary>
        /// 风阻：正整数
        /// </summary>
        public int WindResis { get; set; }
        public DragCalcModel DragModel { get; set; }
        /// <summary>
        /// 备注
        /// </summary>
        public string Remark { get; set; }
        /// <summary>
        /// 风机形式
        /// </summary>
        public EnumFanModelType VentStyle { get; set; }
        /// <summary>
        /// 风机能效
        /// </summary>
        public EnumFanEnergyConsumption VentLev { get; set; }
        /// <summary>
        /// 电机能效等级
        /// </summary>
        public EnumFanEnergyConsumption EleLev { get; set; }
        public string FanModelCCCF { get; set; }
        public CalcFanModel FanModelTypeCalcModel { get; set; }
        /// <summary>
        /// 风机的控制方式
        /// </summary>
        public EnumFanControl Control { get; set; }
        /// <summary>
        /// 安装方式
        /// </summary>
        public EnumMountingType MountType { get; set; }
        /// <summary>
        ///  进风形式
        /// </summary>
        public EnumFanAirflowDirection IntakeForm { get; set; }
        /// <summary>
        /// 输入总阻力是否偏小
        /// </summary>
        public bool IsPointSafe { get; set; }
        /// <summary>
        /// 减震方式
        /// </summary>
        public EnumDampingType VibrationMode { get; set; }
        /// <summary>
        /// 风机选择状态信息
        /// </summary>
        public FanSelectionStateInfo FanSelectionStateMsg { get; set; }
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
        /// 是否是子风机
        /// </summary>
        public bool IsChildFan { get; set; }
        /// <summary>
        /// 排序ID
        /// </summary>
        public int SortID { get; set; }
        /// <summary>
        /// 应用场景排序
        /// </summary>
        public int SortScenario { get; set; }
        /// <summary>
        /// 风机型号是否有错误
        /// </summary>
        public bool IsSelectFanError { get; set; }
    }
}
