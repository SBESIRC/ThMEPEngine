using System;
using AcHelper;
using Linq2Acad;
using System.IO;
using System.Linq;
using ThCADCore.NTS;
using ThMEPEngineCore.IO;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Diagnostics;
using ThMEPElectrical.AFAS;
using ThMEPElectrical.AFAS.Data;
using ThMEPElectrical.AFAS.Model;
using ThMEPElectrical.AFAS.Utils;
using ThMEPElectrical.AFAS.ViewModel;
using ThMEPElectrical.FireAlarmArea;
using ThMEPElectrical.FireAlarmArea.Data;
using ThMEPElectrical.FireAlarmArea.Command;
using ThMEPElectrical.FireAlarmArea.Service;
using ThMEPElectrical.FireAlarmFixLayout.Data;
using ThMEPElectrical.FireAlarmFixLayout.Command;
using ThCADExtension;
using ThMEPEngineCore.Algorithm;

#if (ACAD2016 || ACAD2018)
using CLI;
using ThMEPElectrical.AFAS.Command;
using ThMEPElectrical.FireAlarmDistance.Data;
using ThMEPElectrical.FireAlarmDistance.Command;
#endif

namespace ThMEPElectrical
{
    public class THAFASDebugCmds
    {

        //        [System.Diagnostics.Conditional("DEBUG")]
        //        [CommandMethod("TIANHUACAD", "THFaDistData", CommandFlags.Modal)]
        //        public void THFaDistData()
        //        {
        //#if (ACAD2016 || ACAD2018)
        //            using (AcadDatabase acadDatabase = AcadDatabase.Active())
        //            {
        //                var referBeam = ThAFASUtils.SettingBoolean("\n不考虑梁（0）考虑梁（1）");
        //                var needConverage = referBeam == true ? true : false;

        //                var scale = 100;
        //                var extractBlkList = ThFaCommon.BlkNameList;
        //                var cleanBlkName = new List<string>() { ThFaCommon.BlkName_Broadcast_Ceiling, ThFaCommon.BlkName_Broadcast_Wall };
        //                var avoidBlkName = ThFaCommon.BlkNameList.Where(x => cleanBlkName.Contains(x) == false).ToList();

        //                //画框，提数据，转数据
        //                var framePts = ThAFASUtils.GetFrame();
        //                if (framePts.Count == 0)
        //                {
        //                    return;
        //                }

        //                var geos = ThAFASUtils.GetDistLayoutData(framePts, extractBlkList, referBeam, needConverage);

        //                var data = new ThAFASDistanceDataQueryService(geos, avoidBlkName);
        //                //data.PrepareData();
        //                //data.CleanPreviousEquipment();
        //                //data.ExtendEquipment(cleanBlkName, scale);
        //                //data.FilterBeam();
        //                //data.ProcessRoomPlacementLabel(ThFaDistCommon.BroadcastTag);

        //                data.Print();

        //                var room = data.Room;
        //                //for (int i = 0; i < room.Count; i++)
        //                //{
        //                //    var pl = room[i].Boundary as Polyline;
        //                //    var pt = pl.GetCentroidPoint();
        //                //    DrawUtils.ShowGeometry(pt, String.Format("placement：{0}", room[i].Properties["Placement"]), "l0RoomPlacement", 3, 25, 200);
        //                //    DrawUtils.ShowGeometry(new Point3d(pt.X, pt.Y - 300 * 1, 0), String.Format("name：{0}", room[i].Properties["Name"]), "l0RoomName", 3, 25, 200);
        //                //    DrawUtils.ShowGeometry(new Point3d(pt.X, pt.Y - 300 * 2, 0), String.Format("Privacy：{0}", room[i].Properties["Privacy"]), "l0RoomPrivacy", 3, 25, 200);
        //                //}

        //                var fileInfo = new FileInfo(Active.Document.Name);
        //                var path = fileInfo.Directory.FullName;
        //                ThGeoOutput.Output(geos, path, fileInfo.Name);
        //            }
        //#endif
        //        }

