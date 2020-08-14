using Autodesk.AutoCAD.Runtime;
using System.IO;
using System.Collections;
using System.Web.Script.Serialization;
using System;
using ThMEPEngineCore.xBIM;
using Xbim.Ifc4.ProductExtension;
using Xbim.Ifc4.SharedBldgElements;
using Xbim.IO;
using ThMEPEngineCore.Service;
using System.Collections.Generic;
using Linq2Acad;
using AcHelper;
using ThMEPEngineCore.Engine;
using ThMEPEngineCore.BeamInfo.Model;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

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
            using (ThBeamRecognitionEngine beamEngine = new ThBeamRecognitionEngine())
            {
                beamEngine.Recognize();
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
        [CommandMethod("TIANHUACAD", "TestCreateBeam", CommandFlags.Modal)]
        public void TestCreateBeam()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var curve1Res = Active.Editor.GetEntity("\nSelect first line");
                var curve2Res = Active.Editor.GetEntity("\nSelect second line");

                Line firstLine = acadDatabase.Element<Line>(curve1Res.ObjectId);
                Line secondLine = acadDatabase.Element<Line>(curve2Res.ObjectId);

                LineBeam lineBeam = new LineBeam(firstLine, secondLine);
                Line upLine = new Line(lineBeam.UpStartPoint, lineBeam.UpEndPoint);
                upLine.ColorIndex = 1;
                Line downLine = new Line(lineBeam.DownStartPoint, lineBeam.DownEndPoint);
                downLine.ColorIndex = 2;

                acadDatabase.ModelSpace.Add(upLine);
                acadDatabase.ModelSpace.Add(downLine);

                Circle startCircle = new Circle(lineBeam.StartPoint, Vector3d.ZAxis, 10.0);
                startCircle.ColorIndex = 3;
                Circle endCircle = new Circle(lineBeam.EndPoint, Vector3d.ZAxis, 10.0);
                endCircle.ColorIndex = 4;
                acadDatabase.ModelSpace.Add(startCircle);
                acadDatabase.ModelSpace.Add(endCircle);
            }
        }
    }
}
