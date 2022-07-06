using Autodesk.AutoCAD.DatabaseServices;
using Dreambuild.AutoCAD;
using NFox.Cad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Service;

namespace ThMEPStructure.StructPlane.Service
{
    internal class ThWallBuildAreaService
    {
        public ThWallBuildAreaService()
        {
        }
        public List<ThGeometry> BuildArea(List<ThGeometry> geos)
        {
            var results = new List<ThGeometry>();
            var wallGeos = geos
                .Where(o => o.Properties != null)
                .Where(o => o.Properties.ContainsKey("type"))
                .Where(o => o.Properties["type"].ToString() == "IfcWall")
                .ToList();

            // 
            geos.Where(o => !wallGeos.Contains(o))
                .ForEach(o=> results.Add(o));

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
