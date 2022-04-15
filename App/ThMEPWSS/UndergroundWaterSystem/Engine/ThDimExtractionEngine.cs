using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using DotNetARX;
using Linq2Acad;
using NFox.Cad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThMEPWSS.UndergroundWaterSystem.Model;

namespace ThMEPWSS.UndergroundWaterSystem.Engine
{
    public class ThDimExtractionEngine
    {
        public List<ThDimModel> GetDimList(Point3dCollection pts)
        {
            using (var database = AcadDatabase.Active())
            {
                var retList = new List<ThDimModel>();
                var entities = database.ModelSpace.OfType<Entity>();
                DBObjectCollection dbObjs = null;
                if (pts.Count > 0)
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
