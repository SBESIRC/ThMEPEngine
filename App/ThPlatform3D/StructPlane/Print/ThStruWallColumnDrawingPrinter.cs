﻿using System.Linq;
using NFox.Cad;
using Linq2Acad;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.IO;
using ThMEPEngineCore.IO.SVG;
using ThPlatform3D.Common;
using ThPlatform3D.StructPlane.Service;
using System;

namespace ThPlatform3D.StructPlane.Print
{
    internal class ThStruWallColumnDrawingPrinter : ThStruDrawingPrinter
    {
        public ThStruWallColumnDrawingPrinter(ThSvgParseInfo input, ThPlanePrintParameter printParameter)
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
                var textRes = PrintHeadText(acadDb);
                Append(textRes.Item1);
                Append(textRes.Item2);
                AppendToBlockObjIds(textRes.Item2);

                // 插入基点
                var basePointId = InsertBasePoint(acadDb, _printParameter.BasePoint);
                Append(basePointId);
                AppendToBlockObjIds(basePointId);

                // 打印层高表
                //PrintElevationTable(acadDb);

                // 过滤无效Id
                ObjIds = ObjIds.OfType<ObjectId>().Where(o => o.IsValid && !o.IsErased).ToCollection();

                // 成块的对象
                AppendToBlockObjIds(GetBlockObjIds(acadDb, ObjIds));
            }
        }

        private ObjectIdCollection GetBlockObjIds(AcadDatabase acadDb, ObjectIdCollection floorObjIds)
        {
            var blkIds = new ObjectIdCollection();
            if (floorObjIds.Count == 0)
            {
                return blkIds;
            }
            floorObjIds.OfType<ObjectId>().ForEach(o =>
            {
                var entity = acadDb.Element<Entity>(o);
                if (entity.Layer == ThPrintLayerManager.BeamLayerName ||
                entity.Layer == ThPrintLayerManager.BelowColumnLayerName ||
                entity.Layer == ThPrintLayerManager.BelowColumnHatchLayerName ||
                entity.Layer == ThPrintLayerManager.ColumnLayerName ||
                entity.Layer == ThPrintLayerManager.ColumnHatchLayerName ||
                entity.Layer == ThPrintLayerManager.BelowShearWallLayerName ||
                entity.Layer == ThPrintLayerManager.BelowShearWallHatchLayerName ||
                entity.Layer == ThPrintLayerManager.ShearWallLayerName ||
                entity.Layer == ThPrintLayerManager.ShearWallHatchLayerName ||
                entity.Layer == ThPrintLayerManager.PCWallLayer ||
                entity.Layer == ThPrintLayerManager.PCWallHatchLayer 
                )
                {
                    blkIds.Add(o);
                }
            });
            return blkIds;
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
                        var description = o.Properties.GetDescription();
                        if(description.IsStandardColumn())
                        {
                            if (o.IsBelowFloorColumn())
                            {
                                Append(PrintUpperColumn(acadDb, o));
                            }
                        }                                              
                    }
                    else if (category == ThIfcCategoryManager.WallCategory)
                    {
                        var description = o.Properties.GetDescription();
                        if(description.IsStandardWall())
                        {
                            if (o.IsBelowFloorShearWall())
                            {
                                Append(PrintUpperShearWall(acadDb, o));
                            }
                        }
                        else if(description.IsPCWall())
                        {
                            Append(PrintPCWall(acadDb, o));
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
        private Tuple<ObjectIdCollection, ObjectIdCollection> PrintHeadText(AcadDatabase acadDb)
        {
            // 打印自然层标识, eg 一层~五层结构平面层
            var flrRange = _floorInfos.GetFloorHeightRange(_flrBottomEle);
            if(flrRange==null)
            {
                return Tuple.Create(new ObjectIdCollection(), new ObjectIdCollection());
            }
            else
            {
                var btmElevationStr = flrRange.Item1.ToString("N3");
                var topElevationStr = flrRange.Item2.ToString("N3");
                var flrRangeStr = btmElevationStr + "m" + " ~ " + topElevationStr + "m" + " 墙柱平面图";
                var stdFlrInfo = _floorInfos.GetStdFlrInfo(_flrBottomEle);
                var newStdFlrInfo = Tuple.Create(btmElevationStr, topElevationStr, stdFlrInfo.Item3);
                return PrintHeadText(acadDb, flrRangeStr, newStdFlrInfo);
            }  
        }
    }
}
