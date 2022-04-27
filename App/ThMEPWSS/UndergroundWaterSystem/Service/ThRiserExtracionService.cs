using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using DotNetARX;
using Linq2Acad;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThMEPEngineCore.Engine;
using ThMEPEngineCore.Model.Hvac;
using ThMEPWSS.CADExtensionsNs;
using ThMEPWSS.UndergroundWaterSystem.Model;
using static ThMEPWSS.UndergroundWaterSystem.Utilities.GeoUtils;

namespace ThMEPWSS.UndergroundWaterSystem.Service
{
    public class ThRiserExtracionService
    {
        public List<ThRiserModel> GetRiserModelList(List<Line> pipeLines, Point3dCollection pts, int index = -1)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var pipeResult = new List<ThIfcVirticalPipe>();
                var TCHPipeRecognize = new ThTCHVPipeRecognitionEngine();
                TCHPipeRecognize.RecognizeMS(acadDatabase.Database, pts);
                pipeResult.AddRange(TCHPipeRecognize.Elements.OfType<ThIfcVirticalPipe>().ToList());

                var cPipeRecognize = new ThCircleVPipeRecognitionEngine()
                {
                    Radius = new List<double> { 100 / 2, 150 / 2 }
                };
                cPipeRecognize.RecognizeMS(acadDatabase.Database, pts);
                pipeResult.AddRange(cPipeRecognize.Elements.OfType<ThIfcVirticalPipe>().ToList());

                var blkPipeRecognize = new ThBlockVPipeRecognitionEngine();
                blkPipeRecognize.RecognizeMS(acadDatabase.Database, pts);
                pipeResult.AddRange(blkPipeRecognize.Elements.OfType<ThIfcVirticalPipe>().ToList());
                var retModel = new List<ThRiserModel>();
                foreach(var pipe in pipeResult)
                {
                    var model = new ThRiserModel();
                    model.Initialization(pipe.Data);
                    model.FloorIndex = index;                   
                    retModel.Add(model);
                }
                //提取与横贯相连的匿名图块
                retModel.AddRange(GetAnonymousRiserBlockConnectedToPipeLines(pts, pipeLines, index));
                RiserComparer comparer = new RiserComparer();
                retModel = retModel.Distinct(comparer).ToList();
                return retModel;
            }
        }
        private class RiserComparer : IEqualityComparer<ThRiserModel>
        {
            public bool Equals(ThRiserModel a, ThRiserModel b)
            {
                if (a.Position.DistanceTo(b.Position) < 0.001) return true;
                return false;
            }
            public int GetHashCode(ThRiserModel riser)
            {
                int code = ((int)Math.Floor(riser.Position.X))+((int)Math.Floor(riser.Position.Y));
                return code.GetHashCode();
            }
        }

        private List<ThRiserModel> GetAnonymousRiserBlockConnectedToPipeLines(Point3dCollection pts,List<Line> pipeLines, int index)
        {
            var results = new List<ThRiserModel>();
            var pline = new Polyline() { Closed = true, };
            if (pts.Count > 0) pline.CreatePolyline(pts);
            using (AcadDatabase adb = AcadDatabase.Active())
            {
                var blocks = adb.ModelSpace.OfType<BlockReference>().ToArray();
                blocks = blocks.Where(e => !e.GetEffectiveName().Contains("带定位立管")).ToArray();
                var riseCircles = blocks.Where(e =>
                {
                    var objs = e.ExplodeToDBObjectCollection().OfType<Circle>().ToList();
                    if (objs.Count == 1)
                    {
                        var circle = (Circle)objs[0];
                        double tol = 1;
                        var cond = Math.Abs(circle.Radius - 50) < tol || Math.Abs(circle.Radius - 75) < tol || Math.Abs(circle.Radius - 100) < tol;
                        if (cond) return true;
                    }
                    return false;
                }).Select(e =>
                {
                    var objs = e.ExplodeToDBObjectCollection().OfType<Circle>().ToList();
                    return (Circle)objs[0];
                })
                .Where(e => pline.Contains(e.Center))
                .Where(e => IsConnectedToLines(pipeLines, e.Center, 10 + e.Radius)).ToArray();
                foreach (var circle in riseCircles)
                {
                    var model = new ThRiserModel();
                    model.FloorIndex = index;
                    model.Position = circle.Center;
                    model.MarkName = "";
                    results.Add(model);
                }
            }
            return results;
        }

    }
}
