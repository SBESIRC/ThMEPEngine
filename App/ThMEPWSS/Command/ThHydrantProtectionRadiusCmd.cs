#if (ACAD2016 || ACAD2018)
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
                var frame = ThWindowInteraction.GetPolyline(PointCollector.Shape.Window, new List<string> { "请框选一个范围" });
                if (frame.Area <= 1e-4)
                {
                    return;
                }
                var nFrame = ThMEPFrameService.Normalize(frame);
                var pts = nFrame.Vertices();

                ICheck checkService = null;
                ThStopWatchService.Start();
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
                ThStopWatchService.Reset();
                ThStopWatchService.ReStart();
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
                AcHelper.Active.Editor.WriteMessage("\n校核耗时：" + ThStopWatchService.TimeSpan() + "秒");
                ThStopWatchService.Reset();
                AcHelper.Active.Editor.WriteMessage("\n执行完成！");
            }
        }
#else
        public void Execute()
        {

        }
#endif
    }
}
