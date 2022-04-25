using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Linq2Acad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPWSS.UndergroundWaterSystem.Model;
using ThMEPWSS.JsonExtensionsNs;
using ThMEPWSS.CADExtensionsNs;
using DotNetARX;
using ThCADCore.NTS;
using static ThMEPWSS.UndergroundWaterSystem.Utilities.GeoUtils;
using ThMEPEngineCore.Engine;
using ThMEPWSS.UndergroundFireHydrantSystem.Extract;
using NFox.Cad;
using System.IO;
using ThCADExtension;
using Dreambuild.AutoCAD;
using ThMEPEngineCore.CAD;

namespace ThMEPWSS.UndergroundWaterSystem.Engine
{
    public class ThDimExtractionEngine
    {
        public List<ThDimModel> GetDimListOptimized(Point3dCollection pts)
        {
            using (var adb = AcadDatabase.Active())
            {
                var results = new List<ThDimModel>();
                var bound = CreatePolyFromPoints(pts.Cast<Point3d>().ToArray());
                var entities = adb.ModelSpace.OfType<Entity>()
                    .Where(e => IsLayer(e.Layer))
                    .Where(e => e is DBText || IsTianZhengElement(e))
                    .Where(e =>
                    {
                        try { return bound.Contains(e.GeometricExtents.CenterPoint()); }
                        catch { return true; }
                    });
                foreach (var entity in entities)
                {
                    if (entity is DBText text && text.TextString.Contains("DN"))
                    {
                        ThDimModel thDim = new ThDimModel();
                        thDim.StrText = text.TextString;
                        thDim.Position = text.Position;
                        results.Add(thDim);
                    }
                    else if (IsTianZhengElement(entity))
                    {
                        var ents = GetAllEntitiesByExplodingTianZhengElementThoroughly(entity)
                            .Where(t => t is DBText text && text.TextString.Contains("DN")).Select(e => (DBText)e)
                            .Where(e => bound.Contains(e.GeometricExtents.CenterPoint()));
                        foreach (var ent in ents)
                        {
                            ThDimModel thDim = new ThDimModel();
                            thDim.StrText = ent.TextString;
                            thDim.Position = ent.Position;
                            results.Add(thDim);
                        }
                    }
                }
                return results;
            }
        }
        public List<ThDimModel> GetDimList(Point3dCollection pts = null)
        {
            using (var database = AcadDatabase.Active())
            {
                var retList = new List<ThDimModel>();
                var entities = database.ModelSpace.OfType<Entity>();
                DBObjectCollection dbObjs = null;
                if (pts!=null)
                {
                    var spatialIndex = new ThCADCoreNTSSpatialIndex(entities.ToCollection());
                    var pline = new Polyline()
                    {
                        Closed = true,
                    };
                    pline.CreatePolyline(pts);
                    dbObjs = spatialIndex.SelectCrossingPolygon(pline);
                }
                else
                {
                    dbObjs = entities.ToCollection();
                }
                foreach (var obj in dbObjs)
                {
                    var ent = obj as Entity;
                    if(IsLayer(ent.Layer))
                    {
                        var dim = new ThDimModel();
                        dim.Initialization(ent);
                        if(dim.StrText.Contains("DN"))
                        {
                            retList.Add(dim);
                        }
                    }
                }
                return retList;
            }

        }
        public bool IsLayer(string layer)
        {
            return true;
            if (layer.ToUpper().Contains("W-") && layer.ToUpper().Contains("-DIMS"))
            {
                return true;
            }
            return false;
        }
    }
}
