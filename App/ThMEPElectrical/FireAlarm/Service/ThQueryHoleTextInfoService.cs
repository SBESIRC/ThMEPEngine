using System.Linq;
using ThCADCore.NTS;
using Dreambuild.AutoCAD;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPElectrical.FireAlarm.Service
{
    public class ThQueryHoleTextInfoService
    {
        public Dictionary<Polyline,List<string>> Query(List<Polyline> holes, List<Entity> texts)
        {
            var results = new Dictionary<Polyline, List<string>>();
            var objs = new DBObjectCollection();
            texts.ForEach(p => objs.Add(p));
            var spatialIndex = new ThCADCoreNTSSpatialIndex(objs);
            foreach(Polyline hole in holes)
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
