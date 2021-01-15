﻿using Linq2Acad;
using ThCADExtension;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPWSS.Pipe.Engine;
using ThMEPWSS.Pipe.Model;
using ThMEPWSS.Pipe.Geom;
using ThMEPWSS.Pipe.Tools;
using static ThMEPWSS.ThPipeCmds;
using AcHelper;

namespace ThMEPWSS.Pipe.Output
{
    public class ThWTopFloorOutPutEngine
    {
        public void LayoutTopFloor(ThWCompositeFloorRecognitionEngine FloorEngines, ThWTopParameters parameters0, AcadDatabase acadDatabase, InputInfo userInfo)
        {
            parameters0.divideLines = FloorEngines.TopFloors[0].DivisionLines;
            parameters0.pboundary = FloorEngines.TopFloors[0].FirstFloor.Boundary as Polyline;
            var thWPipeOutputFunction=new ThWPipeOutputFunction();
            AddCompositeRooms(FloorEngines,  parameters0,  acadDatabase,  userInfo,thWPipeOutputFunction);
            AddCompositeCompanyRooms(FloorEngines, parameters0, acadDatabase, userInfo, thWPipeOutputFunction);
        }
        private static void AddCompositeRooms(ThWCompositeFloorRecognitionEngine FloorEngines, ThWTopParameters parameters0, AcadDatabase acadDatabase, InputInfo userInfo, ThWPipeOutputFunction thWPipeOutputFunction)
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
                var zone = new ThWPipeZone();
                var toiletEngines = new ThWToiletPipeEngine()
                {
                    Zone = zone,
                    Parameters = new ThWToiletPipeParameters(userInfo.IsSeparation, userInfo.IsCaisson, userInfo.FloorValue),

                };
                var kitchenEngines = new ThWKitchenPipeEngine()
                {
                    Zone = zone,
                    Parameters = new ThWKitchenPipeParameters(1, userInfo.FloorValue),
                };
                var compositeEngine = new ThWCompositePipeEngine(kitchenEngines, toiletEngines);
                compositeEngine.Run(parameters.boundary, parameters.outline, parameters.basinline,
                    parameters.pype, parameters.boundary1, parameters.outline1, parameters.closestool);
                //var toiletfloorEngines = new ThWToiletFloordrainEngine();
                AddCompositeKitchenPipe(compositeEngine, acadDatabase, parameters0);
                AddCompositeToiletPipe( compositeEngine, acadDatabase, parameters0);             
                AddKitchenFloors(parameters, composite, acadDatabase, parameters0);
            }
        }
       
        private static void AddCompositeCompanyRooms(ThWCompositeFloorRecognitionEngine FloorEngines, ThWTopParameters parameters0, AcadDatabase acadDatabase, InputInfo userInfo, ThWPipeOutputFunction thWPipeOutputFunction)
        {
            foreach (var compositeBalcony in FloorEngines.TopFloors[0].CompositeBalconyRooms)
            {   //判断是否为正确的Balcony
                if (thWPipeOutputFunction.IsValidBalconyForFloorDrain(compositeBalcony.Balcony))
                {
                    var parameters = new ThWTopBalconyParameters();
                    ThWPipeOutputFunction.GetListFloorDrain(compositeBalcony, parameters).ForEach(o => parameters.bfloordrain.Add(o));                
                    parameters.bboundary = compositeBalcony.Balcony.Balcony.Boundary as Polyline;
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
                        parameters.device = compositeBalcony.DevicePlatforms[0].DevicePlatforms[0].Boundary.Clone() as Polyline;
                        parameters.device.Closed = true;
                    }
                    if (compositeBalcony.DevicePlatforms.Count > 1)
                    {
                        Polyline temp = compositeBalcony.DevicePlatforms[1].DevicePlatforms[0].Boundary as Polyline;
                        if ((temp.GetCenter().DistanceTo(parameters.device.GetCenter()) > 2))
                        {
                            parameters.device_other = compositeBalcony.DevicePlatforms[1].DevicePlatforms[0].Boundary.Clone() as Polyline;
                            parameters.device_other.Closed = true;
                        }
                        else
                        {
                            parameters.device_other = compositeBalcony.DevicePlatforms[2].DevicePlatforms[0].Boundary.Clone() as Polyline;
                            parameters.device_other.Closed = true;
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
                    OutputBalconyParameters(FloordrainEngine, acadDatabase, parameters0);
                    //阳台输出完毕
                    OutputDeviceplatformParamters(FloordrainEngine, acadDatabase, parameters0, parameters);
                }
            }
        }
         
        private static void AddKitchenPipes(ThWKitchenPipe kitchenPipe,AcadDatabase acadDatabase,ThWTopParameters parameters0,Matrix3d offset)
        {
            foreach (Entity item in kitchenPipe.Representation)
            {
                acadDatabase.ModelSpace.Add(item.GetTransformedCopy(kitchenPipe.Matrix.PostMultiplyBy(offset)));
                Entity polyline = item.GetTransformedCopy(kitchenPipe.Matrix.PostMultiplyBy(offset));
                parameters0.fpipe.Add(ThWPipeOutputFunction.GetCopyPipes(polyline));
                parameters0.copypipes.Add(polyline);
                parameters0.normalCopys.Add(polyline);
            }
        }
        private static void AddKitchenPipes1(ThWKitchenPipe kitchenPipe, AcadDatabase acadDatabase, ThWTopParameters parameters0)
        {
            foreach (Entity item in kitchenPipe.Representation)
            {
                acadDatabase.ModelSpace.Add(item.GetTransformedCopy(kitchenPipe.Matrix));
                Entity polyline = item.GetTransformedCopy(kitchenPipe.Matrix);
                parameters0.fpipe.Add(ThWPipeOutputFunction.GetCopyPipes(polyline));
                parameters0.copypipes.Add(polyline);
                parameters0.normalCopys.Add(polyline);
            }
        }
        private static void AddKitchenFloors(ThWTopCompositeParameters parameters,ThWCompositeRoom composite,AcadDatabase acadDatabase, ThWTopParameters parameters0)
        {
            for (int i = 0; i < parameters.tfloordrain_.Count; i++)
            {
                Matrix3d scale = Matrix3d.Scaling(2.0, parameters.tfloordrain_[i].Position);
                var ent = parameters.tfloordrain_[i].GetTransformedCopy(scale);
                acadDatabase.ModelSpace.Add(ent);
                if (!GeomUtils.PtInLoop(parameters.boundary1, parameters.tfloordrain_[i].Position) && composite.Toilet.CondensePipes.Count > 0)
                {
                    var line = new Line(parameters.tfloordrain_[i].Position + 50 * parameters.tfloordrain_[i].Position.GetVectorTo(composite.Toilet.CondensePipes[0].Outline.GetCenter()).GetNormal(),
                    composite.Toilet.CondensePipes[0].Outline.GetCenter() - 50 * parameters.tfloordrain_[i].Position.GetVectorTo(composite.Toilet.CondensePipes[0].Outline.GetCenter()).GetNormal());
                    acadDatabase.ModelSpace.Add(line);
                    parameters0.normalCopys.Add(line);
                }
            }
        }
        private static void InputToiletParameters(ThWCompositeRoom composite,ThWTopCompositeParameters parameters,ThWTopParameters parameters0)
        {
            parameters.boundary1 = composite.Toilet.Toilet.Boundary as Polyline;
            parameters.outline1 = composite.Toilet.DrainageWells[0].Boundary as Polyline;
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
        }
        private static void AddCompositeKitchenPipe(ThWCompositePipeEngine compositeEngine,AcadDatabase acadDatabase,ThWTopParameters parameters0)
        {
            foreach (var kitchenPipe in compositeEngine.KitchenPipes)
            {
                if (compositeEngine.ToiletPipes.Count > 0 && (compositeEngine.ToiletPipes[0].Center.DistanceTo(kitchenPipe.Center) < 101))
                {
                    var offset = Matrix3d.Displacement(kitchenPipe.Center.GetVectorTo(compositeEngine.ToiletPipes[0].Center));
                    AddKitchenPipes(kitchenPipe, acadDatabase, parameters0, offset);
                }
                else
                {
                    AddKitchenPipes1(kitchenPipe, acadDatabase, parameters0);
                }
            }
        }
        private static void AddCompositeToiletPipe(ThWCompositePipeEngine compositeEngine, AcadDatabase acadDatabase, ThWTopParameters parameters0)
        {
            if (compositeEngine.ToiletPipes.Count > 0)
            {
                if (compositeEngine.KitchenPipes.Count > 0 && compositeEngine.ToiletPipes[0].Center.DistanceTo(compositeEngine.KitchenPipes[0].Center) < 101)
                {
                    ThWCompositeTagOutPutEngine.LayoutToiletPipe(compositeEngine, parameters0, acadDatabase);
                }
                else
                {
                    ThWCompositeTagOutPutEngine.LayoutToiletPipe1(compositeEngine, parameters0, acadDatabase);

                }
            }
            else
            {
                return;
            }
        }
        private static void OutputBalconyParameters(ThWCompositeFloordrainEngine FloordrainEngine,AcadDatabase acadDatabase,ThWTopParameters parameters0)
        {
            for (int i = 0; i < FloordrainEngine.Floordrain.Count; i++)
            {//放大标识其他地漏
                Matrix3d scale = Matrix3d.Scaling(2.0, FloordrainEngine.Floordrain[i].Position);
                var ent = FloordrainEngine.Floordrain[i].GetTransformedCopy(scale);
                acadDatabase.ModelSpace.Add(ent);
            }
            Matrix3d scale_washing = Matrix3d.Scaling(1.0, FloordrainEngine.Floordrain_washing[0].Position);
            var ent_washing = FloordrainEngine.Floordrain_washing[0].GetTransformedCopy(scale_washing);
            acadDatabase.ModelSpace.Add(ent_washing);
            for (int i = 0; i < FloordrainEngine.Downspout_to_Floordrain.Count - 1; i++)
            {
                acadDatabase.ModelSpace.Add(ThWPipeOutputFunction.CreatePolyline(FloordrainEngine.Downspout_to_Floordrain[i], FloordrainEngine.Downspout_to_Floordrain[i + 1]));
                parameters0.normalCopys.Add(ThWPipeOutputFunction.CreatePolyline(FloordrainEngine.Downspout_to_Floordrain[i], FloordrainEngine.Downspout_to_Floordrain[i + 1]));
            }
            FloordrainEngine.new_circle.Color = Autodesk.AutoCAD.Colors.Color.FromRgb(0, 255, 255);
            acadDatabase.ModelSpace.Add(FloordrainEngine.new_circle);
            Polyline downpipe = FloordrainEngine.new_circle.Tessellate(50);
            parameters0.fpipe.Add(downpipe);
            parameters0.copypipes.Add(FloordrainEngine.new_circle);
            parameters0.normalCopys.Add(FloordrainEngine.new_circle);      
            if (FloordrainEngine.Rainpipe_to_Floordrain.Count > 0)
            {
                for (int i = 0; i < FloordrainEngine.Rainpipe_to_Floordrain.Count - 1; i++)
                {
                    acadDatabase.ModelSpace.Add(ThWPipeOutputFunction.CreatePolyline(FloordrainEngine.Rainpipe_to_Floordrain[i], FloordrainEngine.Rainpipe_to_Floordrain[i + 1]));
                    parameters0.normalCopys.Add(ThWPipeOutputFunction.CreatePolyline(FloordrainEngine.Rainpipe_to_Floordrain[i], FloordrainEngine.Rainpipe_to_Floordrain[i + 1]));
                }
            }
            if (FloordrainEngine.Bbasinline_to_Floordrain.Count > 0)
            {
                for (int i = 0; i < FloordrainEngine.Bbasinline_to_Floordrain.Count - 1; i++)
                {
                    acadDatabase.ModelSpace.Add(ThWPipeOutputFunction.CreatePolyline(FloordrainEngine.Bbasinline_to_Floordrain[i], FloordrainEngine.Bbasinline_to_Floordrain[i + 1]));
                    parameters0.normalCopys.Add(ThWPipeOutputFunction.CreatePolyline(FloordrainEngine.Bbasinline_to_Floordrain[i], FloordrainEngine.Bbasinline_to_Floordrain[i + 1]));
                }
                acadDatabase.ModelSpace.Add(new Circle() { Radius = 50, Center = FloordrainEngine.Bbasinline_Center[0] });
                parameters0.normalCopys.Add(new Circle() { Radius = 50, Center = FloordrainEngine.Bbasinline_Center[0] });
            }
        }
        private static void OutputDeviceplatformParamters(ThWCompositeFloordrainEngine FloordrainEngine, AcadDatabase acadDatabase, ThWTopParameters parameters0,ThWTopBalconyParameters parameters)
        {
            for (int i = 0; i < parameters.devicefloordrain.Count; i++)
            {
                Matrix3d scale = Matrix3d.Scaling(2.0, parameters.devicefloordrain[i].Position);
                var ent = parameters.devicefloordrain[i].GetTransformedCopy(scale);
                acadDatabase.ModelSpace.Add(ent);
            }
            if (FloordrainEngine.Condensepipe_tofloordrains.Count > 1)
            {
                foreach (Point3dCollection Rainpipe_ in FloordrainEngine.Condensepipe_tofloordrains)
                {
                    for (int i = 0; i < Rainpipe_.Count - 1; i++)
                    {
                        acadDatabase.ModelSpace.Add(ThWPipeOutputFunction.CreateRainline(Rainpipe_[i], Rainpipe_[i + 1]));
                        parameters0.normalCopys.Add(ThWPipeOutputFunction.CreateRainline(Rainpipe_[i], Rainpipe_[i + 1]));
                    }
                }
            }
            else
            {
                for (int i = 0; i < FloordrainEngine.Condensepipe_tofloordrain.Count - 1; i++)
                {
                    acadDatabase.ModelSpace.Add(ThWPipeOutputFunction.CreateRainline(FloordrainEngine.Condensepipe_tofloordrain[i], FloordrainEngine.Condensepipe_tofloordrain[i + 1]));
                    parameters0.normalCopys.Add(ThWPipeOutputFunction.CreateRainline(FloordrainEngine.Condensepipe_tofloordrain[i], FloordrainEngine.Condensepipe_tofloordrain[i + 1]));
                }
            }
            if (FloordrainEngine.Rainpipe_tofloordrains.Count > 1)
            {
                foreach (Point3dCollection Rainpipe_to in FloordrainEngine.Rainpipe_tofloordrains)
                {
                    for (int i = 0; i < Rainpipe_to.Count - 1; i++)
                    {
                        acadDatabase.ModelSpace.Add(ThWPipeOutputFunction.CreateRainline(Rainpipe_to[i], Rainpipe_to[i + 1]));
                        parameters0.normalCopys.Add(ThWPipeOutputFunction.CreateRainline(Rainpipe_to[i], Rainpipe_to[i + 1]));
                    }
                }
            }
            else
            {
                for (int i = 0; i < FloordrainEngine.Rainpipe_tofloordrain.Count - 1; i++)
                {
                    acadDatabase.ModelSpace.Add(ThWPipeOutputFunction.CreateRainline(FloordrainEngine.Rainpipe_tofloordrain[i], FloordrainEngine.Rainpipe_tofloordrain[i + 1]));
                    parameters0.normalCopys.Add(ThWPipeOutputFunction.CreateRainline(FloordrainEngine.Rainpipe_tofloordrain[i], FloordrainEngine.Rainpipe_tofloordrain[i + 1]));
                }
            }
        }
    }
}
