using AcHelper;
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
using ThMEPWSS.Bussiness;
using ThMEPWSS.Bussiness.LayoutBussiness;
using ThMEPWSS.Model;
using ThMEPWSS.Utils;
using ThWSS;
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

        [CommandMethod("TIANHUACAD", "THGENERATESPRAY", CommandFlags.Modal)]
        public void ThGenerateSpary()
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

                    var filterlist = OpFilter.Bulid(o =>
                    o.Dxf((int)DxfCode.LayerName) == ThWSSCommon.Layout_Line_LayerName &
                    o.Dxf((int)DxfCode.Start) == RXClass.GetClass(typeof(Polyline)).DxfName);

                    var sprayLines = new List<Polyline>();
                    var resLines = Active.Editor.SelectByPolyline(
                    frame,
                    PolygonSelectionMode.Crossing,
                    filterlist);
                    if (result.Status == PromptStatus.OK)
                    {
                        foreach (ObjectId obj in resLines.Value.GetObjectIds())
                        {
                            sprayLines.Add(acdb.Element<Polyline>(obj));
                        }
                    }

                    GenerateSpraysService generateSpraysService = new GenerateSpraysService();
                    generateSpraysService.GenerateSprays(sprayLines);
                }
            }
        }

        [CommandMethod("TIANHUACAD", "THGETBLINDAREA", CommandFlags.Modal)]
        public void ThCreateBlindArea()
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

                    var filterlist = OpFilter.Bulid(o =>
                    o.Dxf((int)DxfCode.LayerName) == ThWSSCommon.SprayLayerName &
                    o.Dxf((int)DxfCode.Start) == RXClass.GetClass(typeof(BlockReference)).DxfName);

                    var sprayLines = new List<BlockReference>();
                    var resLines = Active.Editor.SelectByPolyline(
                    frame,
                    PolygonSelectionMode.Crossing,
                    filterlist);
                    if (resLines.Status == PromptStatus.OK)
                    {
                        foreach (ObjectId obj in resLines.Value.GetObjectIds())
                        {
                            sprayLines.Add(acdb.Element<BlockReference>(obj));
                        }
                    }

                    CalSprayBlindAreaService calSprayBlindAreaService = new CalSprayBlindAreaService();
                    calSprayBlindAreaService.CalSprayBlindArea(sprayLines, plFrame);
                }
            }
        }
    }
}
