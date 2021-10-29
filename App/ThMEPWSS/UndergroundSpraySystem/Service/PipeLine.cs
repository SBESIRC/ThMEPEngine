using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using ThMEPWSS.UndergroundFireHydrantSystem.Service;
using ThMEPWSS.UndergroundSpraySystem.Model;

namespace ThMEPWSS.UndergroundSpraySystem.Service
{
    public class PipeLine
    {
        public static void Split(SprayOut sprayOut)//横向管线打断
        {
            sprayOut.PipeLine = PipeLineList.CleanLaneLines3(sprayOut.PipeLine);//merge

            double tolerance = 1;
            double gap = 100;
            var horizontalPipe = new List<Line>();//横管
            var verticalPipe = new List<Line>();//竖管
            foreach (var line in sprayOut.PipeLine)
            {
                var spt = line.StartPoint;
                var ept = line.EndPoint;
                if(Math.Abs(spt.X - ept.X) < tolerance)//若是竖管
                {
                    verticalPipe.Add(line);
                    continue;
                }
                if (Math.Abs(spt.Y - ept.Y) < tolerance)//若是横管
                {
                    horizontalPipe.Add(line);
                    continue;
                }
            }
            foreach(var line in horizontalPipe.ToList())//遍历横管
            {
                var spt = line.StartPoint;
                var ept = line.EndPoint;
                var vertical = new List<Line>();
                var xs = new List<double>();
                foreach(var line2 in verticalPipe)//遍历竖管
                {
                    var spt2 = line2.StartPoint;
                    var ept2 = line2.EndPoint;
                    if ((spt.X - spt2.X) * (ept.X - spt2.X) < 0 &&
                        (spt.Y - spt2.Y) * (spt.Y - ept2.Y) < 0)
                    {
                        vertical.Add(line2);
                    }
                }
                if(vertical.Count == 0)
                {
                    continue;
                }
                vertical = vertical.OrderBy(e => e.StartPoint.X).ToList();
                
                var leftPt = new Point3d(Math.Min(spt.X, ept.X), spt.Y, 0);
                var rightPt = new Point3d(Math.Max(spt.X, ept.X), spt.Y, 0);
                xs.Add(Math.Min(spt.X, ept.X));
                vertical.ForEach(e => xs.Add(e.StartPoint.X));
                xs.Add(Math.Max(spt.X, ept.X));
                for (int i = 0; i < xs.Count - 1; i++)
                {
                    Line l;
                    if(i == 0)
                    {
                        l = new Line(new Point3d(xs[i], spt.Y, 0), new Point3d(xs[i+1] - gap, spt.Y, 0));
                    }
                    else if(i == xs.Count - 2)
                    {
                        l = new Line(new Point3d(xs[i] + gap, spt.Y, 0), new Point3d(xs[i + 1], spt.Y, 0));
                    }
                    else
                    {
                        l = new Line(new Point3d(xs[i] + gap, spt.Y, 0), new Point3d(xs[i + 1] - gap, spt.Y, 0));
                    }
                    horizontalPipe.Add(l);
                }
                horizontalPipe.Remove(line);
            }
            sprayOut.PipeLine.Clear();
            sprayOut.PipeLine.AddRange(horizontalPipe);
            sprayOut.PipeLine.AddRange(verticalPipe);
        }

    }
}
