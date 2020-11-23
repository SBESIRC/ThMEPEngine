using System;
using System.Collections.Generic;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using Linq2Acad;
using ThMEPWSS.Pipe.Model;

using Dreambuild.AutoCAD;


namespace ThMEPWSS.Pipe
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
        public Point3dCollection Rainpipe_to_Floordrain
        {
            get
            {
                return ThWBalconyFloordrainEngine.Rainpipe_to_Floordrain;
            }
        }

        public Circle new_circle
        {
            get
            {
                return ThWBalconyFloordrainEngine.new_circle;
            }
        }
        //
        public ThWCompositeFloordrainEngine(ThWBalconyFloordrainEngine thWBalconyFloordrainEngine, ThWToiletFloordrainEngine thWToiletFloordrainEngine, ThWDeviceFloordrainEngine thWDeviceFloordrainEngine)
        {
            ThWBalconyFloordrainEngine = thWBalconyFloordrainEngine;
            ThWToiletFloordrainEngine = thWToiletFloordrainEngine;
            ThWDeviceFloordrainEngine = thWDeviceFloordrainEngine;
        }
        public void Run(List<BlockReference> bfloordrain, Polyline bboundary, Polyline rainpipe, Polyline downspout, BlockReference washingmachine, Polyline device, Polyline device_other, Polyline condensepipe, List<BlockReference> tfloordrain, Polyline tboundary, List<BlockReference> devicefloordrain)
        {
            if (Farfromwashmachine(washingmachine, device, device_other))
            {
                ThWBalconyFloordrainEngine.Run(bfloordrain, bboundary, rainpipe, downspout, washingmachine, device, device_other, condensepipe);
            }
            else
            {
                ThWBalconyFloordrainEngine.Run(bfloordrain, bboundary, rainpipe, downspout, washingmachine, device_other, device, condensepipe);
            }
            //ThWToiletFloordrainEngine.Run(tfloordrain, tboundary);
            ThWDeviceFloordrainEngine.Run(rainpipe, device, condensepipe, devicefloordrain);
            ThWDeviceFloordrainEngine.Run(rainpipe, device_other, condensepipe, devicefloordrain);
        }
        private bool Farfromwashmachine(BlockReference washingmachine, Polyline device, Polyline device_other)
        { 
            if(washingmachine.Position.DistanceTo(device.GetCenter())> washingmachine.Position.DistanceTo(device_other.GetCenter()))
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
