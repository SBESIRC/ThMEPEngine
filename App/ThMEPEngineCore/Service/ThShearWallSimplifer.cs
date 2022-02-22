
using Autodesk.AutoCAD.DatabaseServices;
using Dreambuild.AutoCAD;
using NFox.Cad;
using System.Collections.Generic;
using System.Linq;

namespace ThMEPEngineCore.Service
{
    class ThShearWallSimplifier: ThBuildElementSimplifier
    {
        public ThShearWallSimplifier()
        {
            OFFSETDISTANCE= 20.0;
            ClOSED_DISTANC_TOLERANCE = 600.0;
        }
        public DBObjectCollection Close(DBObjectCollection objs)
        {
            var results = new DBObjectCollection();
            var polys = objs.OfType<Polyline>().ToList();
            Close(polys);
            polys.ForEach(p => results.Add(p));
            objs.OfType<Entity>().Where(e => !(e is Polyline)).ForEach(e => results.Add(e));
            return results;
        }
    }
}
