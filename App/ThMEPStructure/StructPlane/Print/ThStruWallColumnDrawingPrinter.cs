﻿using System.Linq;
using NFox.Cad;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.IO;
using ThMEPStructure.Common;
using ThMEPStructure.StructPlane.Service;

namespace ThMEPStructure.StructPlane.Print
{
    internal class ThStruWallColumnDrawingPrinter : ThStruDrawingPrinter
    {
        public ThStruWallColumnDrawingPrinter(ThSvgInput input, ThPlanePrintParameter printParameter)
            :base(input, printParameter)
        {
        }
        public override void Print(Database db)
        {
            // 打印墙柱
            PrintGeos(db);

            // 打印标题
            PrintHeadText(db);

            // 打印层高表
            //PrintElevationTable(db);

            // 过滤无效Id
            ObjIds = ObjIds.OfType<ObjectId>().Where(o => o.IsValid && !o.IsErased).ToCollection();
        }

        private void PrintGeos(Database database)
        {
            // 打印到图纸中
            Geos.ForEach(o =>
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
                        Append(PrintColumn(database, o));
                    }
                    else if (category == ThIfcCategoryManager.WallCategory)
                    {
                        Append(PrintShearWall(database, o));
                    }
                }
            });
        }
       
        private void PrintElevationTable(Database db)
        {
            // 打印柱表
            var maxX = Geos.Where(o => o.Boundary.GeometricExtents != null).Select(o => o.Boundary.GeometricExtents
                 .MaxPoint.X).OrderByDescending(o => o).FirstOrDefault();
            var minY = Geos.Where(o => o.Boundary.GeometricExtents != null).Select(o => o.Boundary.GeometricExtents
                 .MinPoint.Y).OrderBy(o => o).FirstOrDefault();
            var elevationTblBasePt = new Point3d(maxX + 1000.0, minY, 0);
            var elevationInfos = GetElevationInfos();
            elevationInfos = elevationInfos.OrderBy(o => int.Parse(o.FloorNo)).ToList(); // 按自然层编号排序

            var tblBuilder = new ThElevationTableBuilder(elevationInfos);
            var objs = tblBuilder.Build();
            var mt = Matrix3d.Displacement(elevationTblBasePt - Point3d.Origin);
            objs.OfType<Entity>().ForEach(e=>e.TransformBy(mt));
            Append(objs.Print(db));
        }   
    }
}