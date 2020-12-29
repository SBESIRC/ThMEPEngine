using AcHelper;
using Linq2Acad;
using System.Linq;
using ThCADCore.NTS;
using Dreambuild.AutoCAD;
using ThMEPEngineCore.CAD;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using ThMEPLighting.Common;

namespace ThMEPLighting.Garage.Service
{
    public class ThQueryLightBlockService
    {
        private List<Polyline> Regions { get; set; }
        public ThCADCoreNTSSpatialIndex PrimarySpatialIndex { get; set; }
        public Dictionary<Polyline, ThCADCoreNTSSpatialIndex> SecondarySpatialIndex { get; set; }
        private ThQueryLightBlockService(List<Polyline> regions)
        {
            Regions = regions;
            SecondarySpatialIndex = new Dictionary<Polyline, ThCADCoreNTSSpatialIndex>();
        }
        public static ThQueryLightBlockService Create(List<Polyline> regions)
        {
            var instance = new ThQueryLightBlockService(regions);
            instance.create();
            return instance;
        }
        private void create()
        {
            using (var acadDatabase = AcadDatabase.Active())
            {
                var tvs = new TypedValue[]
                {
                    new TypedValue((int)DxfCode.Start,RXClass.GetClass(typeof(BlockReference)).DxfName),
                    new TypedValue((int)DxfCode.ExtendedDataRegAppName,ThGarageLightCommon.ThGarageLightAppName),
                };
                var sf = new SelectionFilter(tvs);
                var psr = Active.Editor.SelectAll(sf);
                if (psr.Status == Autodesk.AutoCAD.EditorInput.PromptStatus.OK)
                {
                    var blocks = new DBObjectCollection();
                    psr.Value.GetObjectIds().ForEach(o => blocks.Add(acadDatabase.Element<BlockReference>(o)));
                    PrimarySpatialIndex = new ThCADCoreNTSSpatialIndex(blocks);
                    Regions.ForEach(o =>
                    {
                        var regionBlocks = PrimarySpatialIndex.SelectCrossingPolygon(o);
                        SecondarySpatialIndex.Add(o, new ThCADCoreNTSSpatialIndex(regionBlocks));
                    });
                }
            }
        }

        public List<Point3d> Query(Line edge, double width = 1.0)
        {
            var results = new List<Point3d>();
            var spatialIndex = PrimarySpatialIndex;
            foreach (var item in SecondarySpatialIndex)
            {
                if(item.Key.Contains(edge))
                {
                    spatialIndex = item.Value;                    
                    break;
                }
            }
            var outline = ThDrawTool.ToOutline(edge.StartPoint, edge.EndPoint, width);
            var blocks = spatialIndex
                .SelectCrossingPolygon(outline)
                .Cast<BlockReference>().ToList();
            results = Filter(blocks, edge, width);
            return results;
        }

        private List<Point3d> Filter(List<BlockReference> blocks,Line edge,double tolerance=1.0)
        {
            var results = new List<Point3d>();
            blocks.ForEach(o =>
                {
                    var projectionPt = ThGeometryTool.GetProjectPtOnLine(o.Position, edge.StartPoint, edge.EndPoint);
                    if(projectionPt.DistanceTo(o.Position)<= tolerance)
                    {
                        if (ThGeometryTool.IsPointOnLine(edge.StartPoint, edge.EndPoint, projectionPt))
                        {
                            results.Add(projectionPt);
                        }
                    }
                });
            return results;
        }
    }
}
