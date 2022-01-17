using AcHelper;
using Autodesk.AutoCAD.DatabaseServices;
using Linq2Acad;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using ThCADCore.NTS;
using ThCADExtension;
using ThControlLibraryWPF.ControlUtils;
using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore.Command;
using ThMEPEngineCore.IO.ExcelService;
using ThMEPHVAC.IndoorFanLayout;
using ThMEPHVAC.IndoorFanLayout.DataEngine;
using ThMEPHVAC.IndoorFanModels;
using ThMEPHVAC.ParameterService;

namespace ThMEPHVAC.Command
{
    class ThHvacIndoorFanExportCmd : ThMEPBaseCommand, IDisposable
    {
        Dictionary<Polyline, List<Polyline>> _selectPLines;
        ThMEPOriginTransformer _originTransformer;
        List<IndoorFanBase> _allFans;
        string exportPath;
        string TemplateExcelPath = "";
        EnumFanType enumFanType;

        public string ShowMsg="";
        public ThHvacIndoorFanExportCmd()
        {
            Dictionary<Polyline, List<Polyline>> selectRoomLines = IndoorFanParameter.Instance.ExportModel.ExportAreas;
            TemplateExcelPath = ThCADCommon.IndoorFanExportTablePath();
            enumFanType = IndoorFanParameter.Instance.ExportModel.FanType;
            CommandName = "THSNJDC";
            ActionName = "室内机导出材料表";
            _selectPLines = new Dictionary<Polyline, List<Polyline>>();
            if (null == selectRoomLines || selectRoomLines.Count < 1)
                return;
            var pt = selectRoomLines.First().Key.StartPoint;
            _originTransformer = new ThMEPOriginTransformer(pt);
            foreach (var pline in selectRoomLines)
            {
                var copyOut = (Polyline)pline.Key.Clone();
                if (null != _originTransformer)
                    _originTransformer.Transform(copyOut);
                var innerPLines = new List<Polyline>();
                if (pline.Value != null)
                {
                    foreach (var item in pline.Value)
                    {
                        var copyInner = (Polyline)item.Clone();
                        if (null != _originTransformer)
                            _originTransformer.Transform(copyInner);
                        innerPLines.Add(copyInner);
                    }
                }
                _selectPLines.Add(copyOut, innerPLines);
            }

            _allFans = new List<IndoorFanBase>();
            foreach (var item in IndoorFanParameter.Instance.ExportModel.TargetFanInfo) 
            {
                _allFans.Add(item);
            }
            exportPath = IndoorFanParameter.Instance.ExportModel.SavePath;
        }
        public void Dispose()
        {
        }

        public override void SubExecute()
        {
            ShowMsg = "";
            if (_selectPLines == null || _selectPLines.Count < 1)
                return;
            string sheetName = CommonUtil.GetEnumDescription(enumFanType);
            if (string.IsNullOrEmpty(sheetName)) 
            {
                ShowMsg = "获取风机类型失败";
                return;
            }
            using (Active.Document.LockDocument())
            using (var acdb = AcadDatabase.Active())
            {
                IndoorFanBlockServices.LoadBlockLayerToDocument(acdb.Database);
            }
            //选择框线，
            var indoorFanData = new ThIndoorFanData(_originTransformer);
            //获取所有的风机块
            var allFans = indoorFanData.GetIndoorFanBlockModels();
            if (allFans.Count < 1)
            {
                ShowMsg = "没有找到任何风机，无法进行导出";
                return; 
            }
            var targetFans = allFans.Where(c => c.FanType == enumFanType).ToList();
            if (targetFans.Count < 1)
            {
                ShowMsg = "框选范围内没有和选中的类型风机，无法进行导出";
                return; 
            }
            //获取房间框线内的块，进行过滤
            var fanNumbers = new List<string>();
            foreach (var block in targetFans)
            {
                var point = block.BlockPosion;
                bool isAdd = _selectPLines.Any(c => c.Key.Contains(point));
                if (isAdd)
                {
                    //获取风机型号
                    fanNumbers.Add(block.FanName);
                }
            }
            if (fanNumbers.Count < 1)
            {
                ShowMsg = "框选范围内没有相应类型的风机，无法进行导出";
                return;
            }
            //获取风机型号
            var dic = fanNumbers.GroupBy(c => c).ToDictionary(c => c.Key, x => x.Count());
            var exportFans = new List<IndoorFanBase>();
            foreach (var item in _allFans) 
            {
                var key = dic.Where(c => c.Key == item.FanNumber).FirstOrDefault().Key;
                if (string.IsNullOrEmpty(key))
                    continue;
                item.FanCount = dic[key].ToString();
                exportFans.Add(item);
            }
            var data = new DataSet();
            var dataSet = new IndoorFanDataToDataSet();
            switch (enumFanType)
            {
                case EnumFanType.FanCoilUnitFourControls:
                case EnumFanType.FanCoilUnitTwoControls:
                    var coilTable = dataSet.FansToDataTable(exportFans.Cast<CoilUnitFan>().ToList());
                    data.Tables.Add(coilTable);
                    break;
                case EnumFanType.IntegratedAirConditionin:
                    var airTable = dataSet.FansToDataTable(exportFans.Cast<AirConditioninFan>().ToList());
                    data.Tables.Add(airTable);
                    break;
                case EnumFanType.VRFConditioninConduit:
                case EnumFanType.VRFConditioninFourSides:
                    bool isPipe = enumFanType == EnumFanType.VRFConditioninConduit;
                    var vrfTable = dataSet.FansToDataTable(exportFans.Cast<VRFFan>().ToList(), isPipe);
                    data.Tables.Add(vrfTable);
                    break;
            }
            try 
            {
                File.Copy(TemplateExcelPath, exportPath, true);
                var writeExcel = new WriteExcelHelper(exportPath);
                writeExcel.DeleteExcelSheet(new List<string> { sheetName }, false);
                writeExcel.WriteDataTableToTemplateExcel(data.Tables[0], sheetName, 4);
                writeExcel.DeleteExcelRow(0,4+ data.Tables[0].Rows.Count-1,-1);
                ShowMsg = "导出成功";
            }
            catch(Exception ex) 
            {
                ShowMsg = "导出数据失败:"+ex.Message;
            }
        }
    }
}
