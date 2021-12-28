using System;
using AcHelper;
using Linq2Acad;
using System.IO;
using System.Linq;
using ThCADCore.NTS;
using ThMEPEngineCore.IO;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;

using ThMEPElectrical.AFAS;
using ThMEPElectrical.AFAS.Model;
using ThMEPElectrical.AFAS.Command;
using ThMEPElectrical.AFAS.ViewModel;
using ThMEPElectrical.AFAS.Utils;
using ThMEPElectrical.AFAS.Data;

using ThMEPElectrical.FireAlarmArea.Command;
using ThMEPElectrical.FireAlarmArea;
using ThMEPElectrical.FireAlarmArea.Data;
using ThMEPElectrical.FireAlarmArea.Service;
using ThMEPElectrical.FireAlarmDistance;
using ThMEPElectrical.FireAlarmDistance.Data;
using ThMEPElectrical.FireAlarmFixLayout.Logic;
using ThMEPElectrical.FireAlarmFixLayout.Command;

using CLI;

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

//        [System.Diagnostics.Conditional("DEBUG")]
//        [CommandMethod("TIANHUACAD", "THFaAreaData", CommandFlags.Modal)]
//        public void ThFaAreaData()
//        {
//            var referBeam = ThAFASUtils.SettingBoolean("\n不考虑梁（0）考虑梁（1）");
//            var wallThick = 0.0;
//            var needDetective = true;
//            if (referBeam == false)
//            {
//                needDetective = false;
//            }
//            else
//            {
//                wallThick = ThAFASUtils.SettingDouble("\n板厚");
//                needDetective = ThAFASUtils.SettingBoolean("\n探测区域：不考虑（0）考虑（1）");
//            }

//            var theta = 0;
//            var floorHight = 2;
//            var layoutType = ThFaSmokeCommon.layoutType.smoke;

//            var extractBlkList = ThFaCommon.BlkNameList;
//            //var cleanBlkName = new List<string>() { ThFaCommon.BlkName_Smoke, ThFaCommon.BlkName_Heat };
//            var cleanBlkName = new List<string>();
//            var avoidBlkName = ThFaCommon.BlkNameList.Where(x => cleanBlkName.Contains(x) == false).ToList();

//            //画框，提数据，转数据
//            var pts = ThAFASUtils.GetFrame();
//            //var pts = ThAFASUtils.GetRoomFrame();

//            if (pts.Count == 0)
//            {
//                return;
//            }

//            var geos = ThAFASUtils.GetAreaLayoutData(pts, extractBlkList, referBeam, wallThick, needDetective);

//            var data = new ThAFASAreaDataQueryService(geos, avoidBlkName);
//            data.Print();

//            ////洞,必须先做找到框线
//            //dataQuery.AnalysisHoles();
//            //dataQuery.ClassifyData();
//            //var roomType = ThFaSmokeRoomTypeService.GetSmokeSensorType(dataQuery.Rooms, dataQuery.RoomFrameDict);

//            //foreach (var frame in dataQuery.FrameList)
//            //{
//            //    var radius = ThFaAreaLayoutParamterCalculationService.CalculateRadius(frame.Area, floorHight, theta, layoutType);//to do...frame.area need to remove hole's area
//            //    var beamGridWidth = ThFaAreaLayoutService.LayoutAreaWidth(dataQuery.FrameLayoutList[frame], radius);
//            //    var bIsAisleArea = ThFaAreaLayoutService.IsAisleArea(frame, dataQuery.FrameHoleList[frame], radius * 0.8, 0.025);

//            //    var type = bIsAisleArea == true ? "centerline" : "grid";
//            //    var centPt = frame.GetCentroidPoint();
//            //    DrawUtils.ShowGeometry(dataQuery.FrameHoleList[frame], string.Format("l0analysisHole"), 190);
//            //    DrawUtils.ShowGeometry(new Point3d(centPt.X, centPt.Y - 350 * 0, 0), string.Format("r:{0} aisle type:{1}", radius, type), "l4lastInfo", 3, 25, 200);
//            //}


