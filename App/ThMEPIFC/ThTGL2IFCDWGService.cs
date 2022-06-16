using System;
using Linq2Acad;
using System.Linq;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPTCH.Model;
using Autodesk.AutoCAD.Geometry;

namespace ThMEPIFC
{
    public class ThTGL2IFCDWGService
    {
        public void LoadDWG(Database database, ThTCHProject project)
        {
            using (var db = AcadDatabase.Use(database))
            {
                var slabs = new DBObjectCollection();
                var railings = new DBObjectCollection();

                db.ModelSpace
                    .OfType<Polyline>()
                    .ForEachDbObject(p =>
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
                    });

                // 指定楼层
                var storey = project.Site.Building.Storeys.Where(s => s.Number.Contains("3")).FirstOrDefault();
                if (storey != null)
                {
                    foreach(Polyline railing in railings)
                    {
                        storey.Railings.Add(CreateRailing(railing));
                    }
                }
            }
        }

        private ThTCHRailing CreateRailing(Polyline pline)
        {
            return new ThTCHRailing()
            {
                Depth = 1200,
                Thickness = 60,
                Outline = pline,
                ExtrudedDirection = Vector3d.ZAxis,
            };
        }
    }
}
