using System.IO;
using System.Linq;
using System.Collections.Generic;
using AcHelper;
using DotNetARX;
using Linq2Acad;
using Dreambuild.AutoCAD;
using GeometryExtensions;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.DatabaseServices;
using ThCADExtension;
using ThMEPEngineCore.IO;
using ThMEPArchitecture.ParkingStallArrangement;
using ThMEPArchitecture.ParkingStallArrangement.IO;
using ThMEPArchitecture.ParkingStallArrangement.Method;
using ThMEPArchitecture.ParkingStallArrangement.Extractor;

namespace ThMEPArchitecture
{
    public partial class ThParkingStallArrangement
    {
        [CommandMethod("TIANHUACAD", "-THZDCWYCL", CommandFlags.Modal)]
        public void ThParkingStallPreprocess()
        {
            using (var cmd = new ThParkingStallPreprocessCmd())
            {
                cmd.Execute();
            }
        }

        [CommandMethod("TIANHUACAD", "-THFGXDD", CommandFlags.Modal)]
        public void ThBreakSegLines()
        {
            using (var cmd = new ThBreakSegLinesCmd())
            {
                cmd.Execute();
            }
        }
        [CommandMethod("TIANHUACAD", "-THDXQYFG", CommandFlags.Modal)]
        public void ThArrangeParkingStall()
        {
            using (var cmd = new ThParkingStallArrangementCmd())
            {
                cmd.Execute();
            }
        }
        [CommandMethod("TIANHUACAD", "-THWFGXCWBZ", CommandFlags.Modal)]
        public void ThArrangeParkingStall3()
        {
            using (var cmd = new WithoutSegLineCmd())
            {
                cmd.Execute();
            }
        }

        [CommandMethod("TIANHUACAD", "-THDXFGXSC", CommandFlags.Modal)]
        public void CreateAllSeglinesCmd()
        {
            using (var cmd = new CreateAllSeglinesCmd())
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

        [CommandMethod("TIANHUACAD", "-THExtractTestDataForZheData", CommandFlags.Modal)]
        public void THExtractTestDataForZheData()
        {
            using (var acadDatabase = AcadDatabase.Active())
            {

                var outerBorder = new OuterBrder();
                InputData.GetOuterBrder(acadDatabase, out outerBorder);

                var dataSetFactory = new ThParkingStallDataSetFactory(outerBorder);
                var dataSet = dataSetFactory.Create(acadDatabase.Database, new Point3dCollection());

                var fileInfo = new FileInfo(Active.Document.Name);
                var path = fileInfo.Directory.FullName;
                ThGeoOutput.Output(dataSet.Container, path, fileInfo.Name);
            }
        }

        [CommandMethod("TIANHUACAD", "THBuildAreas", CommandFlags.Modal)]
        public void THBuildAreas()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var result = Active.Editor.GetSelection();
                if (result.Status != PromptStatus.OK)
                {
                    return;
                }

                var selected = new List<DBObject>();
                foreach (var id in result.Value.GetObjectIds())
                {
                    selected.Add(acadDatabase.Element<DBObject>(id));
                }

                var lines = selected.OfType<Line>().ToList();
                var pls = selected.OfType<Polyline>().ToList();
                for (int i = 0; i < 10000; i++)
                {
                    
                    var areas = lines.SplitArea(pls);
                    areas.ForEach(a => a.Dispose());
                }
                //Active.Editor.WriteLine(areas.Count);
            }
        }
    }

    public partial class ThParkingStallArrangementByFixedLines
    {
        [CommandMethod("TIANHUACAD", "-THDXQYFG2", CommandFlags.Modal)]
        public void ThArrangeParkingStall2()
        {
            using (var cmd = new ThMEPArchitecture.ParkingStallArrangement.GenerateParkingStallDirectlyCmd())
            {
                cmd.Execute();
            }
        }
    }
}
