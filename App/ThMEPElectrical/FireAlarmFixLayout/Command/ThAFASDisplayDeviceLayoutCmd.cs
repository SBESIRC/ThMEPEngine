using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading.Tasks;

using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

using AcHelper;
using Linq2Acad;

using ThCADExtension;
using Autodesk.AutoCAD.EditorInput;
using ThMEPEngineCore.Command;
using ThMEPEngineCore.IO;

using ThMEPElectrical.AFAS;
using ThMEPElectrical.AFAS.Utils;
using ThMEPElectrical.AFAS.ViewModel;
using ThMEPElectrical.FireAlarmFixLayout.Data;
using ThMEPElectrical.FireAlarmFixLayout.Logic;

namespace ThMEPElectrical.FireAlarmFixLayout.Command
{
    class ThAFASDisplayDeviceLayoutCmd : ThMEPBaseCommand, IDisposable
    {
        private bool UseUI { get; set; }
        private BuildingType _buildingType = FireAlarmFixLayout.Data.BuildingType.None;
        private string layoutBlkName = ThFaCommon.BlkName_Display_District;
        private double _scale = 100;

        public ThAFASDisplayDeviceLayoutCmd(bool UI)
        {
            UseUI = UI;
            InitialCmdInfo();
            InitialSetting();
        }
        private void InitialCmdInfo()
        {
            CommandName = "ThFireAlarmDisplayDeviceLayout";
            ActionName = "布置";
        }
        private void InitialSetting()
        {
            if (UseUI == true)
            {
                _buildingType = (BuildingType)FireAlarmSetting.Instance.DisplayBuilding;
                layoutBlkName = FireAlarmSetting.Instance.DisplayBlk == 0 ? ThFaCommon.BlkName_Display_Floor : ThFaCommon.BlkName_Display_District;
                _scale = FireAlarmSetting.Instance.Scale;
            }
        }
        public override void SubExecute()
        {
            FireAlarmDisplayDeviceLayoutExecute();
        }
        public void Dispose()
        {
        }
        private void FireAlarmDisplayDeviceLayoutExecute()
        {
            if (_buildingType == BuildingType.None)
            {
                return;
            }
            using (var doclock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                //画框，提数据，转数据
                //var pts = ThAFASUtils.GetFrame();
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
                var cleanBlkName = new List<string>() { ThFaCommon.BlkName_Display_District, ThFaCommon.BlkName_Display_Floor };
                var avoidBlkName = ThFaCommon.BlkNameList.Where(x => cleanBlkName.Contains(x) == false).ToList();

                //导入块图层。free图层
                ThFireAlarmInsertBlk.prepareInsert(extractBlkList, ThFaCommon.blk_layer.Select(x => x.Value).Distinct().ToList());

                var geos = ThAFASUtils.GetFixLayoutData(pts, extractBlkList);
                if (geos.Count == 0)
                {
                    return;
                }

                //转回原点
                var transformer = ThAFASUtils.TransformToOrig(pts, geos);
                //var newPts = new Autodesk.AutoCAD.Geometry.Point3dCollection();
                //newPts.Add(new Autodesk.AutoCAD.Geometry.Point3d());
                //var transformer = ThAFASUtils.transformToOrig(newPts, geos);

                //布置
                ThFixedPointLayoutService layoutService = null;
                layoutService = new ThDisplayDeviceFixedPointLayoutService(geos, cleanBlkName, avoidBlkName)
                {
                    BuildingType = _buildingType,
                };

                var results = layoutService.Layout();

                // 对结果的重设
                var pairs = new List<KeyValuePair<Point3d, Vector3d>>();
                results.ForEach(p =>
                {
                    var pt = p.Key;
                    transformer.Reset(ref pt);
                    pairs.Add(new KeyValuePair<Point3d, Vector3d>(pt, p.Value));
                });

                //插入真实块
                ThFireAlarmInsertBlk.InsertBlock(pairs, _scale, layoutBlkName, ThFaCommon.blk_layer[layoutBlkName], true);

                ////Print
                //pairs.ForEach(p =>
                //{
                //    var circlePoint = new Circle(p.Key, Vector3d.ZAxis, 50.0);
                //    var circleArea = new Circle(p.Key, Vector3d.ZAxis, 8500.0);
                //    var line = new Line(p.Key, p.Key + p.Value.GetNormal().MultiplyBy(200));
                //    var ents1 = new List<Entity>() { circlePoint, line };
                //    var ents2 = new List<Entity>() { circleArea };
                //    ThMEPEngineCore.CAD.ThAuxiliaryUtils.CreateGroup(ents1, acadDatabase.Database, 1);
                //    ThMEPEngineCore.CAD.ThAuxiliaryUtils.CreateGroup(ents2, acadDatabase.Database, 3);
                //});
                ////pairs.ForEach(x => FireAlarm.Service.DrawUtils.ShowGeometry(x.Key, x.Value, "l0result", 1, 40, 200));
            }
        }
        private void SettingNoUI()
        {
            // select an option
            string strResident = "住宅";
            string strPublic = "公建";

            var options = new PromptKeywordOptions("");
            options.Message = "\nPlease select option:";
            options.Keywords.Add(strResident, "R", "住宅(R)");
            options.Keywords.Add(strPublic, "P", "公建(P)");

            var rst = Active.Editor.GetKeywords(options);
            if (rst.Status != PromptStatus.OK)
                return;

            if (rst.StringResult.Equals(strResident))
            {
                _buildingType = FireAlarmFixLayout.Data.BuildingType.Resident;
            }
            else if (rst.StringResult.Equals(strPublic))
            {
                _buildingType = FireAlarmFixLayout.Data.BuildingType.Public;
            }
            else return;
        }

    }
}
