using System;
using System.Linq;
using System.Collections.Generic;

using AcHelper;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Dreambuild.AutoCAD;
using Linq2Acad;
using NFox.Cad;

using ThCADExtension;
using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Service.Hvac;

namespace ThMEPElectrical.BlockConvert
{
    public static class ThBConvertBlockReferenceDataExtension
    {
        public static List<ThBlockConvertBlock> SourceBConvertRules;
        public static List<ThBlockConvertBlock> TargetBConvertRules;

        public static Point3d GetNewBasePoint(this ThBlockReferenceData data, bool isSourceBlock)
        {
            using (var acadDatabase = AcadDatabase.Use(data.Database))
            {
                var name = ThMEPXRefService.OriginalFromXref(data.EffectiveName);
                if (name.Contains("负载标注2"))
                {
                    name = name.Replace("2", "");
                }
                ThBlockConvertBlock convertRule;
                if (isSourceBlock)
                {
                    var convertRuleList = SourceBConvertRules
                        .Where(rule => (rule.Attributes[ThBConvertCommon.BLOCK_MAP_ATTRIBUTES_BLOCK_NAME] as string).Equals(name))
                        .ToList();
                    if (convertRuleList.Count > 1)
                    {
                        convertRule = convertRuleList.Where(rule => ThStringTools.CompareWithChinesePunctuation(data.CurrentVisibilityStateValue(),
                            rule.Attributes[ThBConvertCommon.BLOCK_MAP_ATTRIBUTES_BLOCK_VISIBILITY] as string)).First();
                    }
                    else
                    {
                        convertRule = convertRuleList[0];
                    }
                }
                else
                {
                    convertRule = TargetBConvertRules
                        .Where(rule => (rule.Attributes[ThBConvertCommon.BLOCK_MAP_ATTRIBUTES_BLOCK_NAME] as string).Equals(name))
                        .First();
                }
                var positionMode = (ThBConvertInsertMode)convertRule.Attributes[ThBConvertCommon.BLOCK_MAP_ATTRIBUTES_BLOCK_POSITION_MODE];
                if (positionMode == ThBConvertInsertMode.BasePoint)
                {
                    return data.Position;
                }
                else if (positionMode == ThBConvertInsertMode.OBBCenter)
                {
                    var entities = new DBObjectCollection();
                    var blkref = acadDatabase.Element<BlockReference>(data.ObjId);
                    blkref.ExplodeWithVisible(entities);
                    var obbLayer = convertRule.Attributes[ThBConvertCommon.BLOCK_MAP_ATTRIBUTES_BLOCK_GEOMETRY_LAYER] as string;
                    if (string.IsNullOrEmpty(obbLayer))
                    {
                        entities = entities.OfType<Entity>()
                            .Where(e => !(e is DBText))
                            .ToCollection();
                    }
                    else
                    {
                        entities = entities.OfType<Entity>()
                            .Where(e => !(e is DBText))
                            .Where(e => ThMEPXRefService.OriginalFromXref(e.Layer).Equals(obbLayer))
                            .ToCollection();
                    }
                    if (entities.Count > 0)
                    {
                        return entities.GeometricExtents().CenterPoint();
                    }
                    else
                    {
                        return blkref.GeometricExtents.CenterPoint();
                    }
                }
                else if (positionMode == ThBConvertInsertMode.BottomCenter)
                {
                    var entities = new DBObjectCollection();
                    var blkref = acadDatabase.Element<BlockReference>(data.ObjId);
                    blkref.ExplodeWithVisible(entities);
                    if (name.Contains("室内消火栓平面"))
                    {
                        var lines = entities.OfType<Line>()
                            .Where(e => e.Layer == "0")
                            .ToList();
                        if (lines.Count == 1)
                        {
                            return GetLineCenter(lines[0].StartPoint, lines[0].EndPoint);
                        }
                        else if (lines.Count > 1)
                        {
                            lines = lines.OrderBy(l => l.DistanceTo(data.Position, false)).ToList();
                            var closeLine = lines[0].Length > lines[1].Length ? lines[0] : lines[1];
                            return GetLineCenter(closeLine.StartPoint, closeLine.EndPoint);
                        }
                        else
                        {
                            throw new NotImplementedException();
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
                }
                else if (positionMode == ThBConvertInsertMode.CircleCenter)
                {
                    var entities = new DBObjectCollection();
                    var blkref = acadDatabase.Element<BlockReference>(data.ObjId);
                    blkref.ExplodeWithVisible(entities);
                    entities = entities.OfType<Circle>()
                        .Where(e => e.Layer == convertRule.Attributes[ThBConvertCommon.BLOCK_MAP_ATTRIBUTES_BLOCK_GEOMETRY_LAYER] as string)
                        .ToCollection();
                    if (entities.Count > 0)
                    {
                        return entities.GeometricExtents().CenterPoint();
                    }
                    else
                    {
                        return blkref.GeometricExtents.CenterPoint();
                    }
                }
                else if (positionMode == ThBConvertInsertMode.TextCenter)
                {
                    var entities = new DBObjectCollection();
                    var blkref = acadDatabase.Element<BlockReference>(data.ObjId);
                    blkref.Explode(entities);
                    var obbLayer = convertRule.Attributes[ThBConvertCommon.BLOCK_MAP_ATTRIBUTES_BLOCK_GEOMETRY_LAYER] as string;
                    if (string.IsNullOrEmpty(obbLayer))
                    {
                        entities = entities.OfType<DBText>().ToCollection();
                    }
                    else
                    {
                        entities = entities.OfType<DBText>()
                            .Where(e => ThMEPXRefService.OriginalFromXref(e.Layer).Equals(obbLayer))
                            .ToCollection();
                    }
                    if (entities.Count > 0)
                    {
                        return entities.GeometricExtents().CenterPoint();
                    }
                    else
                    {
                        return blkref.GeometricExtents.CenterPoint();
                    }
                }
                else if (positionMode == ThBConvertInsertMode.AxialFlowFan)
                {
                    var br = data.ObjId.GetObject(OpenMode.ForRead) as BlockReference;
                    //如果不是动态块，则返回
                    if (br == null || !br.IsDynamicBlock)
                    {
                        return data.Position;
                    }
                    //返回动态块的动态属性
                    var customProperties = br.DynamicBlockReferencePropertyCollection;
                    var rotation = Convert.ToDouble(customProperties.GetValue(ThHvacCommon.BLOCK_DYNAMIC_PROPERTY_ANGLE2));
                    var length = Convert.ToDouble(customProperties.GetValue(ThHvacCommon.BLOCK_DYNAMIC_PROPERTY_LENGTH));
                    var offset = new Point3d(0, -length / 2, 0).TransformBy(Matrix3d.Rotation(rotation, Vector3d.ZAxis, Point3d.Origin));
                    var point = new Point3d(offset.X + Convert.ToDouble(customProperties.GetValue(ThHvacCommon.BLOCK_DYNMAIC_PROPERTY_BASE_POINT_X)),
                        offset.Y + Convert.ToDouble(customProperties.GetValue(ThHvacCommon.BLOCK_DYNMAIC_PROPERTY_BASE_POINT_Y)), 0);
                    return new Point3d(data.Position.X + point.X * data.ScaleFactors.X, data.Position.Y + point.Y * data.ScaleFactors.Y, 0);
                }
                else if (positionMode == ThBConvertInsertMode.EquipmentBase)
                {
                    var br = data.ObjId.GetObject(OpenMode.ForRead) as BlockReference;
                    //如果不是动态块，则返回
                    if (br == null || !br.IsDynamicBlock)
                    {
                        return data.Position;
                    }
                    //返回动态块的动态属性
                    var customProperties = br.DynamicBlockReferencePropertyCollection;
                    return new Point3d(data.Position.X + Convert.ToDouble(customProperties.GetValue(ThHvacCommon.BLOCK_DYNMAIC_PROPERTY_BASE_POINT_X)),
                        data.Position.Y + Convert.ToDouble(customProperties.GetValue(ThHvacCommon.BLOCK_DYNMAIC_PROPERTY_BASE_POINT_Y)), 0);
                }
                else
                {
                    throw new NotImplementedException();
                }
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

        public static void AdjustLoadLabel(this ThBlockReferenceData targetBlockData)
        {
            using (var acadDatabase = AcadDatabase.Use(targetBlockData.Database))
            {
                var targetBlock = acadDatabase.Element<BlockReference>(targetBlockData.ObjId, true);
                //如果不是动态块，则返回
                if (targetBlock == null || !targetBlock.IsDynamicBlock)
                {
                    return;
                }
                var entities = new DBObjectCollection();
                if (targetBlockData.EffectiveName.Equals(ThBConvertCommon.BLOCK_PUMP_LABEL))
                {
                    FilterAndBurst(targetBlock, entities);
                }
                else
                {
                    ThBlockReferenceExtensions.Burst(targetBlock, entities);
                }

                //返回动态块的动态属性
                var customProperties = targetBlock.DynamicBlockReferencePropertyCollection;
                entities = entities.OfType<DBText>().ToCollection();
                var textMaxWidth = entities.GetMaxWidth();
                if (targetBlockData.EffectiveName.Equals(ThBConvertCommon.BLOCK_PUMP_LABEL))
                {
                    textMaxWidth = textMaxWidth > 1000 ? ((int)textMaxWidth / 100 * 100 + 500) : 1500;
                }
                else
                {
                    textMaxWidth = textMaxWidth > 1600 ? ((int)textMaxWidth / 100 * 100 + 500) : 2100;
                }
                customProperties.SetValue(ThBConvertCommon.PROPERTY_TABLE_WIDTH, textMaxWidth);
            }
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
