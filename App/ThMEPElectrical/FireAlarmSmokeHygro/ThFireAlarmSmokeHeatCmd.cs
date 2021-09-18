using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading.Tasks;

using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.DatabaseServices;

using AcHelper;
using Linq2Acad;
using GeometryExtensions;
using NFox.Cad;

using ThCADCore.NTS;
using ThCADExtension;
using ThMEPEngineCore;
using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore.Command;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.IO;
using ThMEPEngineCore.IO.GeoJSON;

using ThMEPElectrical.AlarmSensorLayout.Command;
using ThMEPElectrical.AlarmSensorLayout.Data;

using ThMEPElectrical.FireAlarm.Service;
using ThMEPElectrical.FireAlarmSmokeHeat.Data;
using ThMEPElectrical.FireAlarmSmokeHeat.Service;
using ThMEPElectrical.FireAlarm.ViewModels;
using ThMEPElectrical.FireAlarm;

namespace ThMEPElectrical.FireAlarmSmokeHeat
{
    public class ThFireAlarmSmokeHeatCmdsNoUI
    {
        [CommandMethod("TIANHUACAD", "THSmokeLayout", CommandFlags.Modal)]
        public void FireAlarmSmokeHeatCmd()
        {
            using (var cmd = new ThFireAlarmSmokeHeatCmd())
            {
                cmd.Execute();
            }
        }


        [System.Diagnostics.Conditional("DEBUG")]
        [CommandMethod("TIANHUACAD", "CleanDebugLayer", CommandFlags.Modal)]
        public void ThCleanDebugLayer()
        {
            // 调试按钮关闭且图层不是保护半径有效图层
            var debugSwitch = (Convert.ToInt16(Autodesk.AutoCAD.ApplicationServices.Application.GetSystemVariable("USERR2")) == 1);
            if (debugSwitch)
            {
                Common.ThFaCleanService.ClearDrawing();
            }
        }
    }

    public class ThFireAlarmSmokeHeatCmd : ThMEPBaseCommand, IDisposable
    {
        readonly FireAlarmViewModel _UiConfigs;
        private double _protectR = 5800;
        private int _theta = 0;
        private int _floorHight = 0;
        private double _scale = 100;

        public ThFireAlarmSmokeHeatCmd(FireAlarmViewModel uiConfigs)
        {
            _UiConfigs = uiConfigs;
            CommandName = "THFireAlarmSmokeLayout";
            ActionName = "生成";
            setInfo();
        }

        public ThFireAlarmSmokeHeatCmd()
        {

        }

        public override void SubExecute()
        {
            FireAlarmSmokeHeatLayoutExecute();
        }

        private void setInfo()
        {
            if (_UiConfigs != null)
            {
                _protectR = _UiConfigs.ValueOfD;
                _theta = _UiConfigs.SelectedIndexForAngle;
                _floorHight = _UiConfigs.SelectedIndexForH;
                _scale = _UiConfigs.BlockRatioIndex == 0 ? 100 : 150;
            }
        }

        public void Dispose()
        {
        }

