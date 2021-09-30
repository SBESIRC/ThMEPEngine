using System;
using NFox.Cad;
using Linq2Acad;
using System.Linq;
using ThCADCore.NTS;
using ThCADExtension;
using ThMEPWSS.Service;
using Dreambuild.AutoCAD;
using ThMEPEngineCore.Model;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using ThMEPWSS.Sprinkler.Service;
using Autodesk.AutoCAD.DatabaseServices;
using DotNetARX;

namespace ThMEPWSS.Sprinkler.Analysis
{
    public class ThSprinklerDistanceFromBeamChecker : ThSprinklerChecker
    {
        public List<Polyline> RoomFrames { get; set; }

        public override void Clean(Polyline pline)
        {
            CleanLayoutArea(ThSprinklerCheckerLayer.Layout_Area_LayerName, pline);
            Clean(ThSprinklerCheckerLayer.Distance_Form_Beam_LayerName, pline);
        }

        public override void Check(List<ThIfcDistributionFlowElement> sprinklers, List<ThGeometry> geometries, Polyline pline)
        {
            var areas = LayoutAreas(geometries);
            Present(areas);
            var results = BeamCheck(sprinklers, areas, geometries, pline);
            if (results.Count > 0) 
            {
                Present(results);
            }
        }

        private List<List<Polyline>> LayoutAreas(List<ThGeometry> geometries)
        {
            using (AcadDatabase acdb = AcadDatabase.Active())
            {
                var calHolesService = new CalHolesService();
                var holeDic = calHolesService.CalHoles(RoomFrames);
                var layoutAreas = new List<List<Polyline>>();
                foreach (var holeInfo in holeDic)
                {
                    var plFrame = holeInfo.Key;
                    var holes = holeInfo.Value;

                    //清除原有构件
                    plFrame.ClearLayouArea();

                    //获取构建信息
                    holes.AddRange(GetStructureInfo(geometries, "Column", plFrame));
                    holes.AddRange(GetStructureInfo(geometries, "Wall", plFrame));
                    holes.AddRange(GetStructureInfo(geometries, "DoorOpening", plFrame));
                    holes.AddRange(GetStructureInfo(geometries, "FireproofShutter", plFrame));
                    
                    //不考虑梁
                    if (ThWSSUIService.Instance.Parameter.ConsiderBeam)
                    {
                        holes.AddRange(GetStructureInfo(geometries, "Beam", plFrame));
                    }

                    //计算可布置区域
                    layoutAreas.Add(ThSprinklerCreateLayoutAreaService.GetLayoutArea(plFrame, holes, 300));
                }
                return layoutAreas;
            }
        }

        private List<Polyline> GetStructureInfo(List<ThGeometry> geometries, string structureInfo, Polyline polyline)
        {
            var structure = new List<Polyline>();
            geometries.ForEach(g =>
            {
                if (g.Properties.ContainsKey("Category") && (g.Properties["Category"] as string).Contains(structureInfo))
                {
                    if (g.Boundary is Polyline)
                    {
                        structure.Add(g.Boundary as Polyline);
                    }
                }
            });
            var spatialIndex = new ThCADCoreNTSSpatialIndex(structure.ToCollection());
            return spatialIndex.SelectCrossingPolygon(polyline).Cast<Polyline>().ToList();
        }

        private void CleanLayoutArea(string layerName, Polyline polyline)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                acadDatabase.Database.UnFrozenLayer(layerName);
                acadDatabase.Database.UnLockLayer(layerName);
                acadDatabase.Database.UnOffLayer(layerName);

                var objs = acadDatabase.ModelSpace
                    .OfType<Polyline>()
                    .Where(o => o.Layer == layerName).ToCollection();
                var bufferPoly = polyline.Buffer(1)[0] as Polyline;
                var spatialIndex = new ThCADCoreNTSSpatialIndex(objs);
                spatialIndex.SelectCrossingPolygon(bufferPoly)
                            .OfType<Polyline>()
                            .ToList()
                            .ForEach(o =>
                            {
                                o.UpgradeOpen();
                                o.Erase();
                            });
            }
        }

        private void Present(List<List<Polyline>> layoutAreas)
        {
            using (var acadDatabase = AcadDatabase.Active())
            {
                var layerId = acadDatabase.Database.CreateAISprinklerLayoutAreaLayer();
                layoutAreas.ForEach(o =>
                {
                    o.ForEach(area => 
                    {
                        area.ColorIndex = 3;
                        area.LayerId = layerId;
                        acadDatabase.ModelSpace.Add(area);
                    });
                });
            }
        }

        private HashSet<Line> BeamCheck(List<ThIfcDistributionFlowElement> sprinklers, List<List<Polyline>> layoutAreas, List<ThGeometry> geometries, Polyline pline)
        {
            var polygon = pline.ToNTSPolygon();
            var objs = geometries
                .Where(g => (g.Properties.ContainsKey("Category") && (g.Properties["Category"] as string).Contains("Beam")))
                .Select(g => g.Boundary)
                .Where(g => polygon.Intersects(g.ToNTSGeometry()))
                .ToCollection();
            var spatialIndex = new ThCADCoreNTSSpatialIndex(objs);
            var result = new HashSet<Line>();
            sprinklers.OfType<ThSprinkler>().Where(o => o.Category == Category).ForEach(o =>
            {
                var tag = true;
                foreach (var polylines in layoutAreas)
                {
                    foreach (var polyline in polylines)
                    {
                        if (polyline.Contains(o.Position))
                        {
                            tag = false;
                            break;
                        }
                    }
                    if (!tag)
                    {
                        break;
                    }
                };

                if (tag)
                {
                    var circle = new Circle(o.Position, Vector3d.ZAxis, 300.0);
                    var filter = spatialIndex.SelectCrossingPolygon(circle.TessellateCircleWithArc(10.0 * Math.PI));
                    if (filter.Count > 0)
                    {
                        var closePoint = new Point3d();
                        var closeDistance = double.MaxValue;
                        filter.Cast<Polyline>().ForEach(e =>
                        {
                            var point = e.GetClosestPointTo(o.Position, false);
                            var distance = o.Position.DistanceTo(point);
                            if (distance < closeDistance)
                            {
                                closePoint = point;
                                closeDistance = distance;
                            }
                        });
                        result.Add(new Line(o.Position, closePoint));
                    }
                }
            });
            return result;
        }

        private void Present(HashSet<Line> result)
        {
            using (var acadDatabase = AcadDatabase.Active())
            {
                var layerId = acadDatabase.Database.CreateAISprinklerDistanceFormBeamCheckerLayer();
                Present(result, layerId);
            }
        }
    }
}
