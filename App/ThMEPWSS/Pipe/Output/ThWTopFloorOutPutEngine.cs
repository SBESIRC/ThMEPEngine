using Linq2Acad;
using ThCADExtension;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPWSS.Pipe.Engine;
using ThMEPWSS.Pipe.Model;
using ThMEPWSS.Pipe.Geom;
using ThMEPWSS.Pipe.Tools;
using AcHelper;
using ThMEPWSS.Pipe.Service;
using static ThMEPWSS.Command.ThPipeCreateCmd;

namespace ThMEPWSS.Pipe.Output
{
    public class ThWTopFloorOutPutEngine
    {
        public void LayoutTopFloor(ThWCompositeFloorRecognitionEngine FloorEngines, ThWTopParameters parameters0, AcadDatabase acadDatabase,string W_DRAI_EQPM,string W_DRAI_FLDR,string W_RAIN_PIPE)
        {
            parameters0.standardEntity.Add(new Circle() {Center= parameters0.baseCenter2[0] ,Radius=0.01});
            parameters0.divideLines = FloorEngines.TopFloors[0].DivisionLines;
            parameters0.pboundary = FloorEngines.TopFloors[0].Boundary as Polyline;
            var thWPipeOutputFunction=new ThWPipeOutputFunction();
            AddCompositeRooms(FloorEngines,  parameters0,  acadDatabase,thWPipeOutputFunction, W_DRAI_EQPM,W_DRAI_FLDR);
            AddCompositeCompanyRooms(FloorEngines, parameters0, acadDatabase, thWPipeOutputFunction, ThTagParametersService.PipeLayer, W_DRAI_EQPM, W_DRAI_FLDR, W_RAIN_PIPE);
        }
        private static void AddCompositeRooms(ThWCompositeFloorRecognitionEngine FloorEngines, ThWTopParameters parameters0, AcadDatabase acadDatabase, ThWPipeOutputFunction thWPipeOutputFunction,string W_DRAI_EQPM,string W_DRAI_FLDR)
        {
            foreach (var composite in FloorEngines.TopFloors[0].CompositeRooms)
            {
                var parameters = new ThWTopCompositeParameters();
                if (composite.Kitchen != null)
                {
                    thWPipeOutputFunction.InputKitchenParameters(composite, parameters, parameters0);
                }
                if (thWPipeOutputFunction.IsValidToiletContainer(composite.Toilet))
                {
                    InputToiletParameters(composite, parameters, parameters0);                 
                }
                if (thWPipeOutputFunction.IsValidToiletContainerForFloorDrain(composite.Toilet))
                {
                    InputToiletFloorParameters(composite,  parameters,  parameters0);                 
                }
                var toiletEngines = new ThWToiletPipeEngine()
                {
                    Parameters = new ThWToiletPipeParameters(ThTagParametersService.IsSeparation, ThTagParametersService.IsCaisson, ThTagParametersService.FloorValue),
                };
                var kitchenEngines = new ThWKitchenPipeEngine()
                {
                    Parameters = new ThWKitchenPipeParameters(1, ThTagParametersService.FloorValue),
                };
                var compositeEngine = new ThWCompositePipeEngine(kitchenEngines, toiletEngines);
                compositeEngine.Run(parameters.boundary, parameters.outline, parameters.basinline,
                    parameters.pype, parameters.boundary1, parameters.outline1, parameters.closestool);
                //var toiletfloorEngines = new ThWToiletFloordrainEngine();
                AddCompositeKitchenPipe(compositeEngine, acadDatabase, parameters0, W_DRAI_EQPM);
                AddCompositeToiletPipe( compositeEngine, acadDatabase, parameters0, W_DRAI_EQPM);             
                AddKitchenFloors(parameters, composite, acadDatabase, parameters0, W_DRAI_FLDR);
            }
        }
       
