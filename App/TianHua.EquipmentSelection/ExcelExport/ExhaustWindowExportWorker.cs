using System;
using OfficeOpenXml;
using TianHua.FanSelection.Function;
using TianHua.FanSelection.Model;
using TianHua.Publics.BaseCode;

namespace TianHua.FanSelection.ExcelExport
{
    class ExhaustWindowExportWorker : BaseExportWorker
    {
        public override void ExportToExcel(IFanModel exhaustfanmodel, ExcelWorksheet setsheet, ExcelWorksheet targetsheet, FanDataModel fandatamodel, ExcelRangeCopyOperator copyoperator)
        {
            ExhaustCalcModel exhaustModel = exhaustfanmodel as ExhaustCalcModel;
            setsheet.Cells["D2"].Value = fandatamodel.FanNum;
            setsheet.Cells["D3"].Value = fandatamodel.Name;
            setsheet.Cells["D5"].Value = FuncStr.NullToStr(Math.Max(exhaustModel.MinAirVolume.NullToDouble(), exhaustModel.Final_CalcAirVolum.NullToDouble()));
            setsheet.Cells["D6"].Value = exhaustModel.Final_CalcAirVolum;
            setsheet.Cells["D7"].Value = FuncStr.NullToStr(Math.Round(ExhaustModelCalculator.GetDtValue(exhaustModel)));
            setsheet.Cells["D8"].Value = FuncStr.NullToStr(Math.Round(ExhaustModelCalculator.GetWindowCalcAirVolum(exhaustModel)));
            setsheet.Cells["D9"].Value = FuncStr.NullToStr(Math.Round(ExhaustModelCalculator.GetawValue(exhaustModel)));
            setsheet.Cells["D10"].Value = "1";
            setsheet.Cells["D11"].Value = FuncStr.NullToStr(Math.Round(ExhaustModelCalculator.GeTValue(exhaustModel)));
            setsheet.Cells["D12"].Value = "1.2";
            setsheet.Cells["D13"].Value = "293.15";
            setsheet.Cells["D14"].Value = FuncStr.NullToStr(Math.Round(0.7 * exhaustModel.HeatReleaseRate.NullToDouble()));
            setsheet.Cells["D15"].Value = FuncStr.NullToStr(Math.Round(exhaustModel.HeatReleaseRate.NullToDouble()));
            setsheet.Cells["D16"].Value = "1.01";
            setsheet.Cells["D17"].Value = FuncStr.NullToStr(Math.Round(exhaustModel.SpaceHeight.NullToDouble()));
            setsheet.Cells["D18"].Value = FuncStr.NullToStr(Math.Round(exhaustModel.MinAirVolume.NullToDouble()));
            setsheet.Cells["D19"].Value = FuncStr.NullToStr(Math.Round(exhaustModel.SpaceHeight.NullToDouble()));
            setsheet.Cells["D20"].Value = FuncStr.NullToStr(Math.Round(exhaustModel.Window_WindowArea.NullToDouble()));
            setsheet.Cells["D21"].Value = FuncStr.NullToStr(Math.Round(exhaustModel.Window_WindowHeight.NullToDouble()));
            setsheet.Cells["D22"].Value = FuncStr.NullToStr(Math.Round(exhaustModel.Window_SmokeBottom.NullToDouble()));

            switch (exhaustModel.ExhaustCalcType)
            {
                case "空间-净高小于等于6m":
                case "走道回廊-房间内和走道或回廊都设置排烟":
                    SetWithArea(exhaustModel, setsheet);
                    copyoperator.CopyRangeToOtherSheet(setsheet, 1, 1, 27, 4, targetsheet);
                    break;
                case "空间-净高大于6m":
                case "空间-汽车库":
                case "走道回廊-仅走道或回廊设置排烟":
                case "中庭-周围场所设有排烟系统":
                case "中庭-周围场所不设排烟系统":
                    SetWithoutArea(exhaustModel, setsheet);
                    copyoperator.CopyRangeToOtherSheet(setsheet, 1, 1, 26, 4, targetsheet);
                    break;
                default:
                    break;
            }
        }
        private void SetWithArea(ExhaustCalcModel exhaustModel, ExcelWorksheet setsheet)
        {
            setsheet.Cells["D23"].Value = FuncStr.NullToStr(Math.Round(exhaustModel.CoveredArea.NullToDouble()));
            setsheet.Cells["D24"].Value = FuncStr.NullToStr(Math.Round(exhaustModel.HeatReleaseRate.NullToDouble()));
            setsheet.Cells["D25"].Value = FuncStr.NullToStr(Math.Round(exhaustModel.SmokeThickness.NullToDouble()));
            setsheet.Cells["C26"].Value = exhaustModel.SmokeFactorOption;
            setsheet.Cells["D26"].Value = FuncStr.NullToStr(Math.Round(exhaustModel.SmokeFactorValue.NullToDouble(), 1));
            setsheet.Cells["D27"].Value = FuncStr.NullToStr(Math.Round(exhaustModel.MaxSmokeExtraction.NullToDouble()));
        }
        private void SetWithoutArea(ExhaustCalcModel exhaustModel, ExcelWorksheet setsheet)
        {
            setsheet.Cells["D23"].Value = FuncStr.NullToStr(Math.Round(exhaustModel.HeatReleaseRate.NullToDouble()));
            setsheet.Cells["D24"].Value = FuncStr.NullToStr(Math.Round(exhaustModel.SmokeThickness.NullToDouble()));
            setsheet.Cells["C25"].Value = exhaustModel.SmokeFactorOption;
            setsheet.Cells["D25"].Value = FuncStr.NullToStr(Math.Round(exhaustModel.SmokeFactorValue.NullToDouble(), 1));
            setsheet.Cells["D26"].Value = FuncStr.NullToStr(Math.Round(exhaustModel.MaxSmokeExtraction.NullToDouble()));
        }

    }
}
