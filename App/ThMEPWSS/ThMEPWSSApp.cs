﻿using AcHelper;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using Linq2Acad;
using NFox.Cad;
using System.Collections.Generic;
using System.Linq;
using ThCADExtension;
using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore.Engine;
using ThMEPWSS.Bussiness.LayoutBussiness;
using ThMEPWSS.Model;
using ThMEPWSS.Utils;
using ThWSS.Bussiness;

namespace ThMEPWSS
{
    public class ThMEPWSSApp : IExtensionApplication
    {
        public void Initialize()
        {
            //throw new System.NotImplementedException();
        }

        public void Terminate()
        {
            //throw new System.NotImplementedException();
        }

        [CommandMethod("TIANHUACAD", "THPTLAYOUT", CommandFlags.Modal)]
        public void ThPTLayout()
        {
            PromptSelectionOptions options = new PromptSelectionOptions()
            {
                AllowDuplicates = false,
                MessageForAdding = "选择区域",
                RejectObjectsOnLockedLayers = true,
            };
            var dxfNames = new string[]
            {
                RXClass.GetClass(typeof(Polyline)).DxfName,
            };
            var filter = ThSelectionFilterTool.Build(dxfNames);
            var result = Active.Editor.GetSelection(options, filter);
            if (result.Status != PromptStatus.OK)
            {
                return;
            }

            using (AcadDatabase acdb = AcadDatabase.Active())
            {
                foreach (ObjectId frame in result.Value.GetObjectIds())
                {
                    var plBack = acdb.Element<Polyline>(frame);
                    var plFrame = ThMEPFrameService.Normalize(plBack);

                    var columnEngine = new ThColumnRecognitionEngine();
                    columnEngine.Recognize(acdb.Database, plFrame.Vertices());
                    var columPoly = columnEngine.Elements.Select(o => o.Outline).Cast<Polyline>().ToList();

                    RayLayoutService layoutDemo = new RayLayoutService();
                    var sprayPts = layoutDemo.LayoutSpray(plFrame, columPoly);
                }
            }
        }
    }
}
