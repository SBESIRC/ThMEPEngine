using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Office.Interop.Excel;
using TianHua.FanSelection.Function;
using TianHua.FanSelection.Model;
using TianHua.Publics.BaseCode;

namespace TianHua.FanSelection.ExcelExport
{
    class ExhaustAxisymmetricExportWorker : BaseExportWorker
    {
        public override void ExportToExcel(IFanModel exhaustfanmodel, Worksheet setsheet, Worksheet targetsheet, FanDataModel fandatamodel, ExcelRangeCopyOperator copyoperator)
        {
            ExhaustCalcModel exhaustModel = exhaustfanmodel as ExhaustCalcModel;
            setsheet.SetCellValue("D2", fandatamodel.FanNum);
            setsheet.SetCellValue("D3", fandatamodel.Name);
            setsheet.SetCellValue("D5", FuncStr.NullToStr(Math.Max(exhaustModel.MinAirVolume.NullToDouble(), exhaustModel.Final_CalcAirVolum.NullToDouble())) );
            setsheet.SetCellValue("D6", exhaustModel.Final_CalcAirVolum);
            setsheet.SetCellValue("D7", FuncStr.NullToStr(Math.Round(ExhaustModelCalculator.GetDtValue(exhaustModel))));
            if (ExhaustModelCalculator.IfZBiggerThanZ1(exhaustModel))
            {
                setsheet.SetCellValue("D8", FuncStr.NullToStr(Math.Round(ExhaustModelCalculator.GetAxialCalcAirVolum(exhaustModel))) );
            }
            else
            {
                setsheet.SetCellValue("D9", FuncStr.NullToStr(Math.Round(ExhaustModelCalculator.GetAxialCalcAirVolum(exhaustModel))));
            }
            setsheet.SetCellValue("D10", FuncStr.NullToStr(Math.Round(ExhaustModelCalculator.GetZ1Value(exhaustModel))));
            setsheet.SetCellValue("D11", FuncStr.NullToStr(Math.Round(ExhaustModelCalculator.GetHqValue(exhaustModel))));
            setsheet.SetCellValue("D12", "1");
            setsheet.SetCellValue("D13", FuncStr.NullToStr(Math.Round(ExhaustModelCalculator.GeTValue(exhaustModel))));
            setsheet.SetCellValue("D14", "1.2");
            setsheet.SetCellValue("D15", "293.15");
            setsheet.SetCellValue("D16", FuncStr.NullToStr(Math.Round(0.7* exhaustModel.HeatReleaseRate.NullToDouble())));
            setsheet.SetCellValue("D17", FuncStr.NullToStr(Math.Round(exhaustModel.HeatReleaseRate.NullToDouble())));
            setsheet.SetCellValue("D18", "1.01");
            setsheet.SetCellValue("D19", FuncStr.NullToStr(Math.Round(ExhaustModelCalculator.GetZValue(exhaustModel))));
            setsheet.SetCellValue("D20", FuncStr.NullToStr(Math.Round(exhaustModel.SpaceHeight.NullToDouble())));
            setsheet.SetCellValue("D21", FuncStr.NullToStr(Math.Round(exhaustModel.MinAirVolume.NullToDouble())));
            setsheet.SetCellValue("D22", FuncStr.NullToStr(Math.Round(exhaustModel.SpaceHeight.NullToDouble())));
            setsheet.SetCellValue("D23", FuncStr.NullToStr(Math.Round(exhaustModel.Axial_FuelFloor.NullToDouble())));
            setsheet.SetCellValue("D25", FuncStr.NullToStr(Math.Round(exhaustModel.Axial_HangingWallGround.NullToDouble())));
            setsheet.SetCellValue("D26", FuncStr.NullToStr(Math.Round(exhaustModel.Axial_HighestHeight.NullToDouble())));

            switch (exhaustModel.ExhaustCalcType)
            {
                case "空间-净高小于等于6m":
                case "走道回廊-房间内和走道或回廊都设置排烟":
                    SetWithArea(exhaustModel, setsheet);
                    copyoperator.CopyRangeToOtherSheet(setsheet, "A1:D31", targetsheet);
                    break;
                case "空间-净高大于6m":
                case "空间-汽车库":
                case "走道回廊-仅走道或回廊设置排烟":
                case "中庭-周围场所设有排烟系统":
                case "中庭-周围场所不设排烟系统":
                    SetWithoutArea(exhaustModel, setsheet);
                    copyoperator.CopyRangeToOtherSheet(setsheet, "A1:D30", targetsheet);
                    break;
                default:
                    break;
            }
        }
        private void SetWithArea(ExhaustCalcModel exhaustModel, Worksheet setsheet)
        {
            setsheet.SetCellValue("D27", FuncStr.NullToStr(Math.Round(exhaustModel.CoveredArea.NullToDouble())));
            setsheet.SetCellValue("D28", FuncStr.NullToStr(Math.Round(exhaustModel.HeatReleaseRate.NullToDouble())));
            setsheet.SetCellValue("D29", FuncStr.NullToStr(Math.Round(exhaustModel.SmokeThickness.NullToDouble())));
            setsheet.SetCellValue("C30", exhaustModel.SmokeFactorOption);
            setsheet.SetCellValue("D30", FuncStr.NullToStr(Math.Round(exhaustModel.SmokeFactorValue.NullToDouble(),1)));
            setsheet.SetCellValue("D31", FuncStr.NullToStr(Math.Round(exhaustModel.MaxSmokeExtraction.NullToDouble())));
        }
        private void SetWithoutArea(ExhaustCalcModel exhaustModel, Worksheet setsheet)
        {
            setsheet.SetCellValue("D27", FuncStr.NullToStr(Math.Round(exhaustModel.HeatReleaseRate.NullToDouble())));
            setsheet.SetCellValue("D28", FuncStr.NullToStr(Math.Round(exhaustModel.SmokeThickness.NullToDouble())));
            setsheet.SetCellValue("C29", exhaustModel.SmokeFactorOption);
            setsheet.SetCellValue("D29", FuncStr.NullToStr(Math.Round(exhaustModel.SmokeFactorValue.NullToDouble(),1)));
            setsheet.SetCellValue("D30", FuncStr.NullToStr(Math.Round(exhaustModel.MaxSmokeExtraction.NullToDouble())));
        }

    }
}
