using System.IO;
using System.Linq;
using AcHelper;
using Linq2Acad;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.DatabaseServices;
using ThPlatform3D.ArchitecturePlane.Print;
using ThPlatform3D.Service;
using ThPlatform3D.Common;
using ThMEPEngineCore.IO.JSON;

namespace ThPlatform3D.Command
{
    public class ThGridDrawCmd : ThDrawBaseCmd
    {
        public ThGridDrawCmd()
        {
        }

        public override void Execute()
        {
            var pofo = new PromptOpenFileOptions("\n选择要成图的Ifc文件");
            pofo.Filter = "json files (*.json)|*.json";
            var pfnr = Active.Editor.GetFileNameForOpen(pofo);
            if (pfnr.Status == PromptStatus.OK)
            {
                // 从模板导入要打印的图层
                if (!ThImportDatabaseService.ImportArchDwgTemplate(Active.Database))
                {
                    return;
                }
                PrintGrids(Active.Database, pfnr.StringResult);
            }
        }

        private ObjectIdCollection PrintGrids(Database db,string gridDataFile)
        {
            using (var acadDb = AcadDatabase.Use(db))
            {
                var gridIds = new ObjectIdCollection();
                if (!File.Exists(gridDataFile))
                {
                    return gridIds;
                }
                var gridSystemData = Deserialize(gridDataFile);
                var builder = new ThGridSystemBuilder(gridSystemData);
                builder.Build();

                builder.GridLines.OfType<Curve>().ForEach(c =>
                {
                    var objIds = ThGridPrinter.Print(acadDb, c, ThGridPrinter.GridLineConfig);
                    gridIds.AddRange(objIds);
                });

                builder.DimensionGroups.ForEach(o =>
                {
                    o.OfType<AlignedDimension>().ForEach(a =>
                    {
                        var objIds = ThGridPrinter.Print(acadDb, a, ThGridPrinter.DimensionConfig);
                        gridIds.AddRange(objIds);
                    });
                });

                builder.CircleLabelGroups.ForEach(o =>
                {
                    o.ForEach(a =>
                    {
                        a.OfType<Entity>().ForEach(e =>
                        {
                            if (e is Line || e is Polyline)
                            {
                                var objIds = ThGridPrinter.Print(acadDb, e as Curve, ThGridPrinter.CircleLabelLeaderConfig);
                                gridIds.AddRange(objIds);
                            }
                            else if (e is Circle circle)
                            {
                                var objIds = ThGridPrinter.Print(acadDb, circle, ThGridPrinter.CircleLabelCircleConfig);
                                gridIds.AddRange(objIds);
                            }
                            else if (e is DBText text)
                            {
                                var objIds = ThGridPrinter.Print(acadDb, text, ThGridPrinter.CircleLabelTextConfig);
                                gridIds.AddRange(objIds);
                            }
                        });
                    });
                });

                return gridIds;
            }
        }

        private ThGridLineSyetemData Deserialize(string gridFile)
        {
            var gridData = new ThGridLineSyetemData();
            try
            {
                var jsonString = File.ReadAllText(gridFile);
                gridData = JsonHelper.DeserializeJsonToObject<ThGridLineSyetemData>(jsonString);
            }
            catch
            {
                //
            }
            return gridData;
        }
    }
}
