using System.IO;
using System.Linq;
using System.Collections.Generic;

using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

using AcHelper;
using Linq2Acad;
using NFox.Cad;
using Dreambuild.AutoCAD;
using FireAlarm.Data;

using ThCADExtension;
using ThMEPElectrical.FireAlarm.Logic;
using ThMEPElectrical.FireAlarm;
using ThMEPEngineCore.IO.GeoJSON;
using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.IO;
using Autodesk.AutoCAD.EditorInput;
using ThMEPElectrical.Command;

namespace ThMEPElectrical
{
    public class ThFireAlarmCmds
    {
        [CommandMethod("TIANHUACAD", "THFireAlarmData", CommandFlags.Modal)]
        public void THFireAlarmData()
        {
            //把Cad图纸数据写出到Geojson File中
            using (var cmd = new ThFireAlarmCommand())
            {
                cmd.Execute();
            }
        }

        [CommandMethod("TIANHUACAD", "ThDisplayDevice", CommandFlags.Modal)]
        public void ThDisplayDeviceLayout()
        {
            //选择Geojson File,获取数据
            //测试布置逻辑
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {

                //{
                //    var frame = ThMEPEngineCore.CAD.ThWindowInteraction.GetPolyline(
                //        PointCollector.Shape.Window, new List<string> { "请框选一个范围" });
                //    if (frame.Area < 1e-4)
                //    {
                //        return;
                //    }
                //    var pts = frame.Vertices();
                //    var datasetFactory = new ThFireAlarmDataSetFactory();
                //    var dataset = datasetFactory.Create(acadDatabase.Database, pts);

                //    //var psr = Active.Editor.GetFileNameForOpen("\n选择要打开的Geojson文件");
                //    //if(psr.Status!=PromptStatus.OK)
                //    //{
                //    //    return;
                //    //}
                //    //var geos = ThGeometryJsonReader.ReadFromFile(psr.StringResult);                
                //    var geos = dataset.Container;
                //    //var geosTemp = dataset.Container;
                //    //var geosJsonString = ThGeoOutput.Output(geosTemp);
                //    //var geos = ThGeometryJsonReader.ReadFromContent(geosJsonString);
                //    var objs = geos.Where(o => o.Boundary != null).Select(o => o.Boundary).ToCollection();
                //    var rooms = geos.Where(o => o.Properties["Category"].ToString().ToUpper() == "ROOM").Where(o => o.Boundary != null).Select(o => o.Boundary).ToList();
                //    var doors = geos.Where(o => o.Properties["Category"].ToString().ToUpper() == "DOOROPENING").Where(o => o.Boundary != null).Select(o => o.Boundary).ToList();

                //    var transformer = new ThMEPOriginTransformer(objs);
                //    geos.Where(o=>o.Boundary!=null).ForEach(o =>
                //    {
                //        transformer.Transform(o.Boundary);
                //    });
                //    datasetFactory.MoveToXYPlane(geos);

                //    //ThMEPEngineCore.CAD.ThAuxiliaryUtils.CreateGroup(rooms, acadDatabase.Database, 5);
                //    //ThMEPEngineCore.CAD.ThAuxiliaryUtils.CreateGroup(doors, acadDatabase.Database, 6);

                // select an option
                string strResident = "住宅";
                string strPublic = "公建";

                var options = new PromptKeywordOptions("");
                options.Message = "\nPlease select option:";
                options.Keywords.Add(strResident, "R", "住宅(R)");
                options.Keywords.Add(strPublic, "P", "公建(P)");

                var rst = Active.Editor.GetKeywords(options);
                if (rst.Status != PromptStatus.OK)
                    return;

                var buildingType = FireAlarm.Data.BuildingType.None;
                if (rst.StringResult.Equals(strResident))
                {
                    buildingType = FireAlarm.Data.BuildingType.Resident;
                }
                else if (rst.StringResult.Equals(strPublic))
                {
                    buildingType = FireAlarm.Data.BuildingType.Public;
                }
                else return;

                getData(out var transformer, out var geos);

                if (geos.Count == 0)
                {
                    return;
                }

                var layoutBlkName = new List<string>() { ThFixLayoutCommon.BlkName_Display_Fire, ThFixLayoutCommon.BlkName_Display_Floor };
                var avoidBlkName = ThFixLayoutCommon.BlkNameList.Where(x => layoutBlkName.Contains(x) == false).ToList();

                ThFixedPointLayoutService layoutService = null;

                layoutService = new ThDisplayDeviceFixedPointLayoutService(geos, layoutBlkName, avoidBlkName)
                {
                    BuildingType = buildingType,
                };

                var results = layoutService.Layout();

                // 对结果的重设
                var pairs = new List<KeyValuePair<Point3d, Vector3d>>();
                results.ForEach(p =>
                {
                    var pt = p.Key;
                    transformer.Reset(ref pt);
                    pairs.Add(new KeyValuePair<Point3d, Vector3d>(pt, p.Value));
                });

                //Print
                pairs.ForEach(p =>
                {
                    var circlePoint = new Circle(p.Key, Vector3d.ZAxis, 50.0);
                    var circleArea = new Circle(p.Key, Vector3d.ZAxis, 8500.0);
                    var line = new Line(p.Key, p.Key + p.Value.GetNormal().MultiplyBy(200));
                    var ents1 = new List<Entity>() { circlePoint, line };
                    var ents2 = new List<Entity>() { circleArea };
                    ThMEPEngineCore.CAD.ThAuxiliaryUtils.CreateGroup(ents1, acadDatabase.Database, 1);
                    ThMEPEngineCore.CAD.ThAuxiliaryUtils.CreateGroup(ents2, acadDatabase.Database, 3);
                });
                //pairs.ForEach(x => FireAlarm.Service.DrawUtils.ShowGeometry(x.Key, x.Value, "l0result", 1, 40, 200));
            }
        }