        [System.Diagnostics.Conditional("DEBUG")]
        [CommandMethod("TIANHUACAD", "THFaAreaData", CommandFlags.Modal)]
        public void ThFaAreaData()
        {
            var referBeam = ThAFASUtils.SettingBoolean("\n不考虑梁（0）考虑梁（1）");
            var wallThick = 0.0;
            var needDetective = true;
            if (referBeam == false)
            {
                needDetective = false;
            }
            else
            {
                wallThick = ThAFASUtils.SettingDouble("\n板厚");
                needDetective = ThAFASUtils.SettingBoolean("\n探测区域：不考虑（0）考虑（1）");
            }

            var theta = 0;
            var floorHight = 2;
            var layoutType = ThFaSmokeCommon.layoutType.smoke;

            var extractBlkList = ThFaCommon.BlkNameList;
            //var cleanBlkName = new List<string>() { ThFaCommon.BlkName_Smoke, ThFaCommon.BlkName_Heat };
            var cleanBlkName = new List<string>();
            var avoidBlkName = ThFaCommon.BlkNameList.Where(x => cleanBlkName.Contains(x) == false).ToList();

            //画框，提数据，转数据
            //var pts = ThAFASSelectFrameUtil.GetFrame();
            var pts = ThAFASSelectFrameUtil.GetRoomFrame();

            if (pts.Count == 0)
            {
                return;
            }

            var geos = ThAFASUtils.GetAreaLayoutData(pts, extractBlkList, referBeam, wallThick, needDetective);

            var data = new ThAFASAreaDataQueryService(geos, avoidBlkName);
            data.Print();
            //data.AnalysisHoles();
            //data.ClassifyData();
            data.AddMRoomDict();
            data.ClassifyDataNew();//先分房间再扩大
            var roomType = ThFaSmokeRoomTypeService.GetSmokeSensorType(data.Rooms, data.RoomFrameDict);

            foreach (var frame in data.FrameList)
            {
                var radius = ThFaAreaLayoutParamterCalculationService.CalculateRadius(frame.Area, floorHight, theta, layoutType);//to do...frame.area need to remove hole's area
                var beamGridWidth = ThFaAreaLayoutService.LayoutAreaWidth(data.FrameLayoutList[frame], radius);
                var bIsAisleArea = ThFaAreaLayoutService.IsAisleArea(frame, data.FrameHoleList[frame], beamGridWidth, 0.75);

                var type = bIsAisleArea == true ? "centerline" : "grid";
                var centPt = frame.GetCentroidPoint();
                DrawUtils.ShowGeometry(data.FrameHoleList[frame], string.Format("l0analysisHole"), 190);
                DrawUtils.ShowGeometry(new Point3d(centPt.X, centPt.Y - 350 * 0, 0), string.Format("r:{0} aisle type:{1}", radius, type), "l4lastInfo", 3, 25, 200);
            }


            var fileInfo = new FileInfo(Active.Document.Name);
            var path = fileInfo.Directory.FullName;
            ThGeoOutput.Output(geos, path, fileInfo.Name);
        }





        //[System.Diagnostics.Conditional("DEBUG")]
        //[CommandMethod("TIANHUACAD", "THFAFixData", CommandFlags.Modal)]
        //public void THFAFixData()
        //{
        //    var extractBlkList = ThFaCommon.BlkNameList;

        //    //把Cad图纸数据写出到Geojson File中
        //    using (AcadDatabase acadDatabase = AcadDatabase.Active())
        //    {
        //        var pts = ThAFASUtils.GetFrame();
        //        if (pts.Count == 0)
        //        {
        //            return;
        //        }

        //        var geos = ThAFASUtils.GetFixLayoutData(pts, extractBlkList);

        //        var fileInfo = new FileInfo(Active.Document.Name);
        //        var path = fileInfo.Directory.FullName;
        //        ThGeoOutput.Output(geos, path, fileInfo.Name);
        //    }
        //}



        //////////////////////////////////////////////////////
        [System.Diagnostics.Conditional("DEBUG")]
        [CommandMethod("TIANHUACAD", "THFaDistData2", CommandFlags.Modal)]
        public void THFaDistData2()
        {
#if (ACAD2016 || ACAD2018)
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var referBeam = ThAFASUtils.SettingBoolean("\n不考虑梁（0）考虑梁（1）");
                var needConverage = referBeam == true ? true : false;

                //var scale = 100;
                var extractBlkList = ThFaCommon.BlkNameList;
                var cleanBlkName = new List<string>();
                var avoidBlkName = ThFaCommon.BlkNameList.Where(x => cleanBlkName.Contains(x) == false).ToList();

                //画框，提数据，转数据
                var selectPts = ThAFASSelectFrameUtil.GetFrame();
                if (selectPts.Count == 0)
                {
                    return;
                }

                //var transformer = ThAFASUtils.GetTransformer(selectPts);
                var transformer = new ThMEPEngineCore.Algorithm.ThMEPOriginTransformer(new Point3d(0, 0, 0));
                var extractors = ThAFASUtils.GetBasicArchitectureData(selectPts, transformer);
                ThAFASDataPass.Instance = new ThAFASDataPass();
                ThAFASDataPass.Instance.Extractors = extractors;
                ThAFASDataPass.Instance.Transformer = transformer;
                ThAFASDataPass.Instance.SelectPts = selectPts;

                var geos = ThAFASUtils.GetDistLayoutData2(ThAFASDataPass.Instance, extractBlkList, referBeam, needConverage);

                var data = new ThAFASDistanceDataQueryService(geos, avoidBlkName);
                data.Print();

                var room = data.Rooms;
                //for (int i = 0; i < room.Count; i++)
                //{
                //    var pl = room[i].Boundary as Polyline;
                //    var pt = pl.GetCentroidPoint();
                //    DrawUtils.ShowGeometry(pt, String.Format("placement：{0}", room[i].Properties["Placement"]), "l0RoomPlacement", 3, 25, 200);
                //    DrawUtils.ShowGeometry(new Point3d(pt.X, pt.Y - 300 * 1, 0), String.Format("name：{0}", room[i].Properties["Name"]), "l0RoomName", 3, 25, 200);
                //    DrawUtils.ShowGeometry(new Point3d(pt.X, pt.Y - 300 * 2, 0), String.Format("Privacy：{0}", room[i].Properties["Privacy"]), "l0RoomPrivacy", 3, 25, 200);
                //}

                var fileInfo = new FileInfo(Active.Document.Name);
                var path = fileInfo.Directory.FullName;
                ThGeoOutput.Output(geos, path, fileInfo.Name + "New");
                ThAFASDataPass.Instance = null;
            }
#endif
        }


