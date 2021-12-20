using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.DatabaseServices;

using AcHelper;
using Linq2Acad;
using ThCADExtension;
using ThMEPEngineCore.Command;

using ThMEPElectrical.AFAS;
using ThMEPElectrical.AFAS.ViewModel;
using ThMEPElectrical.AFAS.Utils;

using ThMEPElectrical.FireAlarmArea.Data;
using ThMEPElectrical.FireAlarmArea.Service;
using ThMEPElectrical.FireAlarmArea.Model;

namespace ThMEPElectrical.FireAlarmArea.Command
{
    public class ThAFASGasCmd : ThMEPBaseCommand, IDisposable
    {
        private bool UseUI { get; set; }
        private double _scale = 100;
        private bool _referBeam = true;
        private double _radius = 8000;
        private double _wallThick = 0;

        public ThAFASGasCmd(bool UI)
        {
            UseUI = UI;
            InitialCmdInfo();
            InitialSetting();
        }

        private void InitialCmdInfo()
        {
            ActionName = "布置";
            CommandName = "THFAGAS";
        }

        public override void SubExecute()
        {
            FireAlarmGasLayoutExecute();
        }

        private void InitialSetting()
        {
            if (UseUI == true)
            {
                _scale = FireAlarmSetting.Instance.Scale;
                _referBeam = FireAlarmSetting.Instance.Beam == 1 ? true : false;
                _radius = FireAlarmSetting.Instance.ProtectRadius;
            }
        }

        public void Dispose()
        {

        }

        private void FireAlarmGasLayoutExecute()
        {
            using (var doclock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                //画框，提数据，转数据
                var pts = ThAFASUtils.GetFrameBlk();
                if (pts.Count == 0)
                {
                    return;
                }
                if (UseUI == false)
                {
                    SettingNoUI();
                }

                var extractBlkList = ThFaCommon.BlkNameList;
                var cleanBlkName = new List<string>() { ThFaCommon.BlkName_Gas, ThFaCommon.BlkName_Gas_ExplosionProf };
                var avoidBlkName = ThFaCommon.BlkNameList.Where(x => cleanBlkName.Contains(x) == false).ToList();

                var layoutBlkNameGas = ThFaCommon.BlkName_Gas;
                var layoutBlkNameProfGas = ThFaCommon.BlkName_Gas_ExplosionProf;

                //导入块图层。free图层
                ThFireAlarmInsertBlk.prepareInsert(extractBlkList, ThFaCommon.blk_layer.Select(x => x.Value).Distinct().ToList());

                var geos = ThAFASUtils.GetSmokeData(pts, extractBlkList, _referBeam, _wallThick, false); //38s
                if (geos.Count == 0)
                {
                    return;
                }

                //转回原点
                var transformer = ThAFASUtils.TransformToOrig(pts, geos);

                var dataQuery = new ThAFASAreaDataQueryService(geos, cleanBlkName, avoidBlkName);//19s
                //洞,必须先做找到框线
                dataQuery.AnalysisHoles();
                //墙，柱，可布区域，避让
                dataQuery.ClassifyData();
                var priorityExtend = ThAFASUtils.GetPriorityExtendValue(cleanBlkName, _scale);
                dataQuery.ExtendPriority(priorityExtend);

                var roomType = ThFaGasRoomTypeService.GetGasSensorType(dataQuery.Rooms, dataQuery.RoomFrameDict);
                foreach (var frame in dataQuery.FrameHoleList)
                {
                    DrawUtils.ShowGeometry(frame.Key, string.Format("l0room"), 30);
                    DrawUtils.ShowGeometry(frame.Value, string.Format("l0hole"), 140);
                }

                var layoutParameter = new ThAFASGasLayoutParameter();
                layoutParameter.Scale = _scale;
                layoutParameter.AisleAreaThreshold = ThFaSmokeCommon.AisleAreaThreshold;
                layoutParameter.ProtectRadius = _radius;
                layoutParameter.RoomType = roomType;
                layoutParameter.BlkNameGas = layoutBlkNameGas;
                layoutParameter.BlkNameGasPrf = layoutBlkNameProfGas;

                ThAFASGasEngine.ThFaGasLayoutEngine(dataQuery, layoutParameter, out var layoutResult, out var blindsResult);

                //转回到原始位置
                layoutResult.ForEach(x => x.TransformBack(transformer));

                //打印
                ThFireAlarmInsertBlk.InsertBlock(layoutResult, _scale);

            }
        }


        private void SettingNoUI()
        {
            var beam = Active.Editor.GetInteger("\n不考虑梁（0）考虑梁（1）");
            if (beam.Status != PromptStatus.OK)
            {
                return;
            }
            _referBeam = beam.Value == 1 ? true : false;

            var radius = Active.Editor.GetDouble("\n保护半径：");
            if (radius.Status != PromptStatus.OK)
            {
                return;
            }
            _radius = radius.Value;
        }


    }
}
