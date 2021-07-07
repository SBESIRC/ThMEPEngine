#if ACAD_ABOVE_2016
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

#if ACAD_ABOVE_2016
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
            }
        }
#else
        public void Execute()
        {

        }
#endif
    }
}
