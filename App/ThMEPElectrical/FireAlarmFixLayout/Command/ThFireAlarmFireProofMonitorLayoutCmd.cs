﻿using System;
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


    public class ThFireAlarmFireProofMonitorLayoutCmdNoUI
    {
        [CommandMethod("TIANHUACAD", "ThFireProofMonitor", CommandFlags.Modal)]
        public void ThFireAlarmFireProofMonitorLayoutCmd()
        {
            using (var cmd = new ThFireAlarmFireProofMonitorLayoutCmd())
            {
                cmd.Execute();
            }
        }
    }

    class ThFireAlarmFireProofMonitorLayoutCmd : ThMEPBaseCommand, IDisposable
    {
        readonly FireAlarmViewModel _UiConfigs;
        private double _scale = 100;

        public ThFireAlarmFireProofMonitorLayoutCmd(FireAlarmViewModel uiConfigs)
        {
            _UiConfigs = uiConfigs;
            CommandName = "THFireAlarmProofMonitorLayout";
            ActionName = "生成";
            setInfo();
        }
        public ThFireAlarmFireProofMonitorLayoutCmd()
        {

        }
        public override void SubExecute()
        {
            FireAlarmFireProofMonitorLayoutExecute();
        }
        private void setInfo()
        {
            if (_UiConfigs != null)
            {
                _scale = _UiConfigs.BlockRatioIndex == 0 ? 100 : 150;
            }
        }
        public void Dispose()
        {
        }
        private void FireAlarmFireProofMonitorLayoutExecute()
        {
            using (var doclock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var extractBlkList = ThFaCommon.BlkNameListFixLayout;
                var cleanBlkName = new List<string>() { ThFaCommon.BlkName_Monitor };
                var avoidBlkName = ThFaCommon.BlkNameListFixLayout.Where(x => cleanBlkName.Contains(x) == false).ToList();
                var layoutBlkName = ThFaCommon.BlkName_Monitor;//to do： from UI

                //画框，提数据，转数据
                var pts = ThFireAlarmUtils.getFrame();
                if (pts.Count == 0)
                {
                    return;
                }
                var geos = ThFireAlarmUtils.getFixLayoutData(pts, extractBlkList);
                if (geos.Count == 0)
                {
                    return;
                }
                var transformer = ThFireAlarmUtils.transformToOrig(pts, geos);

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
