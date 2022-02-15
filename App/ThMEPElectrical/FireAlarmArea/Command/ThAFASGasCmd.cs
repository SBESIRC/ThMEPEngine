using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.AutoCAD.DatabaseServices;

using Linq2Acad;
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
        private double _wallThickness = 100;
        private double _bufferDist = 500;
        private bool _floorUpDown = true;

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
            _wallThickness = FireAlarmSetting.Instance.RoofThickness;
            _bufferDist = FireAlarmSetting.Instance.BufferDist;
            _floorUpDown = FireAlarmSetting.Instance.FloorUpDown == 0 ? false : true;
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
                var transformer = ThAFASDataPass.Instance.Transformer;
                var pts = ThAFASDataPass.Instance.SelectPts;

                //--------------初始图块信息
                var extractBlkList = ThFaCommon.BlkNameList;
                var cleanBlkName = ThFaCommon.LayoutBlkList[(int)ThFaCommon.LayoutItemType.Gas];
                var avoidBlkName = ThFaCommon.BlkNameList.Where(x => cleanBlkName.Contains(x) == false).ToList();
                var layoutBlkNameGas = ThFaCommon.BlkName_Gas;
                var layoutBlkNameProfGas = ThFaCommon.BlkName_Gas_ExplosionProf;

                //--------------提取数据
                var beamDataParameter = new ThBeamDataParameter();
                beamDataParameter.ReferBeam = _referBeam;
                beamDataParameter.WallThickness = _wallThickness;
                beamDataParameter.BufferDist = _bufferDist;

                var geos = ThAFASUtils.GetAreaLayoutData(ThAFASDataPass.Instance, extractBlkList, beamDataParameter, false); //38s
                if (geos.Count == 0)
                {
                    return;
                }

                //--------------数据转回原点
                ThAFASUtils.TransformToZero(transformer, geos);

                //--------------处理数据：找洞。分类数据：墙，柱，可布区域，避让。扩大避让。定义房间名称类型
                var dataQuery = new ThAFASAreaDataQueryService(geos, avoidBlkName);//19s
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
                blindsResult.ForEach(x => transformer.Reset(x));

                //打印
                if (_floorUpDown == true)
                {
                    layoutResult.ForEach(x => x.Dir = new Autodesk.AutoCAD.Geometry.Vector3d(0, 1, 0));
                }
                ThFireAlarmInsertBlk.InsertBlock(layoutResult, _scale);
                ThFireAlarmInsertBlk.InsertPolyline(blindsResult, ThFaSmokeCommon.Layer_Gas_Blind, ThFaSmokeCommon.Color_Blind);

            }
        }
    }
}
