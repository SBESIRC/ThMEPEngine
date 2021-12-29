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
using ThMEPElectrical.AFAS.Model;
using ThMEPElectrical.AFAS.Utils;
using ThMEPElectrical.AFAS.ViewModel;
using ThMEPElectrical.FireAlarmFixLayout.Logic;
using ThMEPElectrical.FireAlarmFixLayout.Data;

namespace ThMEPElectrical.FireAlarmFixLayout.Command
{
    class ThAFASFireTelLayoutCmd : ThMEPBaseCommand, IDisposable
    {
        private double _scale = 100;
        public ThAFASFireTelLayoutCmd()
        {
            InitialCmdInfo();
            InitialSetting();
        }
        private void InitialCmdInfo()
        {
            ActionName = "布置";
            CommandName = "THFATEL";
        }
        private void InitialSetting()
        {
            _scale = FireAlarmSetting.Instance.Scale;
        }

        public override void SubExecute()
        {
            FireAlarmFireTelLayoutExecute();
        }
        public void Dispose()
        {
        }
        private void FireAlarmFireTelLayoutExecute()
        {
            using (var doclock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                ////------------画框，提数据，转数据
                //var pts = ThAFASUtils.GetFrameBlk();
                //if (pts.Count == 0)
                //{
                //    return;
                //}

                //------------
                var transformer = ThAFASDataPass.Instance.Transformer;
                var pts = ThAFASDataPass.Instance.SelectPts;

                //--------------初始图块信息
                var extractBlkList = ThFaCommon.BlkNameList;
                var layoutBlkName = ThFaCommon.BlkName_FireTel;
                var cleanBlkName = ThFaCommon.LayoutBlkList[3];
                var avoidBlkName = ThFaCommon.BlkNameList.Where(x => cleanBlkName.Contains(x) == false).ToList();
                //ThFireAlarmInsertBlk.PrepareInsert(extractBlkList, ThFaCommon.Blk_Layer.Select(x => x.Value).Distinct().ToList());

                //--------------提取数据
                //var geos = ThAFASUtils.GetFixLayoutData(pts, extractBlkList);
                var geos = ThAFASUtils.GetFixLayoutData2(ThAFASDataPass.Instance, extractBlkList);
                if (geos.Count == 0)
                {
                    return;
                }
                //------------转回原点
                //var transformer = ThAFASUtils.TransformToOrig(pts, geos);
                ThAFASUtils.TransformToZero(transformer, geos);

                //--------------处理数据：找洞。分类数据：墙，柱，可布区域，避让。扩大避让。
                var dataQuery = new ThAFASFixDataQueryService(geos, avoidBlkName);
                dataQuery.ExtendEquipment(cleanBlkName, _scale);
                dataQuery.AddAvoidence();
                dataQuery.MapGeometry();

                //------------布置
                ThFireTelFixedPointLayoutService layoutService = null;
                layoutService = new ThFireTelFixedPointLayoutService(dataQuery);
                var results = layoutService.Layout();

                //------------对对结果的重设
                var pairs = new List<KeyValuePair<Point3d, Vector3d>>();
                results.ForEach(p =>
                {
                    var pt = p.Key;
                    transformer.Reset(ref pt);
                    pairs.Add(new KeyValuePair<Point3d, Vector3d>(pt, p.Value));
                });

                //------------对插入真实块
                ThFireAlarmInsertBlk.InsertBlock(pairs, _scale, layoutBlkName, ThFaCommon.Blk_Layer[layoutBlkName], true);
                ThAFASUtils.TransformReset(transformer, geos);
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
