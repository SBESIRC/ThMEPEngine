using System.Linq;
using System.Collections.Generic;
using NFox.Cad;
using ThCADCore.NTS;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Service;

namespace ThPlatform3D.StructPlane.Service
{
    internal class ThWallBuildAreaService
    {
        public ThWallBuildAreaService()
        {
        }
        public List<ThGeometry> BuildArea(List<ThGeometry> geos)
        {
            var wallGeos = geos.GetStandardWallGeos();
            var results = geos.Except(wallGeos).ToList();
            
            var groups = wallGeos.GroupBy(x =>
            x.Properties["LineType"].ToString());            
            foreach (var group in groups)
            {
                var subGeos = group.ToList();
                if(subGeos.Count==0)
                {
                    continue;
                }
                var properties = subGeos.First().Properties;
                var polygons = BuildArea(subGeos.Select(o => o.Boundary).ToCollection());
                results.AddRange(polygons.OfType<Entity>().Select(o => ThGeometry.Create(o, properties)));
            }
            return results;
        }
        private DBObjectCollection BuildArea(DBObjectCollection objs)
        {
            var polygons = objs.FilterSmallArea(1.0);
            var simplifier = new ThPolygonalElementSimplifier();
            polygons = simplifier.Normalize(polygons);
            polygons = polygons.FilterSmallArea(1.0);
            polygons = simplifier.MakeValid(polygons);
            polygons = polygons.FilterSmallArea(1.0);
            polygons = simplifier.Simplify(polygons);
            polygons = polygons.FilterSmallArea(1.0);
            var results = polygons.BuildArea();
            results = results.FilterSmallArea(1.0);
            return results;
        }
    }
}