        [CommandMethod("TIANHUACAD", "ThFireProofMonitor", CommandFlags.Modal)]
        public void ThFireProofMonitorLayout()
        {
            //选择Geojson File,获取数据
            //测试布置逻辑

            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {

                getData(out var transformer, out var geos);
                if (geos.Count == 0)
                {
                    return;
                }

                var layoutBlkName = new List<string>() { ThFixLayoutCommon.BlkName_Monitor };
                var avoidBlkName = ThFixLayoutCommon.BlkNameList.Where(x => layoutBlkName.Contains(x) == false).ToList();

                ThFixedPointLayoutService layoutService = null;
                layoutService = new ThFireProofMonitorFixedPointLayoutService(geos, layoutBlkName, avoidBlkName);

                var results = layoutService.Layout();

                // 对结果的重设
                var pairs = new List<KeyValuePair<Point3d, Vector3d>>();
                results.ForEach(p =>
                {
                    var pt = p.Key;
                    transformer.Reset(ref pt);
                    pairs.Add(new KeyValuePair<Point3d, Vector3d>(pt, p.Value));
                });

                //Print
                pairs.ForEach(p =>
                {
                    var circlePoint = new Circle(p.Key, Vector3d.ZAxis, 50.0);
                    var circleArea = new Circle(p.Key, Vector3d.ZAxis, 8500.0);
                    var line = new Line(p.Key, p.Key + p.Value.GetNormal().MultiplyBy(200));
                    var ents1 = new List<Entity>() { circlePoint, line };
                    var ents2 = new List<Entity>() { circleArea };
                    ThMEPEngineCore.CAD.ThAuxiliaryUtils.CreateGroup(ents1, acadDatabase.Database, 1);
                    ThMEPEngineCore.CAD.ThAuxiliaryUtils.CreateGroup(ents2, acadDatabase.Database, 3);
                });
            }

        }

        [CommandMethod("TIANHUACAD", "ThFireTel", CommandFlags.Modal)]
        public void ThFireTelLayout()

