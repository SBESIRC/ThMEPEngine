using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.AutoCAD.DatabaseServices;
using Dreambuild.AutoCAD;
using ThCADCore.NTS;

namespace ThMEPLighting.IlluminationLighting.Service
{
    public class ThQueryHoleTextInfoService
    {
        public Dictionary<Polyline, List<string>> Query(List<Polyline> holes, List<Entity> texts)
        {
            var results = new Dictionary<Polyline, List<string>>();
            var objs = new DBObjectCollection();
            texts.ForEach(p => objs.Add(p));
            var spatialIndex = new ThCADCoreNTSSpatialIndex(objs);
            foreach (Polyline hole in holes)
            {
                var querys = spatialIndex.SelectWindowPolygon(hole);
                var textInfos = new List<string>();
                querys.Cast<Entity>().ForEach(e =>
                {
                    if (e is DBText dbText)
                    {
                        textInfos.Add(dbText.TextString);
                    }
                    else if (e is MText mText)
                    {
                        textInfos.Add(mText.Contents);
                    }
                });
                results.Add(hole, textInfos);
            }
            return results;
        }
    }
}
