using AcHelper;
using Linq2Acad;
using ThMEPWSS.Pipe;
using Dreambuild.AutoCAD;
using ThMEPWSS.Pipe.Model;
using ThMEPWSS.Pipe.Engine;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.DatabaseServices;
using System.Linq;
using System;
using ThCADCore.NTS;
using ThCADExtension;
using ThMEPWSS.Pipe.Geom;

namespace ThMEPWSS
{
    public class ThPipeCmds
    {
        [CommandMethod("TIANHUACAD", "THPIPECOMPOSITE", CommandFlags.Modal)]
        public void Thpipecomposite()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            using (var compositeEngines = new ThWCompositeRoomRecognitionEngine())
            {
                var parameter_floor = new PromptIntegerOptions("请输入楼层");
                var floorResult = Active.Editor.GetInteger(parameter_floor);
                if (floorResult.Status != PromptStatus.OK)
                {
                    return;
                }
                var separation_key = new PromptKeywordOptions("\n污废分流");
                separation_key.Keywords.Add("是", "Y", "是(Y)");
                separation_key.Keywords.Add("否", "N", "否(N)");
                separation_key.Keywords.Default = "否";
                var result = Active.Editor.GetKeywords(separation_key);
                if (result.Status != PromptStatus.OK)
                {
                    return;
                }
                bool isSeparation = result.StringResult == "是";
                var caisson_key = new PromptKeywordOptions("\n沉箱");
                caisson_key.Keywords.Add("有", "Y", "有(Y)");
                caisson_key.Keywords.Add("没有", "N", "没有(N)");
                caisson_key.Keywords.Default = "没有";
                result = Active.Editor.GetKeywords(caisson_key);
                if (result.Status != PromptStatus.OK)
                {
                    return;
                }
                bool isCaisson = result.StringResult == "有";
                compositeEngines.Recognize(acadDatabase.Database, new Point3dCollection());                                          
                var tfloordrain = new List<BlockReference>();
                foreach (var composite in compositeEngines.Rooms)
                {
                    var tfloordrain_ = new List<BlockReference>();
                    Polyline boundary1 = null;
                    Polyline outline1 = null;
                    Polyline closestool = null;
                    Polyline boundary = null;
                    Polyline outline = null;
                    BlockReference basinline = null;
                    Polyline pype = null;
                    BlockReference floordrain = null;
                    if (IsValidKitchenContainer(composite.Kitchen))
                    {
                        boundary = composite.Kitchen.Kitchen.Boundary as Polyline;
                        outline = composite.Kitchen.DrainageWells[0].Boundary as Polyline;
                        basinline = composite.Kitchen.BasinTools[0].Outline as BlockReference;
                        if (composite.Kitchen.Pypes.Count > 0)
                        {
                            pype = composite.Kitchen.Pypes[0].Boundary as Polyline;
                        }
                        else
                        {
                            pype = new Polyline();
                        }
                    }
                    if (IsValidToiletContainer(composite.Toilet))
                    {                    
                        boundary1 = composite.Toilet.Toilet.Boundary as Polyline;
                        outline1 = composite.Toilet.DrainageWells[0].Boundary as Polyline;
                        closestool = composite.Toilet.Closestools[0].Outline as Polyline;                        
                    }
                    if(IsValidToiletContainerForFloorDrain(composite.Toilet))
                    {
                        foreach (var FloorDrain in composite.Toilet.FloorDrains)
                        {
                            floordrain = FloorDrain.Outline as BlockReference;
                            tfloordrain_.Add(floordrain);
                            tfloordrain.Add(floordrain);
                        }
                    }                  
                    var zone = new ThWPipeZone();
                    var toiletEngines = new ThWToiletPipeEngine()
                    {
                        Zone = zone,
                        Parameters = new ThWToiletPipeParameters(isSeparation, isCaisson, floorResult.Value),

                    };
                    var kitchenEngines = new ThWKitchenPipeEngine()
                    {
                        Zone = zone,
                        Parameters = new ThWKitchenPipeParameters(1, floorResult.Value),
                    };
                    var compositeEngine = new ThWCompositePipeEngine(kitchenEngines, toiletEngines);
                    compositeEngine.Run(boundary, outline, basinline, pype, boundary1, outline1, closestool);
                    var toiletfloorEngines = new ThWToiletFloordrainEngine();                
                    foreach (var kitchenPipe in compositeEngine.KitchenPipes)
                    {//要修改厨房管井的不正确位置，使其与卫生间管井共线
                        if(compositeEngine.ToiletPipes.Count>0 && (compositeEngine.ToiletPipes[0].Center.DistanceTo(kitchenPipe.Center) < 101))
                        {
                            var offset = Matrix3d.Displacement(kitchenPipe.Center.GetVectorTo(compositeEngine.ToiletPipes[0].Center));
                            foreach (Entity item in kitchenPipe.Representation)
                            {
                                acadDatabase.ModelSpace.Add(item.GetTransformedCopy(kitchenPipe.Matrix.PostMultiplyBy(offset)));
                            }
                        }    
                        else
                        {
                            foreach (Entity item in kitchenPipe.Representation)
                            {
                                acadDatabase.ModelSpace.Add(item.GetTransformedCopy(kitchenPipe.Matrix));
                            }
                        }
                    }
                    if (compositeEngine.ToiletPipes.Count > 0)
                    {
                            if (compositeEngine.KitchenPipes.Count>0&&compositeEngine.ToiletPipes[0].Center.DistanceTo(compositeEngine.KitchenPipes[0].Center)<101)
                            {
                                for (int i = 0; i < compositeEngine.ToiletPipes.Count; i++)
                                {
                                    var toilet = compositeEngine.ToiletPipes[i]; /*+ ;*/
                                    var radius = compositeEngine.ToiletPipeEngine.Parameters.Diameter[i] / 2.0;
                                    foreach (Entity item in toilet.Representation)
                                    {
                                        var offset = Matrix3d.Displacement(200 * compositeEngine.ToiletPipes[0].Center.GetVectorTo(compositeEngine.ToiletPipes[1].Center).GetNormal());
                                        acadDatabase.ModelSpace.Add(item.GetTransformedCopy(toilet.Matrix.PostMultiplyBy(offset)));
                                    }
                                }
                            }
                        
                        else
                        {
                            for (int i = 0; i < compositeEngine.ToiletPipes.Count; i++)
                            {
                                var toilet = compositeEngine.ToiletPipes[i];
                                var radius = compositeEngine.ToiletPipeEngine.Parameters.Diameter[i] / 2.0;
                                foreach (Entity item in toilet.Representation)
                                {
                                    acadDatabase.ModelSpace.Add(item.GetTransformedCopy(toilet.Matrix));
                                }
                            }
                        }
                    }
                    for (int i = 0; i < tfloordrain_.Count; i++)
                    {
                        Matrix3d scale = Matrix3d.Scaling(2.0, tfloordrain_[i].Position);
                        var ent = tfloordrain_[i].GetTransformedCopy(scale);
                        acadDatabase.ModelSpace.Add(ent);
                    }
                }           
                foreach (var compositeBalcony in compositeEngines.FloorDrainRooms)
                {   //判断是否为正确的Balcony
                    if (IsValidBalconyForFloorDrain(compositeBalcony.Balcony))
                    {
                        Polyline roofrainpipe = null;
                        Polyline tboundary = null;
                        Polyline bboundary = null;
                        Polyline downspout = null;
                        BlockReference washingmachine = null;
                        Polyline device = null;
                        Polyline condensepipe = null;
                        Polyline device_other = null;
                        BlockReference floordrain = null;
                        BlockReference bbasinline = null;
                        List<Polyline> condensepipes = new List<Polyline>();
                        var bfloordrain = new List<BlockReference>();
                        var devicefloordrain = new List<BlockReference>();
                        var rainpipe = new List<Polyline>();
                        foreach (var FloorDrain in compositeBalcony.Balcony.FloorDrains)
                        {
                            floordrain = FloorDrain.Outline as BlockReference;
                            bfloordrain.Add(floordrain);
                        }

                        bboundary = compositeBalcony.Balcony.Balcony.Boundary as Polyline;
                        if (compositeBalcony.Balcony.RainPipes.Count > 0)
                        {
                            foreach (var RainPipe in compositeBalcony.Balcony.RainPipes)
                            {
                                var ent = RainPipe.Outline as Polyline;
                                rainpipe.Add(ent);
                            }
                        }                
                            foreach (var devicePlatform in compositeBalcony.DevicePlatforms)
                            {
                                if (devicePlatform.RainPipes.Count > 0)
                                {
                                    foreach (var RainPipe in devicePlatform.RainPipes)
                                    {
                                        var ent = RainPipe.Outline as Polyline;
                                        rainpipe.Add(ent);
                                    }
                                }
                            }
                        if(!(rainpipe.Count>0))
                            { Active.Editor.WriteMessage("\n 缺雨水管"); }                                                  
                        if (compositeBalcony.Balcony.Washmachines.Count > 0)
                        {
                            washingmachine = compositeBalcony.Balcony.Washmachines[0].Outline as BlockReference;
                        }
                        if (compositeBalcony.DevicePlatforms.Count > 0)
                        {
                            device = compositeBalcony.DevicePlatforms[0].DevicePlatforms[0].Boundary as Polyline;
                        }
                        if (compositeBalcony.DevicePlatforms.Count > 1)
                        {
                            Polyline temp = compositeBalcony.DevicePlatforms[1].DevicePlatforms[0].Boundary as Polyline;
                            if (!(temp.Equals(device)))
                            {
                                device_other = compositeBalcony.DevicePlatforms[1].DevicePlatforms[0].Boundary as Polyline;
                            }
                            else
                            {
                                device_other = compositeBalcony.DevicePlatforms[2].DevicePlatforms[0].Boundary as Polyline;
                            }
                        }
                        foreach (var devicePlatform in compositeBalcony.DevicePlatforms)
                        {
                            if (devicePlatform.CondensePipes.Count > 0)
                            {
                                condensepipe = devicePlatform.CondensePipes[0].Outline as Polyline;
                                break;
                            }
                        }
                        foreach (var devicePlatform in compositeBalcony.DevicePlatforms)//考虑多余一个冷凝管
                        {
                            if (devicePlatform.CondensePipes.Count > 0)
                            {
                                Polyline s = devicePlatform.CondensePipes[0].Outline as Polyline;
                                condensepipes.Add(s);
                            }
                        }
                        foreach (var devicePlatform in compositeBalcony.DevicePlatforms)
                        {
                            if (devicePlatform.RoofRainPipes.Count > 0)
                            {
                                roofrainpipe = devicePlatform.RoofRainPipes[0].Outline as Polyline;
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
                            devicefloordrain.Add(devicefloordrains);
                        }
                        if (compositeBalcony.Balcony.BasinTools.Count > 0)
                        {
                            bbasinline = compositeBalcony.Balcony.BasinTools[0].Outline as BlockReference;
                        }

                        var thWBalconyFloordrainEngine = new ThWBalconyFloordrainEngine();
                        var thWToiletFloordrainEngine = new ThWToiletFloordrainEngine();
                        var thWDeviceFloordrainEngine = new ThWDeviceFloordrainEngine();
                        var FloordrainEngine = new ThWCompositeFloordrainEngine(thWBalconyFloordrainEngine, thWToiletFloordrainEngine, thWDeviceFloordrainEngine);
                        FloordrainEngine.Run(bfloordrain, bboundary, rainpipe, downspout, washingmachine, device, device_other, condensepipe, tfloordrain, tboundary, devicefloordrain, roofrainpipe, bbasinline,condensepipes);
                        for (int i = 0; i < FloordrainEngine.Floordrain.Count; i++)
                        {
                            Matrix3d scale = Matrix3d.Scaling(2.0, FloordrainEngine.Floordrain[i].Position);
                            var ent = FloordrainEngine.Floordrain[i].GetTransformedCopy(scale);
                            acadDatabase.ModelSpace.Add(ent);
                        }
                        Matrix3d scale_washing = Matrix3d.Scaling(1.0, FloordrainEngine.Floordrain_washing[0].Position);
                        var ent_washing = FloordrainEngine.Floordrain_washing[0].GetTransformedCopy(scale_washing);
                        acadDatabase.ModelSpace.Add(ent_washing);
                        for (int i = 0; i < FloordrainEngine.Downspout_to_Floordrain.Count - 1; i++)
                        {

                            Polyline ent_line1 = new Polyline();
                            ent_line1.AddVertexAt(0, FloordrainEngine.Downspout_to_Floordrain[i].ToPoint2d(), 0, 35, 35);
                            ent_line1.AddVertexAt(1, FloordrainEngine.Downspout_to_Floordrain[i + 1].ToPoint2d(), 0, 35, 35);
                            //ent_line1.Linetype = "DASHDED";
                            //ent_line1.Layer = "W-DRAI-DOME-PIPE";
                            ent_line1.Color = Autodesk.AutoCAD.Colors.Color.FromColorIndex(Autodesk.AutoCAD.Colors.ColorMethod.ByLayer, 256);
                            acadDatabase.ModelSpace.Add(ent_line1);
                        }
                        acadDatabase.ModelSpace.Add(FloordrainEngine.new_circle);
                        if (FloordrainEngine.Rainpipe_to_Floordrain.Count > 0)
                        {
                            for (int i = 0; i < FloordrainEngine.Rainpipe_to_Floordrain.Count - 1; i++)
                            {
                                Polyline ent_line1 = new Polyline();
                                ent_line1.AddVertexAt(0, FloordrainEngine.Rainpipe_to_Floordrain[i].ToPoint2d(), 0, 35, 35);
                                ent_line1.AddVertexAt(1, FloordrainEngine.Rainpipe_to_Floordrain[i + 1].ToPoint2d(), 0, 35, 35);
                                //ent_line1.Linetype = "DASHDOT";
                                //ent_line1.Layer = "W-RAIN-PIPE";
                                ent_line1.Color = Autodesk.AutoCAD.Colors.Color.FromColorIndex(Autodesk.AutoCAD.Colors.ColorMethod.ByLayer, 256);
                                acadDatabase.ModelSpace.Add(ent_line1);
                            }
                        }
                        if (FloordrainEngine.Bbasinline_to_Floordrain.Count > 0)
                        {
                            for (int i = 0; i < FloordrainEngine.Bbasinline_to_Floordrain.Count - 1; i++)
                            {
                                Polyline ent_line1 = new Polyline();
                                ent_line1.AddVertexAt(0, FloordrainEngine.Bbasinline_to_Floordrain[i].ToPoint2d(), 0, 35, 35);
                                ent_line1.AddVertexAt(1, FloordrainEngine.Bbasinline_to_Floordrain[i + 1].ToPoint2d(), 0, 35, 35);
                                //ent_line1.Linetype = "DASHDED";
                                //ent_line1.Layer = "W-DRAI-DOME-PIPE";
                                ent_line1.Color = Autodesk.AutoCAD.Colors.Color.FromColorIndex(Autodesk.AutoCAD.Colors.ColorMethod.ByLayer, 256);
                                acadDatabase.ModelSpace.Add(ent_line1);
                            }
                            acadDatabase.ModelSpace.Add(new Circle() { Radius = 50, Center = FloordrainEngine.Bbasinline_Center[0] });

                        }
                        //阳台输出完毕
                        for (int i = 0; i < devicefloordrain.Count; i++)
                        {
                            Matrix3d scale = Matrix3d.Scaling(2.0, devicefloordrain[i].Position);
                            var ent = devicefloordrain[i].GetTransformedCopy(scale);
                            acadDatabase.ModelSpace.Add(ent);
                        }
                        if (FloordrainEngine.Condensepipe_tofloordrains.Count > 1)
                        {
                            foreach (Point3dCollection Rainpipe_to in FloordrainEngine.Condensepipe_tofloordrains)
                            {
                                for (int i = 0; i < Rainpipe_to.Count - 1; i++)
                                {
                                    Polyline ent_line1 = new Polyline();
                                    ent_line1.AddVertexAt(0, Rainpipe_to[i].ToPoint2d(), 0, 35, 35);
                                    ent_line1.AddVertexAt(1, Rainpipe_to[i + 1].ToPoint2d(), 0, 35, 35);
                                    //ent_line1.Linetype = "DASHDOT";
                                    //ent_line1.Layer = "W-RAIN-PIPE";
                                    ent_line1.Color = Autodesk.AutoCAD.Colors.Color.FromColorIndex(Autodesk.AutoCAD.Colors.ColorMethod.ByLayer, 256);
                                    acadDatabase.ModelSpace.Add(ent_line1);
                                }
                            }
                        }
                        else
                        {
                            for (int i = 0; i < FloordrainEngine.Condensepipe_tofloordrain.Count - 1; i++)
                            {
                                Polyline ent_line1 = new Polyline();
                                ent_line1.AddVertexAt(0, FloordrainEngine.Condensepipe_tofloordrain[i].ToPoint2d(), 0, 35, 35);
                                ent_line1.AddVertexAt(1, FloordrainEngine.Condensepipe_tofloordrain[i + 1].ToPoint2d(), 0, 35, 35);
                                //ent_line1.Linetype = "DASHDOT";
                                //ent_line1.Layer = "W-RAIN-PIPE";
                                ent_line1.Color = Autodesk.AutoCAD.Colors.Color.FromColorIndex(Autodesk.AutoCAD.Colors.ColorMethod.ByLayer, 256);
                                acadDatabase.ModelSpace.Add(ent_line1);
                            }
                        }
                        if (FloordrainEngine.Rainpipe_tofloordrains.Count > 1)
                        {
                            foreach (Point3dCollection Rainpipe_to in FloordrainEngine.Rainpipe_tofloordrains)
                            {
                                for (int i = 0; i < Rainpipe_to.Count - 1; i++)
                                {
                                    Polyline ent_line1 = new Polyline();
                                    ent_line1.AddVertexAt(0, Rainpipe_to[i].ToPoint2d(), 0, 35, 35);
                                    ent_line1.AddVertexAt(1, Rainpipe_to[i + 1].ToPoint2d(), 0, 35, 35);
                                    //ent_line1.Linetype = "DASHDOT";
                                    //ent_line1.Layer = "W-RAIN-PIPE";
                                    ent_line1.Color = Autodesk.AutoCAD.Colors.Color.FromColorIndex(Autodesk.AutoCAD.Colors.ColorMethod.ByLayer, 256);
                                    acadDatabase.ModelSpace.Add(ent_line1);
                                }
                            }
                        }
                        else
                        {
                            for (int i = 0; i < FloordrainEngine.Rainpipe_tofloordrain.Count - 1; i++)
                            {
                                Polyline ent_line1 = new Polyline();
                                ent_line1.AddVertexAt(0, FloordrainEngine.Rainpipe_tofloordrain[i].ToPoint2d(), 0, 35, 35);
                                ent_line1.AddVertexAt(1, FloordrainEngine.Rainpipe_tofloordrain[i + 1].ToPoint2d(), 0, 35, 35);
                                //ent_line1.Linetype = "DASHDOT";
                                //ent_line1.Layer = "W-RAIN-PIPE";
                                ent_line1.Color = Autodesk.AutoCAD.Colors.Color.FromColorIndex(Autodesk.AutoCAD.Colors.ColorMethod.ByLayer, 256);
                                acadDatabase.ModelSpace.Add(ent_line1);
                            }
                        }
                    }
                }
                
            }
        }
        private bool IsValidToiletContainer(ThWToiletRoom toiletContainer)
        {
            return toiletContainer.Toilet != null &&
                toiletContainer.DrainageWells.Count == 1 &&
                toiletContainer.Closestools.Count == 1 &&
                toiletContainer.FloorDrains.Count > 0;
        }
        private bool IsValidKitchenContainer(ThWKitchenRoom kitchenContainer)
        {
            return (kitchenContainer.Kitchen != null && kitchenContainer.DrainageWells.Count == 1);                        
        }
        private bool IsValidToiletContainerForFloorDrain(ThWToiletRoom toiletContainer)
        {
            return toiletContainer.Toilet != null && 
                toiletContainer.FloorDrains.Count > 0;
        }
        private bool IsValidBalconyForFloorDrain(ThWBalconyRoom balconyContainer)
        {
            return balconyContainer.FloorDrains.Count > 0&& balconyContainer.Washmachines.Count>0;
        }
        private static List<Polyline> GetNewPipes(List<Polyline> rain_pipe)
        {
            var rain_pipes = new List<Polyline>();
            if (rain_pipe.Count > 0)
            {
                rain_pipes.Add(rain_pipe[0]);
                for (int i = 1; i < rain_pipe.Count; i++)
                {
                    for (int j = i - 1; j >= 0; j--)
                    {
                        if (rain_pipe[j].GetCenter() == rain_pipe[i].GetCenter())
                        {
                            break;
                        }
                        else
                        {
                            if (j > 0)
                            {
                                continue;
                            }
                            else
                            {
                                rain_pipes.Add(rain_pipe[i]);
                            }
                        }
                    }
                }
            }
            return rain_pipes;
        }
        [CommandMethod("TIANHUACAD", "THPIPETAG", CommandFlags.Modal)]
        public void Thpipetag()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            using (var compositeEngines = new ThWRoofDeviceFloorRecognitionEngine())
            {
                compositeEngines.Recognize(acadDatabase.Database, new Point3dCollection());
                Polyline boundary = null;
                var gravityWaterBucket= new List<BlockReference>();
                var sideWaterBucket= new List<BlockReference>();
                var roofRainPipe= new List<Polyline>();
                var engine = new ThWWaterBucketEngine();

                foreach (var composite in compositeEngines.Rooms)
                {
                    boundary = composite.RoofDeviceFloor.Boundary as Polyline;
                    foreach (var gravity in composite.GravityWaterBuckets)
                    {
                        BlockReference block = null;
                        block = gravity.Outline as BlockReference;
                        gravityWaterBucket.Add(block);
                    }
                    foreach (var side in composite.SideEntryWaterBuckets)
                    {
                        BlockReference block = null;
                        block = side.Outline as BlockReference;
                        sideWaterBucket.Add(block);
                    }
                    foreach (var pipe in composite.RoofRainPipes)
                    {
                        Polyline block = null;
                        block = pipe.Outline as Polyline;
                        roofRainPipe.Add(block);
                    }

                    engine.Run(gravityWaterBucket, sideWaterBucket, roofRainPipe, boundary);
                    for (int i = 0; i < engine.GravityWaterBucketCenter.Count; i++)
                    {
                        Line ent_line = new Line(engine.GravityWaterBucketCenter[i], engine.GravityWaterBucketTag[i]);
                        Line ent_line1 = new Line(engine.GravityWaterBucketTag[i], engine.GravityWaterBucketTag[i + 1]);
                        //ent_line.Layer = "W-DRAI-NOTE";
                        ent_line.Color = Autodesk.AutoCAD.Colors.Color.FromColorIndex(Autodesk.AutoCAD.Colors.ColorMethod.ByLayer, 256);
                        acadDatabase.ModelSpace.Add(ent_line);
                        acadDatabase.ModelSpace.Add(ent_line1);
                        DBText taggingtext = new DBText()
                        {
                            Height = 200,
                            Position = engine.GravityWaterBucketTag[i + 2],
                            TextString = "DN100",
                        };
                        acadDatabase.ModelSpace.Add(taggingtext);
                    }
                    for (int i = 0; i < engine.SideWaterBucketCenter.Count; i++)
                    {
                        Line ent_line = new Line(engine.SideWaterBucketCenter[i], engine.SideWaterBucketTag[i]);
                        Line ent_line1 = new Line(engine.SideWaterBucketTag[i], engine.SideWaterBucketTag[i + 1]);
                        //ent_line.Layer = "W-DRAI-NOTE";
                        ent_line.Color = Autodesk.AutoCAD.Colors.Color.FromColorIndex(Autodesk.AutoCAD.Colors.ColorMethod.ByLayer, 256);
                        acadDatabase.ModelSpace.Add(ent_line);
                        acadDatabase.ModelSpace.Add(ent_line1);
                        DBText taggingtext = new DBText()
                        {
                            Height = 200,
                            Position = engine.SideWaterBucketTag[i + 2],
                            TextString = "DN75",
                        };
                        acadDatabase.ModelSpace.Add(taggingtext);
                    }
                    for (int i = 0; i < engine.Center_point.Count; i++)
                    {
                        acadDatabase.ModelSpace.Add(new Circle() { Radius = 50, Center = engine.Center_point[i] });
                    }
                }

            }
        }
        [CommandMethod("TIANHUACAD", "THPIPETAG1", CommandFlags.Modal)]
        public void Thpipetag1()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            using (var compositeEngines = new ThWRoofFloorRecognitionEngine())
            {
                compositeEngines.Recognize(acadDatabase.Database, new Point3dCollection());
                Polyline boundary = null;
                var gravityWaterBucket = new List<BlockReference>();
                var sideWaterBucket = new List<BlockReference>();
                var roofRainPipe = new List<Polyline>();
                var baseCircles = new List<Polyline>();
                var engine = new ThWWaterBucketEngine();

                foreach (var composite in compositeEngines.Rooms)
                {
                    boundary = composite.RoofFloor.Boundary as Polyline;
                    foreach (var gravity in composite.GravityWaterBuckets)
                    {
                        BlockReference block = null;
                        block = gravity.Outline as BlockReference;
                        gravityWaterBucket.Add(block);
                    }
                    foreach (var side in composite.SideEntryWaterBuckets)
                    {
                        BlockReference block = null;
                        block = side.Outline as BlockReference;
                        sideWaterBucket.Add(block);
                    }
                    foreach (var pipe in composite.RoofRainPipes)
                    {
                        Polyline block = null;
                        block = pipe.Outline as Polyline;
                        roofRainPipe.Add(block);
                    }
                    foreach (var circle in composite.BaseCircles)
                    {
                        Polyline block = null;
                        block = circle.Boundary as Polyline;
                        baseCircles.Add(block);
                    }

                    engine.Run(gravityWaterBucket, sideWaterBucket, roofRainPipe, boundary);
                    for (int i = 0; i < engine.GravityWaterBucketCenter.Count; i++)
                    {
                        Line ent_line = new Line(engine.GravityWaterBucketCenter[i], engine.GravityWaterBucketTag[i]);
                        Line ent_line1 = new Line(engine.GravityWaterBucketTag[i], engine.GravityWaterBucketTag[i + 1]);
                        //ent_line.Layer = "W-DRAI-NOTE";
                        ent_line.Color = Autodesk.AutoCAD.Colors.Color.FromColorIndex(Autodesk.AutoCAD.Colors.ColorMethod.ByLayer, 256);
                        acadDatabase.ModelSpace.Add(ent_line);
                        acadDatabase.ModelSpace.Add(ent_line1);
                        DBText taggingtext = new DBText()
                        {
                            Height = 200,
                            Position = engine.GravityWaterBucketTag[i + 2],
                            TextString = "DN100",
                        };
                        acadDatabase.ModelSpace.Add(taggingtext);
                    }
                    for (int i = 0; i < engine.SideWaterBucketCenter.Count; i++)
                    {
                        Line ent_line = new Line(engine.SideWaterBucketCenter[i], engine.SideWaterBucketTag[i]);
                        Line ent_line1 = new Line(engine.SideWaterBucketTag[i], engine.SideWaterBucketTag[i + 1]);
                        //ent_line.Layer = "W-DRAI-NOTE";
                        ent_line.Color = Autodesk.AutoCAD.Colors.Color.FromColorIndex(Autodesk.AutoCAD.Colors.ColorMethod.ByLayer, 256);
                        acadDatabase.ModelSpace.Add(ent_line);
                        acadDatabase.ModelSpace.Add(ent_line1);
                        DBText taggingtext = new DBText()
                        {
                            Height = 200,
                            Position = engine.SideWaterBucketTag[i + 2],
                            TextString = "DN75",
                        };
                        acadDatabase.ModelSpace.Add(taggingtext);
                    }
                    for (int i = 0; i < engine.Center_point.Count; i++)
                    {
                        acadDatabase.ModelSpace.Add(new Circle() { Radius = 50, Center = engine.Center_point[i] });
                    }
                }

            }
        }
        [CommandMethod("TIANHUACAD", "THPIPETAG2", CommandFlags.Modal)]
        public void Thpipetag2()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            using (var compositeEngines = new ThWTopFloorRecognitionEngine())
            {
                compositeEngines.Recognize(acadDatabase.Database, new Point3dCollection());
            }
        }

        [CommandMethod("TIANHUACAD", "THPIPLINE", CommandFlags.Modal)]
        public void THPIPLINE()
        {
            using (var db = AcadDatabase.Active())
            {
                var tests = db.ModelSpace.OfType<Line>().ToList();
                var lines = db.ModelSpace.OfType<Line>().Where(lineInfo => lineInfo.Layer.Contains(ThWPipeCommon.AD_FLOOR_AREA)).ToList();
                foreach (var line in lines)
                {
                    var lineClone = line.Clone() as Line;
                    lineClone.ColorIndex = 3;
                    var id = db.ModelSpace.Add(lineClone);
                }

            }
        }

        [CommandMethod("TIANHUACAD", "THPYS", CommandFlags.Modal)]
        public void ThPYS()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            using (var FloorEngines = new ThWCompositeFloorRecognitionEngine())
            {
                FloorEngines.Recognize(acadDatabase.Database, new Point3dCollection());
                //第一类屋顶设备层布置
                Polyline d_boundary = null;
                var gravityWaterBucket = new List<BlockReference>();
                var sideWaterBucket = new List<BlockReference>();
                var roofRainPipe = new List<Polyline>();
                var engine = new ThWWaterBucketEngine();
                var baseCircles = new List<Polyline>();
                var baseCenter0 = new Point3dCollection();
                var waterbuckets1= new Point3dCollection();
                var waterbuckets2 = new Point3dCollection();
                if (FloorEngines.RoofDeviceFloors.Count > 0)//存在屋顶设备层
                {
                    foreach (var composite in FloorEngines.RoofDeviceFloors)
                    {
                        var basecircle0 = composite.BaseCircles[0].Boundary.GetCenter();
                        baseCenter0.Add(basecircle0);
                        d_boundary = composite.RoofDeviceFloor.Boundary as Polyline;
                        foreach (var gravity in composite.GravityWaterBuckets)
                        {
                            BlockReference block = null;
                            block = gravity.Outline as BlockReference;
                            gravityWaterBucket.Add(block);
                        }
                        foreach (var side in composite.SideEntryWaterBuckets)
                        {
                            BlockReference block = null;
                            block = side.Outline as BlockReference;
                            sideWaterBucket.Add(block);
                        }
                        foreach (var pipe in composite.RoofRainPipes)
                        {
                            Polyline block = null;
                            block = pipe.Outline as Polyline;
                            roofRainPipe.Add(block);
                        }

                        engine.Run(gravityWaterBucket, sideWaterBucket, roofRainPipe, d_boundary);
                        waterbuckets1 = engine.SideWaterBucketCenter;
                        for (int i = 0; i < engine.GravityWaterBucketCenter.Count; i++)
                        {
                            Line ent_line = new Line(engine.GravityWaterBucketCenter[i], engine.GravityWaterBucketTag[4*i]);
                            Line ent_line1 = new Line(engine.GravityWaterBucketTag[4*i], engine.GravityWaterBucketTag[4*i + 1]);
                            //ent_line.Layer = "W-DRAI-NOTE";
                            //ent_line.Color = Autodesk.AutoCAD.Colors.Color.FromColorIndex(Autodesk.AutoCAD.Colors.ColorMethod.ByLayer, 256);
                            ent_line.Color= Autodesk.AutoCAD.Colors.Color.FromRgb(0, 255, 255);
                            ent_line1.Color = Autodesk.AutoCAD.Colors.Color.FromRgb(0, 255, 255);
                            acadDatabase.ModelSpace.Add(ent_line);
                            acadDatabase.ModelSpace.Add(ent_line1);
                            DBText taggingtext = new DBText()
                            {
                                Height = 200,
                                Position = engine.GravityWaterBucketTag[4*i + 2],
                                TextString = "DN100",
                                Color = Autodesk.AutoCAD.Colors.Color.FromRgb(0, 255, 255),
                            };
                            acadDatabase.ModelSpace.Add(taggingtext);
                            DBText taggingtext1 = new DBText()
                            {
                                Height = 200,
                                Position = engine.GravityWaterBucketTag[4 * i + 3],
                                TextString = "重力型雨水斗",
                                Color = Autodesk.AutoCAD.Colors.Color.FromRgb(0, 255, 255),
                            };
                            acadDatabase.ModelSpace.Add(taggingtext1);
                        }
                        for (int i = 0; i < engine.SideWaterBucketCenter.Count; i++)
                        {
                            Line ent_line = new Line(engine.SideWaterBucketCenter[i], engine.SideWaterBucketTag[4*i]);
                            Line ent_line1 = new Line(engine.SideWaterBucketTag[4*i], engine.SideWaterBucketTag[4*i + 1]);
                            //ent_line.Layer = "W-DRAI-NOTE";
                            //ent_line.Color = Autodesk.AutoCAD.Colors.Color.FromColorIndex(Autodesk.AutoCAD.Colors.ColorMethod.ByLayer, 256);
                            ent_line.Color = Autodesk.AutoCAD.Colors.Color.FromRgb(0, 255, 255);
                            ent_line1.Color = Autodesk.AutoCAD.Colors.Color.FromRgb(0, 255, 255);
                            acadDatabase.ModelSpace.Add(ent_line);
                            acadDatabase.ModelSpace.Add(ent_line1);
                            DBText taggingtext = new DBText()
                            {
                                Height = 200,
                                Position = engine.SideWaterBucketTag[4*i + 2],
                                TextString = "DN75",
                                Color = Autodesk.AutoCAD.Colors.Color.FromRgb(0, 255, 255),
                            };
                            acadDatabase.ModelSpace.Add(taggingtext);
                            DBText taggingtext1 = new DBText()
                            {
                                Height = 200,
                                Position = engine.SideWaterBucketTag[4 * i + 3],
                                TextString = "侧入式雨水斗",
                                Color = Autodesk.AutoCAD.Colors.Color.FromRgb(0, 255, 255),
                            };
                            acadDatabase.ModelSpace.Add(taggingtext1);
                        }
                        for (int i = 0; i < engine.Center_point.Count; i++)
                        {
                            acadDatabase.ModelSpace.Add(new Circle() { Radius = 50, Center = engine.Center_point[i] });
                        }
                    }
                }
                //第二类屋顶层布置
                Polyline r_boundary = null;
                var gravityWaterBucket1 = new List<BlockReference>();
                var sideWaterBucket1 = new List<BlockReference>();
                var roofRainPipe1 = new List<Polyline>();
                var engine1 = new ThWWaterBucketEngine();
                var baseCenter1 = new Point3dCollection();
                var roofRoofRainPipes = new List<Polyline>();
                if (FloorEngines.RoofFloors.Count > 0)//存在屋顶层
                {
                    foreach (var composite in FloorEngines.RoofFloors)
                    {
                        var basecircle1 = composite.BaseCircles[0].Boundary.GetCenter();
                        baseCenter1.Add(basecircle1);
                        r_boundary = composite.RoofFloor.Boundary as Polyline;
                        foreach (var gravity in composite.GravityWaterBuckets)
                        {
                            BlockReference block = null;
                            block = gravity.Outline as BlockReference;
                            gravityWaterBucket1.Add(block);
                        }
                        foreach (var side in composite.SideEntryWaterBuckets)
                        {
                            BlockReference block = null;
                            block = side.Outline as BlockReference;
                            sideWaterBucket1.Add(block);
                        }
                        foreach (var pipe in composite.RoofRainPipes)
                        {
                            Polyline block = null;
                            block = pipe.Outline as Polyline;
                            roofRainPipe1.Add(block);
                        }
                        roofRoofRainPipes = roofRainPipe1;
                        foreach (var circle in composite.BaseCircles)
                        {
                            Polyline block = null;
                            block = circle.Boundary as Polyline;
                            baseCircles.Add(block);
                        }

                        engine1.Run(gravityWaterBucket1, sideWaterBucket1, roofRainPipe1, r_boundary);
                        waterbuckets2 = engine1.SideWaterBucketCenter;
                        for (int i = 0; i < engine1.GravityWaterBucketCenter.Count; i++)
                        {
                            Line ent_line = new Line(engine1.GravityWaterBucketCenter[i], engine1.GravityWaterBucketTag[4*i]);
                            Line ent_line1 = new Line(engine1.GravityWaterBucketTag[4*i], engine1.GravityWaterBucketTag[4*i + 1]);
                            //ent_line.Layer = "W-DRAI-NOTE";
                            //ent_line.Color = Autodesk.AutoCAD.Colors.Color.FromColorIndex(Autodesk.AutoCAD.Colors.ColorMethod.ByLayer, 256);
                            ent_line.Color = Autodesk.AutoCAD.Colors.Color.FromRgb(0, 255, 255);
                            ent_line1.Color = Autodesk.AutoCAD.Colors.Color.FromRgb(0, 255, 255);
                            acadDatabase.ModelSpace.Add(ent_line);
                            acadDatabase.ModelSpace.Add(ent_line1);
                            DBText taggingtext = new DBText()
                            {
                                Height = 200,
                                Position = engine1.GravityWaterBucketTag[4*i + 2],
                                TextString = "DN100",
                                Color= Autodesk.AutoCAD.Colors.Color.FromRgb(0, 255, 255),
                        };
                            acadDatabase.ModelSpace.Add(taggingtext);
                            DBText taggingtext1 = new DBText()
                            {
                                Height = 200,
                                Position = engine1.GravityWaterBucketTag[4 * i + 3],
                                TextString = "重力型雨水斗",
                                Color = Autodesk.AutoCAD.Colors.Color.FromRgb(0, 255, 255),
                            };
                            acadDatabase.ModelSpace.Add(taggingtext1);
                        }
                        for (int i = 0; i < engine1.SideWaterBucketCenter.Count; i++)
                        {
                            Line ent_line = new Line(engine1.SideWaterBucketCenter[i], engine1.SideWaterBucketTag[4*i]);
                            Line ent_line1 = new Line(engine1.SideWaterBucketTag[4*i], engine1.SideWaterBucketTag[4*i + 1]);
                            //ent_line.Layer = "W-DRAI-NOTE";
                            //ent_line.Color = Autodesk.AutoCAD.Colors.Color.FromColorIndex(Autodesk.AutoCAD.Colors.ColorMethod.ByLayer, 256);
                            ent_line.Color = Autodesk.AutoCAD.Colors.Color.FromRgb(0, 255, 255);
                            ent_line1.Color = Autodesk.AutoCAD.Colors.Color.FromRgb(0, 255, 255);
                            acadDatabase.ModelSpace.Add(ent_line);
                            acadDatabase.ModelSpace.Add(ent_line1);
                            DBText taggingtext = new DBText()
                            {
                                Height = 200,
                                Position = engine1.SideWaterBucketTag[4*i + 2],
                                TextString = "DN75",
                                Color = Autodesk.AutoCAD.Colors.Color.FromRgb(0, 255, 255),
                            };
                            acadDatabase.ModelSpace.Add(taggingtext);
                            DBText taggingtext1 = new DBText()
                            {
                                Height = 200,
                                Position = engine1.SideWaterBucketTag[4 * i + 3],
                                TextString = "侧入式雨水斗",
                                Color = Autodesk.AutoCAD.Colors.Color.FromRgb(0, 255, 255),
                            };
                            acadDatabase.ModelSpace.Add(taggingtext1);
                        }
                        for (int i = 0; i < composite.RoofRainPipes.Count; i++)
                        {
                            acadDatabase.ModelSpace.Add(new Circle()
                            {
                                Radius = 50,
                                Center = composite.RoofRainPipes[i].Outline.GetCenter(),
                                Color = Autodesk.AutoCAD.Colors.Color.FromRgb(0, 255, 255)
                            });
                        }
                        for (int i = 0; i < engine1.Center_point.Count; i++)
                        {
                            acadDatabase.ModelSpace.Add(new Circle() { Radius = 50, Center = engine1.Center_point[i],
                                Color=Autodesk.AutoCAD.Colors.Color.FromRgb(0, 255, 255)
                        });
                        }
                    }
                }
                //第三类顶层布置

                var parameter_floor = new PromptIntegerOptions("请输入楼层");
                var floorResult = Active.Editor.GetInteger(parameter_floor);
                if (floorResult.Status != PromptStatus.OK)
                {
                    return;
                }
                var separation_key = new PromptKeywordOptions("\n污废分流");
                separation_key.Keywords.Add("是", "Y", "是(Y)");
                separation_key.Keywords.Add("否", "N", "否(N)");
                separation_key.Keywords.Default = "否";
                var result = Active.Editor.GetKeywords(separation_key);
                if (result.Status != PromptStatus.OK)
                {
                    return;
                }
                bool isSeparation = result.StringResult == "是";
                var caisson_key = new PromptKeywordOptions("\n沉箱");
                caisson_key.Keywords.Add("有", "Y", "有(Y)");
                caisson_key.Keywords.Add("没有", "N", "没有(N)");
                caisson_key.Keywords.Default = "没有";
                result = Active.Editor.GetKeywords(caisson_key);
                if (result.Status != PromptStatus.OK)
                {
                    return;
                }
                bool isCaisson = result.StringResult == "有";              
                var tfloordrain = new List<BlockReference>();
                var baseCenter2 = new Point3dCollection();
                var basecircle2 = FloorEngines.TopFloors[0].BaseCircles[0].Boundary.GetCenter();
                baseCenter2.Add(basecircle2);
                List<Entity> copypipes = new List<Entity>();//要复制的特征
                List<Entity> copyroofpipes = new List<Entity>();//要复制的屋顶雨水管
                List<Entity> copyrooftags = new List<Entity>();
                List<Entity> normalCopys = new List<Entity>();//要复制到其他标准层的立管
                //标注变量
                var fpipe = new List<Polyline>();
                var tpipe = new List<Polyline>();
                var wpipe = new List<Polyline>();
                var ppipe = new List<Polyline>();
                var dpipe = new List<Polyline>();
                var npipe = new List<Polyline>();
                var rain_pipe = new List<Polyline>();
                Polyline pboundary = null;
                var divideLines = new List<Line>();
                var roofrain_pipe = new List<Polyline>();
                if (FloorEngines.TopFloors.Count > 0) //存在顶层
                {
                    divideLines = FloorEngines.TopFloors[0].DivisionLines;
                    pboundary = FloorEngines.TopFloors[0].FirstFloor.Boundary as Polyline;
                    foreach (var composite in FloorEngines.TopFloors[0].CompositeRooms)
                    {
                        var tfloordrain_ = new List<BlockReference>();
                        Polyline boundary = null;
                        Polyline outline = null;
                        BlockReference basinline = null;
                        Polyline pype = null;
                        Polyline boundary1 = null;
                        Polyline outline1 = null;
                        Polyline closestool = null;
                        BlockReference floordrain = null;
                        if (composite.Kitchen != null)
                        {
                            if (IsValidKitchenContainer(composite.Kitchen))
                            {
                                boundary = composite.Kitchen.Kitchen.Boundary as Polyline;
                                outline = composite.Kitchen.DrainageWells[0].Boundary as Polyline;
                                basinline = composite.Kitchen.BasinTools[0].Outline as BlockReference;
                                if (composite.Kitchen.Pypes.Count > 0)
                                {
                                    pype = composite.Kitchen.Pypes[0].Boundary as Polyline;
                                }
                                else
                                {
                                    pype = new Polyline();
                                }
                                if (composite.Kitchen.RainPipes.Count > 0)
                                {
                                    rain_pipe.Add(composite.Kitchen.RainPipes[0].Outline as Polyline);
                                }
                                if (composite.Kitchen.RoofRainPipes.Count > 0)
                                {
                                    Polyline s = composite.Kitchen.RoofRainPipes[0].Outline as Polyline;
                                    roofrain_pipe.Add(s);
                                    copyroofpipes.Add(new Circle() { Center = s.GetCenter(), Radius = 38.5 });
                                    copyroofpipes.Add(new Circle() { Center = s.GetCenter(), Radius = 55.0 });
                                }
                            }
                        }
                        if (IsValidToiletContainer(composite.Toilet))
                        {
                            boundary1 = composite.Toilet.Toilet.Boundary as Polyline;
                            outline1 = composite.Toilet.DrainageWells[0].Boundary as Polyline;
                            closestool = composite.Toilet.Closestools[0].Outline as Polyline;
                            if (composite.Toilet.CondensePipes.Count > 0)
                            {
                                foreach (var pipe in composite.Toilet.CondensePipes)
                                {
                                    npipe.Add(pipe.Outline as Polyline);
                                }
                            }
                            if (composite.Toilet.RoofRainPipes.Count > 0)
                            {
                                Polyline s = composite.Toilet.RoofRainPipes[0].Outline as Polyline;
                                roofrain_pipe.Add(s);
                                copyroofpipes.Add(new Circle() { Center = s.GetCenter(), Radius = 38.5 });
                                copyroofpipes.Add(new Circle() { Center = s.GetCenter(), Radius = 55.0 });
                            }                          
                        }
                        if (IsValidToiletContainerForFloorDrain(composite.Toilet))
                        {
                            foreach (var FloorDrain in composite.Toilet.FloorDrains)
                            {
                                floordrain = FloorDrain.Outline as BlockReference;
                                tfloordrain.Add(floordrain);
                                tfloordrain_.Add(floordrain);
                            }
                        }
                        var zone = new ThWPipeZone();
                        var toiletEngines = new ThWToiletPipeEngine()
                        {
                            Zone = zone,
                            Parameters = new ThWToiletPipeParameters(isSeparation, isCaisson, floorResult.Value),

                        };
                        var kitchenEngines = new ThWKitchenPipeEngine()
                        {
                            Zone = zone,
                            Parameters = new ThWKitchenPipeParameters(1, floorResult.Value),
                        };
                        var compositeEngine = new ThWCompositePipeEngine(kitchenEngines, toiletEngines);
                        compositeEngine.Run(boundary, outline, basinline, pype, boundary1, outline1, closestool);
                        var toiletfloorEngines = new ThWToiletFloordrainEngine();
                        foreach (var kitchenPipe in compositeEngine.KitchenPipes)
                        {
                            if (compositeEngine.ToiletPipes.Count > 0 && (compositeEngine.ToiletPipes[0].Center.DistanceTo(kitchenPipe.Center) < 101))
                            {
                                var offset = Matrix3d.Displacement(kitchenPipe.Center.GetVectorTo(compositeEngine.ToiletPipes[0].Center));
                                foreach (Entity item in kitchenPipe.Representation)
                                {
                                    acadDatabase.ModelSpace.Add(item.GetTransformedCopy(kitchenPipe.Matrix.PostMultiplyBy(offset)));
                                    Entity polyline = item.GetTransformedCopy(kitchenPipe.Matrix.PostMultiplyBy(offset));
                                    Circle circle = polyline as Circle;
                                    Polyline pipe = circle.Tessellate(50);
                                    fpipe.Add(pipe);
                                    copypipes.Add(polyline);
                                    normalCopys.Add(polyline);
                                }
                            }
                            else
                            {
                                foreach (Entity item in kitchenPipe.Representation)
                                {
                                    acadDatabase.ModelSpace.Add(item.GetTransformedCopy(kitchenPipe.Matrix));
                                    Entity polyline = item.GetTransformedCopy(kitchenPipe.Matrix);
                                    Circle circle = polyline as Circle;
                                    Polyline pipe = circle.Tessellate(50);
                                    fpipe.Add(pipe);
                                    copypipes.Add(polyline);
                                    normalCopys.Add(polyline);
                                }
                            }                        
                        }
                        if (compositeEngine.ToiletPipes.Count > 0)
                        {
                            if (compositeEngine.KitchenPipes.Count > 0 && compositeEngine.ToiletPipes[0].Center.DistanceTo(compositeEngine.KitchenPipes[0].Center) < 101)
                            {
                                for (int i = 0; i < compositeEngine.ToiletPipes.Count; i++)
                                {
                                    var toilet = compositeEngine.ToiletPipes[i]; /*+ ;*/
                                    var radius = compositeEngine.ToiletPipeEngine.Parameters.Diameter[i] / 2.0;
                                    if(toilet.Identifier.Contains('F'))
                                    {
                                        foreach (Entity item in toilet.Representation)
                                        {
                                            var offset = Matrix3d.Displacement(200 * compositeEngine.ToiletPipes[0].Center.GetVectorTo(compositeEngine.ToiletPipes[1].Center).GetNormal());
                                            Entity polyline= item.GetTransformedCopy(toilet.Matrix.PostMultiplyBy(offset));
                                            Circle circle = polyline as Circle;
                                            Polyline pipe = circle.Tessellate(50);
                                            fpipe.Add(pipe);
                                            copypipes.Add(polyline);
                                            normalCopys.Add(polyline);
                                        }
                                    }
                                    if (toilet.Identifier.Contains('P'))
                                    {
                                        foreach (Entity item in toilet.Representation)
                                        {
                                            var offset = Matrix3d.Displacement(200 * compositeEngine.ToiletPipes[0].Center.GetVectorTo(compositeEngine.ToiletPipes[1].Center).GetNormal());
                                            var polyline = item.GetTransformedCopy(toilet.Matrix.PostMultiplyBy(offset));
                                            Circle circle = polyline as Circle;
                                            Polyline pipe = circle.Tessellate(50);
                                            ppipe.Add(pipe);
                                            copypipes.Add(polyline);
                                            normalCopys.Add(polyline);
                                        }
                                    }
                                    if (toilet.Identifier.Contains('W'))
                                    {
                                        foreach (Entity item in toilet.Representation)
                                        {
                                            var offset = Matrix3d.Displacement(200 * compositeEngine.ToiletPipes[0].Center.GetVectorTo(compositeEngine.ToiletPipes[1].Center).GetNormal());
                                            var polyline = item.GetTransformedCopy(toilet.Matrix.PostMultiplyBy(offset)) ;
                                            Circle circle = polyline as Circle;
                                            Polyline pipe = circle.Tessellate(50);                    
                                            wpipe.Add(pipe);
                                            copypipes.Add(polyline);
                                            normalCopys.Add(polyline);
                                        }
                                    }
                                    if (toilet.Identifier.Contains('T'))
                                    {
                                        foreach (Entity item in toilet.Representation)
                                        {
                                            var offset = Matrix3d.Displacement(200 * compositeEngine.ToiletPipes[0].Center.GetVectorTo(compositeEngine.ToiletPipes[1].Center).GetNormal());
                                            var polyline = item.GetTransformedCopy(toilet.Matrix.PostMultiplyBy(offset));
                                            Circle circle = polyline as Circle;
                                            Polyline pipe = circle.Tessellate(50);
                                            tpipe.Add(pipe);
                                            normalCopys.Add(polyline);                
                                        }
                                    }
                                    if (toilet.Identifier.Contains('D'))
                                    {
                                        foreach (Entity item in toilet.Representation)
                                        {
                                            var offset = Matrix3d.Displacement(200 * compositeEngine.ToiletPipes[0].Center.GetVectorTo(compositeEngine.ToiletPipes[1].Center).GetNormal());
                                            var polyline = item.GetTransformedCopy(toilet.Matrix.PostMultiplyBy(offset));
                                            Circle circle = polyline as Circle;
                                            Polyline pipe = circle.Tessellate(50);
                                            dpipe.Add(pipe);
                                            normalCopys.Add(polyline);
                                        }
                                    }                             
                                    foreach (Entity item in toilet.Representation)//在顶层打印
                                    {
                                        var offset = Matrix3d.Displacement(200 * compositeEngine.ToiletPipes[0].Center.GetVectorTo(compositeEngine.ToiletPipes[1].Center).GetNormal());                    
                                        acadDatabase.ModelSpace.Add(item.GetTransformedCopy(toilet.Matrix.PostMultiplyBy(offset)));                              
                                    }
                                }
                            }
                            else
                            {
                                for (int i = 0; i < compositeEngine.ToiletPipes.Count; i++)
                                {
                                    var toilet = compositeEngine.ToiletPipes[i];
                                    var radius = compositeEngine.ToiletPipeEngine.Parameters.Diameter[i] / 2.0;
                                    if (toilet.Identifier.Contains('F'))
                                    {
                                        foreach (Entity item in toilet.Representation)
                                        {
                                           
                                            var polyline = item.GetTransformedCopy(toilet.Matrix);
                                            Circle circle = polyline as Circle;
                                            Polyline pipe = circle.Tessellate(50);
                                            fpipe.Add(pipe);
                                            copypipes.Add(polyline);
                                            normalCopys.Add(polyline);
                                        }
                                    }
                                    if (toilet.Identifier.Contains('P'))
                                    {
                                        foreach (Entity item in toilet.Representation)
                                        {
                                           
                                            var polyline = item.GetTransformedCopy(toilet.Matrix);
                                            Circle circle = polyline as Circle;
                                            Polyline pipe = circle.Tessellate(50);
                                            ppipe.Add(pipe);
                                            copypipes.Add(polyline);
                                            normalCopys.Add(polyline);
                                        }
                                    }
                                    if (toilet.Identifier.Contains('W'))
                                    {
                                        foreach (Entity item in toilet.Representation)
                                        {
                                            
                                            var polyline = item.GetTransformedCopy(toilet.Matrix);
                                            Circle circle = polyline as Circle;
                                            Polyline pipe = circle.Tessellate(50);
                                            wpipe.Add(pipe);
                                            copypipes.Add(polyline);
                                            normalCopys.Add(polyline);
                                        }
                                    }
                                    if (toilet.Identifier.Contains('T'))
                                    {
                                        foreach (Entity item in toilet.Representation)
                                        {
                                           
                                            var polyline = item.GetTransformedCopy(toilet.Matrix);
                                            Circle circle = polyline as Circle;
                                            Polyline pipe = circle.Tessellate(50);
                                            tpipe.Add(pipe);
                                            normalCopys.Add(polyline);
                                        }
                                    }
                                    if (toilet.Identifier.Contains('D'))
                                    {
                                        foreach (Entity item in toilet.Representation)
                                        {
                                            
                                            var polyline = item.GetTransformedCopy(toilet.Matrix);
                                            Circle circle = polyline as Circle;
                                            Polyline pipe = circle.Tessellate(50);
                                            dpipe.Add(pipe);
                                            normalCopys.Add(polyline);
                                        }
                                    }                                 
                                    foreach (Entity item in toilet.Representation)
                                    {
                                        acadDatabase.ModelSpace.Add(item.GetTransformedCopy(toilet.Matrix));                               
                                    }
                                }
                            }
                        }
                        else
                        {
                            return;
                        }
                        for (int i = 0; i < tfloordrain_.Count; i++)
                        {
                            Matrix3d scale = Matrix3d.Scaling(2.0, tfloordrain_[i].Position);
                            var ent = tfloordrain_[i].GetTransformedCopy(scale);
                            acadDatabase.ModelSpace.Add(ent);
                            if (!GeomUtils.PtInLoop(boundary1, tfloordrain_[i].Position)&& composite.Toilet.CondensePipes.Count>0)
                            {
                                var line = new Line(tfloordrain_[i].Position + 50 * tfloordrain_[i].Position.GetVectorTo(composite.Toilet.CondensePipes[0].Outline.GetCenter()).GetNormal(),
                                composite.Toilet.CondensePipes[0].Outline.GetCenter() - 50 * tfloordrain_[i].Position.GetVectorTo(composite.Toilet.CondensePipes[0].Outline.GetCenter()).GetNormal());
                                acadDatabase.ModelSpace.Add(line);
                                normalCopys.Add(line);
                            }
                        }
                    }
                    foreach (var compositeBalcony in FloorEngines.TopFloors[0].CompositeBalconyRooms)
                    {   //判断是否为正确的Balcony
                        if (IsValidBalconyForFloorDrain(compositeBalcony.Balcony))
                        {
                            Polyline roofrainpipe = null;
                            Polyline tboundary = null;
                            Polyline bboundary = null;
                            Polyline downspout = null;
                            BlockReference washingmachine = null;
                            Polyline device = null;
                            Polyline condensepipe = null;
                            Polyline device_other = null;
                            BlockReference floordrain = null;
                            BlockReference bbasinline = null;
                            List<Polyline> condensepipes = new List<Polyline>();
                            var bfloordrain = new List<BlockReference>();
                            var devicefloordrain = new List<BlockReference>();
                            var rainpipe = new List<Polyline>();
                            foreach (var FloorDrain in compositeBalcony.Balcony.FloorDrains)
                            {
                                floordrain = FloorDrain.Outline as BlockReference;
                                bfloordrain.Add(floordrain);
                            }

                            bboundary = compositeBalcony.Balcony.Balcony.Boundary as Polyline;
                            if (compositeBalcony.Balcony.RainPipes.Count > 0)
                            {
                                foreach (var RainPipe in compositeBalcony.Balcony.RainPipes)
                                {
                                    var ent = RainPipe.Outline as Polyline;
                                    rainpipe.Add(ent);
                                    rain_pipe.Add(ent);
                                }
                            }                           
                                foreach (var devicePlatform in compositeBalcony.DevicePlatforms)
                                {
                                    if (devicePlatform.RainPipes.Count > 0)
                                    {
                                        foreach (var RainPipe in devicePlatform.RainPipes)
                                        {
                                            var ent = RainPipe.Outline as Polyline;
                                            rainpipe.Add(ent);
                                            rain_pipe.Add(ent);
                                        }
                                    }
                                }
                           if(!(rainpipe.Count>0))
                           { Active.Editor.WriteMessage("\n 缺雨水管"); }                                                                                   
                            if (compositeBalcony.Balcony.Washmachines.Count > 0)
                            {
                                washingmachine = compositeBalcony.Balcony.Washmachines[0].Outline as BlockReference;
                            }
                            if (compositeBalcony.DevicePlatforms.Count > 0)
                            {                             
                                device = compositeBalcony.DevicePlatforms[0].DevicePlatforms[0].Boundary.Clone() as Polyline;
                                device.Closed = true;
                            }
                            if (compositeBalcony.DevicePlatforms.Count>1)
                            {
                                Polyline temp = compositeBalcony.DevicePlatforms[1].DevicePlatforms[0].Boundary as Polyline;
                                if ((temp.GetCenter().DistanceTo(device.GetCenter())>2))
                                {
                                    device_other = compositeBalcony.DevicePlatforms[1].DevicePlatforms[0].Boundary.Clone() as Polyline;
                                    device_other.Closed = true;
                                }
                                else
                                {
                                    device_other = compositeBalcony.DevicePlatforms[2].DevicePlatforms[0].Boundary.Clone() as Polyline;
                                    device_other.Closed = true;
                                }
                            }                        
                            foreach (var devicePlatform in compositeBalcony.DevicePlatforms)
                            {
                                if (devicePlatform.CondensePipes.Count > 0)
                                {
                                    condensepipe = devicePlatform.CondensePipes[0].Outline as Polyline;                                   
                                    break;
                                }
                            }                           
                            foreach (var devicePlatform in compositeBalcony.DevicePlatforms)
                            {
                                if (devicePlatform.RoofRainPipes.Count > 0)
                                {
                                    roofrainpipe = devicePlatform.RoofRainPipes[0].Outline as Polyline;
                                    roofrain_pipe.Add(roofrainpipe);
                                    copyroofpipes.Add(new Circle() { Center = roofrainpipe.GetCenter(), Radius = 50.0 });
                                    copyroofpipes.Add(new Circle() { Center=roofrainpipe.GetCenter(), Radius=38.5 });
                                    copyroofpipes.Add(new Circle() { Center = roofrainpipe.GetCenter(), Radius = 55.0 });
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
                                devicefloordrain.Add(devicefloordrains);
                            }
                            if (compositeBalcony.Balcony.BasinTools.Count > 0)
                            {
                                bbasinline = compositeBalcony.Balcony.BasinTools[0].Outline as BlockReference;
                            }
                            foreach (var devicePlatform in compositeBalcony.DevicePlatforms)//考虑多余一个冷凝管
                            {
                                if (devicePlatform.CondensePipes.Count > 0)
                                {
                                    Polyline s = devicePlatform.CondensePipes[0].Outline as Polyline;
                                    npipe.Add(s);
                                    condensepipes.Add(s);
                                }
                            }
                            var thWBalconyFloordrainEngine = new ThWBalconyFloordrainEngine();
                            var thWToiletFloordrainEngine = new ThWToiletFloordrainEngine();
                            var thWDeviceFloordrainEngine = new ThWDeviceFloordrainEngine();
                            var FloordrainEngine = new ThWCompositeFloordrainEngine(thWBalconyFloordrainEngine, thWToiletFloordrainEngine, thWDeviceFloordrainEngine);
                            FloordrainEngine.Run(bfloordrain, bboundary, rainpipe, downspout, washingmachine, device, device_other, condensepipe, tfloordrain, tboundary, devicefloordrain, roofrainpipe, bbasinline, condensepipes);
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

                                Polyline ent_line1 = new Polyline();
                                ent_line1.AddVertexAt(0, FloordrainEngine.Downspout_to_Floordrain[i].ToPoint2d(), 0, 35, 35);
                                ent_line1.AddVertexAt(1, FloordrainEngine.Downspout_to_Floordrain[i + 1].ToPoint2d(), 0, 35, 35);
                                //ent_line1.Linetype = "DASHDED";
                                //ent_line1.Layer = "W-DRAI-DOME-PIPE";
                                //ent_line1.Color = Autodesk.AutoCAD.Colors.Color.FromColorIndex(Autodesk.AutoCAD.Colors.ColorMethod.ByLayer, 256);
                                ent_line1.Color=Autodesk.AutoCAD.Colors.Color.FromRgb(0, 255, 255);
                                acadDatabase.ModelSpace.Add(ent_line1);
                                normalCopys.Add(ent_line1);
                            }
                            FloordrainEngine.new_circle.Color= Autodesk.AutoCAD.Colors.Color.FromRgb(0, 255, 255);
                            acadDatabase.ModelSpace.Add(FloordrainEngine.new_circle);
                            Polyline downpipe = FloordrainEngine.new_circle.Tessellate(50);
                            fpipe.Add(downpipe);
                            copypipes.Add(FloordrainEngine.new_circle);
                            normalCopys.Add(FloordrainEngine.new_circle);
                            if (FloordrainEngine.Rainpipe_to_Floordrain.Count > 0)
                            {
                                for (int i = 0; i < FloordrainEngine.Rainpipe_to_Floordrain.Count - 1; i++)
                                {
                                    Polyline ent_line1 = new Polyline();
                                    ent_line1.AddVertexAt(0, FloordrainEngine.Rainpipe_to_Floordrain[i].ToPoint2d(), 0, 35, 35);
                                    ent_line1.AddVertexAt(1, FloordrainEngine.Rainpipe_to_Floordrain[i + 1].ToPoint2d(), 0, 35, 35);
                                    //ent_line1.Linetype = "DASHDOT";
                                    //ent_line1.Layer = "W-RAIN-PIPE";
                                    //ent_line1.Color = Autodesk.AutoCAD.Colors.Color.FromColorIndex(Autodesk.AutoCAD.Colors.ColorMethod.ByLayer, 256);
                                    ent_line1.Color= Autodesk.AutoCAD.Colors.Color.FromRgb(0, 255, 255);
                                    acadDatabase.ModelSpace.Add(ent_line1);
                                    normalCopys.Add(ent_line1);
                                }
                            }
                            if (FloordrainEngine.Bbasinline_to_Floordrain.Count > 0)
                            {
                                for (int i = 0; i < FloordrainEngine.Bbasinline_to_Floordrain.Count - 1; i++)
                                {
                                    Polyline ent_line1 = new Polyline();
                                    ent_line1.AddVertexAt(0, FloordrainEngine.Bbasinline_to_Floordrain[i].ToPoint2d(), 0, 35, 35);
                                    ent_line1.AddVertexAt(1, FloordrainEngine.Bbasinline_to_Floordrain[i + 1].ToPoint2d(), 0, 35, 35);
                                    //ent_line1.Linetype = "DASHDED";
                                    //ent_line1.Layer = "W-DRAI-DOME-PIPE";
                                    //ent_line1.Color = Autodesk.AutoCAD.Colors.Color.FromColorIndex(Autodesk.AutoCAD.Colors.ColorMethod.ByLayer, 256);
                                    ent_line1.Color = Autodesk.AutoCAD.Colors.Color.FromRgb(0, 255, 255);
                                    acadDatabase.ModelSpace.Add(ent_line1);
                                    normalCopys.Add(ent_line1);
                                }
                                acadDatabase.ModelSpace.Add(new Circle() { Radius = 50, Center = FloordrainEngine.Bbasinline_Center[0] });
                                normalCopys.Add(new Circle() { Radius = 50, Center = FloordrainEngine.Bbasinline_Center[0] });
                            }
                            //阳台输出完毕
                            for (int i = 0; i < devicefloordrain.Count; i++)
                            {
                                Matrix3d scale = Matrix3d.Scaling(2.0, devicefloordrain[i].Position);
                                var ent = devicefloordrain[i].GetTransformedCopy(scale);
                                acadDatabase.ModelSpace.Add(ent);
                            }
                            if (FloordrainEngine.Condensepipe_tofloordrains.Count > 1)
                            {
                                foreach (Point3dCollection Rainpipe_ in FloordrainEngine.Condensepipe_tofloordrains)
                                {
                                    for (int i = 0; i < Rainpipe_.Count - 1; i++)
                                    {
                                        Polyline ent_line1 = new Polyline();
                                        ent_line1.AddVertexAt(0, Rainpipe_[i].ToPoint2d(), 0, 35, 35);
                                        ent_line1.AddVertexAt(1, Rainpipe_[i + 1].ToPoint2d(), 0, 35, 35);
                                        //ent_line1.Linetype = "DASHDOT";
                                        //ent_line1.Layer = "W-RAIN-PIPE";
                                        ent_line1.Color = Autodesk.AutoCAD.Colors.Color.FromColorIndex(Autodesk.AutoCAD.Colors.ColorMethod.ByLayer, 256);
                                        acadDatabase.ModelSpace.Add(ent_line1);
                                        normalCopys.Add(ent_line1);
                                    }
                                }
                            }
                            else
                            {
                                for (int i = 0; i < FloordrainEngine.Condensepipe_tofloordrain.Count - 1; i++)
                                {
                                    Polyline ent_line1 = new Polyline();
                                    ent_line1.AddVertexAt(0, FloordrainEngine.Condensepipe_tofloordrain[i].ToPoint2d(), 0, 35, 35);
                                    ent_line1.AddVertexAt(1, FloordrainEngine.Condensepipe_tofloordrain[i + 1].ToPoint2d(), 0, 35, 35);
                                    //ent_line1.Linetype = "DASHDOT";
                                    //ent_line1.Layer = "W-RAIN-PIPE";
                                    //ent_line1.Color = Autodesk.AutoCAD.Colors.Color.FromColorIndex(Autodesk.AutoCAD.Colors.ColorMethod.ByLayer, 256);
                                    ent_line1.Color = Autodesk.AutoCAD.Colors.Color.FromRgb(0, 255, 255);
                                    acadDatabase.ModelSpace.Add(ent_line1);
                                    normalCopys.Add(ent_line1);
                                }
                            }
                            if (FloordrainEngine.Rainpipe_tofloordrains.Count > 1)
                            {
                                foreach (Point3dCollection Rainpipe_to in FloordrainEngine.Rainpipe_tofloordrains)
                                {
                                    for (int i = 0; i < Rainpipe_to.Count - 1; i++)
                                    {
                                        Polyline ent_line1 = new Polyline();
                                        ent_line1.AddVertexAt(0, Rainpipe_to[i].ToPoint2d(), 0, 35, 35);
                                        ent_line1.AddVertexAt(1, Rainpipe_to[i + 1].ToPoint2d(), 0, 35, 35);
                                       // ent_line1.Linetype = "DASHDOT";
                                        //ent_line1.Layer = "W-RAIN-PIPE";
                                        //ent_line1.Color = Autodesk.AutoCAD.Colors.Color.FromColorIndex(Autodesk.AutoCAD.Colors.ColorMethod.ByLayer, 256);
                                        ent_line1.Color = Autodesk.AutoCAD.Colors.Color.FromRgb(0, 255, 255);
                                        acadDatabase.ModelSpace.Add(ent_line1);
                                        normalCopys.Add(ent_line1);
                                    }
                                }
                            }
                            else
                            {
                                for (int i = 0; i < FloordrainEngine.Rainpipe_tofloordrain.Count - 1; i++)
                                {
                                    Polyline ent_line1 = new Polyline();
                                    ent_line1.AddVertexAt(0, FloordrainEngine.Rainpipe_tofloordrain[i].ToPoint2d(), 0, 35, 35);
                                    ent_line1.AddVertexAt(1, FloordrainEngine.Rainpipe_tofloordrain[i + 1].ToPoint2d(), 0, 35, 35);
                                    ent_line1.Linetype = "DASHDOT";
                                    //ent_line1.Layer = "W-RAIN-PIPE";
                                    //ent_line1.Color = Autodesk.AutoCAD.Colors.Color.FromColorIndex(Autodesk.AutoCAD.Colors.ColorMethod.ByLayer, 256);
                                    ent_line1.Color = Autodesk.AutoCAD.Colors.Color.FromRgb(0, 255, 255);
                                    acadDatabase.ModelSpace.Add(ent_line1);
                                    normalCopys.Add(ent_line1);
                                }
                            }
                        }                  
                    }                       
                }   
                var PipeindexEngine = new ThWInnerPipeIndexEngine();
                var composite_Engine = new ThWCompositeIndexEngine(PipeindexEngine);
                //开始标注 
                var rain_pipes = GetNewPipes(rain_pipe);//雨水管去重复
                var npipes = GetNewPipes(npipe);//冷凝管去重
                var roofrain_pipes = GetNewPipes(roofrain_pipe);//屋顶雨水管去重                            
                double x = 0.0;
                double y = 0.0;
                double x1 = 0.0;
                double y1 = 0.0;
                for (int i=0;i< FloorEngines.TopFloors[0].CompositeRooms.Count;i++)
                {
                    Polyline line = FloorEngines.TopFloors[0].CompositeRooms[i].Toilet.Toilet.Boundary as Polyline;
                    y += line.GetCenter().Y;
                    x += line.GetCenter().X;
                }
                for (int i = 0; i < FloorEngines.TopFloors[0].CompositeBalconyRooms.Count; i++)
                {
                    Polyline line = FloorEngines.TopFloors[0].CompositeBalconyRooms[i].Balcony.Balcony.Boundary as Polyline;
                    y1 += line.GetCenter().Y;
                    x1 += line.GetCenter().X;
                }
                //卫生间空间形心
                Point3d toiletpoint = new Point3d(x / FloorEngines.TopFloors[0].CompositeRooms.Count,
                    y/ FloorEngines.TopFloors[0].CompositeRooms.Count,0);
                //阳台空间形心
                Point3d balconypoint = new Point3d(x1/ FloorEngines.TopFloors[0].CompositeBalconyRooms.Count,
                    y1/ FloorEngines.TopFloors[0].CompositeBalconyRooms.Count,0);
                ThCADCoreNTSSpatialIndex obstacle = null;
                composite_Engine.Run(fpipe, tpipe, wpipe, ppipe, dpipe, npipes, rain_pipes, pboundary, divideLines, roofrain_pipes, toiletpoint, balconypoint,obstacle);
                for (int j = 0; j < composite_Engine.PipeEngine.Fpipeindex.Count; j++)
                {
                    for (int i = 0; i < composite_Engine.PipeEngine.Fpipeindex[j].Count; i++)
                    {
                        int num = 0;
                        if (composite_Engine.FpipeDublicated.Count > 0)
                        {
                            foreach (var points in composite_Engine.FpipeDublicated[j])
                            {
                                if (points[0].X == PipeindexEngine.Fpipeindex[j][i].X)
                                {
                                    for (int k = 0; k < points.Count; k++)
                                    {
                                        if (points[k].Y == PipeindexEngine.Fpipeindex[j][i].Y)
                                        {
                                            num = k;
                                        }
                                    }

                                }
                            }
                        }
                        double Yoffset = 250 * num;
                        Vector3d s = new Vector3d(0.0, Yoffset, 0.0);
                        var Matrix = Matrix3d.Displacement(s);
                        var tag1 = PipeindexEngine.Fpipeindex_tag[j][3 * i].TransformBy(Matrix);
                        var tag2 = PipeindexEngine.Fpipeindex_tag[j][3 * i + 1].TransformBy(Matrix);
                        var tag3 = PipeindexEngine.Fpipeindex_tag[j][3 * i + 2].TransformBy(Matrix);
                        Line ent_line = new Line(PipeindexEngine.Fpipeindex[j][i], tag1);
                        Line ent_line1 = new Line(tag1, tag2);
                       //ent_line.Layer = "W-DRAI-NOTE";
                        //ent_line.Color = Autodesk.AutoCAD.Colors.Color.FromColorIndex(Autodesk.AutoCAD.Colors.ColorMethod.ByLayer, 256);
                        ent_line.Color= ent_line1.Color = Autodesk.AutoCAD.Colors.Color.FromRgb(0, 255, 255);
                        ent_line1.Color = ent_line1.Color = Autodesk.AutoCAD.Colors.Color.FromRgb(0, 255, 255);
                        acadDatabase.ModelSpace.Add(ent_line);
                        copypipes.Add(ent_line);
                        normalCopys.Add(ent_line);
                        acadDatabase.ModelSpace.Add(ent_line1);
                        copypipes.Add(ent_line1);
                        normalCopys.Add(ent_line1);
                        DBText taggingtext = new DBText()
                        {
                            Height = 175,
                            Position = tag3,
                            TextString = $"FL{j/2}-{i + 1}",//原来为{floor.Value}
                            Color= Autodesk.AutoCAD.Colors.Color.FromRgb(0, 255, 255),
                    };
                        DBText taggingtext1 = new DBText()
                        {
                            Height = 175,
                            Position = tag3,
                            TextString = $"FL-{i + 1}",//原来为{floor.Value} 
                            Color = Autodesk.AutoCAD.Colors.Color.FromRgb(0, 255, 255),
                        };
                        DBText taggingtext2 = new DBText()
                        {
                            Height = 175,
                            Position = tag3,
                            TextString = $"FL{j/2}-{i + 1}‘",//原来为{floor.Value} 
                            Color = Autodesk.AutoCAD.Colors.Color.FromRgb(0, 255, 255),
                        };
                        DBText taggingtext3 = new DBText()
                        {
                            Height = 175,
                            Position = tag3,
                            TextString = $"FL-{i + 1}’",//原来为{floor.Value} 
                            Color = Autodesk.AutoCAD.Colors.Color.FromRgb(0, 255, 255),
                        };
                        if (j == 0)
                        {
                            acadDatabase.ModelSpace.Add(taggingtext1);
                            copypipes.Add(taggingtext1);
                            normalCopys.Add(taggingtext1);
                        }
                        else if (j == 1)
                        {
                            acadDatabase.ModelSpace.Add(taggingtext3);
                            copypipes.Add(taggingtext3);
                            normalCopys.Add(taggingtext3);
                        }
                        else if (j%2 == 1)
                        {
                            acadDatabase.ModelSpace.Add(taggingtext2);
                            copypipes.Add(taggingtext2);
                            normalCopys.Add(taggingtext2);
                        }
                        else 
                        {
                            acadDatabase.ModelSpace.Add(taggingtext);
                            copypipes.Add(taggingtext);
                            normalCopys.Add(taggingtext);
                        }
                    }
                }
                for (int j = 0; j < composite_Engine.PipeEngine.Tpipeindex.Count; j++)
                {
                    for (int i = 0; i < composite_Engine.PipeEngine.Tpipeindex[j].Count; i++)
                    {
                        int num = 0;
                        if (composite_Engine.FpipeDublicated.Count > 0)
                        {
                            foreach (var points in composite_Engine.FpipeDublicated[j])
                            {
                                if (points[0].X == PipeindexEngine.Tpipeindex[j][i].X)
                                {
                                    for (int k = 0; k < points.Count; k++)
                                    {
                                        if (points[k].Y == PipeindexEngine.Tpipeindex[j][i].Y)
                                        {
                                            num = k;
                                        }
                                    }

                                }
                            }
                        }
                        double Yoffset = 250 * num;
                        Vector3d s = new Vector3d(0.0, Yoffset, 0.0);
                        var Matrix = Matrix3d.Displacement(s);
                        var tag1 = PipeindexEngine.Tpipeindex_tag[j][3 * i].TransformBy(Matrix);
                        var tag2 = PipeindexEngine.Tpipeindex_tag[j][3 * i + 1].TransformBy(Matrix);
                        var tag3 = PipeindexEngine.Tpipeindex_tag[j][3 * i + 2].TransformBy(Matrix);
                        Line ent_line = new Line(PipeindexEngine.Tpipeindex[j][i], tag1);
                        Line ent_line1 = new Line(tag1, tag2);
                        //ent_line.Layer = "W-DRAI-NOTE";
                        //ent_line1.Layer = "W-DRAI-NOTE";
                        // ent_line.Color = Autodesk.AutoCAD.Colors.Color.FromColorIndex(Autodesk.AutoCAD.Colors.ColorMethod.ByLayer, 256);
                        ent_line.Color = Autodesk.AutoCAD.Colors.Color.FromRgb(0, 255, 255);
                        ent_line1.Color = Autodesk.AutoCAD.Colors.Color.FromRgb(0, 255, 255);
                        acadDatabase.ModelSpace.Add(ent_line);
                        normalCopys.Add(ent_line);
                        acadDatabase.ModelSpace.Add(ent_line1);
                        normalCopys.Add(ent_line1);
                        DBText taggingtext = new DBText()
                        {
                            Height = 175,
                            Position = tag3,
                            TextString = $"TL{j/2}-{i + 1}",
                            Color = Autodesk.AutoCAD.Colors.Color.FromRgb(0, 255, 255),
                    };
                        DBText taggingtext1 = new DBText()
                        {
                            Height = 175,
                            Position = tag3,
                            TextString = $"TL-{i + 1}",
                            Color= Autodesk.AutoCAD.Colors.Color.FromRgb(0, 255, 255),
                        };
                        DBText taggingtext2 = new DBText()
                        {
                            Height = 175,
                            Position = tag3,
                            TextString = $"TL{j/2}-{i + 1}‘",
                            Color = Autodesk.AutoCAD.Colors.Color.FromRgb(0, 255, 255),
                        };
                        DBText taggingtext3 = new DBText()
                        {
                            Height = 175,
                            Position = tag3,
                            TextString = $"TL-{i + 1}’",
                            Color = Autodesk.AutoCAD.Colors.Color.FromRgb(0, 255, 255),
                        };
                        if (j == 0)
                        {
                            acadDatabase.ModelSpace.Add(taggingtext1);
                            normalCopys.Add(taggingtext1);
                        }
                        else if (j == 1)
                        {
                            acadDatabase.ModelSpace.Add(taggingtext3);
                            normalCopys.Add(taggingtext3);
                        }
                        else if(j%2==1)
                        {
                            acadDatabase.ModelSpace.Add(taggingtext2);
                            normalCopys.Add(taggingtext2);
                        }
                        else
                        {
                            acadDatabase.ModelSpace.Add(taggingtext);
                            normalCopys.Add(taggingtext);
                        }
                    }
                }
                for (int j = 0; j < composite_Engine.PipeEngine.Wpipeindex.Count; j++)
                {
                    for (int i = 0; i < composite_Engine.PipeEngine.Wpipeindex[j].Count; i++)
                    {
                        int num = 0;
                        if (composite_Engine.FpipeDublicated.Count > 0)
                        {
                            foreach (var points in composite_Engine.FpipeDublicated[j])
                            {
                                if (points[0].X == PipeindexEngine.Wpipeindex[j][i].X)
                                {
                                    for (int k = 0; k < points.Count; k++)
                                    {
                                        if (points[k].Y == PipeindexEngine.Wpipeindex[j][i].Y)
                                        {
                                            num = k;
                                        }
                                    }

                                }
                            }
                        }
                        double Yoffset = 250 * num;
                        Vector3d s = new Vector3d(0.0, Yoffset, 0.0);
                        var Matrix = Matrix3d.Displacement(s);
                        var tag1 = PipeindexEngine.Wpipeindex_tag[j][3 * i].TransformBy(Matrix);
                        var tag2 = PipeindexEngine.Wpipeindex_tag[j][3 * i + 1].TransformBy(Matrix);
                        var tag3 = PipeindexEngine.Wpipeindex_tag[j][3 * i + 2].TransformBy(Matrix);
                        Line ent_line = new Line(PipeindexEngine.Wpipeindex[j][i], tag1);
                        Line ent_line1 = new Line(tag1, tag2);
                        //ent_line.Layer = "W-DRAI-NOTE";
                        //ent_line1.Layer = "W-DRAI-NOTE";
                        //ent_line.Color = Autodesk.AutoCAD.Colors.Color.FromColorIndex(Autodesk.AutoCAD.Colors.ColorMethod.ByLayer, 256);
                        ent_line.Color = Autodesk.AutoCAD.Colors.Color.FromRgb(0, 255, 255);
                        ent_line1.Color = Autodesk.AutoCAD.Colors.Color.FromRgb(0, 255, 255);
                        acadDatabase.ModelSpace.Add(ent_line);
                        copypipes.Add(ent_line);
                        normalCopys.Add(ent_line);
                        acadDatabase.ModelSpace.Add(ent_line1);
                        copypipes.Add(ent_line1);
                        normalCopys.Add(ent_line1);
                        DBText taggingtext = new DBText()
                        {
                            Height = 175,
                            Position = tag3,
                            TextString = $"WL{j/2}-{i + 1}",
                            Color = Autodesk.AutoCAD.Colors.Color.FromRgb(0, 255, 255),
                    };
                        DBText taggingtext1 = new DBText()
                        {
                            Height = 175,
                            Position = tag3,
                            TextString = $"WL-{i + 1}",
                            Color = Autodesk.AutoCAD.Colors.Color.FromRgb(0, 255, 255),
                        };
                        DBText taggingtext2 = new DBText()
                        {
                            Height = 175,
                            Position = tag3,
                            TextString = $"WL{j / 2}-{i + 1}‘",
                            Color = Autodesk.AutoCAD.Colors.Color.FromRgb(0, 255, 255),
                        };
                        DBText taggingtext3 = new DBText()
                        {
                            Height = 175,
                            Position = tag3,
                            TextString = $"WL-{i + 1}’",
                            Color = Autodesk.AutoCAD.Colors.Color.FromRgb(0, 255, 255),
                        };
                        if (j==0)
                        {
                            acadDatabase.ModelSpace.Add(taggingtext1);
                            copypipes.Add(taggingtext1);
                            normalCopys.Add(taggingtext1);
                        }
                        else if (j == 1)
                        {
                            acadDatabase.ModelSpace.Add(taggingtext3);
                            copypipes.Add(taggingtext3);
                            normalCopys.Add(taggingtext3);
                        }
                        else if(j%2==1)
                        {
                            acadDatabase.ModelSpace.Add(taggingtext2);
                            copypipes.Add(taggingtext2);
                            normalCopys.Add(taggingtext2);
                        }
                        else
                        {
                            acadDatabase.ModelSpace.Add(taggingtext);
                            copypipes.Add(taggingtext);
                            normalCopys.Add(taggingtext);
                        }
                    }
                }
                for (int j = 0; j < composite_Engine.PipeEngine.Ppipeindex.Count; j++)
                {
                    for (int i = 0; i < composite_Engine.PipeEngine.Ppipeindex[j].Count; i++)
                    {
                        int num = 0;
                        if (composite_Engine.FpipeDublicated.Count > 0)
                        {
                            foreach (var points in composite_Engine.FpipeDublicated[j])
                            {
                                if (points[0].X == PipeindexEngine.Ppipeindex[j][i].X)
                                {
                                    for (int k = 0; k < points.Count; k++)
                                    {
                                        if (points[k].Y == PipeindexEngine.Ppipeindex[j][i].Y)
                                        {
                                            num = k;
                                        }
                                    }

                                }
                            }
                        }
                        double Yoffset = 250 * num;
                        Vector3d s = new Vector3d(0.0, Yoffset, 0.0);
                        var Matrix = Matrix3d.Displacement(s);
                        var tag1 = PipeindexEngine.Ppipeindex_tag[j][3 * i].TransformBy(Matrix);
                        var tag2 = PipeindexEngine.Ppipeindex_tag[j][3 * i + 1].TransformBy(Matrix);
                        var tag3 = PipeindexEngine.Ppipeindex_tag[j][3 * i + 2].TransformBy(Matrix);
                        Line ent_line = new Line(PipeindexEngine.Ppipeindex[j][i], tag1);
                        Line ent_line1 = new Line(tag1, tag2);
                        //ent_line.Layer = "W-DRAI-NOTE";
                        //ent_line1.Layer = "W-DRAI-NOTE";
                        //ent_line.Color = Autodesk.AutoCAD.Colors.Color.FromColorIndex(Autodesk.AutoCAD.Colors.ColorMethod.ByLayer, 256);
                        ent_line.Color = Autodesk.AutoCAD.Colors.Color.FromRgb(0, 255, 255);
                        ent_line1.Color = Autodesk.AutoCAD.Colors.Color.FromRgb(0, 255, 255);
                        acadDatabase.ModelSpace.Add(ent_line);
                        copypipes.Add(ent_line);
                        normalCopys.Add(ent_line);
                        acadDatabase.ModelSpace.Add(ent_line1);
                        copypipes.Add(ent_line1);
                        normalCopys.Add(ent_line1);
                        DBText taggingtext = new DBText()
                        {
                            Height = 175,
                            Position = tag3,
                            TextString = $"PL{j/2}-{i + 1}",
                            Color= Autodesk.AutoCAD.Colors.Color.FromRgb(0, 255, 255),
                    };
                        DBText taggingtext1 = new DBText()
                        {
                            Height = 175,
                            Position = tag3,
                            TextString = $"PL-{i + 1}",
                            Color = Autodesk.AutoCAD.Colors.Color.FromRgb(0, 255, 255),
                        };
                        DBText taggingtext2 = new DBText()
                        {
                            Height = 175,
                            Position = tag3,
                            TextString = $"PL{j / 2}-{i + 1}‘",
                            Color = Autodesk.AutoCAD.Colors.Color.FromRgb(0, 255, 255),
                        };
                        DBText taggingtext3 = new DBText()
                        {
                            Height = 175,
                            Position = tag3,
                            TextString = $"PL-{i + 1}’",
                            Color = Autodesk.AutoCAD.Colors.Color.FromRgb(0, 255, 255),
                        };
                        if (j==0)
                        {
                            acadDatabase.ModelSpace.Add(taggingtext1);
                            copypipes.Add(taggingtext1);
                            normalCopys.Add(taggingtext1);
                        }
                        else if (j == 1)
                        {
                            acadDatabase.ModelSpace.Add(taggingtext3);
                            copypipes.Add(taggingtext3);
                            normalCopys.Add(taggingtext3);
                        }
                        else if(j%2==1)
                        {
                            acadDatabase.ModelSpace.Add(taggingtext2);
                            copypipes.Add(taggingtext2);
                            normalCopys.Add(taggingtext2);
                        }
                        else
                        {
                            acadDatabase.ModelSpace.Add(taggingtext);
                            copypipes.Add(taggingtext);
                            normalCopys.Add(taggingtext);
                        }
                    }
                }
                for (int j = 0; j < composite_Engine.PipeEngine.Dpipeindex.Count; j++)
                {
                    for (int i = 0; i < composite_Engine.PipeEngine.Dpipeindex[j].Count; i++)
                    {
                        int num = 0;
                        if (composite_Engine.FpipeDublicated.Count > 0)
                        {
                            foreach (var points in composite_Engine.FpipeDublicated[j])
                            {
                                if (points[0].X == PipeindexEngine.Dpipeindex[j][i].X)
                                {
                                    for (int k = 0; k < points.Count; k++)
                                    {
                                        if (points[k].Y == PipeindexEngine.Dpipeindex[j][i].Y)
                                        {
                                            num = k;
                                        }
                                    }

                                }
                            }
                        }
                        double Yoffset = 250 * num;
                        Vector3d s = new Vector3d(0.0, Yoffset, 0.0);
                        var Matrix = Matrix3d.Displacement(s);
                        var tag1 = PipeindexEngine.Dpipeindex_tag[j][3 * i].TransformBy(Matrix);
                        var tag2 = PipeindexEngine.Dpipeindex_tag[j][3 * i + 1].TransformBy(Matrix);
                        var tag3 = PipeindexEngine.Dpipeindex_tag[j][3 * i + 2].TransformBy(Matrix);
                        Line ent_line = new Line(PipeindexEngine.Dpipeindex[j][i], tag1);
                        Line ent_line1 = new Line(tag1, tag2);
                        //ent_line.Layer = "W-DRAI-NOTE";
                        //ent_line1.Layer = "W-DRAI-NOTE";
                        //ent_line.Color = Autodesk.AutoCAD.Colors.Color.FromColorIndex(Autodesk.AutoCAD.Colors.ColorMethod.ByLayer, 256);
                        ent_line.Color = Autodesk.AutoCAD.Colors.Color.FromRgb(0, 255, 255);
                        ent_line1.Color = Autodesk.AutoCAD.Colors.Color.FromRgb(0, 255, 255);
                        acadDatabase.ModelSpace.Add(ent_line);
                        normalCopys.Add(ent_line);
                        acadDatabase.ModelSpace.Add(ent_line1);
                        normalCopys.Add(ent_line1);
                        DBText taggingtext = new DBText()
                        {
                            Height = 175,
                            Position = tag3,
                            TextString = $"DL{j/2}-{i + 1}",
                            Color= Autodesk.AutoCAD.Colors.Color.FromRgb(0, 255, 255),
                    };
                        DBText taggingtext1 = new DBText()
                        {
                            Height = 175,
                            Position = tag3,
                            TextString = $"DL-{i + 1}",
                            Color = Autodesk.AutoCAD.Colors.Color.FromRgb(0, 255, 255),
                        };
                        DBText taggingtext2 = new DBText()
                        {
                            Height = 175,
                            Position = tag3,
                            TextString = $"DL{j / 2}-{i + 1}‘",
                            Color = Autodesk.AutoCAD.Colors.Color.FromRgb(0, 255, 255),
                        };
                        DBText taggingtext3 = new DBText()
                        {
                            Height = 175,
                            Position = tag3,
                            TextString = $"DL-{i + 1}’",
                            Color = Autodesk.AutoCAD.Colors.Color.FromRgb(0, 255, 255),
                        };
                        if (j==0)
                        {
                            acadDatabase.ModelSpace.Add(taggingtext1);
                            normalCopys.Add(taggingtext1);
                        }
                        else if (j == 1)
                        {
                            acadDatabase.ModelSpace.Add(taggingtext3);
                            normalCopys.Add(taggingtext3);
                        }
                        else if(j%2==1)
                        {
                            acadDatabase.ModelSpace.Add(taggingtext2);
                            normalCopys.Add(taggingtext2);
                        }
                        else
                        {
                            acadDatabase.ModelSpace.Add(taggingtext);
                            normalCopys.Add(taggingtext);
                        }
                    }
                }
                for (int j = 0; j < composite_Engine.PipeEngine.Npipeindex.Count; j++)
                {
                    for (int i = 0; i < composite_Engine.PipeEngine.Npipeindex[j].Count; i++)
                    {
                        int num = 0;
                        if (composite_Engine.FpipeDublicated.Count > 0)
                        {
                            foreach (var points in composite_Engine.FpipeDublicated[j])
                            {
                                if (points[0].X == PipeindexEngine.Npipeindex[j][i].X)
                                {
                                    for (int k = 0; k < points.Count; k++)
                                    {
                                        if (points[k].Y == PipeindexEngine.Npipeindex[j][i].Y)
                                        {
                                            num = k;
                                        }
                                    }

                                }
                            }
                        }
                        double Yoffset = 250 * num;
                        Vector3d s = new Vector3d(0.0, Yoffset, 0.0);
                        var Matrix = Matrix3d.Displacement(s);
                        var tag1 = PipeindexEngine.Npipeindex_tag[j][3 * i].TransformBy(Matrix);
                        var tag2 = PipeindexEngine.Npipeindex_tag[j][3 * i + 1].TransformBy(Matrix);
                        var tag3 = PipeindexEngine.Npipeindex_tag[j][3 * i + 2].TransformBy(Matrix);
                        Circle circle = new Circle
                        {
                            Center = PipeindexEngine.Npipeindex[j][i],
                            Radius = 50,
                            Color = Autodesk.AutoCAD.Colors.Color.FromRgb(0, 255, 255)
                        };
                        acadDatabase.ModelSpace.Add(circle);
                        normalCopys.Add(circle);
                        Line ent_line = new Line(PipeindexEngine.Npipeindex[j][i], tag1);
                        Line ent_line1 = new Line(tag1, tag2);
                        //ent_line.Layer = "W-DRAI-NOTE";
                        //ent_line1.Layer = "W-DRAI-NOTE";
                        //ent_line.Color = Autodesk.AutoCAD.Colors.Color.FromColorIndex(Autodesk.AutoCAD.Colors.ColorMethod.ByLayer, 256);
                        ent_line.Color = Autodesk.AutoCAD.Colors.Color.FromRgb(0, 255, 255);
                        ent_line1.Color = Autodesk.AutoCAD.Colors.Color.FromRgb(0, 255, 255);
                        acadDatabase.ModelSpace.Add(ent_line);
                        normalCopys.Add(ent_line);
                        acadDatabase.ModelSpace.Add(ent_line1);
                        normalCopys.Add(ent_line1);
                        DBText taggingtext = new DBText()
                        {
                            Height = 175,
                            Position = tag3,
                            TextString = $"NL{j/2}-{i + 1}",
                            Color= Autodesk.AutoCAD.Colors.Color.FromRgb(0, 255, 255),
                    };
                        DBText taggingtext1 = new DBText()
                        {
                            Height = 175,
                            Position = tag3,
                            TextString = $"NL-{i + 1}",
                            Color = Autodesk.AutoCAD.Colors.Color.FromRgb(0, 255, 255),
                        };
                        DBText taggingtext2 = new DBText()
                        {
                            Height = 175,
                            Position = tag3,
                            TextString = $"NL{j / 2}-{i + 1}‘",
                            Color = Autodesk.AutoCAD.Colors.Color.FromRgb(0, 255, 255),
                        };
                        DBText taggingtext3 = new DBText()
                        {
                            Height = 175,
                            Position = tag3,
                            TextString = $"NL-{i + 1}’",
                            Color = Autodesk.AutoCAD.Colors.Color.FromRgb(0, 255, 255),
                        };
                        if (j==0)
                        {
                            acadDatabase.ModelSpace.Add(taggingtext1);
                            normalCopys.Add(taggingtext1);
                        }
                        else if (j == 1)
                        {
                            acadDatabase.ModelSpace.Add(taggingtext3);
                            normalCopys.Add(taggingtext3);
                        }
                        else if(j%2==1)
                        {
                            acadDatabase.ModelSpace.Add(taggingtext2);
                            normalCopys.Add(taggingtext2);
                        }
                        else
                        {
                            acadDatabase.ModelSpace.Add(taggingtext);
                            normalCopys.Add(taggingtext);
                        }
                    }
                }
                for (int j = 0; j < composite_Engine.PipeEngine.Rainpipeindex.Count; j++)
                {
                    for (int i = 0; i < composite_Engine.PipeEngine.Rainpipeindex[j].Count; i++)
                    {
                        int num = 0;
                        if (composite_Engine.FpipeDublicated.Count > 0)
                        {
                            foreach (var points in composite_Engine.FpipeDublicated[j])
                            {
                                if (points[0].X == PipeindexEngine.Rainpipeindex[j][i].X)
                                {
                                    for (int k = 0; k < points.Count; k++)
                                    {
                                        if (points[k].Y == PipeindexEngine.Rainpipeindex[j][i].Y)
                                        {
                                            num = k;
                                        }
                                    }

                                }
                            }
                        }
                        double Yoffset = 250 * num;
                        Vector3d s = new Vector3d(0.0, Yoffset, 0.0);
                        var Matrix = Matrix3d.Displacement(s);
                        var tag1 = PipeindexEngine.Rainpipeindex_tag[j][3 * i].TransformBy(Matrix);
                        var tag2 = PipeindexEngine.Rainpipeindex_tag[j][3 * i + 1].TransformBy(Matrix);
                        var tag3 = PipeindexEngine.Rainpipeindex_tag[j][3 * i + 2].TransformBy(Matrix);
                        Circle circle = new Circle
                        {
                            Center = PipeindexEngine.Rainpipeindex[j][i],
                            Radius = 50,
                            Color = Autodesk.AutoCAD.Colors.Color.FromRgb(0, 255, 255)
                        };
                        acadDatabase.ModelSpace.Add(circle);
                        normalCopys.Add(circle);
                        Line ent_line = new Line(PipeindexEngine.Rainpipeindex[j][i], tag1);
                        Line ent_line1 = new Line(tag1, tag2);
                        //ent_line.Layer = "W-DRAI-NOTE";
                        //ent_line1.Layer = "W-DRAI-NOTE";
                        //ent_line.Color = Autodesk.AutoCAD.Colors.Color.FromColorIndex(Autodesk.AutoCAD.Colors.ColorMethod.ByLayer, 256);
                        ent_line.Color = Autodesk.AutoCAD.Colors.Color.FromRgb(0, 255, 255);
                        ent_line1.Color = Autodesk.AutoCAD.Colors.Color.FromRgb(0, 255, 255);
                        acadDatabase.ModelSpace.Add(ent_line);                  
                        normalCopys.Add(ent_line);
                        acadDatabase.ModelSpace.Add(ent_line1);                     
                        normalCopys.Add(ent_line1);
                        DBText taggingtext = new DBText()
                        {
                            Height = 175,
                            Position = tag3,
                            TextString = $"Y2L{j/2}-{i + 1}",
                            Color = Autodesk.AutoCAD.Colors.Color.FromRgb(0, 255, 255),
                    };
                        DBText taggingtext1 = new DBText()
                        {
                            Height = 175,
                            Position = tag3,
                            TextString = $"Y2L-{i + 1}",
                            Color = Autodesk.AutoCAD.Colors.Color.FromRgb(0, 255, 255),
                        };
                        DBText taggingtext2 = new DBText()
                        {
                            Height = 175,
                            Position = tag3,
                            TextString = $"Y2L{j / 2}-{i + 1}‘",
                            Color = Autodesk.AutoCAD.Colors.Color.FromRgb(0, 255, 255),
                        };
                        DBText taggingtext3 = new DBText()
                        {
                            Height = 175,
                            Position = tag3,
                            TextString = $"Y2L-{i + 1}’",
                            Color = Autodesk.AutoCAD.Colors.Color.FromRgb(0, 255, 255),
                        };
                        if (j==0)
                        {
                            acadDatabase.ModelSpace.Add(taggingtext1);                       
                            normalCopys.Add(taggingtext1);
                        }
                        else if (j == 1)
                        {
                            acadDatabase.ModelSpace.Add(taggingtext3);
                            normalCopys.Add(taggingtext3);
                        }
                        else if(j%2==1)
                        {
                            acadDatabase.ModelSpace.Add(taggingtext2);                         
                            normalCopys.Add(taggingtext2);
                        }
                        else
                        {
                            acadDatabase.ModelSpace.Add(taggingtext);
                            normalCopys.Add(taggingtext);
                        }
                    }
                }
                for (int j = 0; j < composite_Engine.PipeEngine.RoofRainpipeindex.Count; j++)
                {
                    for (int i = 0; i < composite_Engine.PipeEngine.RoofRainpipeindex[j].Count; i++)
                    {
                        int num = 0;
                        if (composite_Engine.FpipeDublicated.Count > 0)
                        {
                            foreach (var points in composite_Engine.FpipeDublicated[j])
                            {
                                if (points[0].X == PipeindexEngine.RoofRainpipeindex[j][i].X)
                                {
                                    for (int k = 0; k < points.Count; k++)
                                    {
                                        if (points[k].Y == PipeindexEngine.RoofRainpipeindex[j][i].Y)
                                        {
                                            num = k;
                                        }
                                    }

                                }
                            }
                        }
                        double Yoffset = 250 * num;
                        Vector3d s = new Vector3d(0.0, Yoffset, 0.0);
                        var Matrix = Matrix3d.Displacement(s);
                        var tag1 = PipeindexEngine.RoofRainpipeindex_tag[j][3 * i].TransformBy(Matrix);
                        var tag2 = PipeindexEngine.RoofRainpipeindex_tag[j][3 * i + 1].TransformBy(Matrix);
                        var tag3 = PipeindexEngine.RoofRainpipeindex_tag[j][3 * i + 2].TransformBy(Matrix);
                        Circle circle = new Circle
                        {
                            Center = PipeindexEngine.RoofRainpipeindex[j][i],
                            Radius = 50,
                            Color = Autodesk.AutoCAD.Colors.Color.FromRgb(0, 255, 255)
                        };
                        acadDatabase.ModelSpace.Add(circle);
                        Line ent_line = new Line(PipeindexEngine.RoofRainpipeindex[j][i], tag1);
                        Line ent_line1 = new Line(tag1, tag2);
                        //ent_line.Layer = "W-DRAI-NOTE";
                        //ent_line1.Layer = "W-DRAI-NOTE";
                        //ent_line.Color = Autodesk.AutoCAD.Colors.Color.FromColorIndex(Autodesk.AutoCAD.Colors.ColorMethod.ByLayer, 256);
                        ent_line.Color = Autodesk.AutoCAD.Colors.Color.FromRgb(0, 255, 255);
                        ent_line1.Color = Autodesk.AutoCAD.Colors.Color.FromRgb(0, 255, 255);
                        acadDatabase.ModelSpace.Add(ent_line);
                        copyrooftags.Add(ent_line);
                        normalCopys.Add(ent_line);
                        normalCopys.Add(circle);
                        acadDatabase.ModelSpace.Add(ent_line1);
                        copyrooftags.Add(ent_line1);
                        normalCopys.Add(ent_line1);
                        DBText taggingtext = new DBText()
                        {
                            Height = 175,
                            Position = tag3,
                            TextString = $"Y1L{j/2}-{i + 1}",
                            Color= Autodesk.AutoCAD.Colors.Color.FromRgb(0, 255, 255),
                    };
                        DBText taggingtext1 = new DBText()
                        {
                            Height = 175,
                            Position = tag3,
                            TextString = $"Y1L-{i + 1}",
                            Color = Autodesk.AutoCAD.Colors.Color.FromRgb(0, 255, 255),
                        };
                        DBText taggingtext2 = new DBText()
                        {
                            Height = 175,
                            Position = tag3,
                            TextString = $"Y1L{j / 2}-{i + 1}‘",
                            Color = Autodesk.AutoCAD.Colors.Color.FromRgb(0, 255, 255),
                        };
                        DBText taggingtext3 = new DBText()
                        {
                            Height = 175,
                            Position = tag3,
                            TextString = $"Y1L-{i + 1}’",
                            Color = Autodesk.AutoCAD.Colors.Color.FromRgb(0, 255, 255),
                        };
                        if (j==0)
                        {
                            acadDatabase.ModelSpace.Add(taggingtext1);
                            copyrooftags.Add(taggingtext1);
                            normalCopys.Add(taggingtext1);
                        }
                        else if (j == 1)
                        {
                            acadDatabase.ModelSpace.Add(taggingtext3);
                            copyrooftags.Add(taggingtext3);
                            normalCopys.Add(taggingtext3);
                        }
                        else if(j%2==1)
                        {
                            acadDatabase.ModelSpace.Add(taggingtext2);
                            copyrooftags.Add(taggingtext2);
                            normalCopys.Add(taggingtext2);
                        }
                        else
                        {
                            acadDatabase.ModelSpace.Add(taggingtext);
                            copyrooftags.Add(taggingtext);
                            normalCopys.Add(taggingtext);
                        }
                    }
                }
                if (FloorEngines.RoofFloors.Count > 0)
                {
                    foreach (var ent in copypipes)
                    {
                        if (baseCenter2.Count > 0)
                        {
                            var offset = Matrix3d.Displacement(baseCenter2[0].GetVectorTo(baseCenter1[0]));
                            acadDatabase.ModelSpace.Add(ent.GetTransformedCopy(offset));//管井复制到屋顶层
                            //一定要对屋顶雨水管重排序
                            var PipeindexEngine1 = new ThWInnerPipeIndexEngine();
                            var composite_Engine1 = new ThWCompositeIndexEngine(PipeindexEngine1);
                            List<Line> divideLines1 = new List<Line>();
                            foreach(Line line in divideLines)
                            {
                                divideLines1.Add(new Line(line.StartPoint+ baseCenter2[0].GetVectorTo(baseCenter1[0]),
                                line.EndPoint + baseCenter2[0].GetVectorTo(baseCenter1[0])));
                            }
                            Polyline pboundary1 = null;
                            pboundary1 = FloorEngines.RoofFloors[0].RoofFloor.Boundary as Polyline;
                            List<Polyline> noline = new List<Polyline>();
                            composite_Engine1.Run(noline, noline, noline, noline, noline, noline, noline, pboundary1, divideLines1, roofRoofRainPipes,toiletpoint,balconypoint,obstacle);
                            //对顶层屋顶雨水管重新排序
                            for (int j = 0; j < composite_Engine1.PipeEngine.RoofRainpipeindex.Count; j++)
                            {
                                int count = composite_Engine.PipeEngine.RoofRainpipeindex[j].Count;
                                for (int i = 0; i < composite_Engine1.PipeEngine.RoofRainpipeindex[j].Count; i++)
                                {                             
                                    int num = 0;
                                    if (composite_Engine.FpipeDublicated.Count > 0)
                                    {
                                        foreach (var points in composite_Engine.FpipeDublicated[j])
                                        {
                                            if (points[0].X == PipeindexEngine1.RoofRainpipeindex[j][i].X)
                                            {
                                                for (int k = 0; k < points.Count; k++)
                                                {
                                                    if (points[k].Y == PipeindexEngine1.RoofRainpipeindex[j][i].Y)
                                                    {
                                                        num = k;
                                                    }
                                                }

                                            }
                                        }
                                    }
                                    double Yoffset = 250 * num;
                                    Vector3d s = new Vector3d(0.0, Yoffset, 0.0);
                                    var Matrix = Matrix3d.Displacement(s);
                                    var tag1 = PipeindexEngine1.RoofRainpipeindex_tag[j][3 * i].TransformBy(Matrix);
                                    var tag2 = PipeindexEngine1.RoofRainpipeindex_tag[j][3 * i + 1].TransformBy(Matrix);
                                    var tag3 = PipeindexEngine1.RoofRainpipeindex_tag[j][3 * i + 2].TransformBy(Matrix);
                                    Circle circle = new Circle
                                    {
                                        Center = PipeindexEngine1.RoofRainpipeindex[j][i],
                                        Radius = 50,
                                        Color = Autodesk.AutoCAD.Colors.Color.FromRgb(0, 255, 255)
                                    };
                                    acadDatabase.ModelSpace.Add(circle);
                                    Line ent_line = new Line(PipeindexEngine1.RoofRainpipeindex[j][i], tag1);
                                    Line ent_line1 = new Line(tag1, tag2);
                                    //ent_line.Layer = "W-DRAI-NOTE";
                                    //ent_line1.Layer = "W-DRAI-NOTE";
                                    //ent_line.Color = Autodesk.AutoCAD.Colors.Color.FromColorIndex(Autodesk.AutoCAD.Colors.ColorMethod.ByLayer, 256);
                                    ent_line.Color = Autodesk.AutoCAD.Colors.Color.FromRgb(0, 255, 255);
                                    ent_line1.Color = Autodesk.AutoCAD.Colors.Color.FromRgb(0, 255, 255);
                                    acadDatabase.ModelSpace.Add(ent_line);                                  
                                    acadDatabase.ModelSpace.Add(ent_line1);                                
                                    DBText taggingtext = new DBText()
                                    {
                                        Height = 175,
                                        Position = tag3,
                                        TextString = $"Y1L{j / 2}-{i + 1+count}",
                                        Color = Autodesk.AutoCAD.Colors.Color.FromRgb(0, 255, 255),
                                    };
                                    DBText taggingtext1 = new DBText()
                                    {
                                        Height = 175,
                                        Position = tag3,
                                        TextString = $"Y1L-{i + 1+count}",
                                        Color = Autodesk.AutoCAD.Colors.Color.FromRgb(0, 255, 255),
                                    };
                                    DBText taggingtext2 = new DBText()
                                    {
                                        Height = 175,
                                        Position = tag3,
                                        TextString = $"Y1L{j / 2}-{i + 1+count}‘",
                                        Color = Autodesk.AutoCAD.Colors.Color.FromRgb(0, 255, 255),
                                    };
                                    DBText taggingtext3 = new DBText()
                                    {
                                        Height = 175,
                                        Position = tag3,
                                        TextString = $"Y1L-{i + 1+count}’",
                                        Color = Autodesk.AutoCAD.Colors.Color.FromRgb(0, 255, 255),
                                    };
                                    if (j == 0)
                                    {
                                        acadDatabase.ModelSpace.Add(taggingtext1);                                     
                                    }
                                    else if (j == 1)
                                    {
                                        acadDatabase.ModelSpace.Add(taggingtext3);                                    
                                    }
                                    else if (j % 2 == 1)
                                    {
                                        acadDatabase.ModelSpace.Add(taggingtext2);                                      
                                    }
                                    else
                                    {
                                        acadDatabase.ModelSpace.Add(taggingtext);                                      
                                    }
                                }
                            }
                            if (baseCenter0.Count > 0)
                            {
                                var offset1 = Matrix3d.Displacement(baseCenter2[0].GetVectorTo(baseCenter0[0]));
                                Line s1 = ent as Line;
                                Polyline s2= ent as Polyline;
                                Circle s3 = ent as Circle;
                                DBText s4= ent as DBText;
                                foreach(var bound in FloorEngines.RoofDeviceFloors[0].SubSpaces)
                                {
                                    Polyline boundary = bound.Boundary as Polyline;
                                    if((s1!=null&& GeomUtils.PtInLoop(boundary, s1.StartPoint))|| (s2 != null && GeomUtils.PtInLoop(boundary, s2.StartPoint))
                                        || (s3 != null && GeomUtils.PtInLoop(boundary, s3.Center))|| (s4 != null && GeomUtils.PtInLoop(boundary, s4.Position)))
                                    {
                                        acadDatabase.ModelSpace.Add(ent.GetTransformedCopy(offset1));//管井复制到屋顶设备层
                                    }
                                }
                            }
                        }
                    }
                    //标注顶层雨水管
                    foreach (Circle ent in copyroofpipes)              
                    {                     
                        Polyline bucket = ent.Tessellate(50);
                        Point3d center = Point3d.Origin;
                        Point3d center1 = Point3d.Origin;
                        if (baseCenter2.Count > 0)
                        {
                            int num = 0;                           
                            var offset = Matrix3d.Displacement(baseCenter2[0].GetVectorTo(baseCenter1[0]));                           
                            center = bucket.GetCenter() + baseCenter2[0].GetVectorTo(baseCenter1[0]);
                            int s = 0;
                            foreach(var gravitybucket in gravityWaterBucket1)
                            {
                                if (gravitybucket.Position.DistanceTo(center)<2)
                                {
                                    s = 1;
                                    break;
                                }
                            }                                           
                            Circle alert = new Circle() { Center = center, Radius = 100 };
                            Polyline alertresult = alert.Tessellate(100);
                            foreach (Point3d bucket_1 in waterbuckets2)
                            {
                                if (Checkbucket(center, bucket_1, r_boundary))
                                {
                                        s += 1;                                      
                                        break;                                                                                              
                                }
                            }
                            if (s == 0)
                            {
                                acadDatabase.ModelSpace.Add(ent.GetTransformedCopy(offset));//管井复制到屋顶层                                                         
                            }
                            if (s == 0)
                            {
                                acadDatabase.ModelSpace.Add(alertresult);//生成错误提示    
                            }
                            if (baseCenter0.Count > 0)
                            {                             
                                var offset1 = Matrix3d.Displacement(baseCenter2[0].GetVectorTo(baseCenter0[0]));
                                foreach (var bound in FloorEngines.RoofDeviceFloors[0].SubSpaces)
                                {
                                    Polyline boundary = bound.Boundary as Polyline;
                                    if (GeomUtils.PtInLoop(boundary, ent.Center+ baseCenter2[0].GetVectorTo(baseCenter0[0])))
                                    {
                                        num = 1;
                                        break;
                                    }
                                }
                                if (num == 1)
                                {
                                    center1 = bucket.GetCenter() + baseCenter2[0].GetVectorTo(baseCenter0[0]);
                                    int s1 = 0;
                                    foreach (var gravitybucket in gravityWaterBucket)
                                    {
                                        if (gravitybucket.Position.DistanceTo(center1) < 2)
                                        {
                                            s1 = 1;
                                            break;
                                        }
                                    }                                   
                                    Circle alert1 = new Circle() { Center = center1, Radius = 100 };
                                    Polyline alertresult1 = alert1.Tessellate(100);
                                    foreach (Point3d bucket_1 in waterbuckets1)
                                    {
                                        if (Checkbucket(center1, bucket_1, d_boundary))
                                        {
                                            s1 += 1;
                                            break;
                                        }
                                    }
                                    if (s1 == 0)
                                    {
                                        acadDatabase.ModelSpace.Add(ent.GetTransformedCopy(offset1));//管井复制到屋顶设备层                                                                  
                                    }
                                    if (s1 == 0)
                                    {
                                        acadDatabase.ModelSpace.Add(alertresult1);//生成错误提示
                                    }
                                }
                            }
                        }
                    }
                    //标注顶层雨水管标注
                    for (int i = 0; i < copyrooftags.Count; i += 3)
                    {
                        Line bucket = copyrooftags[i] as Line;
                        Point3d center = Point3d.Origin;
                        Point3d center1 = Point3d.Origin;
                        int num = 0;
                        if (baseCenter2.Count > 0)
                        {
                            var offset = Matrix3d.Displacement(baseCenter2[0].GetVectorTo(baseCenter1[0]));
                            center = bucket.StartPoint + baseCenter2[0].GetVectorTo(baseCenter1[0]);
                            int s = 0;
                            foreach (var gravitybucket in gravityWaterBucket1)
                            {
                                if (gravitybucket.Position.DistanceTo(center) < 2)
                                {
                                    s = 1;
                                    break;
                                }
                            }                           
                            Circle alert = new Circle() { Center = center, Radius = 100 };
                            Polyline alertresult = alert.Tessellate(100);
                            foreach (Point3d bucket_1 in waterbuckets2)
                            {
                                if (Checkbucket(center, bucket_1, r_boundary))
                                {
                                    ++s;
                                    break;
                                }
                            }
                            if (s == 0)
                            {
                                acadDatabase.ModelSpace.Add(copyrooftags[i].GetTransformedCopy(offset));//管井复制到屋顶层  
                                acadDatabase.ModelSpace.Add(copyrooftags[i + 1].GetTransformedCopy(offset));
                                acadDatabase.ModelSpace.Add(copyrooftags[i + 2].GetTransformedCopy(offset));
                            }
                            if (s == 0)
                            {
                                acadDatabase.ModelSpace.Add(alertresult);//生成错误提示    
                            }
                            if (baseCenter0.Count > 0)
                            {
                                var offset1 = Matrix3d.Displacement(baseCenter2[0].GetVectorTo(baseCenter0[0]));
                                foreach (var bound in FloorEngines.RoofDeviceFloors[0].SubSpaces)
                                {
                                    Polyline boundary = bound.Boundary as Polyline;
                                    if (GeomUtils.PtInLoop(boundary, bucket.StartPoint + baseCenter2[0].GetVectorTo(baseCenter0[0])))
                                    {
                                        num = 1;
                                        break;
                                    }
                                }
                                if (num == 1)
                                {
                                    center1 = bucket.StartPoint + baseCenter2[0].GetVectorTo(baseCenter0[0]);
                                    int s1 = 0;
                                    foreach (var gravitybucket in gravityWaterBucket)
                                    {
                                        if (gravitybucket.Position.DistanceTo(center1) < 2)
                                        {
                                            s1 = 1;
                                            break;
                                        }
                                    }                                  
                                    Circle alert1 = new Circle() { Center = center1, Radius = 100 };
                                    Polyline alertresult1 = alert1.Tessellate(100);
                                    foreach (Point3d bucket_1 in waterbuckets1)
                                    {
                                        if (Checkbucket(center1, bucket_1, d_boundary))
                                        {
                                            ++s1;
                                            break;
                                        }
                                    }
                                    if (s1 == 0)
                                    {
                                        acadDatabase.ModelSpace.Add(copyrooftags[i].GetTransformedCopy(offset1));//管井复制到屋顶设备层 
                                        acadDatabase.ModelSpace.Add(copyrooftags[i + 1].GetTransformedCopy(offset1));
                                        acadDatabase.ModelSpace.Add(copyrooftags[i + 2].GetTransformedCopy(offset1));
                                    }
                                    if (s1 == 0)
                                    {
                                        acadDatabase.ModelSpace.Add(alertresult1);//生成错误提示
                                    }
                                }
                            }
                        }
                    }

                    if (FloorEngines.NormalFloors.Count>0)//复制所有管井到标准层
                    {
                        for (int i=0; i< FloorEngines.NormalFloors.Count;i++)
                        {
                            var offset = Matrix3d.Displacement(baseCenter2[0].GetVectorTo(FloorEngines.NormalFloors[0].BaseCircles[i+1].Boundary.GetCenter()));
                            foreach (var ent in normalCopys)
                            {
                                acadDatabase.ModelSpace.Add(ent.GetTransformedCopy(offset));
                            }
                        }
                    }
                }
            }
        }
        private bool Checkbucket(Point3d pipe, Point3d bucket, Polyline wboundary)
        {

            if (pipe.DistanceTo(bucket) < 10)
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
