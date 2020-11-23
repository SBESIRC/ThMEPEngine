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
                    toiletfloorEngines.Run(tfloordrain, boundary1);
                    foreach (Point3d pt in compositeEngine.KitchenPipes)
                    {
                        acadDatabase.ModelSpace.Add(new DBPoint(pt));
                        acadDatabase.ModelSpace.Add(new Circle() { Radius = 50, Center = pt });
                        DBText taggingtext = new DBText()
                        {
                            Height = 20,
                            Position = pt,
                            TextString = kitchenEngines.Parameters.Identifier,
                        };
                        acadDatabase.ModelSpace.Add(taggingtext);
                    }
                    if (compositeEngine.ToiletPipes.Count > 0)
                    {
                        if (compositeEngine.ToiletPipes[0].IsEqualTo(compositeEngine.KitchenPipes[compositeEngine.KitchenPipes.Count - 1]))
                        {
                            for (int i = 0; i < compositeEngine.ToiletPipes.Count; i++)
                            {
                                var toilet = compositeEngine.ToiletPipes[i] + 200 * compositeEngine.ToiletPipes[0].GetVectorTo(compositeEngine.ToiletPipes[1]).GetNormal();
                                var radius = compositeEngine.ToiletPipeEngine.Parameters.Diameter[i] / 2.0;
                                acadDatabase.ModelSpace.Add(new DBPoint(toilet));
                                acadDatabase.ModelSpace.Add(new Circle() { Radius = radius, Center = toilet });
                                DBText taggingtext = new DBText()
                                {
                                    Height = 20,
                                    Position = toilet,
                                    TextString = toiletEngines.Parameters.Identifier[i],
                                };
                                acadDatabase.ModelSpace.Add(taggingtext);
                            }
                        }
                        else
                        {
                            for (int i = 0; i < compositeEngine.ToiletPipes.Count; i++)
                            {
                                var toilet = compositeEngine.ToiletPipes[i];
                                var radius = compositeEngine.ToiletPipeEngine.Parameters.Diameter[i] / 2.0;
                                acadDatabase.ModelSpace.Add(new DBPoint(toilet));
                                acadDatabase.ModelSpace.Add(new Circle() { Radius = radius, Center = toilet });
                                DBText taggingtext = new DBText()
                                {
                                    Height = 20,
                                    Position = toilet,
                                    TextString = toiletEngines.Parameters.Identifier[i],
                                };
                                acadDatabase.ModelSpace.Add(taggingtext);
                            }
                        }
                    }                
                }
                foreach (var compositeBalcony in compositeEngines.FloorDrainRooms)
                {

                    Polyline tboundary = null;
                    Polyline bboundary = null;
                    Polyline rainpipe = null;
                    Polyline downspout = null;
                    BlockReference washingmachine = null;
                    Polyline device = null;
                    Polyline condensepipe = null;
                    Polyline device_other = null;
                    BlockReference floordrain = null;
                    var bfloordrain = new List<BlockReference>();
                    var devicefloordrain = new List<BlockReference>();
                    foreach (var FloorDrain in compositeBalcony.Balcony.FloorDrains)
                    {
                        floordrain = FloorDrain.Outline as BlockReference;
                        bfloordrain.Add(floordrain);
                    }

                    bboundary = compositeBalcony.Balcony.Balcony.Boundary as Polyline;
                    if (compositeBalcony.Balcony.RainPipes.Count > 0)
                    {
                        rainpipe = compositeBalcony.Balcony.RainPipes[0].Outline as Polyline;
                    }
                    else
                    {
                        foreach (var devicePlatform in compositeBalcony.DevicePlatforms)
                        {
                            if (devicePlatform.RainPipes.Count > 0)
                            {
                                rainpipe = devicePlatform.RainPipes[0].Outline as Polyline;
                                break;
                            }
                        }
                    }
                    washingmachine = compositeBalcony.Balcony.Washmachines[0].Outline as BlockReference;
                    device = compositeBalcony.DevicePlatforms[0].DevicePlatform[0].Boundary as Polyline;
                    device_other = compositeBalcony.DevicePlatforms[1].DevicePlatform[0].Boundary as Polyline;
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
                        BlockReference devicefloordrains = null;
                        if (devicePlatform.FloorDrains.Count > 0)
                        {

                            devicefloordrains = devicePlatform.FloorDrains[0].Outline as BlockReference;

                        }
                        devicefloordrain.Add(devicefloordrains);
                    }

                    var thWBalconyFloordrainEngine = new ThWBalconyFloordrainEngine();
                    var thWToiletFloordrainEngine = new ThWToiletFloordrainEngine();
                    var thWDeviceFloordrainEngine = new ThWDeviceFloordrainEngine();
                    var FloordrainEngine = new ThWCompositeFloordrainEngine(thWBalconyFloordrainEngine, thWToiletFloordrainEngine, thWDeviceFloordrainEngine);
                    FloordrainEngine.Run(bfloordrain, bboundary, rainpipe, downspout, washingmachine, device, device_other, condensepipe, tfloordrain, tboundary, devicefloordrain);
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
                    //阳台输出完毕
                    for (int i = 0; i < FloordrainEngine.Devicefloordrain.Count; i++)
                    {
                        Matrix3d scale = Matrix3d.Scaling(2.0, FloordrainEngine.Devicefloordrain[i]);
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
                    //设备平台输出完毕
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
    }
}
