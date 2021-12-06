using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;

using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using AcHelper;

using Linq2Acad;
using ThCADCore.NTS;
using ThMEPEngineCore.Command;
using ThMEPEngineCore.IO;

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
        private bool UseUI { get; set; }

        private int _theta = 0;
        private int _floorHight = 2;
        private double _scale = 100;
        private bool _referBeam = true;
        private double _wallThick = 100;

        public ThAFASSmokeCmd(bool UI)
        {
            UseUI = UI;
            InitialCmdInfo();
            InitialSetting();
        }
        private void InitialCmdInfo()
        {
            CommandName = "THFireAlarmSmokeLayout";
            ActionName = "布置";
        }
        private void InitialSetting()
        {
            if (UseUI == true)
            {
                _theta = FireAlarmSetting.Instance.RoofGrade;
                _floorHight = FireAlarmSetting.Instance.RoofHight;
                _scale = FireAlarmSetting.Instance.Scale;
                _referBeam = FireAlarmSetting.Instance.Beam == 1 ? true : false;
                _wallThick = FireAlarmSetting.Instance.RoofThickness;
            }
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
                var cleanBlkName = new List<string>() { ThFaCommon.BlkName_Smoke, ThFaCommon.BlkName_Heat, ThFaCommon.BlkName_Smoke_ExplosionProf, ThFaCommon.BlkName_Heat_ExplosionProf };
                var avoidBlkName = ThFaCommon.BlkNameList.Where(x => cleanBlkName.Contains(x) == false).ToList();
                var layoutBlkNameSmoke = ThFaCommon.BlkName_Smoke;
                var layoutBlkNameHeat = ThFaCommon.BlkName_Heat;
                var layoutBlkNameSmokePrf = ThFaCommon.BlkName_Smoke_ExplosionProf;
                var layoutBlkNameHeatPrf = ThFaCommon.BlkName_Heat_ExplosionProf;

                //导入块图层。free图层
                ThFireAlarmInsertBlk.prepareInsert(extractBlkList, ThFaCommon.blk_layer.Select(x => x.Value).Distinct().ToList());

                var geos = ThAFASUtils.GetSmokeData(pts, extractBlkList, _referBeam, _wallThick, true);
                if (geos.Count == 0)
                {
                    return;
                }

                //转回原点
                var transformer = ThAFASUtils.TransformToOrig(pts, geos);
                //var newPts = new Autodesk.AutoCAD.Geometry.Point3dCollection();
                //newPts.Add(new Autodesk.AutoCAD.Geometry.Point3d());
                //var transformer = ThAFASUtils.transformToOrig(newPts, geos);

                var dataQuery = new ThAFASAreaDataQueryService(geos, cleanBlkName, avoidBlkName);
                //洞,必须先做找到框线
                dataQuery.AnalysisHoles();
                //墙，柱，可布区域，避让
                dataQuery.ClassifyData();
                var priorityExtend = ThAFASUtils.GetPriorityExtendValue(cleanBlkName, _scale);
                dataQuery.ExtendPriority(priorityExtend);
                var roomType = ThFaSmokeRoomTypeService.GetSmokeSensorType(dataQuery.Rooms, dataQuery.RoomFrameDict);

                foreach (var frame in dataQuery.FrameList)
                {
                    DrawUtils.ShowGeometry(frame, string.Format("l0room"), 30);
                    DrawUtils.ShowGeometry(dataQuery.FrameHoleList[frame], string.Format("l0hole"), 140);
                    //DrawUtils.ShowGeometry(dataQuery.FrameLayoutList[frame].Cast<Entity>().ToList(), "l0PlaceCoverage", 200);
                    DrawUtils.ShowGeometry(frame.GetPoint3dAt(0), string.Format("roomType:{0}", roomType[frame].ToString()), "l0roomType", 25, 25, 200);
                }


                var layoutParameter = new ThAFASSmokeLayoutParameter();
                layoutParameter.FloorHightIdx = _floorHight;
                layoutParameter.RootThetaIdx = _theta;
                layoutParameter.Scale = _scale;
                layoutParameter.AisleAreaThreshold = 0.025;
                layoutParameter.BlkNameHeat = layoutBlkNameHeat;
                layoutParameter.BlkNameSmoke = layoutBlkNameSmoke;
                layoutParameter.BlkNameHeatPrf = layoutBlkNameHeatPrf;
                layoutParameter.BlkNameSmokePrf = layoutBlkNameSmokePrf;
                layoutParameter.RoomType = roomType;
                layoutParameter.framePts = pts;
                layoutParameter.transformer = transformer;
                layoutParameter.priorityExtend = priorityExtend;

                //接入楼梯
                var stairBlkResult = ThStairService.LayoutStair(layoutParameter);
                ////

                ThAFASSmokeEngine.ThFaSmokeHeatLayoutEngine(dataQuery, layoutParameter, out var layoutResult, out var blindsResult);

                //转回到原始位置
                layoutResult.ForEach(x => x.TransformBack(transformer));

                //打印
                ThFireAlarmInsertBlk.InsertBlock(layoutResult, _scale);
                ThFireAlarmInsertBlk.InsertBlockAngle(stairBlkResult, _scale);

            }
        }
        private void SettingNoUI()
        {

            _theta = 0;
            _floorHight = 2;

            var beam = Active.Editor.GetInteger("\n不考虑梁（0）考虑梁（1）");
            if (beam.Status != PromptStatus.OK)
            {
                return;
            }
            _referBeam = beam.Value == 1 ? true : false;


            var wallThick = Active.Editor.GetDouble("\n板厚：");
            if (wallThick.Status != PromptStatus.OK)
            {
                return;
            }
            _wallThick = wallThick.Value;

        }

    }
}
