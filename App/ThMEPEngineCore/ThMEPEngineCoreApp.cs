using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Web.Script.Serialization;
using Linq2Acad;
using AcHelper;
using Xbim.IO;
using Xbim.Ifc4.ProductExtension;
using Xbim.Ifc4.SharedBldgElements;
using Autodesk.AutoCAD.Runtime;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.xBIM;
using ThMEPEngineCore.Service;
using ThMEPEngineCore.Engine;
using ThMEPEngineCore.Model;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore
{
    public class ThMEPEngineCoreApp : IExtensionApplication
    {
        public void Initialize()
        {
            //
        }

        public void Terminate()
        {
            //
        }

        [CommandMethod("TIANHUACAD", "THEXTRACTMODEL", CommandFlags.Modal)]
        public void ThExtractModel()
        {
            JavaScriptSerializer _JavaScriptSerializer = new JavaScriptSerializer();
            var _JsonBlocks = ReadTxt(Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Blocks.json"));
            ArrayList _ListBlocks = _JavaScriptSerializer.Deserialize<ArrayList>(_JsonBlocks);


            var _JsonElementTypes = ReadTxt(Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "ElementTypes.json"));
            ArrayList _ListElementTypes = _JavaScriptSerializer.Deserialize<ArrayList>(_JsonElementTypes);
        }

        [CommandMethod("TIANHUACAD", "THCREATEWALL", CommandFlags.Modal)]
        public void ThCreateWall()
        {
            using (var model = ThModelExtension.CreateAndInitModel("HelloWall"))
            {
                if (model != null)
                {
                    IfcBuilding building = ThModelExtension.CreateBuilding(model, "Default Building");
                    IfcWallStandardCase wall = ThModelExtension.CreateWall(model, 4000, 300, 2400);

                    //if (wall != null) AddPropertiesToWall(model, wall);
                    using (var txn = model.BeginTransaction("Add Wall"))
                    {
                        building.AddElement(wall);
                        txn.Commit();
                    }

                    if (wall != null)
                    {
                        try
                        {
                            Console.WriteLine("Standard Wall successfully created....");
                            //write the Ifc File
                            model.SaveAs(Path.Combine(
                                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "HelloWallIfc4.ifc"), StorageType.Ifc);
                            Console.WriteLine("HelloWallIfc4.ifc has been successfully written");
                        }
                        catch (System.Exception e)
                        {
                            Console.WriteLine("Failed to save HelloWall.ifc");
                            Console.WriteLine(e.Message);
                        }
                    }
                }
                else
                {
                    Console.WriteLine("Failed to initialise the model");
                }
            }
        }

        private string ReadTxt(string _Path)
        {
            try
            {
                using (StreamReader _StreamReader = File.OpenText(_Path))
                {
                    return _StreamReader.ReadToEnd();
                }
            }
            catch
            {
                return string.Empty;
            }
        }
        [CommandMethod("TIANHUACAD", "THExtractColumn", CommandFlags.Modal)]
        public void ThExtractColumn()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            using (var columnDbExtension = new ThStructureColumnDbExtension(Active.Database))
            {
                columnDbExtension.BuildElementCurves();
                columnDbExtension.ColumnCurves.ForEach(o => acadDatabase.ModelSpace.Add(o));
            }
        }
        [CommandMethod("TIANHUACAD", "THExtractBeam", CommandFlags.Modal)]
        public void ThExtractBeam()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            using (ThBeamRecognitionEngine beamEngine = new ThBeamRecognitionEngine())
            {
                beamEngine.Recognize(Active.Database);
                beamEngine.Elements.ForEach(o => acadDatabase.ModelSpace.Add(o.Outline));
            }
        }
        [CommandMethod("TIANHUACAD", "THExtractBeamText", CommandFlags.Modal)]
        public void ThExtractBeamText()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            using (var beamTextDbExtension = new ThStructureBeamTextDbExtension(Active.Database))
            {
                beamTextDbExtension.BuildElementTexts();
                beamTextDbExtension.BeamTexts.ForEach(o => acadDatabase.ModelSpace.Add(o));
            }
        }
        [CommandMethod("TIANHUACAD", "THExtractShearWall", CommandFlags.Modal)]
        public void THExtractShearWall()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            using (var shearWallDbExtension = new ThStructureShearWallDbExtension(Active.Database))
            {
                shearWallDbExtension.BuildElementCurves();
                shearWallDbExtension.ShearWallCurves.ForEach(o => acadDatabase.ModelSpace.Add(o));
            }
        }
        [CommandMethod("TIANHUACAD", "ThExtractBeamConnect", CommandFlags.Modal)]
        public void ThExtractBeamConnect()
        {
            List<ThBeamLink> totalBeamLinks = new List<ThBeamLink>();
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            using (var thBeamTypeRecogitionEngine = new ThBeamConnectRecogitionEngine())
            {
                System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
                stopwatch.Start();
                thBeamTypeRecogitionEngine.Recognize(Active.Database);
                stopwatch.Stop();
                TimeSpan timespan = stopwatch.Elapsed; //  获取当前实例测量得出的总时间
                Active.Editor.WriteMessage("\n本次使用了：" + timespan.TotalSeconds+"秒");
                thBeamTypeRecogitionEngine.PrimaryBeamLinks.ForEach(m => m.Beams.ForEach(n => n.Outline.ColorIndex=1));
                thBeamTypeRecogitionEngine.HalfPrimaryBeamLinks.ForEach(m => m.Beams.ForEach(n => n.Outline.ColorIndex = 2));
                thBeamTypeRecogitionEngine.OverhangingPrimaryBeamLinks.ForEach(m => m.Beams.ForEach(n => n.Outline.ColorIndex = 3));
                thBeamTypeRecogitionEngine.SecondaryBeamLinks.ForEach(m => m.Beams.ForEach(n => n.Outline.ColorIndex = 4));

                thBeamTypeRecogitionEngine.PrimaryBeamLinks.ForEach(m => m.Beams.ForEach(n=>acadDatabase.ModelSpace.Add(n.Outline)));
                thBeamTypeRecogitionEngine.HalfPrimaryBeamLinks.ForEach(m => m.Beams.ForEach(n => acadDatabase.ModelSpace.Add(n.Outline)));
                thBeamTypeRecogitionEngine.OverhangingPrimaryBeamLinks.ForEach(m => m.Beams.ForEach(n => acadDatabase.ModelSpace.Add(n.Outline)));
                thBeamTypeRecogitionEngine.SecondaryBeamLinks.ForEach(m => m.Beams.ForEach(n => acadDatabase.ModelSpace.Add(n.Outline)));

                thBeamTypeRecogitionEngine.PrimaryBeamLinks.ForEach(m => m.Beams.ForEach(n => acadDatabase.ModelSpace.Add(CreateBeamMarkText(n))));
                thBeamTypeRecogitionEngine.HalfPrimaryBeamLinks.ForEach(m => m.Beams.ForEach(n => acadDatabase.ModelSpace.Add(CreateBeamMarkText(n))));
                thBeamTypeRecogitionEngine.OverhangingPrimaryBeamLinks.ForEach(m => m.Beams.ForEach(n => acadDatabase.ModelSpace.Add(CreateBeamMarkText(n))));
                thBeamTypeRecogitionEngine.SecondaryBeamLinks.ForEach(m => m.Beams.ForEach(n => acadDatabase.ModelSpace.Add(CreateBeamMarkText(n))));
            }
        }
        private DBText CreateBeamMarkText(ThIfcBeam thIfcBeam)
        {
            string message = "";
            message += "Type：" + thIfcBeam.ComponentType + "，";
            message += "W：" + thIfcBeam.Width + "，";
            message += "H：" + thIfcBeam.Height;
            DBText dbText = new DBText();
            dbText.TextString = message;
            dbText.Position = ThGeometryTool.GetMidPt(thIfcBeam.StartPoint, thIfcBeam.EndPoint);
            dbText.HorizontalMode = TextHorizontalMode.TextCenter;
            dbText.Layer = "0";
            return dbText;
        }
        [CommandMethod("TIANHUACAD", "ThExtractBeamConnectEx", CommandFlags.Modal)]
        public void ThExtractBeamConnectEx()
        {
            List<ThBeamLink> totalBeamLinks = new List<ThBeamLink>();
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            using (var thBeamTypeRecogitionEngine = new ThBeamConnectRecogitionEngine())
            {
                thBeamTypeRecogitionEngine.Recognize(Active.Database);
                thBeamTypeRecogitionEngine.PrimaryBeamLinks.ForEach(m =>
                {
                   var outline = m.CreateExtendBeamOutline(50.0);
                    outline.Item1.ColorIndex = 1;
                    acadDatabase.ModelSpace.Add(outline.Item1);
                });
                thBeamTypeRecogitionEngine.HalfPrimaryBeamLinks.ForEach(m =>
                {
                    var outline = m.CreateExtendBeamOutline(50.0);
                    outline.Item1.ColorIndex = 2;
                    acadDatabase.ModelSpace.Add(outline.Item1);
                });
                thBeamTypeRecogitionEngine.OverhangingPrimaryBeamLinks.ForEach(m =>
                {
                    var outline = m.CreateExtendBeamOutline(50.0);
                    outline.Item1.ColorIndex = 3;
                    acadDatabase.ModelSpace.Add(outline.Item1);
                });
                thBeamTypeRecogitionEngine.SecondaryBeamLinks.ForEach(m =>
                {
                    var outline = m.CreateExtendBeamOutline(50.0);
                    outline.Item1.ColorIndex = 4;
                    acadDatabase.ModelSpace.Add(outline.Item1);
                });
            }
        }
        [CommandMethod("TIANHUACAD", "ThExtractDivideBeam", CommandFlags.Modal)]
        public void ThExtractDivdeBeam()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                ThIfcLineBeam thIfcLineBeam = new ThIfcLineBeam();
                thIfcLineBeam.StartPoint = Active.Editor.GetPoint("\n Select beam start point：").Value;
                thIfcLineBeam.EndPoint = Active.Editor.GetPoint("\n Select beam end point：").Value;
                thIfcLineBeam.Direction = thIfcLineBeam.StartPoint.GetVectorTo(thIfcLineBeam.EndPoint);
                thIfcLineBeam.Outline = acadDatabase.Element<Polyline>(Active.Editor.GetEntity("\n Select beam outline：").ObjectId);                
                thIfcLineBeam.ComponentType = BeamComponentType.PrimaryBeam;
                thIfcLineBeam.Width = 300;
                thIfcLineBeam.Height = 400;
                thIfcLineBeam.Uuid = Guid.NewGuid().ToString();
                var components = Active.Editor.GetSelection();
                DBObjectCollection dbObjs = new DBObjectCollection();
                foreach(ObjectId objId in components.Value.GetObjectIds())
                {
                    dbObjs.Add(acadDatabase.Element<Polyline>(objId));
                }
                ThSplitLinearBeamSevice thSplitLineBeam = new ThSplitLinearBeamSevice(thIfcLineBeam, dbObjs);
                thSplitLineBeam.Split();
                thSplitLineBeam.SplitBeams.ForEach(o => o.Outline.ColorIndex=1);
                thSplitLineBeam.SplitBeams.ForEach(o => acadDatabase.ModelSpace.Add(o.Outline));
            }
        } 
    }
}
