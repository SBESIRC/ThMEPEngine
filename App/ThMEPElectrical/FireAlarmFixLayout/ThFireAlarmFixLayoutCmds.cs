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

using ThCADExtension;
using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore.Model;
using Autodesk.AutoCAD.EditorInput;
using ThMEPElectrical.Command;

using ThMEPElectrical.FireAlarmFixLayout.Data;
using ThMEPElectrical.FireAlarmFixLayout.Logic;
using ThMEPElectrical.FireAlarmFixLayout;
using ThMEPElectrical.FireAlarm.Service;
using ThMEPElectrical.FireAlarm;

namespace ThMEPElectrical.FireAlarmFixLayout
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
                var _scale = 100;

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

                var buildingType = FireAlarmFixLayout.Data.BuildingType.None;
                if (rst.StringResult.Equals(strResident))
                {
                    buildingType = FireAlarmFixLayout.Data.BuildingType.Resident;
                }
                else if (rst.StringResult.Equals(strPublic))
                {
                    buildingType = FireAlarmFixLayout.Data.BuildingType.Public;
                }
                else return;


                var extractBlkList = ThFaCommon.BlkNameListFixLayout;
                var cleanBlkName = new List<string>() { ThFaCommon.BlkName_Display_Fire, ThFaCommon.BlkName_Display_Floor };
                var avoidBlkName = ThFaCommon.BlkNameListFixLayout.Where(x => cleanBlkName.Contains(x) == false).ToList();
                var layoutBlkName = ThFaCommon.BlkName_Display_Fire;//to do： from UI

                //画框，提数据，转数据
                var pts = ThFireAlarmUtils.getFrame();
                if (pts.Count == 0)
                {
                    return;
                }
                var geos = ThFireAlarmUtils.getFixLayoutData(pts, extractBlkList);
                if (geos.Count == 0)
                {
                    return;
                }
                var transformer = ThFireAlarmUtils.transformToOrig(geos);

                //
                ThFixedPointLayoutService layoutService = null;
                layoutService = new ThDisplayDeviceFixedPointLayoutService(geos, cleanBlkName, avoidBlkName)
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

                //插入真实块
                ThFireAlarmInsertBlk.InsertBlock(pairs,_scale, layoutBlkName, ThFaCommon.blk_layer[layoutBlkName]);

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
            var _scale = 100;
            //选择Geojson File,获取数据
            //测试布置逻辑

            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var extractBlkList = ThFaCommon.BlkNameListFixLayout;
                var cleanBlkName = new List<string>() { ThFaCommon.BlkName_Monitor };
                var avoidBlkName = ThFaCommon.BlkNameListFixLayout.Where(x => cleanBlkName.Contains(x) == false).ToList();
                var layoutBlkName = ThFaCommon.BlkName_Monitor;//to do： from UI

                //画框，提数据，转数据
                var pts = ThFireAlarmUtils.getFrame();
                if (pts.Count == 0)
                {
                    return;
                }
                var geos = ThFireAlarmUtils.getFixLayoutData(pts, extractBlkList);
                if (geos.Count == 0)
                {
                    return;
                }
                var transformer = ThFireAlarmUtils.transformToOrig(geos);

                ThFixedPointLayoutService layoutService = null;
                layoutService = new ThFireProofMonitorFixedPointLayoutService(geos, cleanBlkName, avoidBlkName);

                var results = layoutService.Layout();

                // 对结果的重设
                var pairs = new List<KeyValuePair<Point3d, Vector3d>>();
                results.ForEach(p =>
                {
                    var pt = p.Key;
                    transformer.Reset(ref pt);
                    pairs.Add(new KeyValuePair<Point3d, Vector3d>(pt, p.Value));
                });

                ThFireAlarmInsertBlk.InsertBlock(pairs, _scale, layoutBlkName, ThFaCommon.blk_layer[layoutBlkName]);

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
            var _scale = 100;
            //选择Geojson File,获取数据
            //测试布置逻辑

            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var extractBlkList = ThFaCommon.BlkNameListFixLayout;
                var cleanBlkName = new List<string>() { ThFaCommon.BlkName_FireTel };
                var avoidBlkName = ThFaCommon.BlkNameListFixLayout.Where(x => cleanBlkName.Contains(x) == false).ToList();
                var layoutBlkName = ThFaCommon.BlkName_FireTel;


                //画框，提数据，转数据
                var pts = ThFireAlarmUtils.getFrame();
                if (pts.Count == 0)
                {
                    return;
                }
                var geos = ThFireAlarmUtils.getFixLayoutData(pts, extractBlkList);
                if (geos.Count == 0)
                {
                    return;
                }
                var transformer = ThFireAlarmUtils.transformToOrig(geos);

                ThFixedPointLayoutService layoutService = null;
                layoutService = new ThFireTelFixedPointLayoutService(geos, cleanBlkName, avoidBlkName);

                var results = layoutService.Layout();

                // 对结果的重设
                var pairs = new List<KeyValuePair<Point3d, Vector3d>>();
                results.ForEach(p =>
                {
                    var pt = p.Key;
                    transformer.Reset(ref pt);
                    pairs.Add(new KeyValuePair<Point3d, Vector3d>(pt, p.Value));
                });

                ThFireAlarmInsertBlk.InsertBlock(pairs, _scale, layoutBlkName, ThFaCommon.blk_layer[layoutBlkName]);


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

        //private static List<ThGeometry> getData(Point3dCollection pts, List<string> extractBlkList)
        //{
        //    var geos = new List<ThGeometry>();

        //    using (AcadDatabase acadDatabase = AcadDatabase.Active())
        //    {
        //        var datasetFactory = new ThFaFixLayoutDataSetFactory();
        //        var dataset = datasetFactory.Create(acadDatabase.Database, pts);
        //        geos.AddRange(dataset.Container);
        //        var businessDataFactory = new ThFaFixLayoutBusinessDataSetFactory()
        //        {
        //            BlkNameList = extractBlkList,
        //        };
        //        var businessDataSet = businessDataFactory.Create(acadDatabase.Database, pts);
        //        geos.AddRange(businessDataSet.Container);

        //        return geos;
        //    }
        //}
    }
}
