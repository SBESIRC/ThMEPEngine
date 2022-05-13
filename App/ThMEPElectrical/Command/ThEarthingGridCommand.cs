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

using ThMEPElectrical.EarthingGrid.Generator.Utils;
using ThCADCore.NTS;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Runtime;
using Dreambuild.AutoCAD;
using ThMEPEngineCore.Command;
using System.Linq;

namespace ThMEPElectrical.Command
{
    public class ThEarthingGridCommand : ThMEPBaseCommand, IDisposable
    {
        public ThEarthingGridCommand()
        {
            CommandName = "THJDPM";
            ActionName = "防雷接地网";
        }
        public void Dispose()
        {            
        }
        public override void SubExecute()
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

                //List<Tuple<double, double>> faceSize = new List<Tuple<double, double>> { //测试分割用
                //    new Tuple<double, double>(5000, 5000)};
                var faceSize = new List<Tuple<double, double>>();
                var size = ThEarthingGridDataService.Instance.EarthingGridSize;
                if (size == "10x10或12x8或20x5")
                {
                    faceSize = new List<Tuple<double, double>> {
                        new Tuple<double, double>(10000, 10000),
                        new Tuple<double, double>(12000, 8000),
                        new Tuple<double, double>(20000, 5000)
                    };
                }
                else if (size == "20x20或24x16或40x10")
                {
                    faceSize = new List<Tuple<double, double>> {
                        new Tuple<double, double>(20000, 20000),
                        new Tuple<double, double>(24000, 16000),
                        new Tuple<double, double>(40000, 10000)
                    };
                }
                //1、Extract data
                var dataset = new ThEarthingGridDatasetFactory();
                dataset.Create(acadDb.Database, pts);

                //2、Process Data
                var preProcess = new PreProcess(dataset);
                preProcess.Process();

                bool beMerge = true;
                //3、Generate
                var earthGridLines = GridGenerator.Genterate(preProcess, faceSize, beMerge);

                //4、Display
                string layerName = "E-GRND-WIRE";
                //string layerName = LayerDealer.GetLayer();
                LayerDealer.AddLayer(layerName, 4);
                LayerDealer.Output(earthGridLines, layerName);
            }
        }

        private DBObjectCollection SelectFloorFrames()
        {
            using (var acadDb = AcadDatabase.Active())
            {
                var results = new DBObjectCollection();
                // 获取框线
                var options = new PromptSelectionOptions()
                {
                    AllowDuplicates = false,
                    MessageForAdding = "请选择楼层框定",
                    RejectObjectsOnLockedLayers = true,
                };
                var dxfNames = new string[]
                {
                    RXClass.GetClass(typeof(BlockReference)).DxfName,
                };
                var filter = ThSelectionFilterTool.Build(dxfNames);
                var result = Active.Editor.GetSelection(options, filter);
                if (result.Status != PromptStatus.OK)
                {
                    return results;
                }
                else
                {
                    result.Value
                        .GetObjectIds()
                        .Select(o => acadDb.ElementOrDefault<BlockReference>(o))
                        .Where(o => o.GetEffectiveName().ToUpper() == "AI-楼层框定E")
                        .ForEach(o => results.Add(o));
                    return results;
                }
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
