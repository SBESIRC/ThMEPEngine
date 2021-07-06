using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Linq2Acad;
using System;
using System.Collections.Generic;
using System.Linq;
using ThCADCore.NTS;
using ThCADExtension;
using ThMEPWSS.DrainageSystemAG.DataEngine;
using ThMEPWSS.DrainageSystemAG.Models;
using ThMEPWSS.Model;

namespace ThMEPWSS.DrainageSystemAG
{
    class DrainSysAGCommon
    {
        public static readonly string BLOCKNAMEPREFIX = "TH-AGDRAIN-BLOCK";
        public static List<Line> PolyLineToLines(Polyline polyline)
        {
            List<Line> lines = new List<Line>();
            var newPline = polyline.DPSimplify(10);
            for (int i = 0; i < newPline.NumberOfVertices; i++)
            {
                var sp = newPline.GetPoint3dAt(i);
                var ep = newPline.GetPoint3dAt((i + 1) % newPline.NumberOfVertices);
                if (sp.DistanceTo(ep) < 0.0001)
                    continue;
                lines.Add(new Line(sp, ep));
            }
            return lines;
        }
        public static List<Entity> GetBlockInnerElement<T>(BlockReference blockReference, Matrix3d matrix) 
        {
            List<Entity> resT = new List<Entity>();
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                
                var blockTableRecord = acadDatabase.Blocks.Element(blockReference.BlockTableRecord);
                foreach (var id in blockTableRecord)
                {
                    var dbObj = acadDatabase.Element<Entity>(id);
                    if (null == dbObj)
                        continue;
                    if (dbObj is BlockReference)
                    {
                        var block = dbObj as BlockReference;
                        var mcs2wcs = block.BlockTransform.PreMultiplyBy(matrix);
                        var res = GetBlockInnerElement<T>(block,mcs2wcs);
                        if (res == null || res.Count < 1)
                            continue;
                        resT.AddRange(res);
                    }
                    else if (dbObj is T) 
                    {
                        resT.Add(dbObj.GetTransformedCopy(matrix));
                    }
                }
            }
            return resT;
        }
        public static List<DynBlockWidthLength> GetDynBlockMaxWidth(List<DynBlockWidthLength> dynBlockWidthLengths)
        {
            //这里的块都是正方形，不用考虑朝向问题
            using (AcadDatabase acdb = AcadDatabase.Active()) 
            {
                //这里的块都是正方形，不用考虑朝向问题
                foreach (var item in dynBlockWidthLengths)
                {
                    var objIds = ThDynamicBlockUtils.VisibleEntities(acdb.Database, item.blockName, item.dynName).Cast<ObjectId>().ToList();
                    if (null == objIds || objIds.Count < 1)
                        continue;
                    var entitys = new List<Entity>();
                    foreach (ObjectId id in objIds)
                    {
                        var ent = acdb.Element<Entity>(id);
                        entitys.Add(ent);
                    }
                    if (entitys.Count < 1)
                        continue;
                    var extents = entitys[0].GeometricExtents;
                    for (int i = 1; i < entitys.Count; i++)
                        extents.AddExtents(entitys[i].GeometricExtents);
                    item.width = extents.ToEnvelope().Width;
                    item.length = extents.ToEnvelope().Height;
                }
            }
            return dynBlockWidthLengths;
        }
        public static List<TubeWellsRoomModel> GetTubeWellRoomRelation(List<RoomModel> allToilteKitchenRooms, List<RoomModel> tubeWellRooms)
        {
            List<TubeWellsRoomModel> retRooms = new List<TubeWellsRoomModel>();
            if (null == tubeWellRooms || tubeWellRooms.Count < 1)
                return retRooms;
            foreach (var room in tubeWellRooms)
            {
                var centerPoint = room.GetRoomCenterPoint();
                var bufferGmtry = room.outLine.ToNTSGeometry().Buffer(500);
                TubeWellsRoomModel roomModel = new TubeWellsRoomModel(room, centerPoint);
                //管道井 和 卫生间厨房关系判断
                foreach (var item in allToilteKitchenRooms)
                {
                    var pline = item.outLine;
                    if (pline.Contains(centerPoint))
                    {
                        //在内部
                        roomModel.innerRoomIds.Add(item.thIFCRoom.Uuid);
                    }
                    else if (bufferGmtry.Intersects(item.outLine.ToNTSGeometry()))
                    {
                        //相交
                        roomModel.intersectRoomIds.Add(item.thIFCRoom.Uuid);
                    }
                }
                retRooms.Add(roomModel);
            }
            return retRooms;
        }
    
    
        public static CreateBlockInfo CopyOneBlock(CreateBlockInfo cBlock, Point3d oldBasePoint, Point3d newBasePoint, string floorId)
        {
            var moveVector = cBlock.createPoint - oldBasePoint;
            var newPoint = newBasePoint + moveVector;
            var createBlock = new CreateBlockInfo(floorId, cBlock.blockName, cBlock.layerName, newPoint, cBlock.equipmentType, cBlock.belongBlockId,cBlock.uid);
            createBlock.rotateAngle = cBlock.rotateAngle;
            createBlock.scaleNum = cBlock.scaleNum;
            if (cBlock.attNameValues != null && cBlock.attNameValues.Count > 0)
                foreach (var keyValue in cBlock.attNameValues)
                    createBlock.attNameValues.Add(keyValue.Key, keyValue.Value);
            if (cBlock.dymBlockAttr != null && cBlock.dymBlockAttr.Count > 0)
                foreach (var keyValue in cBlock.dymBlockAttr)
                    createBlock.dymBlockAttr.Add(keyValue.Key, keyValue.Value);
            createBlock.spaceId = cBlock.spaceId;
            createBlock.tag = cBlock.tag;
            return createBlock;
        }
        public static CreateBasicElement CopyBaseElement(CreateBasicElement cElem, Point3d oldBasePoint, Point3d newBasePoint, string floorId)
        {
            CreateBasicElement basicElement = null;
            if (cElem.baseCurce is Line)
            {
                Line line = (Line)cElem.baseCurce;
                var lineSp = line.StartPoint;
                var lineEp = line.EndPoint;
                var spVector = lineSp - oldBasePoint;
                var epVector = lineEp - oldBasePoint;
                lineSp = newBasePoint + spVector;
                lineEp = newBasePoint + epVector;
                Line newLine = new Line(lineSp, lineEp);
                basicElement = new CreateBasicElement(floorId, newLine, cElem.layerName, cElem.belongBlockId,cElem.curveTag, cElem.lineColor);
            }
            else if (cElem.baseCurce is Circle)
            {
                var circle = (Circle)cElem.baseCurce;
                var center = circle.Center;
                var moveVecotor = center - oldBasePoint;
                var newCenter = newBasePoint + moveVecotor;
                var newCircle = new Circle(newCenter, circle.Normal, circle.Radius);
                basicElement = new CreateBasicElement(floorId, newCircle, cElem.layerName, cElem.belongBlockId, cElem.curveTag, cElem.lineColor);
            }
            return basicElement;
        }

