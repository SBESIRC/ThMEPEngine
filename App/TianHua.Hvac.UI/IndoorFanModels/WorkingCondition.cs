using System.Collections.Generic;

namespace TianHua.Hvac.UI.IndoorFanModels
{
    /// <summary>
    /// 风机工况信息
    /// </summary>
    class FanWorkingCondition
    {
        public string SheetId { get; }
        public string WorkingId { get; }
        /// <summary>
        /// 工况名称
        /// </summary>
        public string WorkingCoditionName { get; set; }
        public List<WoringConditonBase> ShowWorkingDatas { get; }
        public FanWorkingCondition(string sheetId, string workingId)
        {
            this.SheetId = sheetId;
            this.WorkingId = workingId;
            this.ShowWorkingDatas = new List<WoringConditonBase>();
        }
    }
    /// <summary>
    /// 室内机工况数据
    /// </summary>
    class CoilUnitFanWorkingData : WoringConditonBase
    {
        /// <summary>
        /// 进风相对湿度
        /// </summary>
        public string AirInletHumidity { get; set; }
        /// <summary>
        /// 进口水温
        /// </summary>
        public string EnterPortWaterTEMP { get; set; }
        /// <summary>
        /// 出口水温
        /// </summary>
        public string ExitWaterTEMP { get; set; }
        public CoilUnitFanWorkingData(string id)
        {
            this.WorkingId = id;
        }

    }
    /// <summary>
    /// VRF工况数据
    /// </summary>
    class VRFFanWorkingData : WoringConditonBase
    {
        public VRFFanWorkingData(string id)
        {
            this.WorkingId = id;
        }
        /// <summary>
        /// 制冷工况 - 进风湿球温度
        /// </summary>
        public string AirInletWetBall { get; set; }
        /// <summary>
        /// 制冷工况 室外温度
        /// </summary>
        public string OutdoorTemperature { get; set; }
    }
    class AirConditioninWorkingData : WoringConditonBase
    {
        public AirConditioninWorkingData(string id)
        {
            this.WorkingId = id;
        }
        public string AirInletWetBall { get; set; }
        /// <summary>
        /// 进口水温
        /// </summary>
        public string EnterPortWaterTEMP { get; set; }
        /// <summary>
        /// 出口水温
        /// </summary>
        public string ExitWaterTEMP { get; set; }
    }
    abstract class WoringConditonBase
    {
        public string WorkingId { get; set; }
        public string ShowName { get; set; }
        /// <summary>
        /// 进风干球温度
        /// </summary>
        public string AirInletDryBall { get; set; }


    }
}
