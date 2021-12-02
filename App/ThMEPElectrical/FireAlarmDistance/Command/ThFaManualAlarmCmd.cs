﻿#if (ACAD2016 || ACAD2018)
using System;
using System.Linq;
using System.IO;
using System.Collections.Generic;

using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.DatabaseServices;
using NetTopologySuite.Features;
using NetTopologySuite.IO;
using Newtonsoft.Json;

using AcHelper;
using Linq2Acad;
using CLI;

using ThCADExtension;
using ThCADCore.NTS;
using ThMEPEngineCore.IO;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Command;

using ThMEPElectrical.FireAlarm;
using ThMEPElectrical.FireAlarm.Service;
using ThMEPElectrical.FireAlarm.ViewModels;
using ThMEPElectrical.FireAlarmDistance.Data;
using ThMEPElectrical.FireAlarmDistance.Service;


namespace ThMEPElectrical.FireAlarmDistance
{
    public class ThFaManualAlarmCmd : ThMEPBaseCommand, IDisposable
    {
        private bool UseUI { get; set; }
        double _scale = 100;
        ThAFASPlacementMountModeMgd _mode = ThAFASPlacementMountModeMgd.Wall;
        double _stepLength = 25000;
        public ThFaManualAlarmCmd(bool UI)
        {
            UseUI = UI;
            InitialCmdInfo();
            InitialSetting();
        }

        private void InitialCmdInfo()
        {
            CommandName = "ThFaManualAlarm";
            ActionName = "生成";
        }

        private void InitialSetting()
        {
            if (UseUI == true)
            {
                _scale = FireAlarmSetting.Instance.Scale;
                _stepLength = FireAlarmSetting.Instance.StepLengthMA;
            }


        }
        public void Dispose()
        {
        }

        public override void SubExecute()
        {
            ThFaManualAlarmExecute();
        }

        public void ThFaManualAlarmExecute()
        {
            using (var doclock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {

                var framePts = ThFireAlarmUtils.GetFrameBlk();
                if (framePts.Count == 0)
                {
                    return;
                }

                if (UseUI == false)
                {
                    SettingNoUI();
                }

                var extractBlkList = ThFaCommon.BlkNameList;
                var cleanBlkName = new List<string>() { ThFaCommon.BlkName_ManualAlarm, ThFaCommon.BlkName_SoundLightAlarm };
                var avoidBlkName = ThFaCommon.BlkNameList.Where(x => cleanBlkName.Contains(x) == false).ToList();
                var layoutBlkNameBottom = ThFaCommon.BlkName_ManualAlarm;
                var layoutBlkNameTop = ThFaCommon.BlkName_SoundLightAlarm;

                //导入块图层。free图层
                ThFireAlarmInsertBlk.prepareInsert(extractBlkList, ThFaCommon.blk_layer.Select(x => x.Value).Distinct().ToList());

                //取数据
                var geos = ThFireAlarmUtils.GetDistLayoutData(framePts, extractBlkList, false, false);
                var data = new ThAFASDistanceDataSet(geos);
                data.ExtendEquipment(cleanBlkName, _scale);
                data.FilterBeam();
                var room = data.GetRoom();

                ///debug
                var roomLable = data.GetRoomGeom();
                for (int i = 0; i < roomLable.Count; i++)
                {
                    var pl = roomLable[i].Boundary as Polyline;
                    var pt = pl.GetCentroidPoint();
                    DrawUtils.ShowGeometry(pt, String.Format("placement：{0}", roomLable[i].Properties["Placement"]), "l0RoomPlacement", 3, 25, 200);
                    DrawUtils.ShowGeometry(new Point3d(pt.X, pt.Y - 300 * 1, 0), String.Format("name：{0}", roomLable[i].Properties["Name"]), "l0RoomName", 3, 25, 200);
                    DrawUtils.ShowGeometry(new Point3d(pt.X, pt.Y - 300 * 2, 0), String.Format("Privacy：{0}", roomLable[i].Properties["Privacy"]), "l0RoomPrivacy", 3, 25, 200);
                }
                ThGeoOutput.Output(data.Data, Active.DocumentDirectory, Active.DocumentName);
                ///

                var geojson = ThGeoOutput.Output(data.Data);
                ThAFASPlacementEngineMgd engine = new ThAFASPlacementEngineMgd();
                ThAFASPlacementContextMgd context = new ThAFASPlacementContextMgd()
                {
                    StepDistance = _stepLength,
                    MountMode = _mode,
                };

                var outJson = engine.Place(geojson, context);
                var features = ThFADistanceLayoutService.Export2NTSFeatures(outJson);

                string path = Path.Combine(Active.DocumentDirectory, string.Format("{0}.output.geojson", Active.DocumentName));
                File.WriteAllText(path, outJson);

                var ptsOutput = ThFADistanceLayoutService.ConvertGeom(features);
                ptsOutput.ForEach(x => DrawUtils.ShowGeometry(x, "l0output", 212, 30, 50));

                var ptDirList = ThFADistanceLayoutService.FindOutputPtsDir(ptsOutput, room);
                var ptDirListTop = ThFADistanceLayoutService.FindOutputPtsOnTop(ptDirList, layoutBlkNameTop, _scale);
                ptDirList.ForEach(x => DrawUtils.ShowGeometry(x.Key, x.Value, "l0Result", 3, 30, 200));

                ThFireAlarmInsertBlk.InsertBlock(ptDirList, _scale, layoutBlkNameTop , ThFaCommon.blk_layer[layoutBlkNameTop], true);
                ThFireAlarmInsertBlk.InsertBlock(ptDirList, _scale, layoutBlkNameBottom , ThFaCommon.blk_layer[layoutBlkNameBottom], true);
            }
        }

        public void SettingNoUI()
        {
            var stepDistanceP = Active.Editor.GetDouble("\n步距：");
            if (stepDistanceP.Status != PromptStatus.OK)
            {
                return;
            }
            _stepLength = stepDistanceP.Value;

        }
    }
}
#endif
