﻿#if (ACAD2016 || ACAD2018)
using AcHelper;
using Linq2Acad;
using System.Linq;
using ThCADExtension;
using ThMEPEngineCore.CAD;
using ThMEPWSS.Hydrant.Model;
using ThMEPWSS.Hydrant.Service;
using ThMEPEngineCore.Algorithm;
using System.Collections.Generic;
#endif

using System;
using AcHelper.Commands;
using ThMEPWSS.ViewModel;
using ThMEPEngineCore.Diagnostics;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPWSS.Command
{
    public class ThHydrantProtectionRadiusCmd : IAcadCommand, IDisposable
    {
        public static ThFireHydrantVM FireHydrantVM { get; set; }
        public void Dispose()
        {
        }

#if (ACAD2016 || ACAD2018)
        public void Execute()
        {
            using (var lockDoc = Active.Document.LockDocument())
            using (var acadDb = AcadDatabase.Active())
            {              
                var frame = SelectFrame(acadDb);
                if (frame.Area <= 1e-4)
                {
                    return;
                }
                var nFrame = ThMEPFrameService.Normalize(frame);
                var pts = nFrame.Vertices();

                ICheck checkService = null;
                if (FireHydrantVM.Parameter.CheckObjectOption== CheckObjectOps.FireHydrant)
                {
                    checkService = new ThCheckFireHydrantService(FireHydrantVM);
                }
                else
                {
                    checkService = new ThCheckFireExtinguisherService(FireHydrantVM);
                }
                checkService.Check(acadDb.Database, pts);
                checkService.Print(acadDb.Database); //仅供测试用，后续删除

                // 校核
                ThStopWatchService.Start();
                var regionCheckService = new ThCheckRegionService()
                {
                    Covers = checkService.Covers.SelectMany(o=>o.Item3).ToList(),
                    Rooms = checkService.Rooms,
                    IsSingleStrands = FireHydrantVM.Parameter.GetProtectStrength,
                };
                regionCheckService.Check(); 

                //输出
                var printService = new ThHydrantPrintService(
                    acadDb.Database,
                    ThCheckExpressionControlService.CheckExpressionLayer);
                printService.Print(regionCheckService.CheckResults);
                ThStopWatchService.Stop();
                ThStopWatchService.Print("校核耗时：");
            }
        }
        private Polyline SelectFrame(AcadDatabase acadDb)
        {
            var frame = new Polyline();
            var options = new PromptKeywordOptions("\n选择区域");
            options.Keywords.Add("框选矩形区域", "K", "框选矩形区域(K)");
            options.Keywords.Add("使用已绘区域", "P", "使用已绘区域(P)");
            options.Keywords.Default = "框选矩形区域";
            var keyRes = Active.Editor.GetKeywords(options);
            if (keyRes.Status != PromptStatus.OK)
            {
                return frame;
            }
            if (keyRes.StringResult == "框选矩形区域")
            {
                frame = ThWindowInteraction.GetPolyline(
                    PointCollector.Shape.Window, new List<string> { "请框选一个范围" });
            }
            else
            {
                var per = Active.Editor.GetEntity("\n请框选一个框");
                if (per.Status != PromptStatus.OK)
                {
                    return frame;
                }
                var entity = acadDb.Element<Entity>(per.ObjectId);
                if(entity is Polyline)
                {
                    frame = entity as Polyline;
                }
            }
            return frame;
        }
#else
        public void Execute()
        {

        }
#endif
    }
}
