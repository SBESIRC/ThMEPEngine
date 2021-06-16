using System;
using AcHelper.Commands;
using ThMEPEngineCore.GeojsonExtractor;
using NFox.Cad;
using ThMEPWSS.Hydrant.Service;

#if ACAD2016
using CLI;
using AcHelper;
using Linq2Acad;
using System.Linq;
using ThCADExtension;
using ThMEPWSS.FlushPoint;
using ThMEPEngineCore.IO;
using ThMEPEngineCore.Model;
using ThMEPWSS.FlushPoint.Data;
using ThMEPEngineCore.Algorithm;
using System.Collections.Generic;
using ThMEPWSS.FlushPoint.Service;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.DatabaseServices;
#endif

namespace ThMEPWSS.Command
{
    public class ThHydrantProtectionRadiusCmd : IAcadCommand, IDisposable
    {
        public void Dispose()
        {
        }

        public void Execute()
        {
#if ACAD2016
            using (var acadDb = AcadDatabase.Active())
            {
                var per = Active.Editor.GetEntity("\n选择一个范围框");
                if (per.Status != PromptStatus.OK)
                {
                    return;
                }
                var entity = acadDb.Element<Entity>(per.ObjectId);
                if (!(entity is Polyline))
                {
                    return;
                }
                var nFrame = ThMEPFrameService.Normalize(entity as Polyline);
                var pts = nFrame.VerticesEx(100.0);

                //收集数据(建筑墙、剪力墙、柱子、门扇[暂时不支持]、门洞、设备、外部空间提取)
                var extractors = new List<ThExtractorBase>()
                {
                    new ThArchitectureExtractor(){ ColorIndex=1,IsolateSwitch=true},
                    new ThShearwallExtractor(){ ColorIndex=2,IsolateSwitch=true},
                    new ThColumnExtractor(){ ColorIndex=3,IsolateSwitch=true},
                    new ThDoorOpeningExtractor(){ ColorIndex=4},
                };
                extractors.ForEach(o => o.Extract(acadDb.Database, pts));

                var geos = new List<ThGeometry>();
                extractors.ForEach(o => geos.AddRange(o.BuildGeometries()));
            }
#endif
        }

        public void Test()
        {
            using (var acadDb = AcadDatabase.Active())
            {
                var per = Active.Editor.GetEntity("\n选择一个范围框");
                if (per.Status != PromptStatus.OK)
                {
                    return;
                }
                var roomEntity = acadDb.Element<Entity>(per.ObjectId);

                var psr = Active.Editor.GetSelection();
                if (psr.Status != PromptStatus.OK)
                {
                    return;
                }
                var protectAreas = psr.Value.GetObjectIds().Cast<ObjectId>().Select(o => acadDb.Element<Polyline>(o)).ToList();

                var service = new ThDivideRoomService(roomEntity, protectAreas);
                service.Divide();
                service.Print(acadDb.Database);
            }
        }

#if ACAD2016
        private void BuildHydrantParam()
        {
            
        }
#endif
    }
}
