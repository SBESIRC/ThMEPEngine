﻿using System;
using System.Linq;
using System.Collections.Generic;

using AcHelper;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Dreambuild.AutoCAD;
using Linq2Acad;
using NFox.Cad;

using ThCADExtension;
using ThMEPEngineCore.CAD;

namespace ThMEPElectrical.BlockConvert
{
    public static class ThBConvertBlockReferenceDataExtension
    {
        public static Point3d GetCentroidPoint(this ThBlockReferenceData data)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Use(data.Database))
            {
                var entities = new DBObjectCollection();
                var blkref = acadDatabase.Element<BlockReference>(data.ObjId);
                blkref.ExplodeWithVisible(entities);
                var name = data.EffectiveName;
                if (name.Contains("风机") ||
                         name.Contains("组合式空调器") ||
                         name.Contains("暖通其他设备标注") ||
                         name.Contains("风冷热泵") ||
                         name.Contains("冷水机组") ||
                         name.Contains("冷却塔"))
                {
                    entities = entities.Cast<Entity>()
                        .Where(e => e.Layer == "0" || e.Layer.Contains("H-EQUP"))
                        .Where(e => !e.Layer.Contains("DIMS"))
                        .Where(e => e is Curve || e is BlockReference)
                        .ToCollection();
                }
                else if (name.Contains("防火阀"))
                {
                    entities = entities.Cast<Entity>()
                        .Where(e => e.Layer != "DEFPOINTS")
                        .Where(e => e is Circle || e is BlockReference)
                        .ToCollection();
                }
                else
                {
                    entities = entities.Cast<Entity>()
                        .Where(e => e.Layer != "DEFPOINTS")
                        .Where(e => e is Curve || e is BlockReference)
                        .ToCollection();
                }
                if (entities.Count == 0)
                {
                    return Point3d.Origin;
                }
                return entities.GeometricExtents().CenterPoint();
            }
        }

        public static Polyline GetBlockOBB(this BlockReference targetBlock)
        {
            var entities = new DBObjectCollection();
            ThBlockReferenceExtensions.Burst(targetBlock, entities);
            entities = entities.OfType<Entity>().Where(e => e.Visible && e.Bounds.HasValue).ToCollection();
            if (entities.Count > 0)
            {
                return entities.GeometricExtents().ToRectangle();
            }
            return new Polyline();
        }

        public static Point3d GetBottomCenter(this ThBlockReferenceData data)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Use(data.Database))
            {
                var entities = new DBObjectCollection();
                var blkref = acadDatabase.Element<BlockReference>(data.ObjId);
                blkref.ExplodeWithVisible(entities);
                var name = data.EffectiveName;
                if (name.Contains("室内消火栓平面"))
                {
                    var lines = new List<Line>();
                    lines = entities.OfType<Line>()
                        .Where(e => e.Layer == "0")
                        .ToList();
                    lines = lines.OrderByDescending(o => o.Length).ToList();

                    if(lines.Count > 0)
                    {
                        var closeDist = lines[0].DistanceTo(data.Position, false);
                        var closePt = GetLineCenter(lines[0].StartPoint, lines[0].EndPoint);
                        for (int i = 1; i < lines.Count && i < 3; i++)
                        {
                            var distance = lines[i].DistanceTo(data.Position, false);
                            var center = GetLineCenter(lines[i].StartPoint, lines[i].EndPoint);
                            if (distance < closeDist + 1.0)
                            {
                                closeDist = distance;
                                closePt = center;
                            }
                        }
                        return closePt;
                    }
                }
                else if (name.Contains("E-BFAS610"))
                {
                    var centerLine = entities.OfType<Line>().Where(line => line.Layer.Contains("E-UNIV-EL")).First();
                    var centerPtTidal = GetLineCenter(centerLine.StartPoint, centerLine.EndPoint);
                    var pline = entities.OfType<Polyline>().Where(line => line.Layer.Contains("0")).First();
                    var outlines = new DBObjectCollection();
                    pline.Explode(outlines);
                    var closeDist = -1.0;
                    var closePt = new Point3d();
                    outlines.OfType<Line>().ForEach(line =>
                    {
                        var lineCenter = GetLineCenter(line.StartPoint, line.EndPoint);
                        var distance = centerPtTidal.DistanceTo(lineCenter);
                        if (closeDist < 0 || distance < closeDist + 1.0)
                        {
                            closeDist = distance;
                            closePt = lineCenter;
                        }
                    });
                    return closePt;
                }
                else
                {
                    throw new NotImplementedException();
                }
                return new Point3d();
            }
        }

        public static void AdjustLoadLabel(this BlockReference targetBlock)
        {
            var entities = new DBObjectCollection();
            var targetBlockData = new ThBlockReferenceData(targetBlock.ObjectId);
            if (targetBlockData.EffectiveName == "水泵标注")
            {
                FilterAndBurst(targetBlock, entities);
            }
            else
            {
                ThBlockReferenceExtensions.Burst(targetBlock, entities);
            }

            entities = entities.OfType<DBText>().ToCollection();
            var textMaxWidth = entities.GetMaxWidth();
            if (targetBlockData.EffectiveName == "水泵标注")
            {
                textMaxWidth = textMaxWidth > 1000 ? ((int)textMaxWidth / 100 * 100 + 500) : 1500;
            }
            else
            {
                textMaxWidth = textMaxWidth > 1600 ? ((int)textMaxWidth / 100 * 100 + 500) : 2100;
            }
            targetBlockData.CustomProperties.SetValue("标注表格宽度", textMaxWidth);
        }

        private static double GetMaxWidth(this DBObjectCollection objs)
        {
            return objs.OfType<DBText>()
                       .Where(o => o.Visible)
                       .Where(o => o.Bounds.HasValue)
                       .Select(o => o.GeometricExtents.Width())
                       .OrderByDescending(o => o)
                       .First();
        }

        public static void FilterAndBurst(this BlockReference blockReference, DBObjectCollection blockEntities)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                // 对于动态块，BlockReference.Name返回的可能是一个匿名块的名字（*Uxxx）
                // 对于这样的动态块，我们并不需要访问到它的“原始”动态块定义，我们只关心它“真实”的块定义
                var blockTableRecord = acadDatabase.Blocks.Element(blockReference.Name);

                // 如果没有属性定义，执行正常的Explode()操作
                if (!blockTableRecord.HasAttributeDefinitions)
                {
                    blockReference.Explode(blockEntities);
                    return;
                }

                // 先检查常量（可见）属性
                foreach (var attDef in blockTableRecord.GetAttributeDefinitions())
                {
                    if (attDef.Constant && !attDef.Invisible)
                    {
                        blockEntities.Add(attDef.ConvertAttributeDefinitionToText());
                    }
                }

                // 再检查非常量（可见）属性
                foreach (ObjectId attRefId in blockReference.AttributeCollection)
                {
                    var attRef = acadDatabase.Element<AttributeReference>(attRefId);
                    if (!attRef.Invisible && attRef.Tag != "主备关系")
                    {
                        blockEntities.Add(attRef.ConvertAttributeReferenceToText());
                    }
                }

                // Explode块引用，忽略属性定义
                using (DBObjectCollection dbObjs = new DBObjectCollection())
                {
                    blockReference.Explode(dbObjs);
                    foreach (Entity dbObj in dbObjs)
                    {
                        if (dbObj is AttributeDefinition)
                        {
                            continue;
                        }

                        blockEntities.Add(dbObj);
                    }
                }
            }
        }

        private static Point3d GetLineCenter(Point3d first, Point3d second)
        {
            return new Point3d((first.X + second.X) / 2, (first.Y + second.Y) / 2, 0);
        }
    }
}
