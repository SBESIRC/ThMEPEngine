#if (ACAD2016 || ACAD2018)
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

using ThMEPElectrical.AFAS;
using ThMEPElectrical.AFAS.Utils;
using ThMEPElectrical.AFAS.ViewModel;
using ThMEPElectrical.FireAlarmDistance.Data;
using ThMEPElectrical.FireAlarmDistance.Service;

namespace ThMEPElectrical.FireAlarmDistance
{
    public class ThAFASManualAlarmCmd : ThMEPBaseCommand, IDisposable
    {
        private bool UseUI { get; set; }
        double _scale = 100;
        ThAFASPlacementMountModeMgd _mode = ThAFASPlacementMountModeMgd.Wall;
        double _stepLength = 25000;
        bool _referBeam = false;

        public ThAFASManualAlarmCmd(bool UI)
        {
            UseUI = UI;
            InitialCmdInfo();
            InitialSetting();
        }

        private void InitialCmdInfo()
        {
            ActionName = "布置";
            CommandName = "THFAMANUALALARM";
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

                var framePts = ThAFASUtils.GetFrameBlk();
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
                var needConverage = _mode == ThAFASPlacementMountModeMgd.Wall ? false : true;
                var geos = ThAFASUtils.GetDistLayoutData(framePts, extractBlkList, _referBeam, needConverage);
                var data = new ThAFASDistanceDataSet(geos, cleanBlkName, avoidBlkName);
                data.ClassifyData();
                data.CleanPreviousEquipment();
                data.ExtendEquipment(cleanBlkName, _scale);
                data.FilterBeam();
                data.ProcessRoomPlacementLabel(ThFaDistCommon.BroadcastTag);

                //布置手动报警
                var geojson = ThGeoOutput.Output(data.Data);
                ThAFASPlacementEngineMgd engine = new ThAFASPlacementEngineMgd();
                ThAFASPlacementContextMgd context = new ThAFASPlacementContextMgd()
                {
                    StepDistance = _stepLength,
                    MountMode = _mode,
                };

                var outJson = engine.Place(geojson, context);
                var features = ThAFASDistanceLayoutService.Export2NTSFeatures(outJson);
#if DEBUG
                string path = Path.Combine(Active.DocumentDirectory, string.Format("{0}.output.geojson", Active.DocumentName));
                File.WriteAllText(path, outJson);
#endif
                var ptsOutput = ThAFASDistanceLayoutService.ConvertGeom(features);
                ptsOutput.ForEach(x => DrawUtils.ShowGeometry(x, "l0output", 212, 30, 50));


                var roomBoundary = data.GetRoomBoundary();
                var ptDirList = ThAFASDistanceLayoutService.FindOutputPtsDir(ptsOutput, roomBoundary);
                var ptDirListTop = ThAFASDistanceLayoutService.FindOutputPtsOnTop(ptDirList, layoutBlkNameTop, _scale);
                ptDirList.ForEach(x => DrawUtils.ShowGeometry(x.Key, x.Value, "l0Result", 3, 30, 200));

                ThFireAlarmInsertBlk.InsertBlock(ptDirList, _scale, layoutBlkNameTop, ThFaCommon.blk_layer[layoutBlkNameTop], true);
                ThFireAlarmInsertBlk.InsertBlock(ptDirListTop, _scale, layoutBlkNameBottom, ThFaCommon.blk_layer[layoutBlkNameBottom], true);
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
