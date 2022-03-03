using AcHelper;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using Dreambuild.AutoCAD;
using NFox.Cad;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ThCADCore.NTS;

namespace ThMEPArchitecture.PartitionLayout
{
    public class PartitionLayoutMultiThreadedTest
    {
        [CommandMethod("TIANHUACAD", "ThPLMultiThreadedTest", CommandFlags.Modal)]
        public void ThPLMultiThreadedTest()
        {
            func();
        }

        private void func()
        {
            Stopwatch sw = new Stopwatch();
            List<Point3d> points = new List<Point3d>();
            List<Line> lines = new List<Line>();
            List<Polyline> plys = new List<Polyline>();
            ThCADCoreNTSSpatialIndex spatialIndex = new ThCADCoreNTSSpatialIndex(new DBObjectCollection());
            for (int i = 0; i < 500; i++)
            {
                points.Add(new Point3d(0, 0, 0));
                lines.Add(new Line(new Point3d(0, 0, 0), new Point3d(1, 1, 1)));
                plys.Add(GeoUtilities.CreatePolyFromLine(lines[0]));
            }
            sw.Start();
            //foreach (var t in plys)
            //{
            //    t.GetClosestPointTo(new Point3d(1, 0, 1), false);
            //    t.Contains(new Point3d(0, 0, 0));
            //    var lin = new Line(new Point3d(5, 5, 5), new Point3d(10, 10, 1));
            //    var res = t.Intersect(lin, Intersect.OnBothOperands);
            //    spatialIndex.Update((new List<Polyline>() { t }).ToCollection(), new DBObjectCollection());
            //    spatialIndex.SelectCrossingPolygon(lin.Buffer(1));
            //    int id = Thread.CurrentThread.ManagedThreadId;
            //    Active.Editor.WriteMessage(id + "\n");
            //}
            plys.AsParallel().ForAll(t =>
            {
                t.GetClosestPointTo(new Point3d(1, 0, 1), false);
                t.Contains(new Point3d(0, 0, 0));
                var lin = new Line(new Point3d(5, 5, 5), new Point3d(10, 10, 1));
                var res = t.Intersect(lin, Intersect.OnBothOperands);
                spatialIndex.Update((new List<Polyline>() { t }).ToCollection(), new DBObjectCollection());
                spatialIndex.SelectCrossingPolygon(lin.Buffer(1));
                int id = Thread.CurrentThread.ManagedThreadId;
                Active.Editor.WriteMessage(id + "\n");
            });
            sw.Stop();
            TimeSpan ts = sw.Elapsed;
            var time = ts.TotalMilliseconds.ToString();
            Active.Editor.WriteMessage("总计用时：" + time + "\n");
        }
    }
}