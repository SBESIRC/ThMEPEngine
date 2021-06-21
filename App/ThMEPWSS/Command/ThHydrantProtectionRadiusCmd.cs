using System;
using AcHelper;
using Linq2Acad;
using System.Linq;
using ThCADExtension;
using AcHelper.Commands;
using ThMEPEngineCore.CAD;
using ThMEPWSS.Hydrant.Service;
using ThMEPEngineCore.Algorithm;
using ThMEPWSS.Diagram.ViewModel;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPWSS.Command
{
    public class ThHydrantProtectionRadiusCmd : IAcadCommand, IDisposable
    {
        public static ThFireHydrantVM FireHydrantVM { get; set; }
        public void Dispose()
        {
        }

        public void Execute()
        {
#if ACAD2016
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
                if (FireHydrantVM.Parameter.CheckFireHydrant)
                {
                    checkService = new ThCheckFireHydrantService(FireHydrantVM);
                }
                else
                {
                    checkService = new ThCheckFireExtinguisherService(FireHydrantVM);
                }
                checkService.Check(acadDb.Database, pts);

#if DEBUG
                // 打印倪同学的保护区域(后续删除)
                checkService.Covers
                    .Select(o => o.Clone() as Entity)
                    .ToList()
                    .CreateGroup(acadDb.Database, 5);
#endif
                // 校核
                var regionCheckService = new ThCheckRegionService()
                {
                    Covers = checkService.Covers,
                    Rooms = checkService.Rooms,
                    IsSingleStrands = FireHydrantVM.Parameter.GetProtectStrength,
                };
                regionCheckService.Check();

                //输出
                var printService = new ThHydrantPrintService(
                    acadDb.Database,
                    ThCheckExpressionControlService.CheckExpressionLayer);
                printService.Print(regionCheckService.CheckResults);
            }
        }
#endif
    }

}