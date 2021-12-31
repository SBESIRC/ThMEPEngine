using AcHelper;
using DotNetARX;
using Linq2Acad;
using Dreambuild.AutoCAD;
using GeometryExtensions;
using System.Collections.Generic;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThCADExtension;
using ThMEPEngineCore.IO;
using ThMEPArchitecture.ParkingStallArrangement;

namespace ThMEPArchitecture
{

    public partial class ThParkingStallArrangement
    {
        [CommandMethod("TIANHUACAD", "-THDXQYFG", CommandFlags.Modal)]
        public void ThArrangeParkingStall()
        {
            using (var cmd = new ThParkingStallArrangementCmd())
            {
                cmd.Execute();
            }
        }
        [CommandMethod("TIANHUACAD", "-THDXQYFG3", CommandFlags.Modal)]
        public void ThArrangeParkingStall3()
        {
            using (var cmd = new WithoutSegLineCmd())
            {
                cmd.Execute();
            }
        }
        [CommandMethod("TIANHUACAD", "-THExtractTestData", CommandFlags.Modal)]
        public void THExtractTestData()
        {
            using (var acadDatabase = AcadDatabase.Active())
            using (var pc = new PointCollector(PointCollector.Shape.Window, new List<string>()))
            {
                try
                {
                    pc.Collect();
                }
                catch
                {
                    return;
                }
                Point3dCollection winCorners = pc.CollectedPoints;
                var frame = new Polyline();
                frame.CreateRectangle(winCorners[0].ToPoint2d(), winCorners[1].ToPoint2d());
                frame.TransformBy(Active.Editor.UCS2WCS());

                var userDataSet = new ThMEPArchitecture.ParkingStallArrangement.Extractor.ThUserDatasetFactory();
                var dataSet = userDataSet.Create(acadDatabase.Database, frame.Vertices());

                var geoString = ThGeoOutput.Output(dataSet.Container);
                //ThGeoOutput.Output(dataSet.Container, "", "");
            }
        }
    }

    public partial class ThParkingStallArrangementByFixedLines
    {
        [CommandMethod("TIANHUACAD", "-THDXQYFG2", CommandFlags.Modal)]
        public void ThArrangeParkingStall2()
        {
            using (var cmd = new ThMEPArchitecture.ParkingStallArrangement.OneGenerationCmd())
            {
                cmd.Execute();
            }
        }
    }
}
