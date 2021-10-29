using System.Collections.Generic;

namespace ThMEPHVAC.LoadCalculation.Model
{
    public class LoadCalculationParameterFromUI
    {
        public string RoomFunctionName { get; set; } //房间功能名称
        public List<DynamicLoadCalculationModelData> ModelDataList { get; set; }
        public bool chk_Area { get; set; } //UI-CheckBox-面积
        public bool chk_ColdL { get; set; } //UI-CheckBox-冷负荷
        public bool chk_ColdW { get; set; } //UI-CheckBox-冷水量
        public bool chk_ColdWP { get; set; } //UI-CheckBox-冷水管径
        public int chk_ColdWP_Index { get; set; } //UI-CheckBox-冷水管径参数
        public bool chk_CondensateWP { get; set; } //UI-CheckBox-冷凝水管径
        public bool chk_HotL { get; set; } //UI-CheckBox-热负荷
        public bool chk_HotW { get; set; } //UI-CheckBox-热水量
        public bool chk_HotWP { get; set; } //UI-CheckBox-热水管径
        public int chk_HotWP_Index { get; set; } //UI-CheckBox-热水管径参数
        public bool chk_AirVolume { get; set; } //UI-CheckBox-新风量
        public bool chk_FumeExhaust { get; set; } //UI-CheckBox-排油烟量
        public bool chk_FumeSupplementary { get; set; } //UI-CheckBox-油烟补风量
        public bool chk_AccidentExhaust { get; set; } //UI-CheckBox-事故排风量
        public bool chk_NormalAirVolume { get; set; } //UI-CheckBox-平时排风量
        public bool chk_NormalFumeSupplementary { get; set; } //UI-CheckBox-平时补风量

        public bool HasPrefix { get; set; } = true; //前缀
        public string PerfixContent { get; set; } = "N-1F-"; //前缀内容
        public string StartingNum { get; set; } = "01"; //起始序号
    }
}
