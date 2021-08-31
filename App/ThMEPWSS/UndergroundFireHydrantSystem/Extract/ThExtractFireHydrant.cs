using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Linq2Acad;
using NFox.Cad;
using System.Collections.Generic;
using System.Linq;
using ThCADCore.NTS;
using ThMEPWSS.CADExtensionsNs;
using ThMEPWSS.Pipe.Service;

namespace ThMEPWSS.UndergroundFireHydrantSystem.Extract
{
    public class ThExtractFireHydrant//室内消火栓平面
    {
        public List<Entity> Results { get; private set; }
        public DBObjectCollection DBobjs { get; private set; }
        public void Extract(Database database, Point3dCollection polygon)
        {
            using (var acadDatabase = AcadDatabase.Use(database))
            {
                Results = acadDatabase
                   .ModelSpace
                   .OfType<Entity>()
                   .Where(o => IsHYDTPipeLayer(o.Layer))
                   .ToList();

                var spatialIndex = new ThCADCoreNTSSpatialIndex(Results.ToCollection());
                var dbObjs = spatialIndex.SelectCrossingPolygon(polygon);


                DBobjs = new DBObjectCollection();
                foreach (var db in dbObjs)
                {
                    if (db is DBPoint)
                    {
                        continue;
                    }
                    if (db is BlockReference)
                    {
                        if (IsFireHydrant((db as BlockReference).GetEffectiveName()))
                        {
                            DBobjs.Add((DBObject)db);
                        }
                        else
                        {
                            var objs = new DBObjectCollection();

                            var blockRecordId = (db as BlockReference).BlockTableRecord;
                            var btr = acadDatabase.Blocks.Element(blockRecordId);

                            int indx = 0;
                            var indxFlag = false;
                            foreach (var entId in btr)
                            {
                                var dbObj = acadDatabase.Element<Entity>(entId);
                                if (dbObj is BlockReference)
                                {
                                    if (IsFireHydrant((dbObj as BlockReference).GetEffectiveName()))
                                    {
                                        indxFlag = true;
                                        break;
                                    }
                                }
                                indx += 1;
                            }

                            (db as BlockReference).Explode(objs);
                            if (indxFlag)
                            {
                                if (indx > objs.Count - 1)
                                {
                                    continue;
                                }
                                DBobjs.Add((DBObject)objs[indx]);
                            }

                        }
                    }
                }
            }
        }
        private bool IsHYDTPipeLayer(string layer)
        {
            return layer.ToUpper() == "W-FRPT-HYDT" || layer.ToUpper() == "0";
        }

        private bool IsFireHydrant(string valve)
        {
            return valve.ToUpper().Contains("室内消火栓平面");
        }
    }

}
