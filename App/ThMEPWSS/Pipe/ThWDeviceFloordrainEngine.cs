using System;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPWSS.Pipe
{
    public class ThWDeviceFloordrainEngine : IDisposable
    {
        public void Dispose()
        {
        }
        public Point3dCollection Devicefloordrain { get; set; }
        public Point3dCollection Condensepipe_tofloordrain { get; set; }
        public Point3dCollection Rainpipe_tofloordrain { get; set; }
        public ThWDeviceFloordrainEngine()
        {
            Devicefloordrain = new Point3dCollection();
            Condensepipe_tofloordrain = new Point3dCollection();
            Rainpipe_tofloordrain = new Point3dCollection();
        }

        public void Run(Polyline rainpipe, Polyline device, Polyline condensepipe, BlockReference devicefloordrain)
        {
            Devicefloordrain.Add(Isinsidedevice(device, devicefloordrain));
            if (Condensepipe_floordrain(device, condensepipe, devicefloordrain).Count > 0)
            {
                Condensepipe_tofloordrain = Condensepipe_floordrain(device, condensepipe, devicefloordrain);
            }
            else if (Rainpipe_floordrain(device, rainpipe, devicefloordrain).Count > 0)
            {
                Rainpipe_tofloordrain = Rainpipe_floordrain(device, rainpipe, devicefloordrain);
            }
            else
            {
                throw new ArgumentNullException("Drawing errors");
            }

        }
        private Point3d Isinsidedevice(Polyline device, BlockReference devicefloordrain)
        {
            var pts = new Point3dCollection();
            Line line = new Line(device.GetCenter(), devicefloordrain.Position);
            device.IntersectWith(line, Intersect.ExtendArgument, pts, (IntPtr)0, (IntPtr)0);
            if (pts[0].GetVectorTo(devicefloordrain.Position).IsCodirectionalTo(devicefloordrain.Position.GetVectorTo(pts[1])))
            {
                return devicefloordrain.Position;
            }
            else
            {
                throw new ArgumentNullException("devicefloordraine was Null");
            }
        }
        private Point3dCollection Condensepipe_floordrain(Polyline device, Polyline condensepipe, BlockReference devicefloordrain)
        {
            var pts = new Point3dCollection();
            Line line = new Line(device.GetCenter(), condensepipe.GetCenter());
            device.IntersectWith(line, Intersect.ExtendArgument, pts, (IntPtr)0, (IntPtr)0);
            var condensepipe_floordrain = new Point3dCollection();
            if (pts[0].GetVectorTo(condensepipe.GetCenter()).IsCodirectionalTo(condensepipe.GetCenter().GetVectorTo(pts[1])))
            {
                condensepipe_floordrain.Add(condensepipe.GetCenter() + 50 * condensepipe.GetCenter().GetVectorTo(devicefloordrain.Position).GetNormal());
                condensepipe_floordrain.Add(devicefloordrain.Position - 50 * condensepipe.GetCenter().GetVectorTo(devicefloordrain.Position).GetNormal());
            }

            return condensepipe_floordrain;
        }
        private Point3dCollection Rainpipe_floordrain(Polyline device, Polyline rainpipe, BlockReference devicefloordrain)
        {
            var pts = new Point3dCollection();
            Line line = new Line(device.GetCenter(), rainpipe.GetCenter());
            device.IntersectWith(line, Intersect.ExtendArgument, pts, (IntPtr)0, (IntPtr)0);
            var rainpipe_floordrain = new Point3dCollection();
            if (pts[0].GetVectorTo(rainpipe.GetCenter()).IsCodirectionalTo(rainpipe.GetCenter().GetVectorTo(pts[1])))
            {
                rainpipe_floordrain.Add(rainpipe.GetCenter() + 50 * rainpipe.GetCenter().GetVectorTo(devicefloordrain.Position).GetNormal());
                rainpipe_floordrain.Add(devicefloordrain.Position - 50 * rainpipe.GetCenter().GetVectorTo(devicefloordrain.Position).GetNormal());
            }
            else
            {
                throw new ArgumentNullException("devicefloordraine was Null");
            }
            return rainpipe_floordrain;
        }
    }
}
