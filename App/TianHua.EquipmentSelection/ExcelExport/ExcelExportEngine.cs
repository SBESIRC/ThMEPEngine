using Microsoft.Office.Interop.Excel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TianHua.FanSelection.Model;

namespace TianHua.FanSelection.ExcelExport
{
    public class ExcelExportEngine
    {
        //==============SINGLETON============
        //fourth version from:
        //http://csharpindepth.com/Articles/General/Singleton.aspx
        private static readonly ExcelExportEngine instance = new ExcelExportEngine();
        // Explicit static constructor to tell C# compiler
        // not to mark type as beforefieldinit    
        static ExcelExportEngine() { }
        internal ExcelExportEngine() { }
        public static ExcelExportEngine Instance { get { return instance; } }
        //-------------SINGLETON-----------------

        public Workbook Sourcebook { get; set; }
        public Worksheet Targetsheet { get; set; }
        public FanDataModel Model { get; set; }
        public ExcelRangeCopyOperator RangeCopyOperator { get; set; }

        public void Run()
        {
            if (Model.FanVolumeModel == null)
            {
                return;
            }
            var worker = BaseExportWorker.Create(Model.FanVolumeModel);
            if (worker != null)
            {
                var sourcesheet = Sourcebook.GetSheetFromSheetName(Model.FanVolumeModel.FireScenario);
                worker.ExportToExcel(Model.FanVolumeModel, sourcesheet, Targetsheet, Model, RangeCopyOperator);
            }
        }

        public void RunExhaustExport()
        {
            if (Model.ExhaustModel == null)
            {
                return;
            }
            var worker = BaseExportWorker.Create(Model.ExhaustModel);
            if (worker != null)
            {
                var sourcesheet = Sourcebook.GetSheetFromSheetName(ExcelSheetNameFromSpatialTypes(Model.ExhaustModel));
                worker.ExportToExcel(Model.ExhaustModel, sourcesheet, Targetsheet, Model, RangeCopyOperator);
            }
        }

        //模型中空间类型与Excel中sheet名称的映射
        private string ExcelSheetNameFromSpatialTypes(ExhaustCalcModel model)
        {
            switch (model.ExhaustCalcType)
            {
                case "空间-净高小于等于6m":
                    if (model.PlumeSelection == "轴对称型")
                    {
                        return "1.1 净高小于6m-轴对称型";
                    }
                    else
                    {
                        if (model.PlumeSelection == "阳台溢出型")
                        {
                            return "1.2 净高小于6m-阳台溢出型";
                        }
                        else
                        {
                            return "1.3 净高小于6m-窗口型";
                        }
                    }
                    
                case "空间-净高大于6m":
                    if (model.PlumeSelection == "轴对称型")
                    {
                        return "2.1 净高大于6m-轴对称型";
                    }
                    else
                    {
                        if (model.PlumeSelection == "阳台溢出型")
                        {
                            return "2.2 净高大于6m-阳台溢出型";
                        }
                        else
                        {
                            return "2.3 净高大于6m-窗口型";
                        }
                    }

                case "空间-汽车库":
                    if (model.PlumeSelection == "轴对称型")
                    {
                        return "3.1 汽车库-轴对称型";
                    }
                    else
                    {
                        if (model.PlumeSelection == "阳台溢出型")
                        {
                            return "3.2 汽车库-阳台溢出型";
                        }
                        else
                        {
                            return "3.3 汽车库-窗口型";
                        }
                    }

                case "走道回廊-仅走道或回廊设置排烟":
                    if (model.PlumeSelection == "轴对称型")
                    {
                        return "4.1 仅走道回廊-轴对称型";
                    }
                    else
                    {
                        if (model.PlumeSelection == "阳台溢出型")
                        {
                            return "4.2 仅走道回廊-阳台溢出型";
                        }
                        else
                        {
                            return "4.3 仅走道回廊-窗口型";
                        }
                    }

                case "走道回廊-房间内和走道或回廊都设置排烟":
                    if (model.PlumeSelection == "轴对称型")
                    {
                        return "5.1 房间和走道-轴对称型";
                    }
                    else
                    {
                        if (model.PlumeSelection == "阳台溢出型")
                        {
                            return "5.2 房间和走道-阳台溢出型";
                        }
                        else
                        {
                            return "5.3 房间和走道-窗口型";
                        }
                    }

                case "中庭-周围场所设有排烟系统":
                case "中庭-周围场所不设排烟系统":
                    if (model.PlumeSelection == "轴对称型")
                    {
                        return "6.1 中庭-轴对称型";
                    }
                    else
                    {
                        if (model.PlumeSelection == "阳台溢出型")
                        {
                            return "6.2 中庭-阳台溢出型";
                        }
                        else
                        {
                            return "6.3 中庭-窗口型";
                        }
                    }

                default:
                    return "";
            }
        }
    }
}
