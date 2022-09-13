using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Linq2Acad;
using NFox.Cad;
using System.Collections.Generic;
using System.Linq;
using ThCADCore.NTS;
using ThMEPWSS.Assistant;
using ThMEPWSS.CADExtensionsNs;
using ThMEPWSS.Pipe.Service;
using ThMEPWSS.UndergroundSpraySystem.General;

namespace ThMEPWSS.UndergroundSpraySystem.Model
{
    public class SprayPipe
    {
        public DBObjectCollection DBObjs { get; set; }
        public SprayPipe()
        {
            DBObjs = new DBObjectCollection();
        }

        public void Extract(Database database, Point3dCollection polygon)
        {
            using (var acadDatabase = AcadDatabase.Use(database))
            {
                var lines = ThDrainageSystemServiceGeoCollector.GetLines(
                        acadDatabase.ModelSpace.OfType<Entity>().ToList(),
                        layer => IsTargetLayer(layer));
                DBObjs = GeoFac
                        .CreateIntersectsSelector(lines.Select(x => x.ToLineString()).ToList())(polygon.ToRect().ToPolygon())
                        .SelectMany(x => x.ToDbCollection().OfType<DBObject>())
                        .ToCollection();
            }
        }

        private bool IsTargetLayer(string layer)
        {
            return layer.Contains("W-") && layer.Contains("FRPT") && layer.Contains("SPRL") && layer.Contains("PIPE");
        }

        public List<Line> CreateSprayLines()
        {
            var pipeLines = new List<Line>();
            foreach (var pipe in DBObjs)
            {
                if (pipe is Line line)
                {
                    if(line.Length >10)
                    {
                        var pt1 = new Point3d(line.StartPoint.X, line.StartPoint.Y, 0);
                        var pt2 = new Point3d(line.EndPoint.X, line.EndPoint.Y, 0);
                        pipeLines.Add(new Line(pt1, pt2));
                    }
                }
                else if (pipe is Polyline pline)
                {
                    foreach(var l in pline.Pline2Lines())
                    {
                        if(l.Length >10)
                        {
                            pipeLines.Add(l);
                        }
                    }
                }
            }
            
            return pipeLines;
        }
    }
}
