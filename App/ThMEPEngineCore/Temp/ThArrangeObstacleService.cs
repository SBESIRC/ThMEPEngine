using System;
using System.Linq;
using System.Collections.Generic;
using ThMEPEngineCore.CAD;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.Temp
{
    public class ThArrangeObstacleService
    {
        private double Length { get; set; }
        private double Width { get; set; }
        private List<Polyline> Obstacles { get; set; }
        private Entity RegionPoly { get; set; }
        private double regionArea;
        private ThArrangeObstacleService(Entity region)
        {
            RegionPoly = region;
            Length = 5000;
            Width = 5000;
            Obstacles = new List<Polyline>();
            regionArea = GetRegionArea();
        }
        public static List<Polyline> Arrange(Entity region)
        {
            var instance = new ThArrangeObstacleService(region);
            instance.Arrange();
            return instance.Obstacles;
        }
        private void Arrange()
        {
            var geometricExtents = RegionPoly.GeometricExtents;
            double minX = geometricExtents.MinPoint.X;
            double minY = geometricExtents.MinPoint.Y;

            double maxX = geometricExtents.MaxPoint.X;
            double maxY = geometricExtents.MaxPoint.Y;

            Random random = new Random();

            while (Obstacles.Sum(o => o.Area) <= regionArea * 0.15)
            {
                double x = NextDouble(random, minX, maxX);
                double y = NextDouble(random, minY, maxY);
                var center = new Point3d(x,y,0);
                if(RegionPoly.IsContains(center))
                {
                   var rec = center.CreateRectangle(Length, Width);
                    if(RegionPoly.IsContains(rec))
                    {
                        Obstacles.Add(rec);
                    }
                }
            }
        }
        private double GetRegionArea()
        {
            if(RegionPoly is Polyline polyline)
            {
                return polyline.Area;
            }
            else if (RegionPoly is MPolygon mPolygon)
            {
                return mPolygon.Area;
            }
            else if(RegionPoly is Circle circle)
            {
                return circle.Area;
            }
            else if (RegionPoly is Ellipse ellipse)
            {
                return ellipse.Area;
            }
            else
            {
                return 0.0;
            }
        }
        private double NextDouble(Random random, double miniDouble, double maxiDouble)
        {
            if (random != null)
            {
                return random.NextDouble() * (maxiDouble - miniDouble) + miniDouble;
            }
            else
            {
                return 0.0d;
            }
        }
    }
}
