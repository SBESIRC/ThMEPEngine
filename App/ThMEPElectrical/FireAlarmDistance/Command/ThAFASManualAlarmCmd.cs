﻿#if (ACAD2016 || ACAD2018)
using System;
using AcHelper;
using Linq2Acad;
using System.IO;
using System.Linq;
using ThMEPEngineCore.IO;
using ThMEPEngineCore.Command;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.ApplicationServices;

using ThMEPElectrical.AFAS;
using ThMEPElectrical.AFAS.Model;
using ThMEPElectrical.AFAS.Utils;
using ThMEPElectrical.AFAS.ViewModel;
using ThMEPElectrical.FireAlarmDistance.Data;
using ThMEPElectrical.FireAlarmDistance.Service;

using CLI;

namespace ThMEPElectrical.FireAlarmDistance.Command
{
    public class ThAFASManualAlarmCmd : ThMEPBaseCommand, IDisposable
    {
        private bool _referBeam = false;
        private double _scale = 100;
        private double _stepLength = 25000;
        ThAFASPlacementMountModeMgd _mode = ThAFASPlacementMountModeMgd.Wall;

        public ThAFASManualAlarmCmd()
        {
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

            _scale = FireAlarmSetting.Instance.Scale;
            _stepLength = FireAlarmSetting.Instance.StepLengthMA;

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
                var transformer = ThAFASDataPass.Instance.Transformer;
                var pts = ThAFASDataPass.Instance.SelectPts;

                //--------------初始图块信息!!!!!!避让计算有问题
                var extractBlkList = ThFaCommon.BlkNameList;
                var cleanBlkName = ThFaCommon.LayoutBlkList[5];
                var avoidBlkName = ThFaCommon.BlkNameList.Where(x => cleanBlkName.Contains(x) == false).ToList();
                var layoutBlkNameBottom = ThFaCommon.BlkName_ManualAlarm;
                var layoutBlkNameTop = ThFaCommon.BlkName_SoundLightAlarm;
                //ThFireAlarmInsertBlk.prepareInsert(extractBlkList, ThFaCommon.Blk_Layer.Select(x => x.Value).Distinct().ToList());

                //--------------提取数据
                var needConverage = _mode == ThAFASPlacementMountModeMgd.Wall ? false : true;
                //var geos = ThAFASUtils.GetDistLayoutData(framePts, extractBlkList, _referBeam, needConverage);
                var geos = ThAFASUtils.GetDistLayoutData2(ThAFASDataPass.Instance, extractBlkList, _referBeam, needConverage);
                if (geos.Count == 0)
                {
                    return;
                }

                var data = new ThAFASDistanceDataQueryService(geos, avoidBlkName);
                data.ExtendPriority(cleanBlkName, _scale);
                data.FilterBeam();
                data.ProcessRoomPlacementLabel(ThFaDistCommon.ManualAlartTag);

                //--------------布置手动报警
                var geojson = ThGeoOutput.Output(data.Data);
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

                var roomBoundary = data.GetRoomBoundary();
                var ptDirList = ThAFASDistanceLayoutService.FindOutputPtsDir(ptsOutput, roomBoundary);
                var ptDirListTop = ThAFASDistanceLayoutService.FindOutputPtsOnTop(ptDirList, layoutBlkNameTop, _scale);
                ptDirList.ForEach(x => DrawUtils.ShowGeometry(x.Key, x.Value, "l0Result", 3, 30, 200));

                ThFireAlarmInsertBlk.InsertBlock(ptDirList, _scale, layoutBlkNameTop, ThFaCommon.Blk_Layer[layoutBlkNameTop], true);
                ThFireAlarmInsertBlk.InsertBlock(ptDirListTop, _scale, layoutBlkNameBottom, ThFaCommon.Blk_Layer[layoutBlkNameBottom], true);
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
