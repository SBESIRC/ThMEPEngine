using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading.Tasks;

using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

using Linq2Acad;
using ThMEPEngineCore.Command;

using ThMEPElectrical.AFAS;
using ThMEPElectrical.AFAS.Utils;
using ThMEPElectrical.AFAS.ViewModel;

using ThMEPElectrical.FireAlarmFixLayout.Logic;

namespace ThMEPElectrical.FireAlarmFixLayout.Command
{
    class ThAFASFireProofMonitorLayoutCmd : ThMEPBaseCommand, IDisposable
    {
        private bool UseUI { get; set; }
        private double _scale = 100;

        public ThAFASFireProofMonitorLayoutCmd(bool UI)
        {
            UseUI = UI;
            InitialCmdInfo();
            InitialSetting();
        }
        private void InitialCmdInfo()
        {
            ActionName = "布置";
            CommandName = "THFAMONITOR";
        }
        private void InitialSetting()
        {
            if (UseUI == true)
            {
                _scale = FireAlarmSetting.Instance.Scale;
            }
        }

        public override void SubExecute()
        {
            FireAlarmFireProofMonitorLayoutExecute();
        }
        
        public void Dispose()
        {
        }
        private void FireAlarmFireProofMonitorLayoutExecute()
        {
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

                var extractBlkList = ThFaCommon.BlkNameList;
                var cleanBlkName = new List<string>() { ThFaCommon.BlkName_Monitor };
                var avoidBlkName = ThFaCommon.BlkNameList.Where(x => cleanBlkName.Contains(x) == false).ToList();
                var layoutBlkName = ThFaCommon.BlkName_Monitor;//to do： from UI

                //导入块图层。free图层
                ThFireAlarmInsertBlk.prepareInsert(extractBlkList, ThFaCommon.blk_layer.Select(x => x.Value).Distinct().ToList());

                var geos = ThAFASUtils.GetFixLayoutData(pts, extractBlkList);
                if (geos.Count == 0)
                {
                    return;
                }
                var transformer = ThAFASUtils.TransformToOrig(pts, geos);

                ThFixedPointLayoutService layoutService = null;
                layoutService = new ThFireProofMonitorFixedPointLayoutService(geos, cleanBlkName, avoidBlkName);

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
            }
        }
    }
}
