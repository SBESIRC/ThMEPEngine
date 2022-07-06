using NFox.Cad;
using AcHelper;
using System;
using System.Linq;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Command;
using ThMEPLighting.Garage.Service;
using System.Collections.Generic;

namespace ThMEPLighting.Command
{
    public class ThRemoveSharpTestComand : ThMEPBaseCommand, IDisposable
    {
        public ThRemoveSharpTestComand()
        {
            CommandName = "BBB";
            ActionName = "AAA";
        }

        public void Dispose()
        {
            //
        }

        public override void SubExecute()
        {
            using (var acadDB = Linq2Acad.AcadDatabase.Active())
            {
                var dxLines = new DBObjectCollection();
                var fdxLines = new DBObjectCollection();
                var per1 = Active.Editor.GetSelection();
                if (per1.Status == PromptStatus.OK)
                {
                    per1.Value.GetObjectIds()
                        .OfType<ObjectId>()
                        .Select(o => acadDB.Element<Entity>(o))
                        .OfType<Line>()
                        .ForEach(o => dxLines.Add(o));
                }
                var per2 = Active.Editor.GetSelection();
                if (per2.Status == PromptStatus.OK)
                {
                    per2.Value.GetObjectIds()
                       .OfType<ObjectId>()
                       .Select(o => acadDB.Element<Entity>(o))
                       .OfType<Line>()
                       .ForEach(o => fdxLines.Add(o));
                }
                if (dxLines.Count > 0)
                {
                    var nodeService = new ThLineNodingService(
                        dxLines.OfType<Line>().ToList(),
                        fdxLines.OfType<Line>().ToList(),
                        new List<Line>());
                    nodeService.Noding();
                    var handler = new ThSharpAngleHandleService(nodeService.DxLines, nodeService.FdxLines, nodeService.SingleRowLines, 2700);
                    handler.Handle();
                    handler.Dxs.OfType<Entity>().ForEach(e =>
                    {
                        acadDB.ModelSpace.Add(e);
                        e.ColorIndex = 5;
                    });
                    handler.Fdxs.OfType<Entity>().ForEach(e =>
                    {
                        acadDB.ModelSpace.Add(e);
                        e.ColorIndex = 6;
                    });
                }
            }
        }
    }
}
