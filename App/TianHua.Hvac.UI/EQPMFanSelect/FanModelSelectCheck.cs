using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;
using ThMEPHVAC.EQPMFanModelEnums;
using ThMEPHVAC.EQPMFanSelect;

namespace TianHua.Hvac.UI.EQPMFanSelect
{
    class FanModelSelectCheck
    {
        public static void CheckAxialFanSelectModel(FanDataModel pFanModel, FanDataModel cFanModel, FanModelPicker pPick, FanModelPicker cPick, List<AxialFanParameters> childAxialFanParameters)
        {
            if (string.IsNullOrEmpty(pFanModel.FanModelCCCF))
            {
                pFanModel.FanSelectionStateMsg.FanSelectionState = EnumFanSelectionState.HighNotFound;
                return;
            }
            switch (pFanModel.Control)
            {
                case EnumFanControl.TwoSpeed:
                    //双速时
                    if (cPick != null)
                    {
                        //低速档风机选型点处于安全范围
                        if (!cFanModel.IsPointSafe)
                        {
                            //高速档风机选型点处于安全范围
                            if (!pFanModel.IsPointSafe)
                            {
                                cFanModel.FanSelectionStateMsg.FanSelectionState = EnumFanSelectionState.HighAndLowBothSafe;
                                pFanModel.FanSelectionStateMsg.FanSelectionState = EnumFanSelectionState.HighAndLowBothSafe;
                            }
                            //高速档风机选型点处于危险范围
                            else
                            {
                                cFanModel.FanSelectionStateMsg.FanSelectionState = EnumFanSelectionState.HighUnsafe;
                                pFanModel.FanSelectionStateMsg.FanSelectionState = EnumFanSelectionState.HighUnsafe;
                            }
                        }
                        //低速档风机选型点处于危险范围
                        else
                        {
                            //高速档风机选型点处于安全范围
                            if (!pFanModel.IsPointSafe)
                            {
                                cFanModel.FanSelectionStateMsg.FanSelectionState = EnumFanSelectionState.LowUnsafe;
                                pFanModel.FanSelectionStateMsg.FanSelectionState = EnumFanSelectionState.LowUnsafe;
                            }
                            //高速档风机选型点处于危险范围
                            else
                            {
                                cFanModel.FanSelectionStateMsg.FanSelectionState = EnumFanSelectionState.HighAndLowBothUnsafe;
                                pFanModel.FanSelectionStateMsg.FanSelectionState = EnumFanSelectionState.HighAndLowBothUnsafe;
                            }
                        }
                    }
                    else 
                    {
                        //低速风机为找到
                        var lowgeometry = childAxialFanParameters.ToGeometries(new AxialModelNumberComparer(), "高");

                        if (pPick != null)
                        {
                            var highreferencepoint = pPick.ModelGeometry.ReferenceModelPoint(new List<double>() { pFanModel.AirVolume, pFanModel.WindResis }, lowgeometry.First());
                            List<double> recommendPointInLow = new List<double> { 0, 0 };
                            if (highreferencepoint.Count != 0)
                            {
                                recommendPointInLow = new List<double> { Math.Round(highreferencepoint.First().X), Math.Round(highreferencepoint.First().Y) };
                            }
                            //cFanModel.FanSelectionStateMsg.RecommendPointInLow = recommendPointInLow;
                            pFanModel.FanSelectionStateMsg.RecommendPointInLow = recommendPointInLow;
                        }
                        //cFanModel.FanSelectionStateMsg.FanSelectionState = EnumFanSelectionState.LowNotFound;
                        pFanModel.FanSelectionStateMsg.FanSelectionState = EnumFanSelectionState.LowNotFound;
                    }
                    break;
                default:
                    if (!pFanModel.IsPointSafe)
                    {
                        pFanModel.FanSelectionStateMsg.FanSelectionState = EnumFanSelectionState.HighUnsafe;
                    }
                    else
                    {
                        pFanModel.FanSelectionStateMsg.FanSelectionState = EnumFanSelectionState.HighAndLowBothSafe;
                    }
                    break;
            }
        }
        public static void CheckFugeFanSelectModel(FanDataModel pFanModel, FanDataModel cFanModel, FanModelPicker pPick, FanModelPicker cPick, List<FanParameters> childFugeFanParameters)
        {
            //高速档未选到风机
            if (string.IsNullOrEmpty(pFanModel.FanModelCCCF))
            {
                if(cFanModel != null)
                    cFanModel.FanSelectionStateMsg.FanSelectionState = EnumFanSelectionState.HighNotFound;
                pFanModel.FanSelectionStateMsg.FanSelectionState = EnumFanSelectionState.HighNotFound;
                return;
            }
            switch (pFanModel.Control)
            {
                case EnumFanControl.TwoSpeed:
                    //双速时
                    if (cPick != null)
                    {
                        //低速档风机选型点处于安全范围
                        if (!cFanModel.IsPointSafe)
                        {
                            //高速档风机选型点处于安全范围
                            if (!pFanModel.IsPointSafe)
                            {
                                cFanModel.FanSelectionStateMsg.FanSelectionState = EnumFanSelectionState.HighAndLowBothSafe;
                                pFanModel.FanSelectionStateMsg.FanSelectionState = EnumFanSelectionState.HighAndLowBothSafe;
                            }
                            //高速档风机选型点处于危险范围
                            else
                            {
                                cFanModel.FanSelectionStateMsg.FanSelectionState = EnumFanSelectionState.HighUnsafe;
                                pFanModel.FanSelectionStateMsg.FanSelectionState = EnumFanSelectionState.HighUnsafe;
                            }
                        }
                        //低速档风机选型点处于危险范围
                        else
                        {
                            //高速档风机选型点处于安全范围
                            if (!pFanModel.IsPointSafe)
                            {
                                cFanModel.FanSelectionStateMsg.FanSelectionState = EnumFanSelectionState.LowUnsafe;
                                pFanModel.FanSelectionStateMsg.FanSelectionState = EnumFanSelectionState.LowUnsafe;
                            }
                            //高速档风机选型点处于危险范围
                            else
                            {
                                cFanModel.FanSelectionStateMsg.FanSelectionState = EnumFanSelectionState.HighAndLowBothUnsafe;
                                pFanModel.FanSelectionStateMsg.FanSelectionState = EnumFanSelectionState.HighAndLowBothUnsafe;
                            }
                        }
                    }
                    else
                    {
                        //低速风机为找到
                        var lowgeometry = childFugeFanParameters.ToGeometries(new CCCFComparer(), "高");
                        if (pPick != null)
                        {
                            var highreferencepoint = pPick.ModelGeometry.ReferenceModelPoint(new List<double>() { pFanModel.AirVolume, pFanModel.WindResis }, lowgeometry.First());
                            List<double> recommendPointInLow = new List<double> { 0, 0 };
                            if (highreferencepoint.Count != 0)
                            {
                                recommendPointInLow = new List<double> { Math.Round(highreferencepoint.First().X), Math.Round(highreferencepoint.First().Y) };
                            }
                            cFanModel.FanSelectionStateMsg.RecommendPointInLow = recommendPointInLow;
                            pFanModel.FanSelectionStateMsg.RecommendPointInLow = recommendPointInLow;
                        }
                        cFanModel.FanSelectionStateMsg.FanSelectionState = EnumFanSelectionState.LowNotFound;
                        pFanModel.FanSelectionStateMsg.FanSelectionState = EnumFanSelectionState.LowNotFound;
                    }
                    break;
                default:
                    if (!pFanModel.IsPointSafe)
                    {
                        pFanModel.FanSelectionStateMsg.FanSelectionState = EnumFanSelectionState.HighUnsafe;
                    }
                    else
                    {
                        pFanModel.FanSelectionStateMsg.FanSelectionState = EnumFanSelectionState.HighAndLowBothSafe;
                    }
                    break;
            }
        }

