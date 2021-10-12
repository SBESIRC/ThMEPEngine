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
using ThMEPEngineCore.Command;

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
    public class ThFanMaterialTableExtractCmd : ThMEPBaseCommand, IDisposable
    {
        public string FilePath { set; private get; }
        public Point3dCollection Areas { set; private get; }
        public void Dispose()
        {
            throw new NotImplementedException();
        }
        override public void SubExecute()
        {
            try
            {
                string configPath = ThCADCommon.FanMaterialTablePath();
                using (var excelpackage = CreateModelExportExcelPackage(configPath))
                {
                    excelpackage.SaveAs(new FileInfo(FilePath));
                }
                var wafFanInfoList = ThFanExtractServiece.GetWAFFanConfigInfoList(Areas);
                HandleFanInfoList(wafFanInfoList, FilePath, "壁式轴流风机");
                var WexhFanInfoList = ThFanExtractServiece.GetWEXHFanConfigInfoList(Areas);
                HandleFanInfoList(WexhFanInfoList, FilePath, "壁式排气扇");
                var cexhFanInfoList = ThFanExtractServiece.GetCEXHFanConfigInfoList(Areas);
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
                tmpItem.StrWeight = d.Value[0].FanWeight.ToString();
                tmpItem.StrCount = d.Value.Count.ToString();
                tmpItem.StrRemark = "";
                formItmes.Add(tmpItem);
            }

            using (var excelpackage = CreateModelExportExcelPackage(saveFile))
            {
                if (type == "壁式轴流风机")
                {
                    SaveAsExecl(excelpackage.Workbook.Worksheets["壁式轴流风机"], formItmes);
                    excelpackage.Save();
                }
                else if (type == "壁式排气扇")
                {
                    SaveAsExecl(excelpackage.Workbook.Worksheets["壁式排气扇"], formItmes);
                    excelpackage.Save();

                }
                else if (type == "吊顶式排气扇")
                {
                    SaveAsExecl(excelpackage.Workbook.Worksheets["吊顶式排气扇"], formItmes);
                    excelpackage.Save();
                }
                else
                {
                    throw new NotSupportedException();
                }
            }
        }

        private ExcelPackage CreateModelExportExcelPackage(string filePath)
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            return new ExcelPackage(new FileInfo(filePath));
        }
        private void SaveAsExecl( ExcelWorksheet _Sheet, List<FanFormItem> itemList)
        {
            var i = 3;

            if(_Sheet.Name == "壁式轴流风机" || _Sheet.Name == "壁式排气扇")
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
            else if(_Sheet.Name == "吊顶式排气扇")
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
