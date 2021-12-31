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
using ThMEPElectrical.FireAlarmArea;
using ThMEPElectrical.FireAlarmArea.Data;

using ThMEPLighting.Lighting.ViewModels;
using ThMEPLighting.IlluminationLighting.Model;
using ThMEPLighting.IlluminationLighting.Service;

namespace ThMEPLighting.IlluminationLighting
{
    public class IlluminationLightingCmd : ThMEPBaseCommand, IDisposable
    {
        readonly LightingViewModel _UiConfigs;

        private LightTypeEnum _lightType = LightTypeEnum.circleCeiling;
        private double _scale = 100;
        private bool _referBeam = true;
        private double _radiusN = 3000;
        private double _radiusE = 6000;
        private bool _ifLayoutEmg = true;
        private bool _ifEmgAsNormal = false;
        private double _wallThick = 0;

        public IlluminationLightingCmd(LightingViewModel uiConfigs)
        {
            _UiConfigs = uiConfigs;
            InitialCmdInfo();
            InitialSetting();
        }

        public IlluminationLightingCmd()
        {
            InitialCmdInfo();
        }
        private void InitialCmdInfo()
        {
            CommandName = "THZM";
            ActionName = "照明灯具布置";
        }

        private void InitialSetting()
        {
            if (_UiConfigs != null)
            {
                _scale = _UiConfigs.ScaleSelectIndex == 0 ? 100 : 150;
                _lightType = _UiConfigs.LightingType;
                _radiusN = _UiConfigs.RadiusNormal;
                _radiusE = _UiConfigs.RadiusEmg;
                _referBeam = _UiConfigs.ShouldConsiderBeam;
                _ifLayoutEmg = _UiConfigs.IfLayoutEmgChecked;
                _ifEmgAsNormal = _UiConfigs.IfEmgUsedForNormal;
            }
            else
            {
                SettingNoUI();
            }
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
                ////画框，提数据，转数据
                //var pts = ThAFASUtils.GetFrameBlk();
                //if (pts.Count == 0)
                //{
                //    return;
                //}

                var transformer = ThAFASDataPass.Instance.Transformer;
                var pts = ThAFASDataPass.Instance.SelectPts;

                //--------------初始图块信息
                var extractBlkList = ThFaCommon.BlkNameList;
                var cleanBlkName = new List<string>() { ThFaCommon.BlkName_CircleCeiling,
                                                        ThFaCommon.BlkName_DomeCeiling,
                                                        ThFaCommon.BlkName_InductionCeiling,
                                                        ThFaCommon.BlkName_Downlight,
                                                       };
                if (_ifLayoutEmg)
                {
                    cleanBlkName.Add(ThFaCommon.BlkName_EmergencyLight);
                }

                var avoidBlkName = ThFaCommon.BlkNameList.Where(x => cleanBlkName.Contains(x) == false).ToList();
                var layoutBlkNameN = ThIlluminationCommon.lightTypeDict[_lightType];
                var layoutBlkNameE = ThFaCommon.BlkName_EmergencyLight;
                //ThFireAlarmInsertBlk.PrepareInsert(extractBlkList, ThFaCommon.Blk_Layer.Select(x => x.Value).Distinct().ToList());

                //--------------提取数据
                //var geos = ThAFASUtils.GetAreaLayoutData(pts, extractBlkList, _referBeam, _wallThick, false);
                var geos = ThAFASUtils.GetAreaLayoutData2(ThAFASDataPass.Instance, extractBlkList, _referBeam, _wallThick, false);
                if (geos.Count == 0)
                {
                    return;
                }

                //--------------数据转回原点
                //var transformer = ThAFASUtils.TransformToOrig(pts, geos);
                ThAFASUtils.TransformToZero(transformer, geos);

                //--------------处理数据：找洞。分类数据：墙，柱，可布区域，避让。扩大避让。定义房间名称类型
                var dataQuery = new ThAFASAreaDataQueryService(geos, avoidBlkName);
                //洞,必须先做找到框线
                //dataQuery.AnalysisHoles();
                //dataQuery.ClassifyData();
                dataQuery.AddMRoomDict();
                dataQuery.ClassifyDataNew();//先分房间再扩大
                var priorityExtend = ThAFASUtils.GetPriorityExtendValue(cleanBlkName, _scale);
                dataQuery.ExtendPriority(priorityExtend);
                var roomType = ThFaIlluminationRoomTypeService.GetIllunimationType(dataQuery.Rooms, dataQuery.RoomFrameDict);

                //foreach (var frame in dataQuery.FrameList)
                //{
                //    DrawUtils.ShowGeometry(frame, string.Format("l0room"), 30);
                //    DrawUtils.ShowGeometry(dataQuery.FrameHoleList[frame], string.Format("l0hole"), 140);
                //    DrawUtils.ShowGeometry(dataQuery.FrameLayoutList[frame].Cast<Entity>().ToList(), "l0PlaceCoverage", 200);
                //}


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


                //接入楼梯
                var stairBlkResult = ThIlluminationStairService.LayoutStair(layoutParameter);
                ////

                ThAFASIlluminationEngine.ThFaIlluminationLayoutEngine(dataQuery, layoutParameter, out var lightResult, out var blindsResult);

                //转回到原始位置
                lightResult.ForEach(x => x.TransformBack(transformer));
                ThAFASUtils.TransformReset(transformer, geos);

                //打印
                ThFireAlarmInsertBlk.InsertBlock(lightResult, _scale);
                ThFireAlarmInsertBlk.InsertBlockAngle(stairBlkResult, _scale);

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

            var radiusN = Active.Editor.GetInteger("\n正常照明灯具布置半径(mm)");
            if (radiusN.Status != PromptStatus.OK)
            {
                return;
            }
            _radiusN = radiusN.Value;

            var radiusE = Active.Editor.GetInteger("\n应急照明灯具布置半径(mm)");
            if (radiusE.Status != PromptStatus.OK)
            {
                return;
            }
            _radiusE = radiusE.Value;

            var layoutEmg = Active.Editor.GetInteger("\n布置应急照明灯 否（0）是（1）");
            if (layoutEmg.Status != PromptStatus.OK)
            {
                return;
            }
            _ifLayoutEmg = layoutEmg.Value == 1 ? true : false;

        }

    }
}
