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
using NFox.Cad;
using Dreambuild.AutoCAD;

using ThCADExtension;
using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore.Model;
using Autodesk.AutoCAD.EditorInput;
using ThMEPEngineCore.Command;

using ThMEPElectrical.Command;
using ThMEPElectrical.FireAlarmFixLayout.Data;
using ThMEPElectrical.FireAlarmFixLayout.Logic;
using ThMEPElectrical.FireAlarmFixLayout;

using ThMEPElectrical.FireAlarm.Service;
using ThMEPElectrical.FireAlarm.ViewModels;
using ThMEPElectrical.FireAlarm;


namespace ThMEPElectrical.FireAlarmFixLayout.Command
{

    public class ThFireAlarmDisplayDeviceLayoutCmdNoUI
    {
        [CommandMethod("TIANHUACAD", "THFireAlarmData", CommandFlags.Modal)]
        public void THFireAlarmData()
        {
            //把Cad图纸数据写出到Geojson File中
            using (var cmd = new ThFireAlarmCommand())
            {
                cmd.Execute();
            }
        }

        [CommandMethod("TIANHUACAD", "ThDisplayDevice", CommandFlags.Modal)]
        public void ThFireAlarmDisplayDeviceLayoutCmd()
        {
            using (var cmd = new ThFireAlarmDisplayDeviceLayoutCmd())
            {
                cmd.Execute();
            }
        }
    }

    class ThFireAlarmDisplayDeviceLayoutCmd : ThMEPBaseCommand, IDisposable
    {
        readonly FireAlarmViewModel _UiConfigs;
        private BuildingType buildingType = FireAlarmFixLayout.Data.BuildingType.None;
        private string layoutBlkName = ThFaCommon.BlkName_Display_Fire;
        private double _scale = 100;
        public ThFireAlarmDisplayDeviceLayoutCmd(FireAlarmViewModel uiConfigs)
        {
            _UiConfigs = uiConfigs;
            CommandName = "THFireAlarmDisplayDeviceLayout";
            ActionName = "生成";
            SetInfo();
        }
        public ThFireAlarmDisplayDeviceLayoutCmd()
        {
            SetInfoNoUI();
        }

        public override void SubExecute()
        {
            FireAlarmDisplayDeviceLayoutExecute();
        }
        private void SetInfo()
        {
            if (_UiConfigs != null)
            {
                if (_UiConfigs.IsResidentChecked == true)
                {
                    buildingType = FireAlarmFixLayout.Data.BuildingType.Resident;
                }
                else
                {
                    buildingType = FireAlarmFixLayout.Data.BuildingType.Public;
                }

                if (_UiConfigs.IsFLChecked == true)

                {
                    layoutBlkName = ThFaCommon.BlkName_Display_Floor;
                }
                else
                {
                    layoutBlkName = ThFaCommon.BlkName_Display_Fire;
                }
                _scale = _UiConfigs.BlockRatioIndex == 0 ? 100 : 150;

            }
        }

        private void SetInfoNoUI()
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
                buildingType = FireAlarmFixLayout.Data.BuildingType.Resident;
            }
            else if (rst.StringResult.Equals(strPublic))
            {
                buildingType = FireAlarmFixLayout.Data.BuildingType.Public;
            }
            else return;
        }

        public void Dispose()
        {
        }
        private void FireAlarmDisplayDeviceLayoutExecute()
        {
            if (buildingType == FireAlarmFixLayout.Data.BuildingType.None)
            {
                return;
            }
            using (var doclock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var extractBlkList = ThFaCommon.BlkNameList;
                var cleanBlkName = new List<string>() { ThFaCommon.BlkName_Display_Fire, ThFaCommon.BlkName_Display_Floor };
                var avoidBlkName = ThFaCommon.BlkNameList.Where(x => cleanBlkName.Contains(x) == false).ToList();

                //导入块图层。free图层
                ThFireAlarmInsertBlk.prepareInsert(extractBlkList, ThFaCommon.blk_layer.Select(x => x.Value).Distinct().ToList());

                //画框，提数据，转数据
                //var pts = ThFireAlarmUtils.GetFrame();
                var pts = ThFireAlarmUtils.GetFrameBlk();
                if (pts.Count == 0)
                {
                    return;
                }
                var geos = ThFireAlarmUtils.GetFixLayoutData(pts, extractBlkList);
                if (geos.Count == 0)
                {
                    return;
                }

                //转回原点
                var transformer = ThFireAlarmUtils.TransformToOrig(pts, geos);
                //var newPts = new Autodesk.AutoCAD.Geometry.Point3dCollection();
                //newPts.Add(new Autodesk.AutoCAD.Geometry.Point3d());
                //var transformer = ThFireAlarmUtils.transformToOrig(newPts, geos);

                //布置
                ThFixedPointLayoutService layoutService = null;
                layoutService = new ThDisplayDeviceFixedPointLayoutService(geos, cleanBlkName, avoidBlkName)
                {
                    BuildingType = buildingType,
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
                ThFireAlarmInsertBlk.InsertBlock(pairs, _scale, layoutBlkName, ThFaCommon.blk_layer[layoutBlkName],true);

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
    }
}
