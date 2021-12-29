using System;
using AcHelper;
using Linq2Acad;
using System.Linq;
using Autodesk.AutoCAD.Runtime;
using System.Collections.Generic;
using ThMEPElectrical.FireAlarmArea.Command;
using ThMEPElectrical.FireAlarmFixLayout.Command;
using ThMEPElectrical.AFAS;
using ThMEPElectrical.AFAS.Data;
using ThMEPElectrical.AFAS.Utils;
using ThMEPElectrical.AFAS.ViewModel;
using ThMEPElectrical.AFAS.Model;

#if (ACAD2016 || ACAD2018)
using CLI;
using ThMEPElectrical.AFAS.Command;
using ThMEPElectrical.FireAlarmDistance.Command;
#endif

namespace ThMEPElectrical
{
    public class ThAFASCmds
    {
        [CommandMethod("TIANHUACAD", "THFASmoke", CommandFlags.Session)]
        public void THFASmoke()
        {
            using (var cmd = new ThAFASSmokeCmd())
            {
                cmd.Execute();
            }
        }

        [CommandMethod("TIANHUACAD", "THFADisplay", CommandFlags.Session)]
        public void THFADisplay()
        {
            using (var cmd = new ThAFASDisplayDeviceLayoutCmd())
            {
                cmd.Execute();
            }
        }

        [CommandMethod("TIANHUACAD", "THFAMonitor", CommandFlags.Session)]
        public void THFAMonitor()
        {
            using (var cmd = new ThAFASFireProofMonitorLayoutCmd())
            {
                cmd.Execute();
            }
        }

        [CommandMethod("TIANHUACAD", "THFATel", CommandFlags.Session)]
        public void THFATel()
        {
            using (var cmd = new ThAFASFireTelLayoutCmd())
            {
                cmd.Execute();
            }
        }

        [CommandMethod("TIANHUACAD", "THFAGas", CommandFlags.Session)]
        public void THFAGas()
        {
            using (var cmd = new ThAFASGasCmd())
            {
                cmd.Execute();
            }

        }

        [CommandMethod("TIANHUACAD", "THFABroadcast", CommandFlags.Session)]
        public void THFABroadcast()
        {
#if (ACAD2016 || ACAD2018)
            using (var cmd = new ThAFASBroadcastCmd())
            {
                cmd.Execute();
            }
#else
            Active.Editor.WriteLine("此功能只支持CAD2016暨以上版本");
#endif
        }

        [CommandMethod("TIANHUACAD", "THFAManualAlarm", CommandFlags.Session)]
        public void THFAManualAlarm()
        {
#if (ACAD2016 || ACAD2018)
            using (var cmd = new ThAFASManualAlarmCmd())
            {
                cmd.Execute();
            }
#else
            Active.Editor.WriteLine("此功能只支持CAD2016暨以上版本");
#endif
        }

        //---------------无ui模式
        [CommandMethod("TIANHUACAD", "THHZBJNoUI", CommandFlags.Session)]
        public void THHZBJNoUI()
        {
#if (ACAD2016 || ACAD2018)
            ///////手动输入设定
            var layoutTypeEnum = Enum.GetValues(typeof(ThFaCommon.LayoutItemType));
            var layoutStringHint = "";
            foreach (var s in layoutTypeEnum)
            {
                layoutStringHint = layoutStringHint + String.Format("{0}({1}) ", s.ToString(), (int)s);
            }
            var layoutInt = ThAFASUtils.SettingString("\n逗号拼接布置："+ layoutStringHint);
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
                    case (int)ThFaCommon.LayoutItemType.Smoke:
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
                    case (int)ThFaCommon.LayoutItemType.Broadcast:
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
                    case (int)ThFaCommon.LayoutItemType.Display:
                        var rst = ThAFASUtils.SettingInt("\n楼层显示器：住宅（0）公建（1）");
                        FireAlarmSetting.Instance.DisplayBuilding = rst;

                        break;
                    case (int)ThFaCommon.LayoutItemType.Tel:
                        break;
                    case (int)ThFaCommon.LayoutItemType.Gas:
                        if (setBeam == false)
                        {
                            beam = ThAFASUtils.SettingInt("\n不考虑梁（0）考虑梁（1）");
                            setBeam = true;
                        }
                        var radius = ThAFASUtils.SettingDouble("\n可燃气保护半径：");
                        FireAlarmSetting.Instance.Beam = beam;
                        FireAlarmSetting.Instance.GasProtectRadius = radius;

                        break;
                    case (int)ThFaCommon.LayoutItemType.ManualAlarm:
                        radius = ThAFASUtils.SettingDouble("\n手动报警步距：");
                        FireAlarmSetting.Instance.StepLengthMA = radius;

                        break;
                    case (int)ThFaCommon.LayoutItemType.Monitor:
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
#endif
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
            FireAlarmSetting.Instance.LayoutItemList.Clear();
            FireAlarmSetting.Instance.LayoutItemList.Add((int)ThFaCommon.LayoutItemType.Smoke);

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
            FireAlarmSetting.Instance.LayoutItemList.Clear();
            FireAlarmSetting.Instance.LayoutItemList.Add((int)ThFaCommon.LayoutItemType.Display);

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
            FireAlarmSetting.Instance.LayoutItemList.Clear();
            FireAlarmSetting.Instance.LayoutItemList.Add((int)ThFaCommon.LayoutItemType.Monitor);

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
            FireAlarmSetting.Instance.LayoutItemList.Clear();
            FireAlarmSetting.Instance.LayoutItemList.Add((int)ThFaCommon.LayoutItemType.Tel);

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
            FireAlarmSetting.Instance.LayoutItemList.Clear();
            FireAlarmSetting.Instance.LayoutItemList.Add((int)ThFaCommon.LayoutItemType.Gas);

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
            FireAlarmSetting.Instance.LayoutItemList.Clear();
            FireAlarmSetting.Instance.LayoutItemList.Add((int)ThFaCommon.LayoutItemType.Broadcast);

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
            FireAlarmSetting.Instance.LayoutItemList.Clear();
            FireAlarmSetting.Instance.LayoutItemList.Add((int)ThFaCommon.LayoutItemType.ManualAlarm);

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
            using (var doclock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                ThAFASDataPass.Instance = new ThAFASDataPass();

                var selectPts = ThAFASSelectFrameUtil.GetFrameBlk();
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

        [CommandMethod("TIANHUACAD", "THFACleanAllBlk", CommandFlags.Modal)]
        public void THFACleanAllBlk()
        {
            using (var doclock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var selectPts = ThAFASSelectFrameUtil.GetFrame();
                if (selectPts.Count == 0)
                {
                    return;
                }

                var transformer = ThAFASUtils.GetTransformer(selectPts);

                ////////导入所有块，图层信息
                var extractBlkList = ThFaCommon.BlkNameList;
                ThFireAlarmInsertBlk.PrepareInsert(extractBlkList, ThFaCommon.Blk_Layer.Select(x => x.Value).Distinct().ToList());

                ////////清除所选的块
                var cleanBlkList = extractBlkList;
                var previousEquipmentData = new ThAFASBusinessDataSetFactory()
                {
                    BlkNameList = cleanBlkList,
                };
                previousEquipmentData.SetTransformer(transformer);
                var localEquipmentData = previousEquipmentData.Create(acadDatabase.Database, selectPts);
                var cleanEquipment = localEquipmentData.Container;
                ThAFASUtils.CleanPreviousEquipment(cleanEquipment);
            }
        }


    }
}
