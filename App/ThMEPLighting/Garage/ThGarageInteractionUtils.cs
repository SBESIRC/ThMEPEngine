using AcHelper;
using NFox.Cad;
using Linq2Acad;
using System.Linq;
using Dreambuild.AutoCAD;
using System.Collections.Generic;
using ThCADExtension;
using ThMEPLighting.Common;
using ThMEPEngineCore.Algorithm;
using ThMEPLighting.Garage.Model;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.LaneLine;
using ThMEPLighting.Garage.Service;

namespace ThMEPLighting.Garage
{
    public static class ThGarageInteractionUtils
    {
        public static List<ThRegionLightEdge> GetFireRegionLights()
        {
            using (AcadDatabase acdb = AcadDatabase.Active())
            {
                var options = new PromptSelectionOptions()
                {
                    AllowDuplicates = false,
                    MessageForAdding = "\n请选择布灯的区域框线",
                    RejectObjectsOnLockedLayers = true,
                };
                var dxfNames = new string[]
                {
                    RXClass.GetClass(typeof(Polyline)).DxfName,
                };
                var results = new List<ThRegionLightEdge>();
                var filter = ThSelectionFilterTool.Build(dxfNames);
                var result = Active.Editor.GetSelection(options, filter);
                if (result.Status == PromptStatus.OK)
                {
                    var racewayParameter = new ThRacewayParameter();
                    result.Value.GetObjectIds().ForEach(o =>
                    {
                        var border = acdb.Element<Polyline>(o);
                        var newBorder = ThMEPFrameService.NormalizeEx(border);
                        if (newBorder.Area > 0)
                        {
                            var lines = acdb.ModelSpace
                            .OfType<Line>()
                            .Where(l => l.Layer == racewayParameter.CenterLineParameter.Layer);
                            var centerLines = newBorder.SpatialFilter(lines.ToCollection()).Cast<Line>().ToList();

                            var blks = acdb.ModelSpace
                            .OfType<BlockReference>()
                            .Where(b => b.Layer == racewayParameter.LaneLineBlockParameter.Layer);
                            var lightBlks = newBorder.SpatialFilter(blks.ToCollection()).Cast<BlockReference>().ToList();

                            var texts = acdb.ModelSpace
                            .OfType<DBText>()
                            .Where(t => t.Layer == racewayParameter.NumberTextParameter.Layer);
                            var numberTexts = newBorder.SpatialFilter(texts.ToCollection()).Cast<DBText>().ToList();

                            var dbOBjs = acdb.ModelSpace
                            .Where(e => ThGarageLightUtils.IsLightCableCarrierCenterline(e)).ToList();                           
                            var laneLines = GetRegionLines(newBorder, dbOBjs.Where(e => 
                            ThGarageLightUtils.IsLightCableCarrierCenterline(e)).ToCollection());
                           
                            var regionLightEdge = new ThRegionLightEdge
                            {
                                Lights = lightBlks,
                                Edges = centerLines,
                                Texts = numberTexts,
                                RegionBorder = newBorder,
                                LaneLines = laneLines
                            };
                            results.Add(regionLightEdge);
                        }
                    });
                }
                return results;
            }
        }