        [System.Diagnostics.Conditional("DEBUG")]
        [CommandMethod("TIANHUACAD", "THFAFixData2", CommandFlags.Modal)]
        public void THFAFixData2()
        {
            var extractBlkList = ThFaCommon.BlkNameList;
            var cleanBlkList = new List<string>();
            var avoidBlkList = ThFaCommon.BlkNameList.Where(x => cleanBlkList.Contains(x) == false).ToList();

            //把Cad图纸数据写出到Geojson File中
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var selectPts = ThAFASSelectFrameUtil.GetFrame();
                if (selectPts.Count == 0)
                {
                    return;
                }

                //var transformer = ThAFASUtils.GetTransformer(selectPts);
                var transformer = new ThMEPEngineCore.Algorithm.ThMEPOriginTransformer(new Point3d(0, 0, 0));
                var extractors = ThAFASUtils.GetBasicArchitectureData(selectPts, transformer);
                ThAFASDataPass.Instance = new ThAFASDataPass();
                ThAFASDataPass.Instance.Extractors = extractors;
                ThAFASDataPass.Instance.Transformer = transformer;
                ThAFASDataPass.Instance.SelectPts = selectPts;

                var geos = ThAFASUtils.GetFixLayoutData2(ThAFASDataPass.Instance, extractBlkList);

                var data = new ThAFASFixDataQueryService(geos, avoidBlkList);
                data.Print();


                var fileInfo = new FileInfo(Active.Document.Name);
                var path = fileInfo.Directory.FullName;
                ThGeoOutput.Output(geos, path, fileInfo.Name + "New");
                ThAFASDataPass.Instance = null;
            }
        }

        [System.Diagnostics.Conditional("DEBUG")]
        [CommandMethod("TIANHUACAD", "THFaAreaData2", CommandFlags.Modal)]
        public void ThFaAreaData2()
        {
            var referBeam = ThAFASUtils.SettingBoolean("\n不考虑梁（0）考虑梁（1）");
            var wallThick = 0.0;
            var needDetective = true;
            if (referBeam == false)
            {
                needDetective = false;
            }
            else
            {
                wallThick = ThAFASUtils.SettingDouble("\n板厚");
                needDetective = ThAFASUtils.SettingBoolean("\n探测区域：不考虑（0）考虑（1）");
            }

            var theta = 0;
            var floorHight = 2;
            var layoutType = ThFaSmokeCommon.layoutType.smoke;

            var extractBlkList = ThFaCommon.BlkNameList;
            var cleanBlkName = new List<string>();
            var avoidBlkName = ThFaCommon.BlkNameList.Where(x => cleanBlkName.Contains(x) == false).ToList();

            //画框，提数据，转数据
            //var selectPts = ThAFASSelectFrameUtil.GetRoomFrame();
            //var selectPts = ThAFASSelectFrameUtil.GetFrameBlk();
            var selectPts = ThAFASSelectFrameUtil.GetRoomFrame();

            if (selectPts.Count == 0)
            {
                return;
            }

            //var transformer = ThAFASUtils.GetTransformer(selectPts);
            var transformer = new ThMEPEngineCore.Algorithm.ThMEPOriginTransformer(new Point3d(0, 0, 0));
            var extractors = ThAFASUtils.GetBasicArchitectureData(selectPts, transformer);

            ThAFASDataPass.Instance = new ThAFASDataPass();
            ThAFASDataPass.Instance.Extractors = extractors;
            ThAFASDataPass.Instance.Transformer = transformer;
            ThAFASDataPass.Instance.SelectPts = selectPts;

            var geos = ThAFASUtils.GetAreaLayoutData2(ThAFASDataPass.Instance, extractBlkList, referBeam, wallThick, needDetective);

            var data = new ThAFASAreaDataQueryService(geos, avoidBlkName);
            data.Print();
            //data.AnalysisHoles();
            //data.ClassifyData();
            data.AddMRoomDict();
            data.ClassifyDataNew();
            var roomType = FireAlarmArea.Service.ThFaSmokeRoomTypeService.GetSmokeSensorType(data.Rooms, data.RoomFrameDict);
            foreach (var frame in data.FrameList)
            {
                var radius = ThFaAreaLayoutParamterCalculationService.CalculateRadius(frame.Area, floorHight, theta, layoutType);//to do...frame.area need to remove hole's area
                var beamGridWidth = ThFaAreaLayoutService.LayoutAreaWidth(data.FrameLayoutList[frame], radius);
                var bIsAisleArea = ThFaAreaLayoutService.IsAisleArea(frame, data.FrameHoleList[frame], beamGridWidth, 0.75);

                var type = bIsAisleArea == true ? "centerline" : "grid";
                var centPt = frame.GetCentroidPoint();

                DrawUtils.ShowGeometry(frame, string.Format("l0roomFrame"), 30);
                DrawUtils.ShowGeometry(data.FrameHoleList[frame], string.Format("l0FrameHole"), 150);
                DrawUtils.ShowGeometry(data.FrameColumnList[frame], string.Format("l0FrameColumn"), 1);
                DrawUtils.ShowGeometry(data.FrameWallList[frame], string.Format("l0FrameWall"), 1);
                data.FrameLayoutList[frame].ForEach(x => DrawUtils.ShowGeometry(x, string.Format("l0Framelayout"), 6));
                DrawUtils.ShowGeometry(data.FrameDetectAreaList[frame], string.Format("l0FrameDetec"), 91);
                DrawUtils.ShowGeometry(data.FramePriorityList [frame], string.Format("l0FrameEquipment"), 152);

                DrawUtils.ShowGeometry(new Point3d(centPt.X, centPt.Y - 350 * 0, 0), string.Format("r:{0} aisle type:{1}", radius, type), "l0lastInfo", 3, 25, 200);


            }

            var fileInfo = new FileInfo(Active.Document.Name);
            var path = fileInfo.Directory.FullName;
            ThGeoOutput.Output(geos, path, fileInfo.Name + "New");
            ThAFASDataPass.Instance = null;
        }

        [System.Diagnostics.Conditional("DEBUG")]
        [CommandMethod("TIANHUACAD", "THFAAllData", CommandFlags.Modal)]
        public void THFAAllData()
        {
            var referBeam = ThAFASUtils.SettingBoolean("\n不考虑梁（0）考虑梁（1）");
            var wallThick = 0.0;
            var needDetective = referBeam;
            if (referBeam == false)
            {
                needDetective = false;
            }
            else
            {
                wallThick = ThAFASUtils.SettingDouble("\n板厚");
            }

            var extractBlkList = ThFaCommon.BlkNameList;
            var cleanBlkList = new List<string>();
            var avoidBlkList = ThFaCommon.BlkNameList.Where(x => cleanBlkList.Contains(x) == false).ToList();

            //把Cad图纸数据写出到Geojson File中
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var selectPts = ThAFASSelectFrameUtil.GetFrame();
                if (selectPts.Count == 0)
                {
                    return;
                }

                //var transformer = ThAFASUtils.GetTransformer(selectPts);
                var transformer = new ThMEPEngineCore.Algorithm.ThMEPOriginTransformer(new Point3d(0, 0, 0));
                var extractors = ThAFASUtils.GetBasicArchitectureData(selectPts, transformer);

                ThAFASDataPass.Instance = new ThAFASDataPass();
                ThAFASDataPass.Instance.Extractors = extractors;
                ThAFASDataPass.Instance.Transformer = transformer;
                ThAFASDataPass.Instance.SelectPts = selectPts;

                var geos = ThAFASUtils.GetAllData(ThAFASDataPass.Instance, extractBlkList, referBeam, wallThick);

                var data = new ThAFASDataQueryService(geos);
                data.Print();


                var fileInfo = new FileInfo(Active.Document.Name);
                var path = fileInfo.Directory.FullName;
                ThGeoOutput.Output(geos, path, fileInfo.Name + "New");
                ThAFASDataPass.Instance = null;
            }
        }

    }
}
