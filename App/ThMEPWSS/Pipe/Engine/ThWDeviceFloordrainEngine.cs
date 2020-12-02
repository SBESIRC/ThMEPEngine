﻿using System;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPWSS.Pipe.Geom;

namespace ThMEPWSS.Pipe.Engine
{
    public class ThWDeviceFloordrainEngine : IDisposable
    {
        public void Dispose()
        {
        }
        public Point3dCollection Devicefloordrain { get; set; }
        public Point3dCollection Condensepipe_tofloordrain { get; set; }
        public Point3dCollection Rainpipe_tofloordrain { get; set; }
        public Point3dCollection RoofRainpipe_tofloordrain { get; set; }
        public ThWDeviceFloordrainEngine()
        {
            Devicefloordrain = new Point3dCollection();
            Condensepipe_tofloordrain = new Point3dCollection();
            Rainpipe_tofloordrain = new Point3dCollection();
            RoofRainpipe_tofloordrain = new Point3dCollection();
        }

        public void Run(Polyline rainpipe, Polyline device, Polyline condensepipe, List<BlockReference> devicefloordrain, Polyline roofrainpipe)
        {
            int num=0;
            for (int i = 0; i < devicefloordrain.Count; i++)
            {
                if (GeomUtils.PtInLoop(device, devicefloordrain[i].Position))
                {
                    num = i;
                }          
            }
            
            Devicefloordrain.Add(Isinsidedevice(device, devicefloordrain[num]));
            if (Condensepipe_floordrain(device, condensepipe, devicefloordrain[num]).Count > 0)
            {
                Condensepipe_tofloordrain = Condensepipe_floordrain(device, condensepipe, devicefloordrain[num]);
            }
            else if (Rainpipe_floordrain(device, rainpipe, devicefloordrain[num]).Count > 0)
            {
                Rainpipe_tofloordrain = Rainpipe_floordrain(device, rainpipe, devicefloordrain[num]);
            }
            else
            {
                //throw new ArgumentNullException("Drawing errors");
            }
            if (roofrainpipe != null)
            {
                if (RoofRainpipe_floordrain(device, roofrainpipe, devicefloordrain[num]).Count > 0)
                {
                    RoofRainpipe_tofloordrain = RoofRainpipe_floordrain(device, roofrainpipe, devicefloordrain[num]);
                }
            }
        }
    
        private Point3d Isinsidedevice(Polyline device, BlockReference devicefloordrain)
        {     
            if (GeomUtils.PtInLoop(device, devicefloordrain.Position))
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
            if (condensepipe != null)
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
            else
            {
                return new Point3dCollection();
            }


        }
        private Point3dCollection Rainpipe_floordrain(Polyline device, Polyline rainpipe, BlockReference devicefloordrain)
        {   
            var rainpipe_floordrain = new Point3dCollection();
            if (GeomUtils.PtInLoop(device, rainpipe.GetCenter()))
            {
                rainpipe_floordrain.Add(rainpipe.GetCenter() + 50 * rainpipe.GetCenter().GetVectorTo(devicefloordrain.Position).GetNormal());
                rainpipe_floordrain.Add(devicefloordrain.Position - 50 * rainpipe.GetCenter().GetVectorTo(devicefloordrain.Position).GetNormal());

            }
            //else
            //{
            //    throw new ArgumentNullException("devicefloordraine was Null");
            //}
            return rainpipe_floordrain;
        }
        private Point3dCollection RoofRainpipe_floordrain(Polyline device, Polyline roofrainpipe, BlockReference devicefloordrain)
        {
            var pts = new Point3dCollection();
            Line line = new Line(device.GetCenter(), roofrainpipe.GetCenter());
            device.IntersectWith(line, Intersect.ExtendArgument, pts, (IntPtr)0, (IntPtr)0);
            var RoofRainpipe_floordrain = new Point3dCollection();
            if (pts[0].GetVectorTo(roofrainpipe.GetCenter()).IsCodirectionalTo(roofrainpipe.GetCenter().GetVectorTo(pts[1])))
            {
                RoofRainpipe_floordrain.Add(roofrainpipe.GetCenter() + 50 * roofrainpipe.GetCenter().GetVectorTo(devicefloordrain.Position).GetNormal());
                RoofRainpipe_floordrain.Add(devicefloordrain.Position - 50 * roofrainpipe.GetCenter().GetVectorTo(devicefloordrain.Position).GetNormal());
            }
            return RoofRainpipe_floordrain;
        }
    }
}