        private static void AddCompositeCompanyRooms(ThWCompositeFloorRecognitionEngine FloorEngines, ThWTopParameters parameters0, AcadDatabase acadDatabase, ThWPipeOutputFunction thWPipeOutputFunction,string pipeLayer,string W_DRAI_EQPM,string W_DRAI_FLDR,string W_RAIN_PIPE)
        {
            foreach (var compositeBalcony in FloorEngines.TopFloors[0].CompositeBalconyRooms)
            {   //判断是否为正确的Balcony
                if (thWPipeOutputFunction.IsValidBalconyForFloorDrain(compositeBalcony.Balcony))
                {
                    if(compositeBalcony.DevicePlatforms.Count>0)
                    {
                        if(compositeBalcony.DevicePlatforms[0]== null)
                        {
                            return;
                        }
                    }
                    var parameters = new ThWTopBalconyParameters();
                    ThWPipeOutputFunction.GetListFloorDrain(compositeBalcony, parameters).ForEach(o => parameters.bfloordrain.Add(o));                
                    parameters.bboundary = compositeBalcony.Balcony.Boundary as Polyline;
                    if (compositeBalcony.Balcony.RainPipes.Count > 0)
                    {
                        if (compositeBalcony.DevicePlatforms.Count == 0)
                        {
                            if (!(GeomUtils.PtInLoop(parameters.bboundary, compositeBalcony.Balcony.RainPipes[0].Outline.GetCenter())) && parameters.bboundary.GetCenter().DistanceTo(compositeBalcony.Balcony.RainPipes[0].Outline.GetCenter()) < ThWPipeCommon.MAX_BALCONY_TO_RAINPIPE_DISTANCE)
                            {
                                parameters.bboundary = ThWPipeOutputFunction.GetkitchenBoundary(parameters.bboundary, compositeBalcony.Balcony.RainPipes[0].Outline as Polyline);
                            }
                        }
                        else
                        {
                            if (!(GeomUtils.PtInLoop(parameters.bboundary, compositeBalcony.Balcony.RainPipes[0].Outline.GetCenter())) && parameters.bboundary.GetCenter().DistanceTo(compositeBalcony.Balcony.RainPipes[0].Outline.GetCenter()) < ThWPipeCommon.MAX_BALCONY_TO_RAINPIPE_DISTANCE)
                            {
                                int num = 0;
                                foreach(var room in compositeBalcony.DevicePlatforms)
                                {
                                    if(GeomUtils.PtInLoop(room.Boundary as Polyline, compositeBalcony.Balcony.RainPipes[0].Outline.GetCenter()))
                                    {
                                        num++;
                                        break;
                                    }                                  
                                }
                                if(num==0)
                                {
                                    parameters.bboundary = ThWPipeOutputFunction.GetkitchenBoundary(parameters.bboundary, compositeBalcony.Balcony.RainPipes[0].Outline as Polyline);
                                }
                                
                            }
                        }
                    }
                    parameters.bboundary.Closed = true;
                    if (compositeBalcony.Balcony.RainPipes.Count > 0)
                    {
                        ThWPipeOutputFunction.GetListRainPipes(compositeBalcony).ForEach(o => parameters.rainpipe.Add(o));
                        ThWPipeOutputFunction.GetListRainPipes(compositeBalcony).ForEach(o => parameters0.rain_pipe.Add(o));                      
                    }
                    foreach (var devicePlatform in compositeBalcony.DevicePlatforms)
                    {
                        if (devicePlatform.RainPipes.Count > 0)
                        {
                            foreach (var RainPipe in devicePlatform.RainPipes)
                            {
                                var ent = RainPipe.Outline as Polyline;
                                ent.Closed = true;
                                parameters.rainpipe.Add(ent);
                                parameters0.rain_pipe.Add(ent);
                            }
                        }
                    }
                    if (!(parameters.rainpipe.Count > 0))
                    { Active.Editor.WriteMessage("\n 缺雨水管"); }
                    if (compositeBalcony.Balcony.Washmachines.Count > 0)
                    {
                        parameters.washingmachine = compositeBalcony.Balcony.Washmachines[0].Outline as BlockReference;
                    }
                    if (compositeBalcony.DevicePlatforms.Count > 0)
                    {
                        if (compositeBalcony.DevicePlatforms.Count < 2)
                        {
                            parameters.device = compositeBalcony.DevicePlatforms[0].Boundary.Clone() as Polyline;
                            parameters.device.Closed = true;
                        }
                        else
                        {
                            double dst = double.MaxValue;
                            if (parameters.washingmachine != null)
                            {
                                for (int i = 0; i < compositeBalcony.DevicePlatforms.Count; i++)
                                {                                   
                                        if ((compositeBalcony.DevicePlatforms[i].Boundary as Polyline).GetCenter().DistanceTo(parameters.washingmachine.Position) < dst)
                                        {
                                            dst = (compositeBalcony.DevicePlatforms[i].Boundary as Polyline).GetCenter().DistanceTo(parameters.washingmachine.Position);
                                            parameters.device = compositeBalcony.DevicePlatforms[i].Boundary.Clone() as Polyline;
                                        }                                                                    
                                }
                            }
                            else
                            {
                                parameters.device = compositeBalcony.DevicePlatforms[0].Boundary.Clone() as Polyline;
                            }
                            parameters.device.Closed = true;
                        }
                    }
                    if (compositeBalcony.DevicePlatforms.Count > 1)
                    {
                        Polyline temp = parameters.device;
                        foreach(var device in compositeBalcony.DevicePlatforms)
                        {
                            var deviceLine = device.Boundary as Polyline;
                            if ((temp.GetCenter().DistanceTo(deviceLine.GetCenter()) > ThWPipeCommon.MAX_DEVICE_TO_DEVICE) && (temp.GetCenter().DistanceTo(parameters.bboundary.GetCenter()) < ThWPipeCommon.MAX_DEVICE_TO_BALCONY))
                            {
                                parameters.device_other = deviceLine;
                                parameters.device_other.Closed = true;
                                break;
                            }
                        }                                     
                    }
                    foreach (var devicePlatform in compositeBalcony.DevicePlatforms)
                    {
                        if (devicePlatform.CondensePipes.Count > 0)
                        {
                            parameters.condensepipe = devicePlatform.CondensePipes[0].Outline as Polyline;
                            break;
                        }
                    }
                    foreach (var devicePlatform in compositeBalcony.DevicePlatforms)
                    {
                        if (devicePlatform.RoofRainPipes.Count > 0)
                        {
                            parameters.roofrainpipe = devicePlatform.RoofRainPipes[0].Outline as Polyline;
                            parameters0.roofrain_pipe.Add(parameters.roofrainpipe);
                            parameters0.copyroofpipes.Add(new Circle() { Center = parameters.roofrainpipe.GetCenter(), Radius = 50.0 });
                            parameters0.copyroofpipes.Add(new Circle() { Center = parameters.roofrainpipe.GetCenter(), Radius = 38.5 });
                            parameters0.copyroofpipes.Add(new Circle() { Center = parameters.roofrainpipe.GetCenter(), Radius = 55.0 });
                            break;
                        }
                    }
                    foreach (var devicePlatform in compositeBalcony.DevicePlatforms)
                    {
                        BlockReference devicefloordrains = null;
                        if (devicePlatform.FloorDrains.Count > 0)
                        {

                            devicefloordrains = devicePlatform.FloorDrains[0].Outline as BlockReference;

                        }
                        parameters.devicefloordrain.Add(devicefloordrains);
                    }
                    if (compositeBalcony.Balcony.BasinTools.Count > 0)
                    {
                        parameters.bbasinline = compositeBalcony.Balcony.BasinTools[0].Outline as BlockReference;
                    }
                    foreach (var devicePlatform in compositeBalcony.DevicePlatforms)//考虑多余一个冷凝管
                    {
                        if (devicePlatform.CondensePipes.Count > 0)
                        {
                            Polyline s = devicePlatform.CondensePipes[0].Outline as Polyline;
                            parameters0.npipe.Add(s);
                            parameters.condensepipes.Add(s);
                        }
                    }
                    var thWBalconyFloordrainEngine = new ThWBalconyFloordrainEngine();
                    var thWToiletFloordrainEngine = new ThWToiletFloordrainEngine();
                    var thWDeviceFloordrainEngine = new ThWDeviceFloordrainEngine();
                    var FloordrainEngine = new ThWCompositeFloordrainEngine(thWBalconyFloordrainEngine, thWToiletFloordrainEngine, thWDeviceFloordrainEngine);
                    FloordrainEngine.Run(parameters.bfloordrain, parameters.bboundary, parameters.rainpipe, parameters.downspout,
                        parameters.washingmachine, parameters.device, parameters.device_other, parameters.condensepipe, parameters0.tfloordrain,
                        parameters.tboundary, parameters.devicefloordrain, parameters.roofrainpipe, parameters.bbasinline, parameters.condensepipes);
                    OutputBalconyParameters(FloordrainEngine, acadDatabase, parameters0, FloorEngines, pipeLayer, W_DRAI_EQPM, W_DRAI_FLDR, W_RAIN_PIPE);
                    //阳台输出完毕
                    OutputDeviceplatformParamters(FloordrainEngine, acadDatabase, parameters0, parameters, W_DRAI_FLDR, W_RAIN_PIPE);
                }
            }
        }
         
