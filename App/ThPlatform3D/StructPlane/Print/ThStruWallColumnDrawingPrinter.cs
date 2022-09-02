using System.Linq;
using NFox.Cad;
using Linq2Acad;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.IO;
using ThPlatform3D.Common;
using ThPlatform3D.StructPlane.Service;

namespace ThPlatform3D.StructPlane.Print
{
    internal class ThStruWallColumnDrawingPrinter : ThStruDrawingPrinter
    {
        public ThStruWallColumnDrawingPrinter(ThSvgInput input, ThPlanePrintParameter printParameter)
            :base(input, printParameter)
        {
        }
        public override void Print(Database db)
        {
            using (var acadDb = AcadDatabase.Use(db))
            {
                // 打印墙柱
                PrintGeos(acadDb);

                // 打印标题
                PrintHeadText(acadDb);

                // 打印层高表
                //PrintElevationTable(acadDb);

                // 过滤无效Id
                ObjIds = ObjIds.OfType<ObjectId>().Where(o => o.IsValid && !o.IsErased).ToCollection();
            }
        }

        private void PrintGeos(AcadDatabase acadDb)
        {
            // 打印到图纸中
            _geos.ForEach(o =>
            {
                // Svg解析的属性信息存在于Properties中
                string category = o.Properties.GetCategory();
                if (o.Boundary is DBText dbText)
                {
                    //TODO
                }
                else
                {
                    if (category == ThIfcCategoryManager.ColumnCategory)
                    {
                        if(o.IsBelowFloorColumn())
                        {
                            Append(PrintUpperColumn(acadDb, o));
                        }                       
                    }
                    else if (category == ThIfcCategoryManager.WallCategory)
                    {
                        if(o.IsBelowFloorShearWall())
                        {
                            Append(PrintUpperShearWall(acadDb, o));
                        }                        
                    }
                }
            });
        }
       
        private void PrintElevationTable(AcadDatabase acadDb)
        {
            // 打印柱表
            var maxX = _geos.Where(o => o.Boundary.GeometricExtents != null).Select(o => o.Boundary.GeometricExtents
                 .MaxPoint.X).OrderByDescending(o => o).FirstOrDefault();
            var minY = _geos.Where(o => o.Boundary.GeometricExtents != null).Select(o => o.Boundary.GeometricExtents
                 .MinPoint.Y).OrderBy(o => o).FirstOrDefault();
            var elevationTblBasePt = new Point3d(maxX + 1000.0, minY, 0);
            var elevationInfos = GetElevationInfos();
            elevationInfos = elevationInfos.OrderBy(o => int.Parse(o.FloorNo)).ToList(); // 按自然层编号排序

            var tblBuilder = new ThElevationTableBuilder(elevationInfos);
            var objs = tblBuilder.Build();
            var mt = Matrix3d.Displacement(elevationTblBasePt - Point3d.Origin);
            objs.OfType<Entity>().ForEach(e=>e.TransformBy(mt));
            Append(objs.Print(acadDb));
        }
        private void PrintHeadText(AcadDatabase acadDb)
        {
            // 打印自然层标识, eg 一层~五层结构平面层
            var flrRange = _floorInfos.GetFloorHeightRange(_flrBottomEle);
            if (string.IsNullOrEmpty(flrRange))
            {
                return;
            }            
            Append(PrintHeadText(acadDb, flrRange)); // 把结果存到ObjIds中
        }
    }
}
