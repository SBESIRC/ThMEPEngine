using AcHelper;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Dreambuild.AutoCAD;
using Linq2Acad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPEngineCore.IO.SVG;
using ThMEPStructure.StructPlane.Print;

namespace ThMEPStructure.StructPlane.Service
{
    internal class ThMultiSvgPrintService
    {
        private Database AcadDb { get; set; }
        private double FloorSpacing { get; set; } = 5000;
        private double ElevationTblSpacing { get; set; } = 2000;
        private List<string> SvgFiles { get; set; }
        public ThMultiSvgPrintService(Database database,List<string> svgFiles)
        {
            AcadDb = database;
            SvgFiles = svgFiles;
        }
        public void Print()
        {
            //
            var printers = PrintToCad();

            Layout(printers.Select(o=>o.ObjIds).ToList());

            // 打印 标高表
            var elevationInfos = GetElevations(printers);
            var extents = ToExtents2d(printers
                .SelectMany(o => o.ObjIds.OfType<ObjectId>())
                .ToObjectIdCollection());
            var elevationObjs = BuildElevationTable(elevationInfos);
            var tblBasePt = new Point3d(extents.MaxPoint.X+ ElevationTblSpacing, 
                extents.MinPoint.Y,0);
            var mt = Matrix3d.Displacement(tblBasePt - Point3d.Origin);
            elevationObjs.OfType<Entity>().ForEach(o => o.TransformBy(mt));
            elevationObjs.Print(AcadDb);
        }

        private List<ElevationInfo> GetElevations(List<ThSvgEntityPrintService> prints)
        {
            var elevationInfos = new List<ElevationInfo>();
            int index = 1;
            prints.ForEach(o =>
            {
                var flrBottomElevation = o.FlrBottomEle / 1000.0;
                var flrElevation = o.FlrElevation / 1000.0;
                elevationInfos.Add(new ElevationInfo()
                {
                    FloorNo = index.ToString(),
                    Elevation = flrBottomElevation.ToString("0.000"),
                    FloorHeight = (flrElevation - flrBottomElevation).ToString("0.000"),
                    WallColumnGrade = "",
                    BeamBoardGrade = "",
                });
                index++;
            });
            return elevationInfos;
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
                    var prevExtents = ToExtents2d(floorObjIds[i - 1]);
                    var currentExtents = ToExtents2d(floorObjIds[i]);
                    var targetY = prevExtents.MaxPoint.Y + FloorSpacing;
                    var sourceY = currentExtents.MinPoint.Y;
                    var mt = Matrix3d.Displacement(new Vector3d(0, targetY- sourceY,0));
                    floorObjIds[i].OfType<ObjectId>().ForEach(o =>
                    {
                        var entity = acadDb.Element<Entity>(o, true);
                        entity.TransformBy(mt);
                    });
                }
            } 
        }

        private Extents2d ToExtents2d(ObjectIdCollection objIds)
        {
            var extents = new Extents2d();
            double minX=double.MaxValue,minY = double.MaxValue, 
                maxX = double.MinValue, maxY = double.MinValue;
            using (var acadDb = AcadDatabase.Use(AcadDb))
            {
                objIds.OfType<ObjectId>().ForEach(o =>
                {
                    var entity = acadDb.Element<Entity>(o);
                    if(entity!=null && !entity.IsErased && entity.GeometricExtents!=null)
                    {
                        if(entity.GeometricExtents.MinPoint.X< minX)
                        {
                            minX = entity.GeometricExtents.MinPoint.X;
                        }
                        if (entity.GeometricExtents.MinPoint.Y < minY)
                        {
                            minY = entity.GeometricExtents.MinPoint.Y;
                        }
                        if (entity.GeometricExtents.MaxPoint.X > maxX)
                        {
                            maxX = entity.GeometricExtents.MaxPoint.X;
                        }
                        if (entity.GeometricExtents.MaxPoint.Y > maxY)
                        {
                            maxY = entity.GeometricExtents.MaxPoint.Y;
                        }
                    }
                });
                extents= new Extents2d(minX,minY,maxX,maxY);    
            }
            return extents;
        }

        private DBObjectCollection BuildElevationTable(List<ElevationInfo> elevations)
        {
            // 在原点生成
            var tblBuilder = new ThElevationTableBuilder(elevations);
            return  tblBuilder.Build();
        }

        private List<ThSvgEntityPrintService> PrintToCad()
        {
            var results = new List<ThSvgEntityPrintService>();
            SvgFiles.ForEach(svgFile =>
            {
                var svg = new ThSVGReader();
                var svgData = svg.ReadFromFile(svgFile);

                var prinService = new ThSvgEntityPrintService(
                    svgData.Item1, svgData.Item2);
                prinService.Print(AcadDb);
                results.Add(prinService);
            });
            return results;
        }
    }
}
