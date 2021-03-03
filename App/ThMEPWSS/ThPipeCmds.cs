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
using ThMEPWSS.Pipe.Layout;
using ThMEPWSS.Pipe.Output;
using ThMEPWSS.Pipe.Tools;
using ThMEPWSS.Pipe.Service;
using NetTopologySuite.Geometries;
using DotNetARX;
using System;

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
                        boundary = composite.Kitchen.Space.Boundary as Polyline;
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
                        boundary1 = composite.Toilet.Space.Boundary as Polyline;
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
                        Parameters = new ThWKitchenPipeParameters(1, ThTagParametersService.KaTFpipe),
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
                                    var radius = compositeEngine.ToiletPipeEngine.Parameters.Identifier[i].Item2 / 2.0;
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
                                var radius = compositeEngine.ToiletPipeEngine.Parameters.Identifier[i].Item2 / 2.0;
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

                        bboundary = compositeBalcony.Balcony.Space.Boundary as Polyline;
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
                            device = compositeBalcony.DevicePlatforms[0].Space.Boundary as Polyline;
                        }
                        if (compositeBalcony.DevicePlatforms.Count > 1)
                        {
                            Polyline temp = compositeBalcony.DevicePlatforms[1].Space.Boundary as Polyline;
                            if (!(temp.Equals(device)))
                            {
                                device_other = compositeBalcony.DevicePlatforms[1].Space.Boundary as Polyline;
                            }
                            else
                            {
                                device_other = compositeBalcony.DevicePlatforms[2].Space.Boundary as Polyline;
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
            return toiletContainer.Space != null &&
                toiletContainer.DrainageWells.Count == 1 &&
                toiletContainer.Closestools.Count == 1 &&
                toiletContainer.FloorDrains.Count > 0;
        }
        private bool IsValidKitchenContainer(ThWKitchenRoom kitchenContainer)
        {
            return (kitchenContainer.Space != null && kitchenContainer.DrainageWells.Count == 1);
        }
        private bool IsValidToiletContainerForFloorDrain(ThWToiletRoom toiletContainer)
        {
            return toiletContainer.Space != null && 
                toiletContainer.FloorDrains.Count > 0;
        }
        private bool IsValidBalconyForFloorDrain(ThWBalconyRoom balconyContainer)
        {
            return balconyContainer.FloorDrains.Count > 0&& balconyContainer.Washmachines.Count>0;
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
                    boundary = composite.Space.Boundary as Polyline;
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
                    //engine.Run(gravityWaterBucket, sideWaterBucket, roofRainPipe, boundary);
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
        public class ThWRoofDeviceParameters
        {
            public Polyline d_boundary = null;
            public List<BlockReference> gravityWaterBucket = new List<BlockReference>();
            public List<BlockReference> sideWaterBucket = new List<BlockReference>();
            public List<Polyline> roofRainPipe = new List<Polyline>();
            public ThWWaterBucketEngine engine = new ThWWaterBucketEngine();
            public List<Polyline> baseCircles = new List<Polyline>();
            public Point3dCollection baseCenter0 = new Point3dCollection();
            public Point3dCollection waterbuckets1 = new Point3dCollection();
            public Point3dCollection waterbuckets2 = new Point3dCollection();
            public List<Entity> roofDeviceEntity = new List<Entity>();
        }
        public class ThWRoofParameters
        {
            public Polyline r_boundary = null;
            public List<BlockReference> gravityWaterBucket1 = new List<BlockReference>();
            public List<BlockReference> sideWaterBucket1 = new List<BlockReference>();
            public List<Polyline> roofRainPipe1 = new List<Polyline>();
            public ThWWaterBucketEngine engine1 = new ThWWaterBucketEngine();
            public Point3dCollection baseCenter1 = new Point3dCollection();
            public List<Polyline> roofRoofRainPipes = new List<Polyline>();
            public List<Entity> roofEntity=new List<Entity>();
        }
        public class ThWTopParameters
        {
            public List<BlockReference> tfloordrain = new List<BlockReference>();
            public Point3dCollection baseCenter2 = new Point3dCollection();
            public List<Entity> copypipes = new List<Entity>();//要复制的特征
            public List<Entity> copyroofpipes = new List<Entity>();//要复制的屋顶雨水管
            public List<Entity> copyrooftags = new List<Entity>();
            public List<Entity> normalCopys = new List<Entity>();//要复制到其他标准层的立管                                                         //标注变量
            public List<Polyline> fpipe = new List<Polyline>();
            public List<Polyline> tpipe = new List<Polyline>();
            public List<Polyline> wpipe = new List<Polyline>();
            public List<Polyline> ppipe = new List<Polyline>();
            public List<Polyline> dpipe = new List<Polyline>();
            public List<Polyline> npipe = new List<Polyline>();
            public List<Polyline> rain_pipe = new List<Polyline>();
            public Polyline pboundary = null;
            public List<Line> divideLines = new List<Line>();
            public List<Polyline> roofrain_pipe = new List<Polyline>();
            public List<Entity> standardEntity = new List<Entity>();
        }
        public class ThWTopCompositeParameters
        {
            public List<BlockReference> tfloordrain_ = new List<BlockReference>();
            public Polyline boundary = null;
            public Polyline outline = null;
            public BlockReference basinline = null;
            public Polyline pype = null;
            public Polyline boundary1 = null;
            public Polyline outline1 = null;
            public Polyline closestool = null;
            public BlockReference floordrain = null;
        }
        public class ThWTopBalconyParameters
        {
            public Polyline roofrainpipe = null;
            public Polyline tboundary = null;
            public Polyline bboundary = null;
            public Polyline downspout = null;
            public BlockReference washingmachine = null;
            public Polyline device = null;
            public Polyline condensepipe = null;
            public Polyline device_other = null;
            public BlockReference floordrain = null;
            public BlockReference bbasinline = null;
            public List<Polyline> condensepipes = new List<Polyline>();
            public List<BlockReference> bfloordrain = new List<BlockReference>();
            public List<BlockReference> devicefloordrain = new List<BlockReference>();
            public List<Polyline> rainpipe = new List<Polyline>();
        }
        public static Line CreateLine(Point3d point1, Point3d point2)
        {
            Line line = new Line(point1, point2);
            //ent_line.Color = Autodesk.AutoCAD.Colors.Color.FromColorIndex(Autodesk.AutoCAD.Colors.ColorMethod.ByLayer, 256);       
            return line;
        }
        public static List<Line> GetCreateLines(Point3dCollection points, Point3dCollection point1s,string W_RAIN_NOTE1)
        {
            var lines = new List<Line>();
            for (int i = 0; i < points.Count; i++)
            {
                Line s = CreateLine(points[i], point1s[4 * i]);
                s.Layer = W_RAIN_NOTE1;
                lines.Add(s);              
            }
            return lines;
        }
        public static List<Line> GetCreateLines1(Point3dCollection points, Point3dCollection point1s, string W_RAIN_NOTE1)
        {
            var lines = new List<Line>();
            for (int i = 0; i < points.Count; i++)
            {
                Line s = CreateLine(point1s[4 * i], point1s[4 * i + 1]);
                s.Layer = W_RAIN_NOTE1;
                lines.Add(s);
            }
            return lines;
        }
        public static Circle CreateCircle(Point3d point1)
        {
            return new Circle()
            {
                Radius = 50,
                Center = point1,
                Layer = ThWPipeCommon.W_RAIN_EQPM,
            };
        }                    
        public class InputInfo
        {                  
            public bool IsCaisson=false ;

            public bool IsSeparation=false ;

            public int FloorValue=0;

            public int ScaleFactor = 0;
            public string PipeLayer = null;
            public  void MakeInputInfo()
            {
                var inputInfo = new InputInfo();
                inputInfo.Do();
                IsCaisson= inputInfo.IsCaisson;
                IsSeparation = inputInfo.IsSeparation;
                FloorValue = inputInfo.FloorValue;
                ScaleFactor = inputInfo.ScaleFactor;
                PipeLayer= inputInfo.PipeLayer;
            }
            public void Do()
            {
                var parameter_floor = new PromptIntegerOptions("请输入楼层");
                var floorResult = Active.Editor.GetInteger(parameter_floor);
                if (floorResult.Status != PromptStatus.OK)
                {
                    return;
                }
                FloorValue = floorResult.Value;
                var separation_key = new PromptKeywordOptions("\n污废分流");
                separation_key.Keywords.Add("是", "Y", "是(Y)");
                separation_key.Keywords.Add("否", "N", "否(N)");
                separation_key.Keywords.Default = "否";
                var result = Active.Editor.GetKeywords(separation_key);
                if (result.Status != PromptStatus.OK)
                {
                    return;
                }
                IsSeparation = result.StringResult == "是";
                var caisson_key = new PromptKeywordOptions("\n沉箱");
                caisson_key.Keywords.Add("有", "Y", "有(Y)");
                caisson_key.Keywords.Add("没有", "N", "没有(N)");
                caisson_key.Keywords.Default = "没有";
                result = Active.Editor.GetKeywords(caisson_key);
                if (result.Status != PromptStatus.OK)
                {
                    return;
                }
                IsCaisson = result.StringResult == "有";
                var scale_key = new PromptKeywordOptions("\n选择比例系数");
                scale_key.Keywords.Add("1:50", "Y", "1:50(Y)");
                scale_key.Keywords.Add("1:100", "N", "1:100(N)");
                scale_key.Keywords.Add("1:150", "o", "1:150(o)");
                scale_key.Keywords.Default = "1:50";
                result = Active.Editor.GetKeywords(scale_key);
                if (result.Status != PromptStatus.OK)
                {
                    return;
                }
                else if(result.StringResult == "1:50")
                {
                    ScaleFactor = 1;
                }
                else if (result.StringResult == "1:100")
                {
                    ScaleFactor = 2;
                }
                else
                {
                    ScaleFactor = 3;
                }
                var pipeStyle_key = new PromptKeywordOptions("\n选择污废处理管样式");
                pipeStyle_key.Keywords.Add("污水管", "Y", "污水管(Y)");
                pipeStyle_key.Keywords.Add("废水管", "N", "废水管(N)");
                pipeStyle_key.Keywords.Add("污废合流管", "o", "污废合流管(o)");
                pipeStyle_key.Keywords.Default = "污水管";
                result = Active.Editor.GetKeywords(pipeStyle_key);
                if (result.Status != PromptStatus.OK)
                {
                    return;
                }
                else if (result.StringResult == "污水管")
                {
                    PipeLayer = "W-DRAI-SEWA-PIPE";
                }
                else if (result.StringResult == "废水管")
                {
                    PipeLayer = "W-DRAI-WAST-PIPE";
                }
                else
                {
                    PipeLayer = "W-DRAI-SEWA-PIPE";
                }
            }
        }
        public class InputObstacles
        {
            public List<Curve> ObstacleParameters = new List<Curve>();
            public void Recognize(ThWCompositeFloorRecognitionEngine FloorEngines)
            {
                var inputInfo = new InputObstacles();
                inputInfo.Do(FloorEngines);
                ObstacleParameters = inputInfo.ObstacleParameters;
            }
            public void Do(ThWCompositeFloorRecognitionEngine FloorEngines)
            {
                var obstacle_key = new PromptKeywordOptions("\n障碍物");
                obstacle_key.Keywords.Add("有", "Y", "有(Y)");
                obstacle_key.Keywords.Add("没有", "N", "没有(N)");
                obstacle_key.Keywords.Default = "没有";
                var result = Active.Editor.GetKeywords(obstacle_key);
                if (result.Status != PromptStatus.OK)
                {
                    return;
                }
                if (result.StringResult == "有")
                {
                    var obstacleParameters_key = new PromptKeywordOptions("\n障碍物选择");
                    obstacleParameters_key.Keywords.Add("全部", "Y", "全部(Y)");
                    obstacleParameters_key.Keywords.Add("非全部", "N", "非全部(N)");
                    result = Active.Editor.GetKeywords(obstacleParameters_key);
                    if (result.StringResult == "全部")
                    {
                        ObstacleParameters = GetObstacleParameters("全部障碍物",FloorEngines.AllObstacles);
                    }
                    else
                    {
                        GetObstacleParameters("空间名称",FloorEngines.TagNameFrames).ForEach(o=> ObstacleParameters.Add(o));
                        GetObstacleParameters("楼梯", FloorEngines.StairFrames).ForEach(o => ObstacleParameters.Add(o));
                        GetObstacleParameters("结构柱", FloorEngines.Columns).ForEach(o => ObstacleParameters.Add(o));
                        GetObstacleParameters("剪力墙", FloorEngines.ShearWalls).ForEach(o => ObstacleParameters.Add(o));
                        GetObstacleParameters("内门", FloorEngines.InnerDoors).ForEach(o => ObstacleParameters.Add(o));
                        GetObstacleParameters("设备", FloorEngines.Devices).ForEach(o => ObstacleParameters.Add(o));
                        GetObstacleParameters("建筑墙", FloorEngines.ArchitectureWalls).ForEach(o => ObstacleParameters.Add(o));
                        GetObstacleParameters("窗", FloorEngines.Windows).ForEach(o => ObstacleParameters.Add(o));
                        GetObstacleParameters("建筑标高", FloorEngines.ElevationFrames).ForEach(o => ObstacleParameters.Add(o));
                        GetObstacleParameters("轴向圆圈标注", FloorEngines.AxialCircleTags).ForEach(o => ObstacleParameters.Add(o));
                        GetObstacleParameters("轴向横线标注", FloorEngines.AxialAxisTags).ForEach(o => ObstacleParameters.Add(o));
                        GetObstacleParameters("外部尺寸标注", FloorEngines.ExternalTags).ForEach(o => ObstacleParameters.Add(o));
                        GetObstacleParameters("本图管井", FloorEngines.Wells).ForEach(o => ObstacleParameters.Add(o));
                        GetObstacleParameters("本图标注", FloorEngines.DimensionTags).ForEach(o => ObstacleParameters.Add(o));
                        GetObstacleParameters("本图水管", FloorEngines.RainPipes).ForEach(o => ObstacleParameters.Add(o));
                        GetObstacleParameters("本图定位尺寸", FloorEngines.PositionTags).ForEach(o => ObstacleParameters.Add(o));
                    }
                }
            }

        }
        public static List<Curve> GetObstacleParameters(string s,List<Curve> curves)
        {
            var obstacle_key = new PromptKeywordOptions(s);
            obstacle_key.Keywords.Add("有", "Y", "有(Y)");
            obstacle_key.Keywords.Add("没有", "N", "没有(N)");
            var result = Active.Editor.GetKeywords(obstacle_key);
            if(result.StringResult == "有")
            {
                return curves;
            }
            return new List<Curve>();
        }
      
        [CommandMethod("TIANHUACAD", "THPYS", CommandFlags.Modal)]
        public void ThPYS()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            using (var FloorEngines = new ThWCompositeFloorRecognitionEngine())
            {               
                var userInfo = new InputInfo();
                userInfo.MakeInputInfo();
                var obstacleInfo = new InputObstacles();
                obstacleInfo.Recognize(FloorEngines);
                if (userInfo.FloorValue == 0)
                    return;
                FloorEngines.Recognize(acadDatabase.Database, new Point3dCollection());
                string W_RAIN_NOTE1 = ThWPipeOutputFunction.Get_Layers1(FloorEngines.Layers, ThWPipeCommon.W_RAIN_NOTE);
                string W_DRAI_EQPM= ThWPipeOutputFunction.Get_Layers2(FloorEngines.Layers, ThWPipeCommon.W_DRAI_EQPM);
                string W_DRAI_FLDR = ThWPipeOutputFunction.Get_Layers3(FloorEngines.Layers, ThWPipeCommon.W_DRAI_FLDR);
                string W_RAIN_PIPE= ThWPipeOutputFunction.Get_Layers4(FloorEngines.Layers, ThWPipeCommon.W_RAIN_PIPE);
                //第一类屋顶设备层布置   
                var parameters2 = new ThWRoofDeviceParameters();
                if (FloorEngines.RoofDeviceFloors.Count > 0)//存在屋顶设备层
                {
                    ThWLayoutRoofDeviceFloorEngine.LayoutRoofDeviceFloor(FloorEngines, parameters2, acadDatabase, userInfo.ScaleFactor, W_RAIN_NOTE1);
                }
                //第二类屋顶层布置
                var parameters1 = new ThWRoofParameters();
                if (FloorEngines.RoofFloors.Count > 0)//存在屋顶层
                {
                    ThWRoofFloorOutPutEngine.LayoutRoofFloor(FloorEngines, parameters2, parameters1, acadDatabase, userInfo.ScaleFactor, W_RAIN_NOTE1);
                }
                ////第三类顶层布置            
               
                var basecircle2 = FloorEngines.TopFloors[0].BaseCircles[0].Boundary.GetCenter();
                var parameters0 = new ThWTopParameters();
                parameters0.baseCenter2.Add(basecircle2);
                if (FloorEngines.TopFloors.Count > 0) //存在顶层
                {
                    var layoutTopFloor = new ThWTopFloorOutPutEngine();
                    layoutTopFloor.LayoutTopFloor(FloorEngines, parameters0, acadDatabase, userInfo, W_DRAI_EQPM, W_DRAI_FLDR, W_RAIN_PIPE);
                }
                var PipeindexEngine = new ThWInnerPipeIndexEngine();
                var composite_Engine = new ThWCompositeIndexEngine(PipeindexEngine);
                //开始标注 
                var layoutTag = new ThWCompositeTagOutPutEngine();
                
                layoutTag.LayoutTag(FloorEngines, parameters0, parameters1, parameters2,acadDatabase, PipeindexEngine,composite_Engine, obstacleInfo.ObstacleParameters, userInfo.ScaleFactor, userInfo.PipeLayer, W_DRAI_EQPM, W_RAIN_NOTE1);               
            }
        }
        [CommandMethod("TIANHUACAD", "THSTOREYFRAME", CommandFlags.Modal)]
        public void THSTOREYFRAME()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                PromptPointOptions sf = new PromptPointOptions("\n 选择要插入的基点位置");              
                var tpipe =new List<Point3d>();
                for (int i = 0; i < 10; i++)
                {
                    var result = Active.Editor.GetPoint(sf);
                    if (result.Status == PromptStatus.OK)
                    {
                        tpipe.Add(result.Value);
                    }
                    else
                    {
                        break;
                    }
                }
                ThInsertStoreyFrameService.Insert(tpipe);
            }
        }
        [CommandMethod("TIANHUACAD", "THAPPLICATIONPIPE", CommandFlags.Modal)]
        public static void THAPPLICATIONPIPE()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {              
                if(!(GetBlockReferences(acadDatabase.Database, ThTagParametersService.sourceFloor).Count>0))
                {
                    PromptPointOptions sf = new PromptPointOptions("\n 来源楼层中没有立管图块");
                    return;
                }
                else
                {
                    var application = new ThTagParametersService();
                    application.Read();
                    ThApplicationPipesEngine.Application(ThTagParametersService.sourceFloor, ThTagParametersService.targetFloors);
                }
            }
        }
        private static  List<BlockReference> GetBlockReferences(Database db, string blockName)
        {
            List<BlockReference> blocks = new List<BlockReference>();
            var trans = db.TransactionManager;
            BlockTable bt = (BlockTable)trans.GetObject(db.BlockTableId, OpenMode.ForRead);                
            blocks = (from b in db.GetEntsInDatabase<BlockReference>()
                      where (b.GetBlockName().Contains(blockName)&& b.GetBlockName().Contains("标准层"))
                      select b).ToList();
            return blocks;
        }
    }
}
