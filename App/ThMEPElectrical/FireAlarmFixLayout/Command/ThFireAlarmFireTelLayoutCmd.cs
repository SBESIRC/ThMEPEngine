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
    public class ThFireAlarmFireTelLayoutCmdNoUI
    {
        [CommandMethod("TIANHUACAD", "ThFireTel", CommandFlags.Modal)]
        public void ThFireAlarmFireTelLayoutCmd()
        {
            using (var cmd = new ThFireAlarmFireTelLayoutCmd())
            {
                cmd.Execute();
            }
        }
    }

    class ThFireAlarmFireTelLayoutCmd : ThMEPBaseCommand, IDisposable
    {
        readonly FireAlarmViewModel _UiConfigs;
        private double _scale = 100;
        public ThFireAlarmFireTelLayoutCmd(FireAlarmViewModel uiConfigs)
        {
            _UiConfigs = uiConfigs;
            CommandName = "THFireAlarmFireTelLayout";
            ActionName = "生成";
            SetInfo();
        }
        public ThFireAlarmFireTelLayoutCmd()
        {

        }
        public override void SubExecute()
        {
            FireAlarmFireTelLayoutExecute();
        }
        private void SetInfo()
        {
            if (_UiConfigs != null)
            {
                _scale = _UiConfigs.BlockRatioIndex == 0 ? 100 : 150;
            }
        }
        public void Dispose()
        {
        }
        private void FireAlarmFireTelLayoutExecute()
        {
            using (var doclock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var extractBlkList = ThFaCommon.BlkNameList;
                var cleanBlkName = new List<string>() { ThFaCommon.BlkName_FireTel };
                var avoidBlkName = ThFaCommon.BlkNameList.Where(x => cleanBlkName.Contains(x) == false).ToList();
                var layoutBlkName = ThFaCommon.BlkName_FireTel;

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
                var transformer = ThFireAlarmUtils.TransformToOrig(pts, geos);

                ThFixedPointLayoutService layoutService = null;
                layoutService = new ThFireTelFixedPointLayoutService(geos, cleanBlkName, avoidBlkName);

                var results = layoutService.Layout();

                // 对结果的重设
                var pairs = new List<KeyValuePair<Point3d, Vector3d>>();
                results.ForEach(p =>
                {
                    var pt = p.Key;
                    transformer.Reset(ref pt);
                    pairs.Add(new KeyValuePair<Point3d, Vector3d>(pt, p.Value));
                });

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