        public static CreateDBTextElement CopyTextElement(CreateDBTextElement cText, Point3d oldBasePoint, Point3d newBasePoint, string floorId)
        {
            var oldPoint = cText.dbText.Position;
            var vectorMove = oldPoint - oldBasePoint;
            var newPoint = newBasePoint + vectorMove;
            DBText text = new DBText
            {
                TextString = cText.dbText.TextString,
                Height = cText.dbText.Height,
                WidthFactor = cText.dbText.WidthFactor,
                HorizontalMode = cText.dbText.HorizontalMode,
                Oblique = cText.dbText.Oblique,
                Position = newPoint,
                Rotation = cText.dbText.Rotation,
            };
            if (!string.IsNullOrEmpty(cText.layerName))
                text.Layer = cText.layerName;
            var copyText = new CreateDBTextElement(floorId, newPoint, text, cText.belongBlockId, cText.layerName,cText.textStyle, cText.uid);
            return copyText;
        }


        public static List<EquipmentBlcokModel> GetFloorBlocks(FloorFramed floor, BlockReferenceDataEngine equipmentData, BasicElementEngine basicElementEngine)
        {
            var resList = new List<EquipmentBlcokModel>();
            if (null == equipmentData)
                return resList;
            //根据轴网过滤元素
            var tempBlocks = equipmentData.GetPolylineEquipmentBlocks(floor.outPolyline);
            if (null == tempBlocks || tempBlocks.Count < 1 || basicElementEngine == null)
                return tempBlocks;
            var axisEntitys = basicElementEngine.GetExtractorEntity(floor.outPolyline, new List<EnumElementType> { EnumElementType.ExternalLineAxis });
            if (null == axisEntitys || axisEntitys.Count < 2)
                return tempBlocks;
            var xAxisPoints = new List<Point3d>();
            var yAxisPoints = new List<Point3d>();
            foreach (var axis in axisEntitys)
            {
                if (axis is Line line)
                {
                    if (line.Length < 5000)
                        continue;
                    var lineDir = (line.EndPoint - line.StartPoint).GetNormal();
                    var angle = lineDir.GetAngleTo(Vector3d.XAxis, Vector3d.ZAxis);
                    var angle0 = angle % Math.PI;
                    var angle90 = angle % (Math.PI / 2);
                    var minAngle = Math.PI * 5 / 180;
                    var maxAngle = Math.PI - minAngle;
                    if (Math.Abs(angle) > minAngle && Math.Abs(angle) < maxAngle)
                        continue;
                    if (Math.Abs(angle0) < minAngle || Math.Abs(angle0) > maxAngle)
                    {
                        xAxisPoints.Add(line.StartPoint);
                        xAxisPoints.Add(line.EndPoint);
                    }
                    else if (Math.Abs(angle90) < minAngle)
                    {
                        yAxisPoints.Add(line.StartPoint);
                        yAxisPoints.Add(line.EndPoint);
                    }
                }
            }
            if (xAxisPoints.Count >3)
            {
                double minY = xAxisPoints.Min(c => c.Y);
                double maxY = xAxisPoints.Max(c => c.Y);
                if (Math.Abs(minY - maxY) > 1000)
                {
                    minY -= 2000;
                    maxY += 2000;
                    foreach (var item in tempBlocks)
                    {
                        if (item.blockReferences == null || item.blockReferences.Count < 1)
                            continue;
                        var thisTypeBlocks = new List<BlockReference>();
                        foreach (var block in item.blockReferences)
                        {
                            if (block.Position.Y < minY || block.Position.Y > maxY)
                                continue;
                            thisTypeBlocks.Add(block);
                        }
                        if (thisTypeBlocks.Count < 1)
                            continue;
                        resList.Add(new EquipmentBlcokModel(item.enumEquipmentType, thisTypeBlocks));
                    }
                }
                else 
                {
                    resList.AddRange(tempBlocks);
                }
            }
            else
            {
                resList.AddRange(tempBlocks);
            }
            if (resList.Count < 1)
                return resList;
            if (yAxisPoints.Count > 3) 
            {
                double minX = yAxisPoints.Min(c => c.X);
                double maxX = yAxisPoints.Max(c => c.X);
                if (Math.Abs(minX - maxX) > 1000)
                {
                    tempBlocks.Clear();
                    tempBlocks.AddRange(resList);
                    resList.Clear();
                    minX -= 2000;
                    maxX += 2000;
                    foreach (var item in tempBlocks)
                    {
                        if (item.blockReferences == null || item.blockReferences.Count < 1)
                            continue;
                        var thisTypeBlocks = new List<BlockReference>();
                        foreach (var block in item.blockReferences)
                        {
                            if (block.Position.X < minX || block.Position.X > maxX)
                                continue;
                            thisTypeBlocks.Add(block);
                        }
                        if (thisTypeBlocks.Count < 1)
                            continue;
                        resList.Add(new EquipmentBlcokModel(item.enumEquipmentType, thisTypeBlocks));
                    }
                }
            }
            return resList;
        }
    }
}