        public static List<ThRegionBorder> GetFireRegionBorders()
        {
            using (AcadDatabase acdb = AcadDatabase.Active())
            {
                var options = new PromptSelectionOptions()
                {
                    AllowDuplicates = false,
                    MessageForAdding = "\n请选择布灯的区域框线",
                    RejectObjectsOnLockedLayers = true,
                };
                var dxfNames = new string[]
                {
                    RXClass.GetClass(typeof(Polyline)).DxfName,
                };
                var results = new List<ThRegionBorder>();
                var filter = ThSelectionFilterTool.Build(dxfNames);
                var result = Active.Editor.GetSelection(options, filter);
                if (result.Status == PromptStatus.OK)
                {
                    result.Value.GetObjectIds().ForEach(o =>
                    {
                        var border = acdb.Element<Polyline>(o);
                        var newBorder = ThMEPFrameService.NormalizeEx(border);
                        if (newBorder.Area > 0)
                        {
                            var dbOBjs = acdb.ModelSpace
                            .Where(e => ThGarageLightUtils.IsLightCableCarrierCenterline(e) ||
                            ThGarageLightUtils.IsNonLightCableCarrierCenterline(e)).ToList();                            
                            var dxLines = GetRegionLines(newBorder, dbOBjs.Where(e => ThGarageLightUtils.IsLightCableCarrierCenterline(e)).ToCollection());
                            var fdxLines = GetRegionLines(newBorder, dbOBjs.Where(e => ThGarageLightUtils.IsNonLightCableCarrierCenterline(e)).ToCollection());
                            if (dxLines.Count > 0)
                            {
                                var regionBorder = new ThRegionBorder
                                {
                                    RegionBorder = newBorder,
                                    DxCenterLines = dxLines,
                                    FdxCenterLines = fdxLines,
                                };
                                results.Add(regionBorder);
                            }
                        }
                    });
                }
                return results;
            }
        }

        private static List<BlockReference> GetRegionLights(Polyline region, DBObjectCollection dbObjs)
        {
            return region.SpatialFilter(dbObjs).Cast<BlockReference>().ToList();
        }
        private static List<Line> GetRegionLines(Polyline region, DBObjectCollection dbObjs)
        {
            return ThLaneLineEngine.Explode(region.SpatialFilter(dbObjs)).Cast<Line>().ToList();
        }

        public static ThLightArrangeParameter GetUiParameters()
        {
            // From UI
            var arrangeParameter = new ThLightArrangeParameter()
            {
                Margin = 800,
                AutoCalculate = ThMEPLightingService.Instance.LightArrangeUiParameter.AutoCalculate,
                AutoGenerate = ThMEPLightingService.Instance.LightArrangeUiParameter.AutoGenerate,
                Interval = ThMEPLightingService.Instance.LightArrangeUiParameter.Interval,
                IsSingleRow = ThMEPLightingService.Instance.LightArrangeUiParameter.IsSingleRow,
                LoopNumber = ThMEPLightingService.Instance.LightArrangeUiParameter.LoopNumber,
                RacywaySpace = ThMEPLightingService.Instance.LightArrangeUiParameter.RacywaySpace,
                Width = ThMEPLightingService.Instance.LightArrangeUiParameter.Width,
            };

            // 自定义
            arrangeParameter.Margin = 800.0;
            arrangeParameter.PaperRatio = 100;
            arrangeParameter.MinimumEdgeLength = 5000;

            return arrangeParameter;
        }

        public static Polyline PolylineJig(short colorIndex)
        {
            using (AcadDatabase acdb = AcadDatabase.Active())
            {
                try
                {
                    var jigger = new ThDrawPolylineJigger(colorIndex);
                    PromptResult jigRes;
                    do
                    {
                        jigRes = Active.Editor.Drag(jigger);
                        if (jigRes.Status == PromptStatus.OK)
                            jigger.AllVertexes.Add(jigger.LastVertex);
                    } while (jigRes.Status == PromptStatus.OK);
                    var wcsVertexes = jigger.WcsVertexes;
                    if (wcsVertexes.Count > 1)
                    {
                        Polyline polyline = new Polyline();
                        for (int i = 0; i < wcsVertexes.Count; i++)
                        {
                            Point3d pt3d = wcsVertexes[i];
                            Point2d pt2d = new Point2d(pt3d.X, pt3d.Y);
                            polyline.AddVertexAt(i, pt2d, 0, 0, 0);
                        }
                        return polyline;
                    }
                    else
                    {
                        return new Polyline();
                    }
                }
                catch (System.Exception ex)
                {
                    Active.Editor.WriteMessage(ex.ToString());
                }
            }
            return new Polyline();
        }
    }
}
