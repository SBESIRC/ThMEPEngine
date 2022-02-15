using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.IO;

using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.DatabaseServices;

using AcHelper;
using Linq2Acad;

using ThMEPEngineCore.Command;

using ThMEPElectrical.AFAS;
using ThMEPElectrical.AFAS.Model;
using ThMEPElectrical.AFAS.Utils;
using ThMEPElectrical.AFAS.ViewModel;
using ThMEPElectrical.FireAlarmArea;
using ThMEPElectrical.FireAlarmArea.Data;

using ThMEPLighting.IlluminationLighting.Model;
using ThMEPLighting.IlluminationLighting.Service;

namespace ThMEPLighting.IlluminationLighting
{
    public class IlluminationLightingCmd : ThMEPBaseCommand, IDisposable
    {
        private ThIlluminationCommon.LightTypeEnum _lightType = ThIlluminationCommon.LightTypeEnum.circleCeiling;
        private double _scale = 100;
        private bool _referBeam = true;
        private double _radiusN = 3000;
        private double _radiusE = 6000;
        private bool _ifLayoutEmg = true;
        private bool _ifEmgAsNormal = false;
        private double _wallThickness = 100;
        private double _bufferDist = 500;
        private bool _floorUpDown = true;

        public IlluminationLightingCmd()
        {
            InitialCmdInfo();
            InitialSetting();
        }

        private void InitialCmdInfo()
        {
            CommandName = "THZM";
            ActionName = "照明灯具布置";
        }

        private void InitialSetting()
        {
            _scale = FireAlarmSetting.Instance.Scale;
            _floorUpDown = FireAlarmSetting.Instance.FloorUpDown == 0 ? false : true;
            _referBeam = FireAlarmSetting.Instance.Beam == 1 ? true : false;
            _wallThickness = FireAlarmSetting.Instance.RoofThickness;
            _bufferDist = FireAlarmSetting.Instance.BufferDist;

            _lightType = (ThIlluminationCommon.LightTypeEnum)FireAlarmSetting.Instance.IlluLightType;
            _radiusN = FireAlarmSetting.Instance.IlluRadiusNormal;
            _radiusE = FireAlarmSetting.Instance.IlluRadiusEmg;
            _ifLayoutEmg = FireAlarmSetting.Instance.IlluIfLayoutEmg;
            _ifEmgAsNormal = FireAlarmSetting.Instance.IlluIfEmgAsNormal;
        }

        public override void SubExecute()
        {
            ThIlluminationLightingLayoutExecute();
        }
        public void Dispose()
        {
        }

        private void ThIlluminationLightingLayoutExecute()
        {
            using (var doclock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {

                var transformer = ThAFASDataPass.Instance.Transformer;
                var pts = ThAFASDataPass.Instance.SelectPts;

                //--------------初始图块信息
                var extractBlkList = ThFaCommon.BlkNameList;
                var layoutBlkNameN = ThIlluminationCommon.lightTypeDict[_lightType];
                var layoutBlkNameE = ThFaCommon.BlkName_EmergencyLight;
                var cleanBlkName = new List<String>();
                cleanBlkName.AddRange(ThFaCommon.LayoutBlkList[(int)ThFaCommon.LayoutItemType.NormalLighting]);
                if (_ifLayoutEmg)
                {
                    cleanBlkName.AddRange(ThFaCommon.LayoutBlkList[(int)ThFaCommon.LayoutItemType.EmergencyLighting]);
                }
                var avoidBlkName = ThFaCommon.BlkNameList.Where(x => cleanBlkName.Contains(x) == false).ToList();

                //--------------提取数据
                var beamDataParameter = new ThBeamDataParameter();
                beamDataParameter.ReferBeam = _referBeam;
                beamDataParameter.WallThickness = _wallThickness;
                beamDataParameter.BufferDist = _bufferDist;

                var geos = ThAFASUtils.GetAreaLayoutData(ThAFASDataPass.Instance, extractBlkList, beamDataParameter, false);
                if (geos.Count == 0)
                {
                    return;
                }

                //--------------数据转回原点
                ThAFASUtils.TransformToZero(transformer, geos);

                //--------------处理数据：找洞。分类数据：墙，柱，可布区域，避让。扩大避让。定义房间名称类型
                var dataQuery = new ThAFASAreaDataQueryService(geos, avoidBlkName);
                dataQuery.AddMRoomDict();
                dataQuery.ClassifyDataNew();//先分房间再扩大
                var priorityExtend = ThAFASUtils.GetPriorityExtendValue(cleanBlkName, _scale);
                dataQuery.ExtendPriority(priorityExtend);
                var roomType = ThFaIlluminationRoomTypeService.GetIllunimationType(dataQuery.Rooms, dataQuery.RoomFrameDict);

                //--------------定义传数据
                string LogFileName = Path.Combine(Active.DocumentDirectory, Active.DocumentName + ".log");
                LogUtil Logger = new LogUtil(LogFileName);

                var layoutParameter = new ThAFASIlluminationLayoutParameter();
                layoutParameter.Scale = _scale;
                layoutParameter.AisleAreaThreshold = ThFaSmokeCommon.AisleAreaThreshold;
                layoutParameter.BlkNameN = layoutBlkNameN;
                layoutParameter.BlkNameE = layoutBlkNameE;
                layoutParameter.radiusN = _radiusN;
                layoutParameter.radiusE = _radiusE;
                layoutParameter.ifLayoutEmg = _ifLayoutEmg;
                layoutParameter.framePts = pts;
                layoutParameter.transformer = transformer;
                layoutParameter.roomType = roomType;
                layoutParameter.priorityExtend = priorityExtend;
                layoutParameter.DoorOpenings = dataQuery.DoorOpenings;
                layoutParameter.Windows = dataQuery.Windows;
                layoutParameter.Log = Logger;

                //接入楼梯
                var stairBlkResult = ThIlluminationStairService.LayoutStair(layoutParameter);

                ThAFASIlluminationEngine.ThFaIlluminationLayoutEngine(dataQuery, layoutParameter, out var lightResult, out var blindsResult);

                //转回到原始位置
                lightResult.ForEach(x => x.TransformBack(transformer));
                ThAFASUtils.TransformReset(transformer, geos);
                blindsResult.ForEach(x => transformer.Reset(x));

                //打印
                if (_floorUpDown == true)
                {
                    lightResult.ForEach(x => x.Dir = new Autodesk.AutoCAD.Geometry.Vector3d(0, 1, 0));
                }

                ThFireAlarmInsertBlk.InsertBlock(lightResult, _scale);
                ThFireAlarmInsertBlk.InsertBlockAngle(stairBlkResult, _scale);
                ThFireAlarmInsertBlk.InsertPolyline(blindsResult, ThIlluminationCommon.Layer_Blind, ThIlluminationCommon.Color_Blind);

            }
        }
    }
}