        private void FireAlarmSmokeHeatLayoutExecute()
        {
            using (var doclock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var extractBlkList = ThFaCommon.BlkNameListAreaLayout;
                var cleanBlkName = new List<string>() { ThFaCommon.BlkName_Smoke, ThFaCommon.BlkName_Heat };
                var avoidBlkName = ThFaCommon.BlkNameListAreaLayout.Where(x => cleanBlkName.Contains(x) == false).ToList();

                //画框，提数据，转数据
                var pts = ThFireAlarmUtils.getFrame();
                if (pts.Count == 0)
                {
                    return;
                }

                var geos = ThFireAlarmUtils.getSmokeData(pts, extractBlkList);
                if (geos.Count == 0)
                {
                    return;
                }

                var transformer = ThFireAlarmUtils.transformToOrig(pts, geos);//debug 不用这个

                var layoutPts = new Dictionary<Point3d, Vector3d>();
                var layoutPtsType = new Dictionary<Point3d, bool>();//isSmokeSenser
                var blindResult = new List<Polyline>();
                var threshold = 0.025;

                var dataQuery = new ThSmokeDataQueryService(geos, cleanBlkName, avoidBlkName);
                //洞,必须先做找到框线
                dataQuery.analysisHoles();
                //墙，柱，可布区域，避让
                dataQuery.ClassifyData();

                var frameSensorType = ThFaAreaLayoutService.getAreaSensorType(dataQuery.frameList);
                //接入楼梯

                //

                foreach (var frame in dataQuery.frameList)
                {
                    try
                    {
                        Dictionary<Point3d, Vector3d> layoutResult = null;

                        var radius = ThFaAreaLayoutService.calculateRadius(frame.Area, _floorHight, _theta, frameSensorType[frame]);//to do...frame.area need to remove hole's area

                        //区域类型
                        var bIsAisleArea = isAisleArea(frame, dataQuery.frameHoleList[frame], radius * 0.8, threshold);
                        if (bIsAisleArea == false)
                        {
                            layoutResult = ThFaAreaLayoutService.ThFaAreaLayoutGrid(frame, dataQuery, radius, out var layoutCmd);
                            blindResult.AddRange(layoutCmd.blinds);
                        }
                        else
                        {
                            layoutResult = ThFaAreaLayoutService.ThFaAreaLayoutCenterline(frame, dataQuery, radius, out var layoutCmd);
                            blindResult.AddRange(layoutCmd.blinds);
                        }

                        foreach (var re in layoutResult)
                        {
                            layoutPts.Add(re.Key, re.Value);
                            layoutPtsType.Add(re.Key, frameSensorType[frame]);
                        }
                    }
                    catch
                    {
                        continue;
                    }
                }
            }
        }

        //private static List<ThGeometry> getData(Point3dCollection pts, List<string> extractBlkList)
        //{
        //    var bReadJson = true;
        //    var fileInfo = new FileInfo(Active.Document.Name);
        //    var path = fileInfo.Directory.FullName;

        //    var geos = new List<ThGeometry>();
        //    using (AcadDatabase acadDatabase = AcadDatabase.Active())
        //    {
        //        if (bReadJson == false)
        //        {
        //            var datasetFactory = new ThFaAreaLayoutDataSetFactory();
        //            var dataset = datasetFactory.Create(acadDatabase.Database, pts);
        //            geos.AddRange(dataset.Container);
        //            var businessDataFactory = new ThFaAreaLayoutBusinessDataSetFactory()
        //            {
        //                BlkNameList = extractBlkList,
        //            };
        //            var businessDataSet = businessDataFactory.Create(acadDatabase.Database, pts);
        //            geos.AddRange(businessDataSet.Container);
        //        }
        //        else
        //        {
        //            var psr = Active.Editor.GetFileNameForOpen("\n选择要打开的Geojson文件");
        //            if (psr.Status != PromptStatus.OK)
        //            {
        //                {
        //                    return geos;
        //                }
        //            }
        //            var sName = psr.StringResult;
        //            geos = ThGeometryJsonReader.ReadFromFile(sName);
        //        }
        //        if (bReadJson == false)
        //        {
        //            ThGeoOutput.Output(geos, path, fileInfo.Name);
        //        }
        //    }

        //    return geos;
        //}

        private static bool isAisleArea(Polyline frame, List<Polyline> HoleList, double shrinkValue, double threshold)
        {
            var objs = new DBObjectCollection();
            objs.Add(frame);
            HoleList.ForEach(x => objs.Add(x));
            var geometry = objs.BuildAreaGeometry();
            var isAisleArea = ThMEPEngineCoreGeUtils.IsAisleArea(geometry, shrinkValue, threshold);

            return isAisleArea;

        }

    }
}
