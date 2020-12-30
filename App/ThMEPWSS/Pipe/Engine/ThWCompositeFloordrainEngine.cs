using System;
using System.Collections.Generic;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPWSS.Pipe.Geom;
using Dreambuild.AutoCAD;
using ThMEPWSS.Assistant;

namespace ThMEPWSS.Pipe.Engine
{
    public class ThWCompositeFloordrainEngine : IDisposable
    {
        public void Dispose()
        {
        }
        public ThWBalconyFloordrainEngine ThWBalconyFloordrainEngine { get; set; }
        public ThWToiletFloordrainEngine ThWToiletFloordrainEngine { get; set; }
        public ThWDeviceFloordrainEngine ThWDeviceFloordrainEngine { get; set; }
        public Point3dCollection Floordrain_toilet
        {
            get
            {
                return ThWToiletFloordrainEngine.Floordrain;
            }
        }
        public Point3dCollection Devicefloordrain
        {
            get
            {
                return ThWDeviceFloordrainEngine.Devicefloordrain;
            }
        }
        public Point3dCollection Condensepipe_tofloordrain
        {
            get
            {
                return ThWDeviceFloordrainEngine.Condensepipe_tofloordrain;
            }
        }
        public Point3dCollection Rainpipe_tofloordrain
        {
            get
            {
                return ThWDeviceFloordrainEngine.Rainpipe_tofloordrain;
            }
        }
        public List<BlockReference> Floordrain_washing
        {
            get
            {
                return ThWBalconyFloordrainEngine.Floordrain_washing;
            }
        }

        public List<BlockReference> Floordrain
        {
            get
            {
                return ThWBalconyFloordrainEngine.Floordrain;
            }
        }
        public Point3dCollection Downspout_to_Floordrain
        {
            get
            {
                return ThWBalconyFloordrainEngine.Downspout_to_Floordrain;
            }
        }
        public Point3dCollection Bbasinline_to_Floordrain
        {
            get
            {
                return ThWBalconyFloordrainEngine.Bbasinline_to_Floordrain;
            }
        }
        public Point3dCollection Rainpipe_to_Floordrain
        {
            get
            {
                return ThWBalconyFloordrainEngine.Rainpipe_to_Floordrain;
            }
        }
        public Point3dCollection Bbasinline_Center
        {
            get
            {
                return ThWBalconyFloordrainEngine.Bbasinline_Center;
            }
        }

        public Circle new_circle
        {
            get
            {
                return ThWBalconyFloordrainEngine.new_circle;
            }
        }
        public List<Point3dCollection> Rainpipe_tofloordrains { get; set; }
        //
        public ThWCompositeFloordrainEngine(ThWBalconyFloordrainEngine thWBalconyFloordrainEngine, ThWToiletFloordrainEngine thWToiletFloordrainEngine, ThWDeviceFloordrainEngine thWDeviceFloordrainEngine)
        {
            ThWBalconyFloordrainEngine = thWBalconyFloordrainEngine;
            ThWToiletFloordrainEngine = thWToiletFloordrainEngine;
            ThWDeviceFloordrainEngine = thWDeviceFloordrainEngine;
            Rainpipe_tofloordrains = new List<Point3dCollection>();
        }
        public void Run(List<BlockReference> bfloordrain, Polyline bboundary, List<Polyline> rainpipe, Polyline downspout, BlockReference washingmachine, Polyline device, Polyline device_other, Polyline condensepipe, List<BlockReference> tfloordrain, Polyline tboundary, List<BlockReference> devicefloordrain, Polyline roofrainpipe, BlockReference bbasinline)
        {
            
            Polyline rainpipe_Device = null;
            Polyline rainpipe_Device_other = null;
            Polyline rainpipe_ = null;
            if (rainpipe.Count>0)
            {//给雨水管分类
                foreach (var rainpipe_boundary in rainpipe)
                {
                    if (GeomUtils.PtInLoop(device, rainpipe_boundary.GetCenter()))
                    {
                        rainpipe_Device = rainpipe_boundary;
                        break;
                    }
                }
                foreach (var rainpipe_boundary in rainpipe)
                { 
                    if (GeomUtils.PtInLoop(device_other, rainpipe_boundary.GetCenter()))
                    {
                        rainpipe_Device_other = rainpipe_boundary;
                        break;
                    }
                }
                foreach (var rainpipe_boundary in rainpipe)
                {
                    if (GeomUtils.PtInLoop(bboundary, rainpipe_boundary.GetCenter()))
                    {
                        rainpipe_ = rainpipe_boundary;
                        break;
                    }
                }
            }     
            else
            {
                //throw new ArgumentNullException();
            }
            if (Farfromwashmachine(washingmachine, device, device_other))
            {
                if(CondensepipeIsRoofPipe(washingmachine, roofrainpipe, condensepipe))
                {             
                     ThWBalconyFloordrainEngine.Run(bfloordrain, bboundary, rainpipe, downspout, washingmachine, device, device_other, roofrainpipe, bbasinline);
                }
                else
                {
                    ThWBalconyFloordrainEngine.Run(bfloordrain, bboundary, rainpipe, downspout, washingmachine, device, device_other, condensepipe, bbasinline);
                }
                
            }
            else
            {
                if (CondensepipeIsRoofPipe(washingmachine, roofrainpipe, condensepipe))
                {
                    ThWBalconyFloordrainEngine.Run(bfloordrain, bboundary, rainpipe, downspout, washingmachine, device_other, device, roofrainpipe, bbasinline);
                }
                else
                {
                    ThWBalconyFloordrainEngine.Run(bfloordrain, bboundary, rainpipe, downspout, washingmachine, device_other, device, condensepipe, bbasinline);
                }
            }         
            ThWDeviceFloordrainEngine.Run(rainpipe_Device, device, condensepipe, devicefloordrain, roofrainpipe);
            Rainpipe_tofloordrains.Add(ThWDeviceFloordrainEngine.Rainpipe_tofloordrain);

            if (device_other != null)
            {
                ThWDeviceFloordrainEngine.Run(rainpipe_Device_other, device_other, condensepipe, devicefloordrain, roofrainpipe);
                Rainpipe_tofloordrains.Add(ThWDeviceFloordrainEngine.Rainpipe_tofloordrain);
            }
        }
        private bool Farfromwashmachine(BlockReference washingmachine, Polyline device, Polyline device_other)
        { 
            if(device_other!=null&&washingmachine.Position.DistanceTo(device.GetCenter())> washingmachine.Position.DistanceTo(device_other.GetCenter()))
            {
                return true;
            }
        else
            {
                return false;
            }
        }
        private bool CondensepipeIsRoofPipe(BlockReference washingmachine, Polyline roofrainpipe, Polyline condensepipe)
        {          
            if ((roofrainpipe!=null&& (washingmachine.Position.DistanceTo(roofrainpipe.GetCenter())<800))||( condensepipe==null))
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
