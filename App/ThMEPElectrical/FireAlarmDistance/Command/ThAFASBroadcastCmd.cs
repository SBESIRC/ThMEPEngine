﻿#if (ACAD2016 || ACAD2018)
using System;
using AcHelper;
using Linq2Acad;
using System.IO;
using System.Linq;
using ThMEPEngineCore.IO;
using ThMEPEngineCore.Command;
using ThMEPEngineCore.Diagnostics;
using System.Collections.Generic;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.ApplicationServices;

using ThMEPElectrical.AFAS;
using ThMEPElectrical.AFAS.Model;
using ThMEPElectrical.AFAS.Utils;
using ThMEPElectrical.AFAS.ViewModel;
using ThMEPElectrical.FireAlarmDistance.Data;
using ThMEPElectrical.FireAlarmDistance.Model;
using ThMEPElectrical.FireAlarmDistance.Service;

using CLI;

namespace ThMEPElectrical.FireAlarmDistance.Command
{
    public class ThAFASBroadcastCmd : ThMEPBaseCommand, IDisposable
    {
        double _scale = 100;
        bool _referBeam = true;
        ThAFASPlacementMountModeMgd _mode = ThAFASPlacementMountModeMgd.Wall;
        double _stepLength = 25000;
        double _wallThickness = 100;

        public ThAFASBroadcastCmd()
        {
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
            _scale = FireAlarmSetting.Instance.Scale;
            _mode = (ThAFASPlacementMountModeMgd)FireAlarmSetting.Instance.BroadcastLayout;
            _stepLength = FireAlarmSetting.Instance.StepLengthBC;
            _referBeam = FireAlarmSetting.Instance.Beam == 1 ? true : false;
            _wallThickness = FireAlarmSetting.Instance.RoofThickness;
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
                var transformer = ThAFASDataPass.Instance.Transformer;
                var pts = ThAFASDataPass.Instance.SelectPts;

                //--------------初始图块信息
                var extractBlkList = ThFaCommon.BlkNameList;
                var layoutBlkName = _mode == ThAFASPlacementMountModeMgd.Wall ? ThFaCommon.BlkName_Broadcast_Wall : ThFaCommon.BlkName_Broadcast_Ceiling;
                var cleanBlkName = new List<string>() { layoutBlkName };
                var avoidBlkName = ThFaCommon.BlkNameList.Where(x => cleanBlkName.Contains(x) == false).ToList();
                //ThFireAlarmInsertBlk.prepareInsert(extractBlkList, ThFaCommon.Blk_Layer.Select(x => x.Value).Distinct().ToList());

                //--------------提取数据
                var needConverage = _mode == ThAFASPlacementMountModeMgd.Wall ? false : true;
                //var geos = ThAFASUtils.GetDistLayoutData(framePts, extractBlkList, _referBeam, needConverage);
                var geos = ThAFASUtils.GetDistLayoutData(ThAFASDataPass.Instance, extractBlkList, _referBeam, _wallThickness, needConverage);
                if (geos.Count == 0)
                {
                    return;
                }

                var dataQuery = new ThAFASDistanceDataQueryService(geos, avoidBlkName);
                dataQuery.ExtendPriority(cleanBlkName, _scale);
                dataQuery.FilterBeam();
                dataQuery.ProcessRoomPlacementLabel(ThFaDistCommon.BroadcastTag);

                //--------------布置广播
                var geojson = ThGeoOutput.Output(dataQuery.Data);

                ThAFASPlacementEngineMgd engine = new ThAFASPlacementEngineMgd();
                ThAFASPlacementContextMgd context = new ThAFASPlacementContextMgd()
                {
                    StepDistance = _stepLength,
                    MountMode = _mode,
                };

#if DEBUG
                {
                    string path = Path.Combine(Active.DocumentDirectory, string.Format("{0}.input.geojson", Active.DocumentName));
                    File.WriteAllText(path, geojson);
                }
#endif

                //--------------处理中
                var outJson = engine.Place(geojson, context);

#if DEBUG
                {
                    string path = Path.Combine(Active.DocumentDirectory, string.Format("{0}.output.geojson", Active.DocumentName));
                    File.WriteAllText(path, outJson);
                }
#endif

                var features = ThAFASDistanceLayoutService.Export2NTSFeatures(outJson);
                var ptsOutput = ThAFASDistanceLayoutService.ConvertGeom(features);
                ptsOutput.ForEach(x => DrawUtils.ShowGeometry(x, "l0output", 212, 30, 50));

                //--------------接入楼梯
                var layoutParameter = new ThAFASBCLayoutParameter()
                {
                    Scale = _scale,
                    framePts = ThAFASDataPass.Instance.SelectPts,
                    Data = dataQuery,
                    BlkNameBroadcast = ThFaCommon.BlkName_Broadcast_Wall,
                };

                var stairBlkResult = ThFABCStairService.LayoutStair(layoutParameter);

                var roomBoundary = dataQuery.GetRoomBoundary();
                ThFABCStairService.CleanStairRoomPt(layoutParameter.StairPartResult, roomBoundary, ref ptsOutput);

                var ptDirList = ThAFASDistanceLayoutService.FindOutputPtsDir(ptsOutput, roomBoundary);
                ptDirList.ForEach(x => DrawUtils.ShowGeometry(x.Key, x.Value, "l0Result", 3, 30, 200));

                ThFireAlarmInsertBlk.InsertBlock(ptDirList, _scale, layoutBlkName, ThFaCommon.Blk_Layer[layoutBlkName], true);
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
