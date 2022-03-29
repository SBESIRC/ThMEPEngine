using System;
using AcHelper;
using System.IO;
using Linq2Acad;
using ThCADExtension;
using AcHelper.Commands;
using ThMEPEngineCore.IO;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Algorithm;
using System.Collections.Generic;
using ThMEPEngineCore.GeojsonExtractor;
using ThMEPElectrical.GroundingGrid.Data;
using ThMEPEngineCore.GeojsonExtractor.Interface;
using ThMEPElectrical.EarthingGrid.Data;
using ThMEPElectrical.EarthingGrid.Service;
using System.Windows.Shapes;
using ThMEPElectrical.EarthingGrid.Generator.Connect;
using ThMEPElectrical.EarthingGrid.Generator.Data;
using Autodesk.AutoCAD.Geometry;

namespace ThMEPElectrical.Command
{
    public class ThEarthingGridCommand : IAcadCommand, IDisposable
    {
        public void Dispose()
        {
            
        }
        public void Execute()
        {
            using (var acadDb = AcadDatabase.Active())
            {
                var frame = ThWindowInteraction.GetPolyline(
                    PointCollector.Shape.Window, new List<string> { "请框选一个范围" });
                if (frame.Area < 1e-4)
                {
                    return;
                }
                var nFrame = ThMEPFrameService.Normalize(frame);
                var pts = nFrame.Vertices();

                List<Tuple<double, double>> faceSize = new List<Tuple<double, double>> {
                    new Tuple<double, double>(20000, 20000),
                    new Tuple<double, double>(24000, 16000),
                    new Tuple<double, double>(40000, 10000) 
                };
                //List<Tuple<double, double>> faceSize = new List<Tuple<double, double>> { //测试分割用
                //    new Tuple<double, double>(5000, 5000)};

                //1、Extract data
                var dataset = new ThEarthingGridDatasetFactory();
                dataset.Create(acadDb.Database, pts);

                //2、Process Data
                var preProcess = new PreProcess(dataset);
                preProcess.Process();

                //3、Generate
                GridGenerator.Genterate(preProcess, faceSize);
            }
        }
        //public void Execute()
        //{
        //    using (var acadDatabase = AcadDatabase.Active())
        //    {
        //        var frame = ThWindowInteraction.GetPolyline(
        //            PointCollector.Shape.Window, new List<string> { "请框选一个范围" });
        //        if (frame.Area < 1e-4)
        //        {
        //            return;
        //        }
        //        var nFrame = ThMEPFrameService.Normalize(frame);
        //        var pts = nFrame.Vertices();

        //        short colorIndex = 1;
        //        var storeyExtractor = new ThGroundStoreyExtractor()
        //        {
        //            ColorIndex = colorIndex++,
        //        };
        //        storeyExtractor.Extract(acadDatabase.Database, pts);

        //        var extractors = new List<ThExtractorBase>()
        //        {
        //            new ThGroundColumnExtractor()
        //            {
        //                UseDb3Engine=true,
        //                ColorIndex=colorIndex++,
        //            },
        //            new ThGroundShearwallExtractor()
        //            {
        //                UseDb3Engine=true,
        //                ColorIndex=colorIndex++,
        //            },
        //            new ThGroundWireExtractor()
        //            {
        //                ElementLayer ="E-GRND-WIRE",
        //                ColorIndex=colorIndex++,
        //            },
        //            new ThFloorOutlineExtractor()
        //            {
        //                ElementLayer="AI-底板轮廓",
        //                ColorIndex=colorIndex++,
        //            },
        //            new ThDownConductorExtractor()
        //            {
        //                ColorIndex=colorIndex++,
        //            },
        //            new ThEarthConductorExtractor()
        //            {
        //                ColorIndex=colorIndex++,
        //            }
        //        };
        //        extractors.ForEach(e => e.Extract(acadDatabase.Database, pts));
        //        extractors.ForEach(e =>
        //        {
        //            if (e is IGroup iGroup)
        //            {
        //                iGroup.Group(storeyExtractor.StoreyIds);
        //            }
        //        });
        //        extractors.ForEach(o => (o as IPrint).Print(acadDatabase.Database));
        //        extractors.Add(storeyExtractor);

        //        //输出Geojson File
        //        var geos = new List<ThGeometry>();
        //        extractors.ForEach(e => geos.AddRange(e.BuildGeometries()));
        //        var fileInfo = new FileInfo(Active.Document.Name);
        //        var path = fileInfo.Directory.FullName;
        //        ThGeoOutput.Output(geos, path, fileInfo.Name);
        //    }
        //}
    }
}