//            var fileInfo = new FileInfo(Active.Document.Name);
//            var path = fileInfo.Directory.FullName;
//            ThGeoOutput.Output(geos, path, fileInfo.Name);
//        }

//        [System.Diagnostics.Conditional("DEBUG")]
//        [CommandMethod("TIANHUACAD", "THFAFixData", CommandFlags.Modal)]
//        public void THFAFixData()
//        {
//            var extractBlkList = ThFaCommon.BlkNameList;

//            //把Cad图纸数据写出到Geojson File中
//            using (AcadDatabase acadDatabase = AcadDatabase.Active())
//            {
//                var pts = ThAFASUtils.GetFrame();
//                if (pts.Count == 0)
//                {
//                    return;
//                }

//                var geos = ThAFASUtils.GetFixLayoutData(pts, extractBlkList);

//                var fileInfo = new FileInfo(Active.Document.Name);
//                var path = fileInfo.Directory.FullName;
//                ThGeoOutput.Output(geos, path, fileInfo.Name);
//            }
//        }



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

                var scale = 100;
                var extractBlkList = ThFaCommon.BlkNameList;
                var cleanBlkName = new List<string>();
                var avoidBlkName = ThFaCommon.BlkNameList.Where(x => cleanBlkName.Contains(x) == false).ToList();

                //画框，提数据，转数据
                var selectPts = ThAFASUtils.GetFrame();
                if (selectPts.Count == 0)
                {
                    return;
                }

                var transformer = ThAFASUtils.GetTransformer(selectPts);
                var extractors = ThAFASUtils.GetBasicArchitectureData(selectPts, transformer);
                ThAFASDataPass.Instance.Extractors = extractors;
                ThAFASDataPass.Instance.Transformer = transformer;
                ThAFASDataPass.Instance.SelectPts = selectPts;

                var geos = ThAFASUtils.GetDistLayoutData2(ThAFASDataPass.Instance, extractBlkList, referBeam, needConverage);

                var data = new ThAFASDistanceDataQueryService(geos, avoidBlkName);
                data.Print();

                var room = data.Room;
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
                var selectPts = ThAFASUtils.GetFrame();
                if (selectPts.Count == 0)
                {
                    return;
                }

                var transformer = ThAFASUtils.GetTransformer(selectPts);
                var extractors = ThAFASUtils.GetBasicArchitectureData(selectPts, transformer);
                ThAFASDataPass.Instance.Extractors = extractors;
                ThAFASDataPass.Instance.Transformer = transformer;
                ThAFASDataPass.Instance.SelectPts = selectPts;

                var geos = ThAFASUtils.GetFixLayoutData2(ThAFASDataPass.Instance, extractBlkList);

                var data = new ThDataQueryService(geos, avoidBlkList);
                data.Print();


                var fileInfo = new FileInfo(Active.Document.Name);
                var path = fileInfo.Directory.FullName;
                ThGeoOutput.Output(geos, path, fileInfo.Name + "New");
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
            var selectPts = ThAFASUtils.GetFrame();
            //var pts = ThAFASUtils.GetRoomFrame();

            if (selectPts.Count == 0)
            { 
                return;
            }

            var transformer = ThAFASUtils.GetTransformer(selectPts);
            var extractors = ThAFASUtils.GetBasicArchitectureData(selectPts, transformer);
            ThAFASDataPass.Instance.Extractors = extractors;
            ThAFASDataPass.Instance.Transformer = transformer;
            ThAFASDataPass.Instance.SelectPts = selectPts;

            var geos = ThAFASUtils.GetAreaLayoutData2(ThAFASDataPass.Instance, extractBlkList, referBeam, wallThick, needDetective);

            var data = new ThAFASAreaDataQueryService(geos, avoidBlkName);
            data.Print();

            ////洞,必须先做找到框线
            //data.AnalysisHoles();
            //data.ClassifyData();
            //var roomType = ThFaSmokeRoomTypeService.GetSmokeSensorType(data.Rooms, data.RoomFrameDict);

            //foreach (var frame in data.FrameList)
            //{
            //    var radius = ThFaAreaLayoutParamterCalculationService.CalculateRadius(frame.Area, floorHight, theta, layoutType);//to do...frame.area need to remove hole's area
            //    var beamGridWidth = ThFaAreaLayoutService.LayoutAreaWidth(data.FrameLayoutList[frame], radius);
            //    var bIsAisleArea = ThFaAreaLayoutService.IsAisleArea(frame, data.FrameHoleList[frame], radius * 0.8, 0.025);

            //    var type = bIsAisleArea == true ? "centerline" : "grid";
            //    var centPt = frame.GetCentroidPoint();
            //    DrawUtils.ShowGeometry(data.FrameHoleList[frame], string.Format("l0analysisHole"), 190);
            //    DrawUtils.ShowGeometry(new Point3d(centPt.X, centPt.Y - 350 * 0, 0), string.Format("r:{0} aisle type:{1}", radius, type), "l4lastInfo", 3, 25, 200);
            //}

            var fileInfo = new FileInfo(Active.Document.Name);
            var path = fileInfo.Directory.FullName;
            ThGeoOutput.Output(geos, path, fileInfo.Name + "New");
        }

        //---------------无ui模式
        [CommandMethod("TIANHUACAD", "THHZBJNoUI", CommandFlags.Session)]
        public void THHZBJNoUI()
        {
            ///////手动输入设定
            var layoutInt = ThAFASUtils.SettingString("\n逗号拼接布置：烟温感（0）广播（1）楼层显示器（2）电话（3）可燃气体探测（4）手动报警按钮（5）防火门监控（6）");
            if (layoutInt == "")
            {
                return;
            }
            var layoutList = layoutInt.Split(',').Select(x => Convert.ToInt32(x)).ToList();

            var setBeam = false;
            var beam = 0;

            foreach (var layout in layoutList)
            {
                switch (layout)
                {
                    case 0:
                        if (setBeam == false)
                        {
                            beam = ThAFASUtils.SettingInt("\n不考虑梁（0）考虑梁（1）");
                            setBeam = true;
                        }

                        var wallThick = ThAFASUtils.SettingDouble("\n烟温感板厚");

                        var hintStringHight = new Dictionary<string, (string, string)>()
                        {{"0",("0","h<=12(0)")},
                        {"1",("1","6<=h<=12(1)")},
                        {"2",("2","h<=6(2)")},
                        {"3",("3","h<=8(3)")},
                        };
                        var RoofHightS = ThAFASUtils.SettingSelection("\n房间高度", hintStringHight, "2");

                        var hintStringGrade = new Dictionary<string, (string, string)>()
                        {{"0",("0","θ<=15°(0)")},
                        {"1",("1","15°<=θ<=30°(1)")},
                        {"2",("2","θ>30°(2)")},
                        };
                        var RoofGradeS = ThAFASUtils.SettingSelection("\n屋顶坡度", hintStringGrade, "0");

                        FireAlarmSetting.Instance.RoofGrade = Convert.ToInt32(RoofGradeS);
                        FireAlarmSetting.Instance.RoofHight = Convert.ToInt32(RoofHightS);
                        FireAlarmSetting.Instance.Beam = beam;
                        FireAlarmSetting.Instance.RoofThickness = wallThick;

                        break;
                    case 1:
                        var isWallPa = ThAFASUtils.SettingInt("\n广播：吊装（0）壁装（1）");
                        if ((ThAFASPlacementMountModeMgd)isWallPa == ThAFASPlacementMountModeMgd.Ceiling && setBeam == false)
                        {
                            beam = ThAFASUtils.SettingInt("\n不考虑梁（0）考虑梁（1）");
                            setBeam = true;
                        }
                        var stepDistanceP = ThAFASUtils.SettingDouble("\n广播步距：");

                        FireAlarmSetting.Instance.BroadcastLayout = isWallPa;
                        FireAlarmSetting.Instance.Beam = beam;
                        FireAlarmSetting.Instance.StepLengthBC = stepDistanceP;

                        break;
                    case 2:
                        var rst = ThAFASUtils.SettingInt("\n楼层显示器：住宅（0）公建（1）");
                        FireAlarmSetting.Instance.DisplayBuilding = rst;

                        break;
                    case 3:
                        break;
                    case 4:
                        if (setBeam == false)
                        {
                            beam = ThAFASUtils.SettingInt("\n不考虑梁（0）考虑梁（1）");
                            setBeam = true;
                        }
                        var radius = ThAFASUtils.SettingDouble("\n可燃气保护半径：");
                        FireAlarmSetting.Instance.Beam = beam;
                        FireAlarmSetting.Instance.GasProtectRadius = radius;

                        break;
                    case 5:
                        radius = ThAFASUtils.SettingDouble("\n手动报警步距：");
                        FireAlarmSetting.Instance.StepLengthMA = radius;

                        break;
                    case 6:
                        break;
                    default:
                        break;
                }
            }

            FireAlarmSetting.Instance.LayoutItemList.Clear();
            FireAlarmSetting.Instance.LayoutItemList.AddRange(layoutList);
            FireAlarmSetting.Instance.LayoutItemList = FireAlarmSetting.Instance.LayoutItemList.OrderBy(x => x).ToList();

            using (var cmd = new ThAFASCommand())
            {
                cmd.Execute();
            }

        }

        [CommandMethod("TIANHUACAD", "THFASmokeNoUI", CommandFlags.Session)]
        public void THFASmokeNoUI()
        {

            var beam = ThAFASUtils.SettingInt("\n不考虑梁（0）考虑梁（1）");
            var wallThick = ThAFASUtils.SettingDouble("\n烟温感板厚");
            var hintStringHight = new Dictionary<string, (string, string)>()
                        {{"0",("0","h<=12(0)")},
                        {"1",("1","6<=h<=12(1)")},
                        {"2",("2","h<=6(2)")},
                        {"3",("3","h<=8(3)")},
                        };
            var RoofHightS = ThAFASUtils.SettingSelection("\n房间高度", hintStringHight, "2");
            var hintStringGrade = new Dictionary<string, (string, string)>()
                        {{"0",("0","θ<=15°(0)")},
                        {"1",("1","15°<=θ<=30°(1)")},
                        {"2",("2","θ>30°(2)")},
                        };
            var RoofGradeS = ThAFASUtils.SettingSelection("\n屋顶坡度", hintStringGrade, "0");

            FireAlarmSetting.Instance.RoofGrade = Convert.ToInt32(RoofGradeS);
            FireAlarmSetting.Instance.RoofHight = Convert.ToInt32(RoofHightS);
            FireAlarmSetting.Instance.Beam = beam;
            FireAlarmSetting.Instance.RoofThickness = wallThick;

            GetData();

            using (var cmd = new ThAFASSmokeCmd())
            {
                cmd.Execute();
            }

            ThAFASDataPass.Instance = null;
        }



        [CommandMethod("TIANHUACAD", "THFADisplayNoUI", CommandFlags.Session)]
        public void THFADisplayNoUI()
        {
            var rst = ThAFASUtils.SettingInt("\n楼层显示器：住宅（0）公建（1）");
            FireAlarmSetting.Instance.DisplayBuilding = rst;

            GetData();

            using (var cmd = new ThAFASDisplayDeviceLayoutCmd())
            {
                cmd.Execute();
            }

            ThAFASDataPass.Instance = null;
        }

        [CommandMethod("TIANHUACAD", "THFAMonitorNoUI", CommandFlags.Session)]
        public void THFAMonitorNoUI()
        {
            GetData();

            using (var cmd = new ThAFASFireProofMonitorLayoutCmd())
            {
                cmd.Execute();
            }
            ThAFASDataPass.Instance = null;
        }

        [CommandMethod("TIANHUACAD", "THFATelNoUI", CommandFlags.Session)]
        public void THFATelNoUI()
        {
            GetData();

            using (var cmd = new ThAFASFireTelLayoutCmd())
            {
                cmd.Execute();
            }
            ThAFASDataPass.Instance = null;
        }

        [CommandMethod("TIANHUACAD", "THFAGasNoUI", CommandFlags.Session)]
        public void THFAGasNoUI()
        {
            var beam = ThAFASUtils.SettingInt("\n不考虑梁（0）考虑梁（1）");
            var radius = ThAFASUtils.SettingDouble("\n可燃气保护半径：");
            FireAlarmSetting.Instance.Beam = beam;
            FireAlarmSetting.Instance.GasProtectRadius = radius;

            GetData();

            using (var cmd = new ThAFASGasCmd())
            {
                cmd.Execute();
            }
            ThAFASDataPass.Instance = null;
        }

        [CommandMethod("TIANHUACAD", "THFABroadcastNoUI", CommandFlags.Session)]
        public void THFABroadcastNoUI()
        {
#if (ACAD2016 || ACAD2018)

            var isWallPa = ThAFASUtils.SettingInt("\n广播：吊装（0）壁装（1）");
            var beam = 0;
            if ((ThAFASPlacementMountModeMgd)isWallPa == ThAFASPlacementMountModeMgd.Ceiling)
            {
                beam = ThAFASUtils.SettingInt("\n不考虑梁（0）考虑梁（1）");
            }
            var stepDistanceP = ThAFASUtils.SettingDouble("\n广播步距：");

            FireAlarmSetting.Instance.BroadcastLayout = isWallPa;
            FireAlarmSetting.Instance.Beam = beam;
            FireAlarmSetting.Instance.StepLengthBC = stepDistanceP;

            GetData();

            using (var cmd = new ThAFASBroadcastCmd())
            {
                cmd.Execute();
            }
            ThAFASDataPass.Instance = null;
#else
            Active.Editor.WriteLine("此功能只支持CAD2016暨以上版本");
#endif
        }

        [CommandMethod("TIANHUACAD", "THFAManualAlarmNoUI", CommandFlags.Session)]
        public void THFAManualAlarmNoUI()
        {
#if (ACAD2016 || ACAD2018)
            var radius = ThAFASUtils.SettingDouble("\n手动报警步距：");
            FireAlarmSetting.Instance.StepLengthMA = radius;

            GetData();

            using (var cmd = new ThAFASManualAlarmCmd())
            {
                cmd.Execute();
            }
            ThAFASDataPass.Instance = null;
#else
            Active.Editor.WriteLine("此功能只支持CAD2016暨以上版本");
#endif
        }

        private void GetData()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                ThAFASDataPass.Instance = new ThAFASDataPass();

                var selectPts = ThAFASUtils.GetFrameBlk();
                if (selectPts.Count == 0)
                {
                    return;
                }

                var transformer = ThAFASUtils.GetTransformer(selectPts);

                ////////导入所有块，图层信息
                var extractBlkList = ThFaCommon.BlkNameList;
                ThFireAlarmInsertBlk.PrepareInsert(extractBlkList, ThFaCommon.Blk_Layer.Select(x => x.Value).Distinct().ToList());

                ////////清除所选的块
                var cleanBlkList = FireAlarmSetting.Instance.LayoutItemList.SelectMany(x => ThFaCommon.LayoutBlkList[x]).ToList();
                var previousEquipmentData = new ThAFASBusinessDataSetFactory()
                {
                    BlkNameList = cleanBlkList,
                    //  InputExtractors = extractors,
                };
                previousEquipmentData.SetTransformer(transformer);
                var localEquipmentData = previousEquipmentData.Create(acadDatabase.Database, selectPts);
                var cleanEquipment = localEquipmentData.Container;
                ThAFASUtils.CleanPreviousEquipment(cleanEquipment);

                ///////////获取数据元素,已转回原位置附近////////
                var extractors = ThAFASUtils.GetBasicArchitectureData(selectPts, transformer);
                ThAFASDataPass.Instance.Extractors = extractors;
                ThAFASDataPass.Instance.Transformer = transformer;
                ThAFASDataPass.Instance.SelectPts = selectPts;
            }
        }
    }
}
