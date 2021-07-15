using Autodesk.AutoCAD.DatabaseServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPWSS.UndergroundFireHydrantSystem.Service
{
    class BlockExplode
    {
        public static void BRExplodeCircle(Entity entity, DBObjectCollection dbObjs)
        {
            var objs = new DBObjectCollection();
            entity.Explode(objs);
            foreach(var db in objs)
            {
                if(db is Circle)
                {
                    dbObjs.Add((DBObject)db);
                    continue;
                }
                if(db is Line || db is Polyline)
                {
                    continue;
                }
                if(db is Entity ent)
                {
                    BRExplodeCircle(ent, dbObjs);
                }
            }
        }
    }
}
