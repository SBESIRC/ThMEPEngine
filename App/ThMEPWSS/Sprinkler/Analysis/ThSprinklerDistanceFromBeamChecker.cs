using System;
using NFox.Cad;
using Linq2Acad;
using System.Linq;
using ThCADCore.NTS;
using ThCADExtension;
using ThMEPWSS.Service;
using Catel.Collections;
using ThMEPEngineCore.Model;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using ThMEPWSS.Sprinkler.Service;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPWSS.Sprinkler.Analysis
{
    public class ThSprinklerDistanceFromBeamChecker
    {
        public List<List<Polyline>> LayoutAreas(List<ThGeometry> geometries)
        {
            var polylines = ThSprinklerLayoutAreaUtils.GetFrames();
            if (polylines.Count <= 0)
            {
                return new List<List<Polyline>>();
            }

            using (AcadDatabase acdb = AcadDatabase.Active())
            {
                var calHolesService = new CalHolesService();
                var holeDic = calHolesService.CalHoles(polylines);
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

        public void Present(Database database, List<List<Polyline>> layoutAreas)
        {
            using (AcadDatabase acdb = AcadDatabase.Active())
            {
                var layerId = database.CreateAISprinklerLayoutAreaLayer();

                layoutAreas.ForEach(o =>
                {
                    o.ForEach(area => 
                    {
                        area.ColorIndex = 3;
                        area.LayerId = layerId;
                        acdb.ModelSpace.Add(area);
                    });
                });
            }
        }

        public List<List<Point3d>> BeamCheck(List<ThIfcDistributionFlowElement> sprinklers, List<List<Polyline>> layoutAreas, List<ThGeometry> geometries)
        {
            var objs = new DBObjectCollection();
            geometries.ForEach(g =>
            {
                if (g.Properties.ContainsKey("Category") && (g.Properties["Category"] as string).Contains("Beam"))
                {
                    objs.Add(g.Boundary);
                }
            });
            var spatialIndex = new ThCADCoreNTSSpatialIndex(objs);

            var result = new List<List<Point3d>>();
            foreach(ThSprinkler sprinkler in sprinklers)
            {
                var tag = true;
                foreach (var polylines in layoutAreas)
                {
                    foreach(var polyline in polylines)
                    {
                        if(polyline.Contains(sprinkler.Position))
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
                    var circle = new Circle(sprinkler.Position, Vector3d.ZAxis, 300.0);
                    var filter = spatialIndex.SelectCrossingPolygon(circle.TessellateCircleWithArc(10.0 * Math.PI));
                    if (filter.Count > 0)
                    {
                        var closePoint = new Point3d();
                        var closeDistance = double.MaxValue;
                        filter.Cast<Polyline>().ForEach(e =>
                        {
                            var point = e.GetClosestPointTo(sprinkler.Position, false);
                            var distance = sprinkler.Position.DistanceTo(point);
                            if (distance < closeDistance) 
                            {
                                closePoint = point;
                                closeDistance = distance;
                            }
                        });

                        result.Add(new List<Point3d>
                                  {
                                     sprinkler.Position,
                                     closePoint
                                  });

                    }
                }
            };

            return result;
        }

        public void Present(Database database, List<List<Point3d>> result)
        {
            using (var acadDatabase = AcadDatabase.Use(database))
            {
                var layerId = database.CreateAISprinklerDistanceFormBeamCheckerLayer();
                var style = "TH-DIM100-W";
                var id = Dreambuild.AutoCAD.DbHelper.GetDimstyleId(style, acadDatabase.Database);
                result.ForEach(o =>
                {
                    var alignedDimension = new AlignedDimension
                    {
                        XLine1Point = o[0],
                        XLine2Point = o[1],
                        DimensionText = "",
                        DimLinePoint = ThSprinklerUtils.VerticalPoint(o[0], o[1], 2000.0),
                        ColorIndex = 256,
                        DimensionStyle = id,
                        LayerId = layerId,
                        Linetype = "ByLayer"
                    };

                    acadDatabase.ModelSpace.Add(alignedDimension);
                });
            }
        }
    }
}
