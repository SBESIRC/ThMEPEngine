using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;

using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using AcHelper;
using Serilog;
using Serilog.Core;
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
        private int _theta = 0;
        private int _floorHight = 2;
        private double _scale = 100;
        private bool _referBeam = true;
        private double _wallThick = 100;
        private bool _needDetective = true;

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
            _wallThick = FireAlarmSetting.Instance.RoofThickness;
            _needDetective = _referBeam == true ? true : false;
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

            var wallThick = Active.Editor.GetDouble("\n板厚");
            if (wallThick.Status != PromptStatus.OK)
            {
                return;
            }
            _wallThick = wallThick.Value;
            _needDetective = _referBeam == true ? true : false;
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
                ////--------------画框，提数据，转数据
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
                var cleanBlkName = ThFaCommon.LayoutBlkList[(int)ThFaCommon.LayoutItemType.Smoke];
                var avoidBlkName = ThFaCommon.BlkNameList.Where(x => cleanBlkName.Contains(x) == false).ToList();
                var layoutBlkNameSmoke = ThFaCommon.BlkName_Smoke;
                var layoutBlkNameHeat = ThFaCommon.BlkName_Heat;
                var layoutBlkNameSmokePrf = ThFaCommon.BlkName_Smoke_ExplosionProf;
                var layoutBlkNameHeatPrf = ThFaCommon.BlkName_Heat_ExplosionProf;
                //ThFireAlarmInsertBlk.prepareInsert(extractBlkList, ThFaCommon.blk_layer.Select(x => x.Value).Distinct().ToList());

                //--------------提取数据
                //var geos = ThAFASUtils.GetAreaLayoutData(pts,extractBlkList, _referBeam, _wallThick, _needDetective);
                var geos = ThAFASUtils.GetAreaLayoutData2(ThAFASDataPass.Instance, extractBlkList, _referBeam, _wallThick, _needDetective);
                if (geos.Count == 0)
                {
                    return;
                }

                //--------------数据转回原点
                //var transformer = ThAFASUtils.TransformToOrig(pts, geos);
                ////var newPts = new Autodesk.AutoCAD.Geometry.Point3dCollection();
                ////newPts.Add(new Autodesk.AutoCAD.Geometry.Point3d());
                ////var transformer = ThAFASUtils.TransformToOrig(newPts, geos);
                ThAFASUtils.TransformToZero(transformer, geos);

                //--------------处理数据：找洞。分类数据：墙，柱，可布区域，避让。扩大避让。定义房间名称类型
                //var dataQuery = new ThAFASAreaDataQueryService(geos, cleanBlkName, avoidBlkName);
                var dataQuery = new ThAFASAreaDataQueryService(geos, avoidBlkName);
                //dataQuery.AnalysisHoles();
                //dataQuery.ClassifyData();//先分房间再扩大
                dataQuery.AddMRoomDict();
                dataQuery.ClassifyDataNew();
                var priorityExtend = ThAFASUtils.GetPriorityExtendValue(cleanBlkName, _scale);
                dataQuery.ExtendPriority(priorityExtend);
                var roomType = ThFaSmokeRoomTypeService.GetSmokeSensorType(dataQuery.Rooms, dataQuery.RoomFrameDict);

                //--------------定义传数据
                //string LogFileName = Path.Combine(System.IO.Path.GetTempPath(), Active.DocumentName + ".log");
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

                //--------------打印最终图块
                ThFireAlarmInsertBlk.InsertBlock(layoutResult, _scale);
                ThFireAlarmInsertBlk.InsertBlockAngle(stairBlkResult, _scale);
            }
        }
    }
}
