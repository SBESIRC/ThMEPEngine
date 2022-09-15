using System;
using Linq2Acad;
using System.Linq;
using ThCADCore.NTS;
using ThCADExtension;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPTCH.Model;

namespace ThMEPIFC
{
    public class ThTGL2IFCDWGService
    {
        public void LoadDWG(Database database, ThTCHProject project)
        {
            using (var db = AcadDatabase.Use(database))
            {
                var texts = new DBObjectCollection();
                var slabs = new DBObjectCollection();
                var railings = new DBObjectCollection();

                db.ModelSpace
                    .OfType<Entity>()
                    .ForEachDbObject(e =>
                    {
                        if (e is Polyline p)
                        {
                            if (p.Layer == "栏杆")
                            {
                                railings.Add(p);
                            }
                            else if (p.Layer == "楼板")
                            {
                                slabs.Add(p);
                            }
                            else if (p.Layer == "降板")
                            {
                                slabs.Add(p);
                            }
                        }
                        else if (e is DBText t)
                        {
                            if (t.Layer == "降板")
                            {
                                texts.Add(t);
                            }
                        }
                    });

                // 指定楼层
                var sp = new ThCADCoreNTSSpatialIndex(texts);
                var storey = project.Site.Building.Storeys.Where(s => s.Number.Contains("3")).FirstOrDefault();
                if (storey != null)
                {
                    foreach(Polyline railing in railings)
                    {
                        storey.Railings.Add(CreateRailing(railing));
                    }

                    foreach (Entity e in slabs.BuildArea())
                    {
                        storey.Slabs.Add(CreateSlab(e, sp));
                    }
                }
            }
        }

        private ThTCHRailing CreateRailing(Polyline pline)
        {
            return new ThTCHRailing()
            {
                Height = 1200,
                Width = 60,
                Outline = pline,
                ExtrudedDirection = Vector3d.ZAxis,
            };
        }

        private ThTCHSlab CreateSlab(Entity e, ThCADCoreNTSSpatialIndex sp)
        {
            if (e is Polyline p)
            {
                return new ThTCHSlab(p, 50.0, Vector3d.ZAxis);
            }
            else if (e is MPolygon mp)
            {
                var slab = new ThTCHSlab(mp.Shell(), 50.0, Vector3d.ZAxis);
                foreach(Polyline pline in mp.Holes())
                {
                    if (GetDescendingHeight(pline, sp, out double height))
                    {
                        slab.Descendings.Add(new ThTCHDescending()
                        {
                            Outline = pline,
                            IsDescending = true,
                            DescendingHeight = Math.Abs(height),
                            DescendingThickness = 50,
                            DescendingWrapThickness = 50,
                        });
                    }
                    else
                    {
                        slab.Descendings.Add(new ThTCHDescending()
                        {
                            Outline = pline,
                        });
                    }
                }
                return slab;
            }
            else
            {
                throw new NotSupportedException();
            }
        }

        private bool GetDescendingHeight(Entity e, ThCADCoreNTSSpatialIndex sp, out double height)
        {
            var results = sp.SelectCrossingPolygon(e);
            if (results.Count == 1 && results[0] is DBText text)
            {
                return double.TryParse(text.TextString, out height);
            }
            height = 0;
            return false;
        }
    }
}
