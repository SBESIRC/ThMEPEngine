using AcHelper;
using Linq2Acad;
using ThMEPWSS.Pipe;
using ThMEPWSS.Pipe.Model;
using ThMEPWSS.Pipe.Engine;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Geometry;
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
                foreach (var composite in compositeEngines.Rooms)
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
                    }
                    if (IsValidToiletContainer(composite.Toilet))
                    {
                        boundary1 = composite.Toilet.Toilet.Boundary as Polyline;
                        outline1 = composite.Toilet.DrainageWells[0].Boundary as Polyline;
                        closestool = composite.Toilet.Closestools[0].Outline as Polyline;
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