        public static SolidColorBrush GetErrorTextColor(EnumFanSelectionState fanSelectionState) 
        {
            SolidColorBrush solidColor =null;
            switch (fanSelectionState)
            {
                case EnumFanSelectionState.HighAndLowBothUnsafe:
                case EnumFanSelectionState.HighUnsafe:
                case EnumFanSelectionState.LowUnsafe:
                    solidColor = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF8066"));
                    break;
                case EnumFanSelectionState.LowNotFound:
                    solidColor = Brushes.Red;
                    break;
                default:
                    solidColor = Brushes.Black;
                    break;
            }
            return solidColor;
        }
        public static string GetFanDataErrorMsg(FanDataModel dataModel)
        {
            string msg = "";
            switch (dataModel.FanSelectionStateMsg.FanSelectionState)
            {
                case EnumFanSelectionState.HighUnsafe:
                    msg = " 高速挡输入的总阻力偏小.";
                    break;
                case EnumFanSelectionState.LowUnsafe:
                    msg = " 低速挡输入的总阻力偏小.";
                    break;
                case EnumFanSelectionState.HighAndLowBothUnsafe:
                    msg = " 高、低速档输入的总阻力都偏小.";
                    break;
                case EnumFanSelectionState.LowNotFound:
                    if (dataModel.FanSelectionStateMsg.RecommendPointInLow.Count == 2)
                    {
                        msg = string.Format(" 低速挡的工况点与高速挡差异过大,低速档风量的推荐值在{0}m³/h左右, ", dataModel.FanSelectionStateMsg.RecommendPointInLow[0]);
                        msg += string.Format("\n总阻力的推荐值小于{0}Pa.", dataModel.FanSelectionStateMsg.RecommendPointInLow[1]);
                    }
                    break;
            }
            return msg;
        }
    }
}
