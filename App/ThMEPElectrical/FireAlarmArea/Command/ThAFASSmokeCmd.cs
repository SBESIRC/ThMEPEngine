using System;
using System.Linq;
using System.IO;

using Autodesk.AutoCAD.DatabaseServices;
using AcHelper;
using Linq2Acad;
using ThMEPEngineCore.Command;

using ThMEPElectrical.AFAS;
using ThMEPElectrical.AFAS.Model;
using ThMEPElectrical.AFAS.Utils;
using ThMEPElectrical.AFAS.ViewModel;

using ThMEPElectrical.FireAlarmArea.Data;
using ThMEPElectrical.FireAlarmArea.Service;
using ThMEPElectrical.FireAlarmArea.Model;

namespace ThMEPElectrical.FireAlarmArea.Command
{
    public class ThAFASSmokeCmd : ThMEPBaseCommand, IDisposable
    {
        private int _theta = 0;
        private int _floorHight = 2;
        private double _scale = 100;
        private bool _referBeam = true;
        private double _wallThickness = 100;
        private double _bufferDist = 500;
        private bool _needDetective = true;
        private bool _floorUpDown = true;

        public ThAFASSmokeCmd()
        {
            InitialCmdInfo();
            InitialSetting();
        }
        private void InitialCmdInfo()
        {
            ActionName = "布置";
            CommandName = "THFASMOKE";
        }
        private void InitialSetting()
        {
            _theta = FireAlarmSetting.Instance.RoofGrade;
            _floorHight = FireAlarmSetting.Instance.RoofHight;
            _scale = FireAlarmSetting.Instance.Scale;
            _referBeam = FireAlarmSetting.Instance.Beam == 1 ? true : false;
            _wallThickness = FireAlarmSetting.Instance.RoofThickness;
            _bufferDist = FireAlarmSetting.Instance.BufferDist;
            _needDetective = _referBeam == true ? true : false;
            _floorUpDown = FireAlarmSetting.Instance.FloorUpDown == 0 ? false : true;
        }

        public override void SubExecute()
        {
            FireAlarmSmokeHeatLayoutExecute();
        }

        public void Dispose()
        {
        }

        private void FireAlarmSmokeHeatLayoutExecute()
        {
            using (var doclock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var transformer = ThAFASDataPass.Instance.Transformer;
                var pts = ThAFASDataPass.Instance.SelectPts;

                //--------------初始图块信息
                var extractBlkList = ThFaCommon.BlkNameList;
                var cleanBlkName = ThFaCommon.LayoutBlkList[(int)ThFaCommon.LayoutItemType.Smoke];
                var avoidBlkName = ThFaCommon.BlkNameList.Where(x => cleanBlkName.Contains(x) == false).ToList();
                var layoutBlkNameSmoke = ThFaCommon.BlkName_Smoke;
                var layoutBlkNameHeat = ThFaCommon.BlkName_Heat;
                var layoutBlkNameSmokePrf = ThFaCommon.BlkName_Smoke_ExplosionProf;
                var layoutBlkNameHeatPrf = ThFaCommon.BlkName_Heat_ExplosionProf;

                //--------------提取数据
                var beamDataParameter = new ThBeamDataParameter();
                beamDataParameter.ReferBeam = _referBeam;
                beamDataParameter.WallThickness = _wallThickness;
                beamDataParameter.BufferDist = _bufferDist;

                var geos = ThAFASUtils.GetAreaLayoutData(ThAFASDataPass.Instance, extractBlkList, beamDataParameter, _needDetective);
                if (geos.Count == 0)
                {
                    return;
                }

                //--------------数据转回原点
                ThAFASUtils.TransformToZero(transformer, geos);

                //--------------处理数据：找洞。分类数据：墙，柱，可布区域，避让。扩大避让。定义房间名称类型
                var dataQuery = new ThAFASAreaDataQueryService(geos, avoidBlkName);
                dataQuery.AddMRoomDict();
                dataQuery.ClassifyDataNew();
                var priorityExtend = ThAFASUtils.GetPriorityExtendValue(cleanBlkName, _scale);
                dataQuery.ExtendPriority(priorityExtend);
                var roomType = ThFaSmokeRoomTypeService.GetSmokeSensorType(dataQuery.Rooms, dataQuery.RoomFrameDict);

                //--------------定义传数据
                string LogFileName = Path.Combine(Active.DocumentDirectory, Active.DocumentName + ".log");
                LogUtil Logger = new LogUtil(LogFileName);

                var layoutParameter = new ThAFASSmokeLayoutParameter();
                layoutParameter.FloorHightIdx = _floorHight;
                layoutParameter.RootThetaIdx = _theta;
                layoutParameter.Scale = _scale;
                layoutParameter.AisleAreaThreshold = ThFaSmokeCommon.AisleAreaThreshold;
                layoutParameter.BlkNameHeat = layoutBlkNameHeat;
                layoutParameter.BlkNameSmoke = layoutBlkNameSmoke;
                layoutParameter.BlkNameHeatPrf = layoutBlkNameHeatPrf;
                layoutParameter.BlkNameSmokePrf = layoutBlkNameSmokePrf;
                layoutParameter.RoomType = roomType;
                layoutParameter.framePts = pts;
                layoutParameter.transformer = transformer;
                layoutParameter.priorityExtend = priorityExtend;
                layoutParameter.DoorOpenings = dataQuery.DoorOpenings;
                layoutParameter.Windows = dataQuery.Windows;
                layoutParameter.Log = Logger;

                //--------------布置楼梯部分
                var stairBlkResult = ThFASmokeStairService.LayoutStair(layoutParameter);

                //--------------布置
                ThAFASSmokeEngine.ThFaSmokeHeatLayoutEngine(dataQuery, layoutParameter, out var layoutResult, out var blindsResult);

                //--------------转回到原始位置
                layoutResult.ForEach(x => x.TransformBack(transformer));
                ThAFASUtils.TransformReset(transformer, geos);
                blindsResult.ForEach(x => transformer.Reset(x));

                //--------------打印最终图块
                if (_floorUpDown == true)
                {
                    layoutResult.ForEach(x => x.Dir = new Autodesk.AutoCAD.Geometry.Vector3d(0, 1, 0));
                }

                ThFireAlarmInsertBlk.InsertBlock(layoutResult, _scale);
                ThFireAlarmInsertBlk.InsertBlockAngle(stairBlkResult, _scale);
                ThFireAlarmInsertBlk.InsertPolyline(blindsResult, ThFaSmokeCommon.Layer_Blind, ThFaSmokeCommon.Color_Blind);
            }
        }
    }
}
