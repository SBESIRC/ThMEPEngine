using System;
using Linq2Acad;
using System.Linq;
using ThCADCore.NTS;
using Catel.Collections;
using ThMEPEngineCore.Model;
using Autodesk.AutoCAD.Geometry;
using ThMEPWSS.Sprinkler.Service;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPWSS.Sprinkler.Analysis
{
    public class ThSprinklerDistanceBetweenSprinklerChecker
    {
        public List<List<Point3d>> DistanceCheck(List<ThIfcDistributionFlowElement> sprinklers,double tolerance)
        {
            var result = new List<List<Point3d>>();
            while (sprinklers.Count > 0) 
            {
                var position = (sprinklers[0] as ThSprinkler).Position;
                sprinklers.RemoveAt(0);
                var kdTree = new ThCADCoreNTSKdTree(1.0);
                sprinklers.Cast<ThSprinkler>().ForEach(o => kdTree.InsertPoint(o.Position));
                var closePointList = ThSprinklerKdTreeService.QueryOther(kdTree,position,tolerance);
                closePointList.ForEach(o => result.Add(new List<Point3d> { position, o }));
            }
            
            return result;
        }

        public List<List<Point3d>> BuildingCheck(List<ThGeometry> geometries, List<List<Point3d>> pointsList)
        {
            var geometriesFilter = new DBObjectCollection();
            geometries.ForEach(g =>
            {
                if (g.Properties.ContainsKey("BottomDistanceToFloor") && Convert.ToInt32(g.Properties["BottomDistanceToFloor"]) < 700)
                {
                    return;
                }
                geometriesFilter.Add(g.Boundary);
            });

            var result = new List<List<Point3d>>();
            var spatialIndex = new ThCADCoreNTSSpatialIndex(geometriesFilter);
            pointsList.ForEach(o =>
            {
                var line = new Line(o[0], o[1]);
                var filter = spatialIndex.SelectCrossingPolygon(line.Buffer(1.0));
                if(filter.Count == 0)
                {
                    result.Add(o);
                }
            });
            return result;
        }

        public void Present(Database database, List<List<Point3d>> result)
        {
            using (var acadDatabase = AcadDatabase.Use(database))
            {
                var layerId = database.CreateAISprinklerDistanceCheckerLayer();
                var style = "TH-DIM100-W";
                var id = Dreambuild.AutoCAD.DbHelper.GetDimstyleId(style, acadDatabase.Database);
                result.ForEach(o =>
                {
                    var alignedDimension = new AlignedDimension
                    {
                        XLine1Point = o[0],
                        XLine2Point = o[1],
                        DimensionText = "",
                        DimLinePoint = VerticalPoint(o[0], o[1], 2000.0),
                        ColorIndex = 256,
                        DimensionStyle = id,
                        LayerId = layerId,
                        Linetype = "ByLayer"
                    };

                    acadDatabase.ModelSpace.Add(alignedDimension);
                });
            }
        }

        private Point3d CenterPoint(Point3d first, Point3d second)
        {
            return new Point3d((first.X + second.X) / 2, (first.Y + second.Y) / 2, 0);
        }

        private Point3d VerticalPoint(Point3d first, Point3d second, double distance)
        {
            var verticalVerctor = second - first;
            return CenterPoint(first, second) + verticalVerctor / verticalVerctor.Length * distance;
        }
    }
}
