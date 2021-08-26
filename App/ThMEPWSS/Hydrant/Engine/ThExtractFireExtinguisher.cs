using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Linq2Acad;
using NFox.Cad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;

namespace ThMEPWSS.Hydrant.Engine
{
    public class ThExtractFireExtinguisher
    {
        public List<Entity> Results { get; private set; }
        public DBObjectCollection DBobjs { get; private set; }
        private string BlockName;
        private ThFireExtinguisherExtractionVisitor visitor;

        public ThExtractFireExtinguisher(string name, ThFireExtinguisherExtractionVisitor v)
        {
            DBobjs = new DBObjectCollection();
            BlockName = name;
            visitor = v;
        }
        public void Extract(Database database, Point3dCollection polygon)
        {
            using (var acadDatabase = AcadDatabase.Use(database))
            {
                Results = acadDatabase
                   .ModelSpace
                   .OfType<Entity>()
                   .ToList();

                var spatialIndex = new ThCADCoreNTSSpatialIndex(Results.ToCollection());
                var dbObjs = spatialIndex.SelectCrossingPolygon(polygon);

                foreach (var db in dbObjs)
                {
                    if (db is BlockReference && (db as BlockReference).Visible)
                    {
                        if (IsFierExtingsher((db as BlockReference).GetEffectiveName()))
                        {
                            DBobjs.Add((DBObject)db);
                        }
                        else if(visitor.CheckBlockReferenceVisibility(db as BlockReference))
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
                                    if (IsFierExtingsher((dbObj as BlockReference).GetEffectiveName()))
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
        private bool IsFierExtingsher(string value)
        {
            return value.ToUpper().Contains(BlockName);
        }
    }
}
