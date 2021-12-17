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
using ThMEPElectrical.FireAlarmDistance.Model;
using ThMEPElectrical.FireAlarmDistance.Service;

namespace ThMEPElectrical.FireAlarmDistance
{
    public class ThAFASBroadcastCmd : ThMEPBaseCommand, IDisposable
    {
        private bool UseUI { get; set; }
        double _scale = 100;
        bool _referBeam = true;
        ThAFASPlacementMountModeMgd _mode = ThAFASPlacementMountModeMgd.Wall;
        double _stepLength = 25000;

        public ThAFASBroadcastCmd(bool UI)
        {
            UseUI = UI;
            InitialCmdInfo();
            InitialSetting();
        }

        private void InitialCmdInfo()
        {
            ActionName = "布置";
            CommandName = "THFABROADCAST";
        }

        private void InitialSetting()
        {
            if (UseUI == true)
            {
                _scale = FireAlarmSetting.Instance.Scale;
                _mode = (ThAFASPlacementMountModeMgd)FireAlarmSetting.Instance.BroadcastLayout;
                _stepLength = FireAlarmSetting.Instance.StepLengthBC;
                _referBeam = FireAlarmSetting.Instance.Beam == 1 ? true : false;
            }


        }
        public void Dispose()
        {
        }

        public override void SubExecute()
        {
            ThFABroadcastExecute();
        }

        public void ThFABroadcastExecute()
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
                var cleanBlkName = new List<string>() { ThFaCommon.BlkName_Broadcast_Ceiling, ThFaCommon.BlkName_Broadcast_Wall };
                var avoidBlkName = ThFaCommon.BlkNameList.Where(x => cleanBlkName.Contains(x) == false).ToList();
                var layoutBlkName = _mode == ThAFASPlacementMountModeMgd.Wall ? ThFaCommon.BlkName_Broadcast_Wall : ThFaCommon.BlkName_Broadcast_Ceiling;

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

                //布置广播
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

                //接入楼梯
                var layoutParameter = new ThAFASBCLayoutParameter()
                {
                    Scale = _scale,
                    framePts = framePts,
                    Data = data,
                    BlkNameBroadcast = ThFaCommon.BlkName_Broadcast_Wall,
                };

                var stairBlkResult = ThFABCStairService.LayoutStair(layoutParameter);

                var roomBoundary = data.GetRoomBoundary();
                ThFABCStairService.CleanStairRoomPt(layoutParameter.StairPartResult, roomBoundary, ref ptsOutput);


                var ptDirList = ThAFASDistanceLayoutService.FindOutputPtsDir(ptsOutput, roomBoundary);
                ptDirList.ForEach(x => DrawUtils.ShowGeometry(x.Key, x.Value, "l0Result", 3, 30, 200));

                ThFireAlarmInsertBlk.InsertBlock(ptDirList, _scale, layoutBlkName, ThFaCommon.blk_layer[layoutBlkName], true);
                ThFireAlarmInsertBlk.InsertBlockAngle(stairBlkResult, _scale);
            }
        }

        public void SettingNoUI()
        {
            var isWallPa = Active.Editor.GetInteger("\n吊装（0）壁装（1）");
            if (isWallPa.Status != PromptStatus.OK)
            {
                return;
            }
            _mode = isWallPa.Value == 1 ? ThAFASPlacementMountModeMgd.Wall : ThAFASPlacementMountModeMgd.Ceiling;
            if (_mode == ThAFASPlacementMountModeMgd.Ceiling)
            {
                var beam = Active.Editor.GetInteger("\n不考虑梁（0）考虑梁（1）");
                if (beam.Status != PromptStatus.OK)
                {
                    return;
                }
                _referBeam = beam.Value == 1 ? true : false;
            }

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
