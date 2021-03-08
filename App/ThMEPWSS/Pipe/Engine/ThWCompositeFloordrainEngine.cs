using System;
using System.Collections.Generic;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPWSS.Pipe.Geom;
using Dreambuild.AutoCAD;

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
        public List<Point3dCollection> Condensepipe_tofloordrains { get; set; }
        //
        public ThWCompositeFloordrainEngine(ThWBalconyFloordrainEngine thWBalconyFloordrainEngine, ThWToiletFloordrainEngine thWToiletFloordrainEngine, ThWDeviceFloordrainEngine thWDeviceFloordrainEngine)
        {
            ThWBalconyFloordrainEngine = thWBalconyFloordrainEngine;
            ThWToiletFloordrainEngine = thWToiletFloordrainEngine;
            ThWDeviceFloordrainEngine = thWDeviceFloordrainEngine;
            Rainpipe_tofloordrains = new List<Point3dCollection>();
            Condensepipe_tofloordrains = new List<Point3dCollection>();
        }
        public void Run(List<BlockReference> bfloordrain, Polyline bboundary, List<Polyline> rainpipe, Polyline downspout, BlockReference washingmachine, Polyline device, Polyline device_other, Polyline condensepipe, List<BlockReference> tfloordrain, Polyline tboundary, List<BlockReference> devicefloordrain, Polyline roofrainpipe, BlockReference bbasinline,List<Polyline> condensepipes)
        {
            
            Polyline rainpipe_Device = null;
            Polyline rainpipe_Device_other = null;
            Polyline rainpipe_ = null;
            if (rainpipe.Count>0)
            {//给雨水管分类
                foreach (var rainpipe_boundary in rainpipe)
                {
                    if (device != null)
                    {
                        if (GeomUtils.PtInLoop(device, rainpipe_boundary.GetCenter()))
                        {
                            rainpipe_Device = rainpipe_boundary;
                            break;
                        }
                    }
                    else
                    {
                        break;
                    }
                }
                foreach (var rainpipe_boundary in rainpipe)
                {
                    if (device_other != null)
                    {
                        if (GeomUtils.PtInLoop(device_other, rainpipe_boundary.GetCenter()))
                        {
                            rainpipe_Device_other = rainpipe_boundary;
                            break;
                        }
                    }
                    else
                    {
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
            var parameters = new ThWBalconyFloordrainEngineParameters()
            {
                
                boundary = bboundary,
                downspout= downspout,     
                rainpipes = rainpipe,
                device = device,
                device_other = device_other,
                condensepipe= condensepipe,
                condensepipes= condensepipes,
                washingmachine = washingmachine,
                basinline= bbasinline,
                floordrains = bfloordrain,
            };
            var parameters1 = new ThWBalconyFloordrainEngineParameters()
            {

                boundary = bboundary,
                downspout = downspout,
                rainpipes = rainpipe,
                device = device,
                device_other = device_other,
                condensepipe = roofrainpipe,
                condensepipes = condensepipes,
                washingmachine = washingmachine,
                basinline = bbasinline,
                floordrains = bfloordrain,
            };
            var parameters2 = new ThWBalconyFloordrainEngineParameters()
            {

                boundary = bboundary,
                downspout = downspout,
                rainpipes = rainpipe,
                device = device_other,
                device_other = device,
                condensepipe = condensepipe,
                condensepipes = condensepipes,
                washingmachine = washingmachine,
                basinline = bbasinline,
                floordrains = bfloordrain,
            };
            var parameters3 = new ThWBalconyFloordrainEngineParameters()
            {

                boundary = bboundary,
                downspout = downspout,
                rainpipes = rainpipe,
                device = device_other,
                device_other = device,
                condensepipe = roofrainpipe,
                condensepipes = condensepipes,
                washingmachine = washingmachine,
                basinline = bbasinline,
                floordrains = bfloordrain,
            };
            if (Farfromwashmachine(washingmachine, device, device_other, bbasinline))
            {
               
                if (CondensepipeIsRoofPipe(washingmachine, roofrainpipe, condensepipe, bbasinline))
                {

                    ThWBalconyFloordrainEngine.Run(parameters1);
                }
                else
                {
                    ThWBalconyFloordrainEngine.Run(parameters);
                }

            }
            else
            {
                if (CondensepipeIsRoofPipe(washingmachine, roofrainpipe, condensepipe, bbasinline))
                {
                    ThWBalconyFloordrainEngine.Run(parameters3);
                }
                else
                {
                    ThWBalconyFloordrainEngine.Run(parameters2);
                }
            }         
            ThWDeviceFloordrainEngine.Run(rainpipe_Device, device, condensepipes, devicefloordrain, roofrainpipe);
            Rainpipe_tofloordrains.Add(ThWDeviceFloordrainEngine.Rainpipe_tofloordrain);
            Condensepipe_tofloordrains.Add(ThWDeviceFloordrainEngine.Condensepipe_tofloordrain);
            if (device_other != null)
            {
                ThWDeviceFloordrainEngine.Run(rainpipe_Device_other, device_other, condensepipes, devicefloordrain, roofrainpipe);
                Rainpipe_tofloordrains.Add(ThWDeviceFloordrainEngine.Rainpipe_tofloordrain);
                Condensepipe_tofloordrains.Add(ThWDeviceFloordrainEngine.Condensepipe_tofloordrain);
            }
        }
        private bool Farfromwashmachine(BlockReference washingmachine, Polyline device, Polyline device_other,BlockReference bbasinline)
        {
            if (washingmachine != null)
            {
                if (device_other != null && washingmachine.Position.DistanceTo(device.GetCenter()) > washingmachine.Position.DistanceTo(device_other.GetCenter()))
                {
                    return true;
                }
            }
            else 
            {
                if (bbasinline != null)
                {
                    if (device_other != null && bbasinline.Position.DistanceTo(device.GetCenter()) > bbasinline.Position.DistanceTo(device_other.GetCenter()))
                    {
                        return true;
                    }
                }
            }
                   
                return false;            
        }
        private bool CondensepipeIsRoofPipe(BlockReference washingmachine, Polyline roofrainpipe, Polyline condensepipe, BlockReference bbasinline)
        {
            if (washingmachine != null)
            {
                if ((roofrainpipe != null && (washingmachine.Position.DistanceTo(roofrainpipe.GetCenter()) < 800)) || (condensepipe == null))
                {
                    return true;
                }
            }
            else
            { 
                if(bbasinline!=null)
                {
                    if ((roofrainpipe != null && (bbasinline.Position.DistanceTo(roofrainpipe.GetCenter()) < 800)) || (condensepipe == null))
                    {
                        return true;
                    }
                }
                
            }
            return false;
        }
    }
}