        {
            //选择Geojson File,获取数据
            //测试布置逻辑

            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {

                getData(out var transformer, out var geos);
                if (geos.Count == 0)
                {
                    return;
                }

                var layoutBlkName = new List<string>() { ThFixLayoutCommon.BlkName_FireTel };
                var avoidBlkName = ThFixLayoutCommon.BlkNameList.Where(x => layoutBlkName.Contains(x) == false).ToList();

                ThFixedPointLayoutService layoutService = null;
                layoutService = new ThFireTelFixedPointLayoutService(geos, layoutBlkName, avoidBlkName);

                var results = layoutService.Layout();

                // 对结果的重设
                var pairs = new List<KeyValuePair<Point3d, Vector3d>>();
                results.ForEach(p =>
                {
                    var pt = p.Key;
                    transformer.Reset(ref pt);
                    pairs.Add(new KeyValuePair<Point3d, Vector3d>(pt, p.Value));
                });

                //Print
                pairs.ForEach(p =>
                {
                    var circlePoint = new Circle(p.Key, Vector3d.ZAxis, 50.0);
                    var circleArea = new Circle(p.Key, Vector3d.ZAxis, 8500.0);
                    var line = new Line(p.Key, p.Key + p.Value.GetNormal().MultiplyBy(200));
                    var ents1 = new List<Entity>() { circlePoint, line };
                    var ents2 = new List<Entity>() { circleArea };
                    ThMEPEngineCore.CAD.ThAuxiliaryUtils.CreateGroup(ents1, acadDatabase.Database, 1);
                    ThMEPEngineCore.CAD.ThAuxiliaryUtils.CreateGroup(ents2, acadDatabase.Database, 3);
                });
                //pairs.ForEach(x => FireAlarm.Service.DrawUtils.ShowGeometry(x.Key, x.Value, "l0result", 1, 40, 200));
            }

        }


        private static void getData(out ThMEPOriginTransformer transformer, out List<ThGeometry> geos)
        {
            geos = new List<ThGeometry>();
            transformer = null;
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {

                var frame = ThMEPEngineCore.CAD.ThWindowInteraction.GetPolyline(
                    PointCollector.Shape.Window, new List<string> { "请框选一个范围" });
                if (frame.Area < 1e-4)
                {
                    return;
                }
                var pts = frame.Vertices();
                var datasetFactory = new ThFireAlarmDataSetFactory();
                var dataset = datasetFactory.Create(acadDatabase.Database, pts);
                //var psr = Active.Editor.GetFileNameForOpen("\n选择要打开的Geojson文件");
                //if(psr.Status!=PromptStatus.OK)
                //{
                //{
                //    return;
                //}
                //var geos = ThGeometryJsonReader.ReadFromFile(psr.StringResult);                
                geos = dataset.Container;
                //var geosTemp = dataset.Container;
                //var geosJsonString = ThGeoOutput.Output(geosTemp);
                //var geos = ThGeometryJsonReader.ReadFromContent(geosJsonString);

                var objs = geos.Where(o => o.Boundary != null).Select(o => o.Boundary).ToCollection();
                var rooms = geos.Where(o => o.Properties["Category"].ToString().ToUpper() == "ROOM").Where(o => o.Boundary != null).Select(o => o.Boundary).ToList();
                var doors = geos.Where(o => o.Properties["Category"].ToString().ToUpper() == "DOOROPENING").Where(o => o.Boundary != null).Select(o => o.Boundary).ToList();

                transformer = new ThMEPOriginTransformer(objs);
                //geos.Where(o => o.Boundary != null).ForEach(o =>
                //{
                //    transformer.Transform(o.Boundary);
                //});
                foreach (var o in geos)
                {
                    if (o.Boundary != null)
                    {
                        transformer.Transform(o.Boundary);
                    }
                }

                datasetFactory.MoveToXYPlane(geos);

                ThMEPEngineCore.CAD.ThAuxiliaryUtils.CreateGroup(rooms, acadDatabase.Database, 5);
                ThMEPEngineCore.CAD.ThAuxiliaryUtils.CreateGroup(doors, acadDatabase.Database, 6);
            }
        }
    }
}
