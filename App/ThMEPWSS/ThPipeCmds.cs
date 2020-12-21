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
                Polyline boundary = null;
                Polyline outline = null;
                BlockReference basinline = null;
                Polyline pype = null;
                Polyline boundary1 = null;
                Polyline outline1 = null;
                Polyline closestool = null;              
                var tfloordrain = new List<BlockReference>();
                foreach (var composite in compositeEngines.Rooms)
                {
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
                    {
                        foreach(Entity item in kitchenPipe.Representation)
                        {
                            acadDatabase.ModelSpace.Add(item.GetTransformedCopy(kitchenPipe.Matrix));
                        }
                    }
                    if (compositeEngine.ToiletPipes.Count > 0)
                    {
                        if (compositeEngine.ToiletPipes[0].Equals(compositeEngine.KitchenPipes[compositeEngine.KitchenPipes.Count - 1]))
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
                    for (int i = 0; i < tfloordrain.Count; i++)
                    {
                        Matrix3d scale = Matrix3d.Scaling(2.0, tfloordrain[i].Position);
                        var ent = tfloordrain[i].GetTransformedCopy(scale);
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
                        else
                        {
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
                            Active.Editor.WriteMessage("\n 缺雨水管");
                        }
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
                        FloordrainEngine.Run(bfloordrain, bboundary, rainpipe, downspout, washingmachine, device, device_other, condensepipe, tfloordrain, tboundary, devicefloordrain, roofrainpipe, bbasinline);
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
            return kitchenContainer.Kitchen != null &&
                kitchenContainer.DrainageWells.Count == 1;
        }
        private bool IsValidToiletContainerForFloorDrain(ThWToiletRoom toiletContainer)
        {
            return toiletContainer.Toilet != null && 
                toiletContainer.FloorDrains.Count > 0;
        }
        private bool IsValidBalconyForFloorDrain(ThWBalconyRoom balconyContainer)
        {
            return balconyContainer.FloorDrains.Count > 0;
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

        [CommandMethod("TIANHUACAD", "THROOFFLOOR", CommandFlags.Modal)]
        public void Throoffloor()
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
                //第二类屋顶层布置
                Polyline r_boundary = null;
                var gravityWaterBucket1 = new List<BlockReference>();
                var sideWaterBucket1 = new List<BlockReference>();
                var roofRainPipe1 = new List<Polyline>();
                var engine1 = new ThWWaterBucketEngine();
                var baseCenter1 = new Point3dCollection();
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
                        foreach (var circle in composite.BaseCircles)
                        {
                            Polyline block = null;
                            block = circle.Boundary as Polyline;
                            baseCircles.Add(block);
                        }

                        engine.Run(gravityWaterBucket1, sideWaterBucket1, roofRainPipe1, r_boundary);
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
                Polyline boundary = null;
                Polyline outline = null;
                BlockReference basinline = null;
                Polyline pype = null;
                Polyline boundary1 = null;
                Polyline outline1 = null;
                Polyline closestool = null;
                var tfloordrain = new List<BlockReference>();
                var baseCenter2 = new Point3dCollection();
                var basecircle2 = FloorEngines.TopFloors[0].BaseCircles[0].Boundary.GetCenter();
                baseCenter2.Add(basecircle2);
                List<Entity> copypipes = new List<Entity>();//要复制的特征
                List<Entity> copyroofpipes = new List<Entity>();//要复制的屋顶雨水管
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
                        if (IsValidToiletContainerForFloorDrain(composite.Toilet))
                        {
                            foreach (var FloorDrain in composite.Toilet.FloorDrains)
                            {
                                floordrain = FloorDrain.Outline as BlockReference;
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
                        if (compositeEngine.ToiletPipes.Count > 0)
                        {
                            if (compositeEngine.ToiletPipes[0].Equals(compositeEngine.KitchenPipes[compositeEngine.KitchenPipes.Count - 1]))
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
                        for (int i = 0; i < tfloordrain.Count; i++)
                        {
                            Matrix3d scale = Matrix3d.Scaling(2.0, tfloordrain[i].Position);
                            var ent = tfloordrain[i].GetTransformedCopy(scale);
                            acadDatabase.ModelSpace.Add(ent);
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
                            else
                            {
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
                                Active.Editor.WriteMessage("\n 缺雨水管");
                            }                           
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
                                    npipe.Add(condensepipe);
                                    break;
                                }
                            }
                            
                            foreach (var devicePlatform in compositeBalcony.DevicePlatforms)
                            {
                                if (devicePlatform.RoofRainPipes.Count > 0)
                                {
                                    roofrainpipe = devicePlatform.RoofRainPipes[0].Outline as Polyline;
                                    roofrain_pipe.Add(roofrainpipe);
                                    copyroofpipes.Add(new Circle() { Center=roofrainpipe.GetCenter(), Radius=38.5 });
                                    copypipes.Add(new Circle() { Center = roofrainpipe.GetCenter(), Radius = 55.0 });
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
                            FloordrainEngine.Run(bfloordrain, bboundary, rainpipe, downspout, washingmachine, device, device_other, condensepipe, tfloordrain, tboundary, devicefloordrain, roofrainpipe, bbasinline);
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
                                ent_line1.Color = Autodesk.AutoCAD.Colors.Color.FromColorIndex(Autodesk.AutoCAD.Colors.ColorMethod.ByLayer, 256);
                                acadDatabase.ModelSpace.Add(ent_line1);
                                normalCopys.Add(ent_line1);
                            }
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
                                    ent_line1.Color = Autodesk.AutoCAD.Colors.Color.FromColorIndex(Autodesk.AutoCAD.Colors.ColorMethod.ByLayer, 256);
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
                                    ent_line1.Color = Autodesk.AutoCAD.Colors.Color.FromColorIndex(Autodesk.AutoCAD.Colors.ColorMethod.ByLayer, 256);
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
                            for (int i = 0; i < FloordrainEngine.Condensepipe_tofloordrain.Count - 1; i++)
                            {
                                Polyline ent_line1 = new Polyline();
                                ent_line1.AddVertexAt(0, FloordrainEngine.Condensepipe_tofloordrain[i].ToPoint2d(), 0, 35, 35);
                                ent_line1.AddVertexAt(1, FloordrainEngine.Condensepipe_tofloordrain[i + 1].ToPoint2d(), 0, 35, 35);
                                //ent_line1.Linetype = "DASHDOT";
                                //ent_line1.Layer = "W-RAIN-PIPE";
                                ent_line1.Color = Autodesk.AutoCAD.Colors.Color.FromColorIndex(Autodesk.AutoCAD.Colors.ColorMethod.ByLayer, 256);
                                acadDatabase.ModelSpace.Add(ent_line1);
                                normalCopys.Add(ent_line1);
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
                                    //ent_line1.Linetype = "DASHDOT";
                                    //ent_line1.Layer = "W-RAIN-PIPE";
                                    ent_line1.Color = Autodesk.AutoCAD.Colors.Color.FromColorIndex(Autodesk.AutoCAD.Colors.ColorMethod.ByLayer, 256);
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
                var rain_pipes = new List<Polyline>();//雨水管去重复
                if (rain_pipe.Count>0)
                {
                    rain_pipes.Add(rain_pipe[0]);
                    for (int i= 1; i< rain_pipe.Count; i++)
                    {
                        for (int j = i - 1; j >=0; j--)
                        {
                            if (rain_pipe[j].GetCenter() == rain_pipe[i].GetCenter())
                            {
                                break;
                            }
                            else
                            {
                                if(j>0)
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
                foreach(var rainp in rain_pipes)
                {
                    copypipes.Add(new Circle() { Center = rainp.GetCenter(), Radius = 37.5 });
                    copypipes.Add(new Line(new Point3d(rainp.GetCenter().X - 37.5, rainp.GetCenter().Y, 0), new Point3d(rainp.GetCenter().X+37.5, rainp.GetCenter().Y, 0)));
                    copypipes.Add(new Line(new Point3d(rainp.GetCenter().X, rainp.GetCenter().Y-37.5, 0), new Point3d(rainp.GetCenter().X, rainp.GetCenter().Y+37.5, 0)));
                }
                composite_Engine.Run(fpipe, tpipe, wpipe, ppipe, dpipe, npipe, rain_pipes, pboundary, divideLines, roofrain_pipe);
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
                        ent_line.Color = Autodesk.AutoCAD.Colors.Color.FromColorIndex(Autodesk.AutoCAD.Colors.ColorMethod.ByLayer, 256);
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
                            TextString = $"FL{j}-{i + 1}"//原来为{floor.Value}                        
                        };
                        DBText taggingtext1 = new DBText()
                        {
                            Height = 175,
                            Position = tag3,
                            TextString = $"FL-{i + 1}"//原来为{floor.Value}                        
                        };
                        if (j > 0)
                        {
                            acadDatabase.ModelSpace.Add(taggingtext);
                            copypipes.Add(taggingtext);
                            normalCopys.Add(taggingtext);
                        }
                        else
                        {
                            acadDatabase.ModelSpace.Add(taggingtext1);
                            copypipes.Add(taggingtext1);
                            normalCopys.Add(taggingtext1);
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
                        ent_line.Color = Autodesk.AutoCAD.Colors.Color.FromColorIndex(Autodesk.AutoCAD.Colors.ColorMethod.ByLayer, 256);
                        acadDatabase.ModelSpace.Add(ent_line);
                        normalCopys.Add(ent_line);
                        acadDatabase.ModelSpace.Add(ent_line1);
                        normalCopys.Add(ent_line1);
                        DBText taggingtext = new DBText()
                        {
                            Height = 175,
                            Position = tag3,
                            TextString = $"TL{j}-{i + 1}",

                        };
                        DBText taggingtext1 = new DBText()
                        {
                            Height = 175,
                            Position = tag3,
                            TextString = $"TL-{i + 1}",

                        };
                        if (j > 0)
                        {
                            acadDatabase.ModelSpace.Add(taggingtext);
                            normalCopys.Add(taggingtext);
                        }
                        else
                        {
                            acadDatabase.ModelSpace.Add(taggingtext1);
                            normalCopys.Add(taggingtext1);
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
                        ent_line.Color = Autodesk.AutoCAD.Colors.Color.FromColorIndex(Autodesk.AutoCAD.Colors.ColorMethod.ByLayer, 256);
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
                            TextString = $"WL{j}-{i + 1}",

                        };
                        DBText taggingtext1 = new DBText()
                        {
                            Height = 175,
                            Position = tag3,
                            TextString = $"WL-{i + 1}",

                        };
                        if (j > 0)
                        {
                            acadDatabase.ModelSpace.Add(taggingtext);
                            copypipes.Add(taggingtext);
                            normalCopys.Add(taggingtext);
                        }
                        else
                        {
                            acadDatabase.ModelSpace.Add(taggingtext1);
                            copypipes.Add(taggingtext1);
                            normalCopys.Add(taggingtext1);
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
                        ent_line.Color = Autodesk.AutoCAD.Colors.Color.FromColorIndex(Autodesk.AutoCAD.Colors.ColorMethod.ByLayer, 256);
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
                            TextString = $"PL{j}-{i + 1}",

                        };
                        DBText taggingtext1 = new DBText()
                        {
                            Height = 175,
                            Position = tag3,
                            TextString = $"PL-{i + 1}",

                        };
                        if (j > 0)
                        {
                            acadDatabase.ModelSpace.Add(taggingtext);
                            copypipes.Add(taggingtext);
                            normalCopys.Add(taggingtext);
                        }
                        else
                        {
                            acadDatabase.ModelSpace.Add(taggingtext1);
                            copypipes.Add(taggingtext1);
                            normalCopys.Add(taggingtext1);
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
                        ent_line.Color = Autodesk.AutoCAD.Colors.Color.FromColorIndex(Autodesk.AutoCAD.Colors.ColorMethod.ByLayer, 256);
                        acadDatabase.ModelSpace.Add(ent_line);
                        normalCopys.Add(ent_line);
                        acadDatabase.ModelSpace.Add(ent_line1);
                        normalCopys.Add(ent_line1);
                        DBText taggingtext = new DBText()
                        {
                            Height = 175,
                            Position = tag3,
                            TextString = $"DL{j}-{i + 1}",

                        };
                        DBText taggingtext1 = new DBText()
                        {
                            Height = 175,
                            Position = tag3,
                            TextString = $"DL-{i + 1}",

                        };
                        if (j > 0)
                        {
                            acadDatabase.ModelSpace.Add(taggingtext);
                            normalCopys.Add(taggingtext);
                        }
                        else
                        {
                            acadDatabase.ModelSpace.Add(taggingtext1);
                            normalCopys.Add(taggingtext1);
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
                        Line ent_line = new Line(PipeindexEngine.Npipeindex[j][i], tag1);
                        Line ent_line1 = new Line(tag1, tag2);
                        //ent_line.Layer = "W-DRAI-NOTE";
                        ent_line.Color = Autodesk.AutoCAD.Colors.Color.FromColorIndex(Autodesk.AutoCAD.Colors.ColorMethod.ByLayer, 256);
                        acadDatabase.ModelSpace.Add(ent_line);
                        acadDatabase.ModelSpace.Add(ent_line1);
                        DBText taggingtext = new DBText()
                        {
                            Height = 175,
                            Position = tag3,
                            TextString = $"NL{j}-{i + 1}",
                        };
                        DBText taggingtext1 = new DBText()
                        {
                            Height = 175,
                            Position = tag3,
                            TextString = $"NL-{i + 1}",

                        };
                        if (j > 0)
                        {
                            acadDatabase.ModelSpace.Add(taggingtext);
                        }
                        else
                        {
                            acadDatabase.ModelSpace.Add(taggingtext1);
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
                        Line ent_line = new Line(PipeindexEngine.Rainpipeindex[j][i], tag1);
                        Line ent_line1 = new Line(tag1, tag2);
                        //ent_line.Layer = "W-DRAI-NOTE";
                        ent_line.Color = Autodesk.AutoCAD.Colors.Color.FromColorIndex(Autodesk.AutoCAD.Colors.ColorMethod.ByLayer, 256);
                        acadDatabase.ModelSpace.Add(ent_line);
                        copypipes.Add(ent_line);
                        acadDatabase.ModelSpace.Add(ent_line1);
                        copypipes.Add(ent_line1);
                        DBText taggingtext = new DBText()
                        {
                            Height = 175,
                            Position = tag3,
                            TextString = $"Y2L{j}-{i + 1}",
                        };
                        DBText taggingtext1 = new DBText()
                        {
                            Height = 175,
                            Position = tag3,
                            TextString = $"Y2L-{i + 1}",
                        };
                        if (j > 0)
                        {
                            acadDatabase.ModelSpace.Add(taggingtext);
                            copypipes.Add(taggingtext);
                        }
                        else
                        {
                            acadDatabase.ModelSpace.Add(taggingtext1);
                            copypipes.Add(taggingtext1);
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
                        Line ent_line = new Line(PipeindexEngine.RoofRainpipeindex[j][i], tag1);
                        Line ent_line1 = new Line(tag1, tag2);
                        //ent_line.Layer = "W-DRAI-NOTE";
                        ent_line.Color = Autodesk.AutoCAD.Colors.Color.FromColorIndex(Autodesk.AutoCAD.Colors.ColorMethod.ByLayer, 256);
                        acadDatabase.ModelSpace.Add(ent_line);
                        copypipes.Add(ent_line);
                        acadDatabase.ModelSpace.Add(ent_line1);
                        copypipes.Add(ent_line1);
                        DBText taggingtext = new DBText()
                        {
                            Height = 175,
                            Position = tag3,
                            TextString = $"Y1L{j}-{i + 1}",
                        };
                        DBText taggingtext1 = new DBText()
                        {
                            Height = 175,
                            Position = tag3,
                            TextString = $"Y1L-{i + 1}",
                        };
                        if (j > 0)
                        {
                            acadDatabase.ModelSpace.Add(taggingtext);
                            copypipes.Add(taggingtext);
                        }
                        else
                        {
                            acadDatabase.ModelSpace.Add(taggingtext1);
                            copypipes.Add(taggingtext1);
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
                         
                            if (baseCenter0.Count > 0)
                            {
                                var offset1 = Matrix3d.Displacement(baseCenter2[0].GetVectorTo(baseCenter0[0]));
                                acadDatabase.ModelSpace.Add(ent.GetTransformedCopy(offset1));//管井复制到屋顶设备层
                            }
                        }
                    }
                    foreach (Circle ent in copyroofpipes)
                    {   
                        Polyline bucket = ent.Tessellate(50);
                        if (baseCenter2.Count > 0)
                        {
                            var offset = Matrix3d.Displacement(baseCenter2[0].GetVectorTo(baseCenter1[0]));
                            acadDatabase.ModelSpace.Add(ent.GetTransformedCopy(offset));//管井复制到屋顶层
                            Point3d center = bucket.GetCenter() + baseCenter2[0].GetVectorTo(baseCenter1[0]);
                            foreach(var gravitybucket in gravityWaterBucket1)
                            {
                                if (gravitybucket.Position == center)
                                {
                                    break;
                                }
                            }
                            foreach (var bucket_1 in sideWaterBucket1)
                            {
                                if (!Checkbucket(center, bucket_1, r_boundary))
                                {
                                    
                                        Circle alert = new Circle() { Center = center, Radius = 100 };
                                        Polyline alertresult = alert.Tessellate(100);                                     
                                        acadDatabase.ModelSpace.Add(alertresult);//生成错误提示
                                                             
                                }
                            }
                            if (baseCenter0.Count > 0)
                            {
                                var offset1 = Matrix3d.Displacement(baseCenter2[0].GetVectorTo(baseCenter0[0]));
                                acadDatabase.ModelSpace.Add(ent.GetTransformedCopy(offset1));//管井复制到屋顶设备层
                                Point3d center1 = bucket.GetCenter() + baseCenter2[0].GetVectorTo(baseCenter0[0]);
                                foreach (var gravitybucket in gravityWaterBucket)
                                {
                                    if (gravitybucket.Position == center)
                                    {
                                        break;
                                    }
                                }
                                foreach (var bucket_1 in sideWaterBucket)
                                {
                                    if (!Checkbucket(center1, bucket_1, d_boundary))
                                    {
                                        Circle alert1 = new Circle() { Center = center1, Radius = 100 };
                                        Polyline alertresult1 = alert1.Tessellate(100);
                                        acadDatabase.ModelSpace.Add(alertresult1);//生成错误提示
                                    }
                                }
                            }
                        }
                    }    
                    if(FloorEngines.NormalFloors.Count>0)//复制所有管井到标准层
                    {
                        foreach (var normalfoor in FloorEngines.NormalFloors)
                        {
                            var offset = Matrix3d.Displacement(baseCenter2[0].GetVectorTo(normalfoor.BaseCircles[0].Boundary.GetCenter()));
                            foreach (var ent in normalCopys)
                            {
                                acadDatabase.ModelSpace.Add(ent.GetTransformedCopy(offset));
                            }
                        }
                    }
                }
            }
        }
        private bool Checkbucket(Point3d pipe, BlockReference bucket, Polyline wboundary)
        {

            if (bucket.Position.X < wboundary.GetCenter().X)
            {
                if ((bucket.Position.X - pipe.X == 160) && (-bucket.Position.Y + pipe.Y == 115))
                {
                    return true;
                }
            }
            else
            {
                if ((-bucket.Position.X + pipe.X == 160) && (-bucket.Position.Y + pipe.Y == 115))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
