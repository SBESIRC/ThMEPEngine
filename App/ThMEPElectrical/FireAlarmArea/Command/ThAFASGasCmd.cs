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
using ThMEPElectrical.AFAS.Model;
using ThMEPElectrical.AFAS.ViewModel;
using ThMEPElectrical.AFAS.Utils;

using ThMEPElectrical.FireAlarmArea.Data;
using ThMEPElectrical.FireAlarmArea.Service;
using ThMEPElectrical.FireAlarmArea.Model;

namespace ThMEPElectrical.FireAlarmArea.Command
{
    public class ThAFASGasCmd : ThMEPBaseCommand, IDisposable
    {
        private double _scale = 100;
        private bool _referBeam = true;
        private double _radius = 8000;
        private double _wallThick = 0;

        public ThAFASGasCmd()
        {
            InitialCmdInfo();
            InitialSetting();
        }

        private void InitialCmdInfo()
        {
            ActionName = "布置";
            CommandName = "THFAGAS";
        }


        private void InitialSetting()
        {
            _scale = FireAlarmSetting.Instance.Scale;
            _referBeam = FireAlarmSetting.Instance.Beam == 1 ? true : false;
            _radius = FireAlarmSetting.Instance.GasProtectRadius;
            _wallThick = FireAlarmSetting.Instance.RoofThickness;
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
        public override void SubExecute()
        {
            FireAlarmGasLayoutExecute();
        }

        public void Dispose()
        {

        }

        private void FireAlarmGasLayoutExecute()
        {
            using (var doclock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                //////--------------画框，提数据，转数据
                //var pts = ThAFASUtils.GetFrameBlk();
                //if (pts.Count == 0)
                //{
                //    return;
                //}
                //if (UseUI == false)
                //{
                //    SettingNoUI();
                //}
                var transformer = ThAFASDataPass.Instance.Transformer;
                var pts = ThAFASDataPass.Instance.SelectPts;

                //--------------初始图块信息
                var extractBlkList = ThFaCommon.BlkNameList;
                var cleanBlkName = ThFaCommon.LayoutBlkList[(int)ThFaCommon.LayoutItemType.Gas];
                var avoidBlkName = ThFaCommon.BlkNameList.Where(x => cleanBlkName.Contains(x) == false).ToList();
                var layoutBlkNameGas = ThFaCommon.BlkName_Gas;
                var layoutBlkNameProfGas = ThFaCommon.BlkName_Gas_ExplosionProf;
                //ThFireAlarmInsertBlk.prepareInsert(extractBlkList, ThFaCommon.Blk_Layer.Select(x => x.Value).Distinct().ToList());

                //--------------提取数据
                //var geos = ThAFASUtils.GetAreaLayoutData(pts, extractBlkList, _referBeam, _wallThick, false); //38s
                var geos = ThAFASUtils.GetAreaLayoutData2(ThAFASDataPass.Instance, extractBlkList, _referBeam, _wallThick, false); //38s
                if (geos.Count == 0)
                {
                    return;
                }

                //--------------数据转回原点
                //var transformer = ThAFASUtils.TransformToOrig(pts, geos);
                ThAFASUtils.TransformToZero(transformer, geos);

                //--------------处理数据：找洞。分类数据：墙，柱，可布区域，避让。扩大避让。定义房间名称类型
                //var dataQuery = new ThAFASAreaDataQueryService(geos, cleanBlkName, avoidBlkName);//19s
                var dataQuery = new ThAFASAreaDataQueryService(geos, avoidBlkName);//19s
                //dataQuery.AnalysisHoles();
                //dataQuery.ClassifyData();//先分房间再扩大
                dataQuery.AddMRoomDict();
                dataQuery.ClassifyDataNew();//先分房间再扩大
                var priorityExtend = ThAFASUtils.GetPriorityExtendValue(cleanBlkName, _scale);
                dataQuery.ExtendPriority(priorityExtend);

                var roomType = ThFaGasRoomTypeService.GetGasSensorType(dataQuery.Rooms, dataQuery.RoomFrameDict);

                //--------------定义传数据
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
                ThAFASUtils.TransformReset(transformer, geos);

                //打印
                ThFireAlarmInsertBlk.InsertBlock(layoutResult, _scale);
            }
        }

    }
}
