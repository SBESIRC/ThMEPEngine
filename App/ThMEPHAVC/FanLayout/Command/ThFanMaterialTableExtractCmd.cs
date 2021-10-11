using System;
using AcHelper;
using AcHelper.Commands;
using System.Collections.Generic;
using ThMEPHVAC.FanLayout.Service;
using ThMEPHVAC.FanLayout.ViewModel;
using Autodesk.AutoCAD.Geometry;
using ThCADExtension;
using Autodesk.AutoCAD.DatabaseServices;
using DotNetARX;
using Dreambuild.AutoCAD;
using GeometryExtensions;
using ThMEPEngineCore.IO.ExcelService;
using System.Data;
using OfficeOpenXml;
using System.IO;

namespace ThMEPHVAC.FanLayout.Command
{
    public class FanFormItem
    {
        public string StrType;//设备类型
        public string StrNumber;//设备编号
        public string StrServiceArea;//服务区域
        public string StrAirVolume;//风量
        public string StrPressure;//全压
        public string StrPower;//功率
        public string StrNoise;//噪声
        public string StrWeight;//重量
        public string StrCount;//台数
        public string StrRemark;//备注
    }
    public class ThFanMaterialTableExtractCmd : IAcadCommand, IDisposable
    {
        public string FilePath { set; private get; }
        public void Dispose()
        {
            throw new NotImplementedException();
        }
        public Point3dCollection SelectAreas()
        {
            using (PointCollector pc = new PointCollector(PointCollector.Shape.Window, new List<string>()))
            {
                try
                {
                    pc.Collect();
                }
                catch
                {
                    return new Point3dCollection();
                }
                Point3dCollection winCorners = pc.CollectedPoints;
                var frame = new Polyline();
                frame.CreateRectangle(winCorners[0].ToPoint2d(), winCorners[1].ToPoint2d());
                frame.TransformBy(Active.Editor.UCS2WCS());
                return frame.Vertices();
            }
        }
        public void Execute()
        {
            try
            {
                var area = SelectAreas();//获取范围
                string configPath = ThCADCommon.FanParameterTablePath();

                using (var excelpackage = CreateModelExportExcelPackage(configPath))
                {
                    excelpackage.SaveAs(new FileInfo(FilePath));
                }
                var wafFanInfoList = ThFanExtractServiece.GetWAFFanConfigInfoList(area);
                HandleFanInfoList(wafFanInfoList, FilePath, "壁式轴流风机");
                var WexhFanInfoList = ThFanExtractServiece.GetWEXHFanConfigInfoList(area);
                HandleFanInfoList(WexhFanInfoList, FilePath, "壁式排气扇");
                var cexhFanInfoList = ThFanExtractServiece.GetCEXHFanConfigInfoList(area);
                HandleFanInfoList(cexhFanInfoList, FilePath, "吊顶式排气扇");
            }
            catch (Exception ex)
            {
                Active.Editor.WriteMessage(ex.Message);
            }
        }
        private void HandleFanInfoList(List<ThFanConfigInfo> infoList, string saveFile, string type)
        {
            var fanDictionary = new Dictionary<string, List<ThFanConfigInfo>>();

            //整理数据，分组操作
            foreach (ThFanConfigInfo fan in infoList)
            {
                string key = fan.FanNumber;
                if (fanDictionary.ContainsKey(key))
                {
                    fanDictionary[key].Add(fan);
                }
                else
                {
                    List<ThFanConfigInfo> tmpFanList = new List<ThFanConfigInfo>();
                    tmpFanList.Add(fan);
                    fanDictionary.Add(key, tmpFanList);
                }
            }

            List<FanFormItem> formItmes = new List<FanFormItem>();
            foreach (var d in fanDictionary)
            {
                FanFormItem tmpItem = new FanFormItem();
                tmpItem.StrType = type;
                tmpItem.StrNumber = d.Key;
                tmpItem.StrServiceArea = "";
                tmpItem.StrAirVolume = d.Value[0].FanVolume.ToString();
                tmpItem.StrPressure = d.Value[0].FanPressure.ToString();
                tmpItem.StrPower = d.Value[0].FanPower.ToString();
                tmpItem.StrNoise = d.Value[0].FanNoise.ToString();
                tmpItem.StrWeight = "";
                tmpItem.StrCount = d.Value.Count.ToString();
                tmpItem.StrRemark = "";
                formItmes.Add(tmpItem);
            }

            using (var excelpackage = CreateModelExportExcelPackage(saveFile))
            {
                if (type == "壁式轴流风机")
                {
                    SaveAsExecl(excelpackage.Workbook.Worksheets[1], formItmes, 1);
                    excelpackage.Save();
                }
                else if (type == "壁式排气扇")
                {
                    SaveAsExecl(excelpackage.Workbook.Worksheets[2], formItmes, 2);
                    excelpackage.Save();

                }
                else if (type == "吊顶式排气扇")
                {
                    SaveAsExecl(excelpackage.Workbook.Worksheets[3], formItmes, 3);
                    excelpackage.Save();
                }
            }
        }

        private ExcelPackage CreateModelExportExcelPackage(string filePath)
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            return new ExcelPackage(new FileInfo(filePath));
        }
        private void SaveAsExecl( ExcelWorksheet _Sheet, List<FanFormItem> itemList,int index)
        {
            var i = 3;

            if(index == 1 || index == 2)
            {
                foreach(var p in itemList)
                {
                    _Sheet.Cells[i, 1].Value = p.StrType;
                    _Sheet.Cells[i, 2].Value = p.StrNumber;
                    _Sheet.Cells[i, 3].Value = p.StrServiceArea;
                    _Sheet.Cells[i, 4].Value = p.StrAirVolume;
                    _Sheet.Cells[i, 5].Value = p.StrPressure;
                    _Sheet.Cells[i, 6].Value = p.StrPower;
                    _Sheet.Cells[i, 7].Value = "220/1/50";
                    _Sheet.Cells[i, 8].Value = p.StrNoise;
                    _Sheet.Cells[i, 9].Value = p.StrWeight;
                    _Sheet.Cells[i, 10].Value = p.StrCount;
                    _Sheet.Cells[i, 11].Value = p.StrRemark;
                    i++;
                }
            }
            else if(index == 3)
            {
                foreach (var p in itemList)
                {
                    _Sheet.Cells[i, 1].Value = p.StrType;
                    _Sheet.Cells[i, 2].Value = p.StrNumber;
                    _Sheet.Cells[i, 3].Value = p.StrServiceArea;
                    _Sheet.Cells[i, 4].Value = p.StrAirVolume;
                    _Sheet.Cells[i, 5].Value = p.StrPower;
                    _Sheet.Cells[i, 6].Value = "220/1/50";
                    _Sheet.Cells[i, 7].Value = p.StrNoise;
                    _Sheet.Cells[i, 8].Value = p.StrWeight;
                    _Sheet.Cells[i, 9].Value = p.StrCount;
                    _Sheet.Cells[i, 10].Value = p.StrRemark;
                    i++;
                }
            }
        }
    }
}
