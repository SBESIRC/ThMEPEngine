using System.Collections.Generic;
using Autodesk.AutoCAD.Geometry;

namespace ThMEPHVAC.Model
{
    public class ThDrawDimension
    {
        public void InsertDirWallPoint(EndlineSegInfo seg)
        {
            if (!ThMEPHVACService.IsVertical(seg.seg.l))
            {
                //非垂直的按X排序
                InsertWallPointByX(seg, seg.dirAlignPoint);
            }
            else
            {
                //按Y排序
                InsertWallPointByY(seg, seg.dirAlignPoint);
            }
        }
        private void InsertWallPointByY(EndlineSegInfo endline, Point3d wallP)
        {
            var Ys = new List<double>();
            foreach (var port in endline.portsInfo)
                Ys.Add(port.position.Y);
            var ascending = Ys[0] < Ys[Ys.Count - 1];
            var falgY = wallP.Y;
            Ys.Add(falgY);
            if (ascending)
                Ys.Sort();
            else
                Ys.Sort((x, y) => -x.CompareTo(y));
            var idx = Ys.IndexOf(falgY);
            endline.portsInfo.Insert(idx, new PortInfo() { position = wallP, portAirVolume = -1 });
        }
        private void InsertWallPointByX(EndlineSegInfo endline, Point3d wallP)
        {
            var Xs = new List<double>();
            foreach (var port in endline.portsInfo)
                Xs.Add(port.position.X);
            var ascending = Xs[0] < Xs[Xs.Count - 1];
            var falgX = wallP.X;
            Xs.Add(falgX);
            if (ascending)
                Xs.Sort();
            else
                Xs.Sort((x, y) => -x.CompareTo(y));
            var idx = Xs.IndexOf(falgX);
            endline.portsInfo.Insert(idx, new PortInfo() { position = wallP, portAirVolume = -1 });
        }
    }
}
