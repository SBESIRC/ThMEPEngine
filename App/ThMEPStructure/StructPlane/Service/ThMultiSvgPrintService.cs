using System.Linq;
using System.Collections.Generic;
using AcHelper;
using DotNetARX;
using Linq2Acad;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using acadApp = Autodesk.AutoCAD.ApplicationServices;
using ThMEPEngineCore.IO.SVG;

namespace ThMEPStructure.StructPlane.Service
{
    internal class ThMultiSvgPrintService
    {
        private double LtScale = 500;
        private int Measurement = 0;
        private Database AcadDb { get; set; }
        private double FloorSpacing { get; set; } = 100000;
        private List<string> SvgFiles { get; set; }
        public ThMultiSvgPrintService(Database database,List<string> svgFiles)
        {
            AcadDb = database;
            SvgFiles = svgFiles;
        }
        public void Print()
        {
            if(SvgFiles.Count==0)
            {
                return;
            }
            // 从模板导入要打印的图层
            ThStructPlaneUtils.Import(AcadDb);
            // 设置系统变量
            SetSysVariables();

            var printers = PrintToCad();
            Layout(printers.Select(o=>o.ObjIds).ToList());
            InsertBasePoint();
        }

        private void Layout(List<ObjectIdCollection> floorObjIds)
        {
            using (var acadDb = AcadDatabase.Use(AcadDb))
            {
                for (int i = 0; i < floorObjIds.Count; i++)
                {
                    if (i == 0)
                    {
                        continue;
                    }
                    var dir = new Vector3d(0, i * FloorSpacing, 0);
                    var mt = Matrix3d.Displacement(dir);
                    floorObjIds[i].OfType<ObjectId>().ForEach(o =>
                    {
                        var entity = acadDb.Element<Entity>(o, true);
                        entity.TransformBy(mt);
                    });
                }
            } 
        }

        private void InsertBasePoint()
        {
            using (var acadDb = AcadDatabase.Active())
            {
                if(acadDb.Blocks.Contains(ThPrintBlockManager.BasePointBlkName) &&
                    acadDb.Layers.Contains(ThPrintLayerManager.DefpointsLayerName))
                {
                    DbHelper.EnsureLayerOn(ThPrintLayerManager.DefpointsLayerName);
                    acadDb.ModelSpace.ObjectId.InsertBlockReference(
                                       ThPrintLayerManager.DefpointsLayerName,
                                       ThPrintBlockManager.BasePointBlkName,
                                       Point3d.Origin,
                                       new Scale3d(1.0),
                                       0.0);
                }
            }
        }

        private List<ThSvgEntityPrintService> PrintToCad()
        {
            var results = new List<ThSvgEntityPrintService>();
            SvgFiles.ForEach(svgFile =>
            {
                var svg = new ThStructureSVGReader();
                svg.ReadFromFile(svgFile);

                var prinService = new ThSvgEntityPrintService(
                    svg.Geos, svg.FloorInfos,svg.DocProperties,"1:100");
                prinService.Print(AcadDb);
                results.Add(prinService);
            });
            return results;
        }
        private void SetSysVariables()
        {
            acadApp.Application.SetSystemVariable("LTSCALE", LtScale);
            acadApp.Application.SetSystemVariable("MEASUREMENT", Measurement);
        }        
    }
}