        private static void AddKitchenPipes(ThWKitchenPipe kitchenPipe,AcadDatabase acadDatabase,ThWTopParameters parameters0,Matrix3d offset,string W_DRAI_EQPM)
        {
            foreach (Entity item in kitchenPipe.Representation)
            {
                Entity polyline = item.GetTransformedCopy(kitchenPipe.Matrix.PostMultiplyBy(offset));
                polyline.Layer = W_DRAI_EQPM;
                parameters0.standardEntity.Add(polyline);               
                parameters0.fpipe.Add(ThWPipeOutputFunction.GetCopyPipes(polyline));
                parameters0.copypipes.Add(polyline);
                parameters0.normalCopys.Add(polyline);
            }
        }
        private static void AddKitchenPipes1(ThWKitchenPipe kitchenPipe, AcadDatabase acadDatabase, ThWTopParameters parameters0,string W_DRAI_EQPM)
        {
            foreach (Entity item in kitchenPipe.Representation)
            {
                Entity polyline = item.GetTransformedCopy(kitchenPipe.Matrix);
                polyline.Layer = W_DRAI_EQPM;
                parameters0.standardEntity.Add(polyline);              
                parameters0.fpipe.Add(ThWPipeOutputFunction.GetCopyPipes(polyline));
                parameters0.copypipes.Add(polyline);
                parameters0.normalCopys.Add(polyline);
            }
        }
        private static void AddKitchenFloors(ThWTopCompositeParameters parameters,ThWCompositeRoom composite,AcadDatabase acadDatabase, ThWTopParameters parameters0,string W_DRAI_FLDR)
        {
            for (int i = 0; i < parameters.tfloordrain_.Count; i++)
            {
                Matrix3d scale = Matrix3d.Scaling(2.0, parameters.tfloordrain_[i].Position);
                var ent = parameters.tfloordrain_[i].GetTransformedCopy(scale);
                ent.Layer = W_DRAI_FLDR;
                parameters0.standardEntity.Add(ent);
                if (!GeomUtils.PtInLoop(parameters.boundary1, parameters.tfloordrain_[i].Position) && composite.Toilet.CondensePipes.Count > 0)
                {
                    var line = new Line(parameters.tfloordrain_[i].Position + 50 * parameters.tfloordrain_[i].Position.GetVectorTo(composite.Toilet.CondensePipes[0].Outline.GetCenter()).GetNormal(),
                    composite.Toilet.CondensePipes[0].Outline.GetCenter() - 50 * parameters.tfloordrain_[i].Position.GetVectorTo(composite.Toilet.CondensePipes[0].Outline.GetCenter()).GetNormal());
                    parameters0.standardEntity.Add(line);
                    parameters0.normalCopys.Add(line);
                }
            }
        }
        private static Polyline GetToiletBoundary(Polyline roofSpaces, Polyline StandardSpaces)
        {
            var pts = new Point3dCollection();
            pts.Add(roofSpaces.GeometricExtents.MinPoint);
            pts.Add(roofSpaces.GeometricExtents.MaxPoint);
            pts.Add(roofSpaces.GeometricExtents.MinPoint);
            pts.Add(roofSpaces.GeometricExtents.MaxPoint);
            double minpt_x = double.MinValue;
            double minpt_y = double.MinValue;
            double maxpt_x = double.MaxValue;
            double maxpt_y = double.MaxValue;
            for (int i = 0; i < pts.Count; i++)
            {
                if (pts[i].X > minpt_x)
                {
                    minpt_x = pts[i].X;
                }
                if (pts[i].Y > minpt_y)
                {
                    minpt_y = pts[i].Y;
                }
                if (pts[i].X < maxpt_x)
                {
                    maxpt_x = pts[i].X;
                }
                if (pts[i].Y < maxpt_y)
                {
                    maxpt_y = pts[i].Y;
                }
            }
            return GetNewPolyline(maxpt_x, maxpt_y, minpt_x, minpt_y);
        }
        private static Polyline GetNewPolyline(double x1, double y1, double x2, double y2)
        {
            Polyline polyline = new Polyline()
            {
                Closed = true
            };
            polyline.AddVertexAt(0, new Point2d(x1, y1), 0.0, 0.0, 0.0);
            polyline.AddVertexAt(1, new Point2d(x2, y1), 0.0, 0.0, 0.0);
            polyline.AddVertexAt(2, new Point2d(x2, y2), 0.0, 0.0, 0.0);
            polyline.AddVertexAt(3, new Point2d(x1, y2), 0.0, 0.0, 0.0);
            return polyline;
        }
        private static void InputToiletParameters(ThWCompositeRoom composite,ThWTopCompositeParameters parameters,ThWTopParameters parameters0)
        {

            parameters.boundary1 = composite.Toilet.Boundary as Polyline;
            if (composite.Toilet.DrainageWells.Count > 1)
            {
                var well = composite.Toilet.DrainageWells[0].Boundary as Polyline;
                var well1= composite.Toilet.DrainageWells[1].Boundary as Polyline;
                if (well != null && well1 != null)
                {
                    if (well.GetCenter().DistanceTo(parameters.boundary1.GetCenter()) < well1.GetCenter().DistanceTo(parameters.boundary1.GetCenter()))
                    {
                        parameters.outline1 = well;
                    }
                    else
                    {
                        parameters.outline1 = well1;
                    }
                }
            }
            else
            {
                parameters.outline1 = composite.Toilet.DrainageWells[0].Boundary as Polyline;
            }
            if (parameters.outline1 != null)
            {
                if (!(GeomUtils.PtInLoop(parameters.boundary1, parameters.outline1.GetCenter())))
                {
                    parameters.boundary1 = GetToiletBoundary(parameters.boundary1, parameters.outline1);
                }
            }
            if (composite.Toilet.DrainageWells.Count > 1)
            {
                foreach (var drainWell in composite.Toilet.DrainageWells)
                {
                    var wellOutline = drainWell.Boundary as Polyline;
                    if (wellOutline != null)
                    {
                        if (GeomUtils.PtInLoop(parameters.boundary1, wellOutline.GetCenter()))
                        {
                            parameters.outline1 = wellOutline;
                            break;
                        }
                    }
                }
            }
            if(parameters.outline1==null)//特殊情况boundary未先确定
            {
                foreach (var drainWell in composite.Toilet.DrainageWells)
                {
                    var wellOutline = drainWell.Boundary as Polyline;
                    if(wellOutline != null)
                    {
                        parameters.outline1 = wellOutline;
                        break;
                    }
                }
                if (!(GeomUtils.PtInLoop(parameters.boundary1, parameters.outline1.GetCenter())))
                {
                    parameters.boundary1 = GetToiletBoundary(parameters.boundary1, parameters.outline1);
                }
            }
            foreach (Point3d point in ThTagParametersService.ToiletWells)
            {
                if (parameters.outline1 != null)
                {
                    if (point.DistanceTo(parameters.outline1.GetCenter()) < 1)
                    {
                        return;
                    }
                }
            }
            ThTagParametersService.ToiletWells.Add(parameters.outline1.GetCenter());
            parameters.closestool = composite.Toilet.Closestools[0].Outline as Polyline;
            if (composite.Toilet.CondensePipes.Count > 0)
            {
                foreach (var pipe in composite.Toilet.CondensePipes)
                {
                    parameters0.npipe.Add(pipe.Outline as Polyline);
                }
            }
            if (composite.Toilet.RoofRainPipes.Count > 0)
            {
                Polyline s = composite.Toilet.RoofRainPipes[0].Outline as Polyline;
                parameters0.roofrain_pipe.Add(s);
                parameters0.copyroofpipes.Add(new Circle() { Center = s.GetCenter(), Radius = 38.5 });
                parameters0.copyroofpipes.Add(new Circle() { Center = s.GetCenter(), Radius = 55.0 });
            }
        }
        private static void InputToiletFloorParameters(ThWCompositeRoom composite, ThWTopCompositeParameters parameters, ThWTopParameters parameters0)
        {
            foreach (var FloorDrain in composite.Toilet.FloorDrains)
            {
                parameters.floordrain = FloorDrain.Outline as BlockReference;
                parameters0.tfloordrain.Add(parameters.floordrain);
                parameters.tfloordrain_.Add(parameters.floordrain);
            }
            foreach (var FloorDrain in composite.Kitchen.FloorDrains)
            {
                parameters.floordrain = FloorDrain.Outline as BlockReference;
                parameters0.tfloordrain.Add(parameters.floordrain);
                parameters.tfloordrain_.Add(parameters.floordrain);
            }

        }
        private static void AddCompositeKitchenPipe(ThWCompositePipeEngine compositeEngine,AcadDatabase acadDatabase,ThWTopParameters parameters0,string W_DRAI_EQPM)
        {
            foreach (var kitchenPipe in compositeEngine.KitchenPipes)
            {
                if (compositeEngine.ToiletPipes.Count > 0 && (compositeEngine.ToiletPipes[0].Center.DistanceTo(kitchenPipe.Center) < 101))
                {
                    var offset = Matrix3d.Displacement(kitchenPipe.Center.GetVectorTo(compositeEngine.ToiletPipes[0].Center));
                    AddKitchenPipes(kitchenPipe, acadDatabase, parameters0, offset, W_DRAI_EQPM);
                }
                else
                {
                    AddKitchenPipes1(kitchenPipe, acadDatabase, parameters0, W_DRAI_EQPM);
                }
            }
        }
        private static void AddCompositeToiletPipe(ThWCompositePipeEngine compositeEngine, AcadDatabase acadDatabase, ThWTopParameters parameters0,string W_DRAI_EQPM)
        {
            if (compositeEngine.ToiletPipes.Count > 0)
            {
                if (compositeEngine.KitchenPipes.Count > 0 && compositeEngine.ToiletPipes[0].Center.DistanceTo(compositeEngine.KitchenPipes[0].Center) < 101)
                {
                    ThWCompositeTagOutPutEngine.LayoutToiletPipe(compositeEngine, parameters0, acadDatabase, W_DRAI_EQPM);
                }
                else
                {
                    ThWCompositeTagOutPutEngine.LayoutToiletPipe1(compositeEngine, parameters0, acadDatabase, W_DRAI_EQPM);

                }
            }
            else
            {
                return;
            }
        }
        private static void OutputBalconyParameters(ThWCompositeFloordrainEngine FloordrainEngine,AcadDatabase acadDatabase,ThWTopParameters parameters0, ThWCompositeFloorRecognitionEngine FloorEngines,string pipeLayer,string W_DRAI_EQPM,string W_DRAI_FLDR,string W_RAIN_PIPE)
        {
            for (int i = 0; i < FloordrainEngine.Floordrain.Count; i++)
            {//放大标识其他地漏
                Matrix3d scale = Matrix3d.Scaling(2.0, FloordrainEngine.Floordrain[i].Position);
                var ent = FloordrainEngine.Floordrain[i].GetTransformedCopy(scale);
                ent.Layer = W_DRAI_FLDR;
                parameters0.standardEntity.Add(ent);
            }
            if (FloordrainEngine.Floordrain_washing.Count > 0)
            {
                Matrix3d scale_washing = Matrix3d.Scaling(1.0, FloordrainEngine.Floordrain_washing[0].Position);
                var ent_washing = FloordrainEngine.Floordrain_washing[0].GetTransformedCopy(scale_washing);
                ent_washing.Layer = W_DRAI_FLDR;
                parameters0.standardEntity.Add(ent_washing);
            }
            for (int i = 0; i < FloordrainEngine.Downspout_to_Floordrain.Count - 1; i++)
            {
                parameters0.standardEntity.Add(ThWPipeOutputFunction.CreatePolyline(FloordrainEngine.Downspout_to_Floordrain[i], FloordrainEngine.Downspout_to_Floordrain[i + 1], FloorEngines.Layers, pipeLayer)) ;
                parameters0.normalCopys.Add(ThWPipeOutputFunction.CreatePolyline(FloordrainEngine.Downspout_to_Floordrain[i], FloordrainEngine.Downspout_to_Floordrain[i + 1],FloorEngines.Layers, pipeLayer));
            }
            FloordrainEngine.new_circle.Color = Autodesk.AutoCAD.Colors.Color.FromRgb(0, 255, 255);
            FloordrainEngine.new_circle.Layer= W_DRAI_EQPM;
            parameters0.standardEntity.Add(FloordrainEngine.new_circle);
            Polyline downpipe = FloordrainEngine.new_circle.Tessellate(50);
            downpipe.Layer = W_DRAI_EQPM;
            parameters0.fpipe.Add(downpipe);
            parameters0.copypipes.Add(FloordrainEngine.new_circle);
            parameters0.normalCopys.Add(FloordrainEngine.new_circle);      
            if (FloordrainEngine.Rainpipe_to_Floordrain.Count > 0)
            {
                for (int i = 0; i < FloordrainEngine.Rainpipe_to_Floordrain.Count - 1; i++)
                {
                    parameters0.standardEntity.Add(ThWPipeOutputFunction.CreateRainlines(FloordrainEngine.Rainpipe_to_Floordrain[i], FloordrainEngine.Rainpipe_to_Floordrain[i + 1], W_RAIN_PIPE));
                    parameters0.normalCopys.Add(ThWPipeOutputFunction.CreateRainlines(FloordrainEngine.Rainpipe_to_Floordrain[i], FloordrainEngine.Rainpipe_to_Floordrain[i + 1], W_RAIN_PIPE));
                }
                if(FloordrainEngine.Bbasinline_to_Floordrain.Count==0)
                {
                    if (FloordrainEngine.Bbasinline_Center.Count > 0)
                    {
                        parameters0.standardEntity.Add(new Circle() { Radius = 50, Center = FloordrainEngine.Bbasinline_Center[0], Layer = W_DRAI_EQPM });
                        parameters0.normalCopys.Add(new Circle() { Radius = 50, Center = FloordrainEngine.Bbasinline_Center[0], Layer = W_DRAI_EQPM });
                    }
                }
            }
            if (FloordrainEngine.Bbasinline_to_Floordrain.Count > 0)
            {
                for (int i = 0; i < FloordrainEngine.Bbasinline_to_Floordrain.Count - 1; i++)
                {
                    parameters0.standardEntity.Add(ThWPipeOutputFunction.CreatePolyline(FloordrainEngine.Bbasinline_to_Floordrain[i], FloordrainEngine.Bbasinline_to_Floordrain[i + 1], FloorEngines.Layers, pipeLayer));
                    parameters0.normalCopys.Add(ThWPipeOutputFunction.CreatePolyline(FloordrainEngine.Bbasinline_to_Floordrain[i], FloordrainEngine.Bbasinline_to_Floordrain[i + 1], FloorEngines.Layers, pipeLayer));
                }
                if (FloordrainEngine.Bbasinline_Center.Count > 0)
                {
                    parameters0.standardEntity.Add(new Circle() { Radius = 50, Center = FloordrainEngine.Bbasinline_Center[0], Layer = W_DRAI_EQPM });
                    parameters0.normalCopys.Add(new Circle() { Radius = 50, Center = FloordrainEngine.Bbasinline_Center[0], Layer = W_DRAI_EQPM });
                }
            }
        }
        private static void OutputDeviceplatformParamters(ThWCompositeFloordrainEngine FloordrainEngine, AcadDatabase acadDatabase, ThWTopParameters parameters0,ThWTopBalconyParameters parameters,string W_DRAI_FLDR,string W_RAIN_PIPE)
        {
            for (int i = 0; i < parameters.devicefloordrain.Count; i++)
            {
                Matrix3d scale = Matrix3d.Scaling(2.0, parameters.devicefloordrain[i].Position);
                var ent = parameters.devicefloordrain[i].GetTransformedCopy(scale);
                ent.Layer = W_DRAI_FLDR;
                parameters0.standardEntity.Add(ent);
            }
            if (FloordrainEngine.Condensepipe_tofloordrains.Count > 1)
            {
                foreach (Point3dCollection Rainpipe_ in FloordrainEngine.Condensepipe_tofloordrains)
                {
                    for (int i = 0; i < Rainpipe_.Count - 1; i++)
                    {
                        parameters0.standardEntity.Add(ThWPipeOutputFunction.CreateRainline(Rainpipe_[i], Rainpipe_[i + 1], W_RAIN_PIPE));
                        parameters0.normalCopys.Add(ThWPipeOutputFunction.CreateRainline(Rainpipe_[i], Rainpipe_[i + 1], W_RAIN_PIPE));
                    }
                }
            }
            else
            {
                for (int i = 0; i < FloordrainEngine.Condensepipe_tofloordrain.Count - 1; i++)
                {
                    parameters0.standardEntity.Add(ThWPipeOutputFunction.CreateRainline(FloordrainEngine.Condensepipe_tofloordrain[i], FloordrainEngine.Condensepipe_tofloordrain[i + 1], W_RAIN_PIPE));
                    parameters0.normalCopys.Add(ThWPipeOutputFunction.CreateRainline(FloordrainEngine.Condensepipe_tofloordrain[i], FloordrainEngine.Condensepipe_tofloordrain[i + 1], W_RAIN_PIPE));
                }
            }
            if (FloordrainEngine.Rainpipe_tofloordrains.Count > 1)
            {
                foreach (Point3dCollection Rainpipe_to in FloordrainEngine.Rainpipe_tofloordrains)
                {
                    for (int i = 0; i < Rainpipe_to.Count - 1; i++)
                    {
                        parameters0.standardEntity.Add(ThWPipeOutputFunction.CreateRainline(Rainpipe_to[i], Rainpipe_to[i + 1], W_RAIN_PIPE));
                        parameters0.normalCopys.Add(ThWPipeOutputFunction.CreateRainline(Rainpipe_to[i], Rainpipe_to[i + 1], W_RAIN_PIPE));
                    }
                }
            }
            else
            {
                for (int i = 0; i < FloordrainEngine.Rainpipe_tofloordrain.Count - 1; i++)
                {
                    parameters0.standardEntity.Add(ThWPipeOutputFunction.CreateRainline(FloordrainEngine.Rainpipe_tofloordrain[i], FloordrainEngine.Rainpipe_tofloordrain[i + 1], W_RAIN_PIPE));
                    parameters0.normalCopys.Add(ThWPipeOutputFunction.CreateRainline(FloordrainEngine.Rainpipe_tofloordrain[i], FloordrainEngine.Rainpipe_tofloordrain[i + 1], W_RAIN_PIPE));
                }
            }
        }
    }
}
