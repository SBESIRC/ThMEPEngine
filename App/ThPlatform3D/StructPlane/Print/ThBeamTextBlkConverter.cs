using System;
using System.Linq;
using System.Collections.Generic;
using NFox.Cad;
using DotNetARX;
using Linq2Acad;
using ThCADCore.NTS;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.CAD;
using ThPlatform3D.StructPlane.Service;
using ThPlatform3D.StructPlane.Model;

namespace ThPlatform3D.StructPlane.Print
{
    /// <summary>
    /// 用于将梁标注文字转成块
    /// </summary>
    internal class ThBeamTextBlkConverter
    {
        private int alignmentIndex = 0;
        /// <summary>
        /// 构造
        /// </summary>
        /// <param name="alignmentIndex">等于0->中心对齐, 小于0->左对齐, 大于0->右对齐</param>
        public ThBeamTextBlkConverter(int alignmentIndex = 0)
        {
            this.alignmentIndex = alignmentIndex;
        }

        public ObjectIdCollection Convert(AcadDatabase acadDb, List<ThBeamMarkBlkInfo> beamTextObjs)
        {
            var results = new ObjectIdCollection();
            beamTextObjs
                .Where(o=>o.Marks.Count>0)
                .ForEach(o =>
            {
                // o 中的文字都是平行的
                var clones = o.Marks.Clone();
                var blkName = clones.GetMultiTextString();
                if (!string.IsNullOrEmpty(blkName))
                {
                    AdjustPosition(clones);
                    CreateBlock(acadDb, clones, blkName);
                    var blkId = InsertBlock(acadDb, o.Marks);
                    ThBeamMarkXDataService.WriteBeamArea(blkId, o.OrginArea,o.TextMoveDir);
                    results.Add(blkId);
                }
            });
            return results;
        }

        public ObjectIdCollection Update(AcadDatabase acadDb, List<ThBeamMarkBlkInfo> generatedBeamBlks)
        {
            var results = new ObjectIdCollection();
            generatedBeamBlks.ForEach(o =>
            {
                var newBlkName = o.Marks.GetMultiTextString();
                if(!string.IsNullOrEmpty(newBlkName))
                {
                    var bt = acadDb.Element<BlockTable>(acadDb.Database.BlockTableId, false);
                    if (!bt.Has(newBlkName))
                    {
                        var clones = o.Marks.Clone();
                        AdjustPosition(clones);
                        CreateBlock(acadDb, clones, newBlkName);
                    }
                    var blkId = acadDb.CurrentSpace.ObjectId.InsertBlockReference(
                        ThPrintLayerManager.BeamTextLayerName, newBlkName, Point3d.Origin, new Scale3d(1.0), 0.0);
                    ThBeamMarkXDataService.WriteBeamArea(blkId, o.OrginArea,o.TextMoveDir);
                    results.Add(blkId);

                    var blkObj = acadDb.Element<BlockReference>(blkId, true);
                    var mt1 = Matrix3d.Rotation(o.GeneratedBlk.Rotation, o.GeneratedBlk.Normal,Point3d.Origin);
                    var mt2 = Matrix3d.Displacement(Point3d.Origin.GetVectorTo(o.GeneratedBlk.Position));
                    blkObj.TransformBy(mt1);
                    blkObj.TransformBy(mt2);
                }
            });
            return results;
        }

        private ObjectId InsertBlock(AcadDatabase acadDb, DBObjectCollection beamTextObjs)
        {
            var blkName = beamTextObjs.GetMultiTextString();
            var blkId = acadDb.CurrentSpace.ObjectId.InsertBlockReference(
                ThPrintLayerManager.BeamTextLayerName, blkName, Point3d.Origin, new Scale3d(1.0), 0.0);
            var blkObj = acadDb.Element<BlockReference>(blkId, true);

            // 把块的中心移动原点
            var blkObjOldCenter = blkObj.GeometricExtents.GetCenter();
            var mt = Matrix3d.Displacement(blkObjOldCenter.GetVectorTo(Point3d.Origin));
            blkObj.TransformBy(mt);

            // 获取块的外包框、中心点
            var blkFrame = GetBlkFrame(blkObj);
            var blkCenter = GetABBCenter(blkObj);

            // 调整块的角度
            var rotation = beamTextObjs.OfType<DBText>().First().Rotation;
            var rotateMt = Matrix3d.Rotation(rotation, Vector3d.ZAxis, blkCenter);
            blkObj.TransformBy(rotateMt);
            blkFrame.TransformBy(rotateMt);
            var blkFrameCenter = GetFrameCenter(blkFrame);

            // 让块的OBB 和 o中装的文字的OBB对齐
            var textGroupFrame = GetParallelTextsFrame(beamTextObjs);
            var textGroupCenter = GetFrameCenter(textGroupFrame);

            // 把块移动到文字组的中心
            var moveMt = Matrix3d.Displacement(blkFrameCenter.GetVectorTo(textGroupCenter));
            blkObj.TransformBy(moveMt);
            blkFrame.Dispose();
            textGroupFrame.Dispose();

            return blkId;
        }

        private Point3d GetFrameCenter(Polyline frame)
        {
            return frame.NumberOfVertices > 3 ? frame.GetPoint3dAt(0).GetMidPt(frame.GetPoint3dAt(2)) : GetABBCenter(frame);
        }

        private Polyline GetParallelTextsFrame(DBObjectCollection texts)
        {
            var clones = texts.Clone();
            var frames =  clones.OfType<DBText>().Select(o => o.TextOBB()).ToCollection();
            var frame = frames.GetMinimumRectangle();
            clones.MDispose();
            frames.MDispose();
            return frame;
        }

        private Point3d GetABBCenter(Entity entity)
        {
            var extents = entity.GeometricExtents;
            return extents.MinPoint.GetMidPt(extents.MaxPoint);
        }

        private Polyline GetBlkFrame(BlockReference br)
        {
            // br 是 0 度
            return ToPolyline(br.GeometricExtents);
        }

        private Polyline ToPolyline(Extents3d extents)
        {
            var pts = new Point3dCollection();
            pts.Add(new Point3d(extents.MinPoint.X, extents.MinPoint.Y, 0.0));
            pts.Add(new Point3d(extents.MaxPoint.X, extents.MinPoint.Y, 0.0));
            pts.Add(new Point3d(extents.MaxPoint.X, extents.MaxPoint.Y, 0.0));
            pts.Add(new Point3d(extents.MinPoint.X, extents.MaxPoint.Y, 0.0));
            return pts.CreatePolyline();
        }

        private void AdjustPosition(DBObjectCollection dbTexts)
        {
            // 因为文字旋转后坐标位置会发生变化
            var textGaps = CalculateTextGaps(dbTexts);
            // 把文字旋转到0度,并将文字移动到原点
            dbTexts.OfType<DBText>().ForEach(o => RotateToHorizontalPosition(o));

            // 把所有文字中心移动到原点
            dbTexts.OfType<DBText>().ForEach(o => MoveTo(o,Point3d.Origin));

            // 调整文字间距
            AjustTextGap(dbTexts,textGaps);

            // 调整对齐方式
            if(alignmentIndex == 0)
            {
                AdjustCenterAlignment(dbTexts);
            }
            else if(alignmentIndex < 0)
            {
                AdjustLeftAlignment(dbTexts);
            }
            else
            {
                AdjustRightAlignment(dbTexts);
            }

            // 把整个文字中心放到原点
            MoveCenterToOrigin(dbTexts);

            // 调整文字中心
            dbTexts.OfType<DBText>().ForEach(o =>
            {
                var center = o.GeometricExtents.GetCenter();
                SetTextCenter(o, center);
            });
        }

        private void AjustTextGap(DBObjectCollection horParallelTexts,List<double> textGaps)
        {
            // horParallelTexts的中心都在原点
            // 保持水平位置的相对性，第一个排在最上面
            if (textGaps.Count==0)
            {
                return;
            } 
            for (int i = 1; i < horParallelTexts.Count; i++)
            {
                var current = horParallelTexts[i] as DBText;
                var moveDistance = textGaps[i-1];
                var mt = Matrix3d.Displacement(new Vector3d(0, -1.0*moveDistance, 0));
                current.TransformBy(mt);
            }
        }

        private List<double> CalculateTextGaps(DBObjectCollection parallelTexts)
        {
            // 计算每一个文字距离第一个文字距离
            var results = new List<double>();
            if(parallelTexts.Count==0)
            {
                return results;
            }
            //parallelTexts 位置是相对有序的，且两两之间是平行的
            var first = parallelTexts.OfType<DBText>().First();
            var firstDir = Vector3d.XAxis.RotateBy(first.Rotation, first.Normal);
            var baseSp = first.Position;
            var baseEp = baseSp + firstDir.MultiplyBy(100.0);

            for(int i=1;i< parallelTexts.Count;i++)
            {
                var second = parallelTexts[i] as DBText;
                var projectionPt = second.Position.GetProjectPtOnLine(baseSp, baseEp);
                results.Add(second.Position.DistanceTo(projectionPt));
            }
            return results;
        }

        private void AdjustCenterAlignment(DBObjectCollection dbTexts)
        {
            // dbTexts 都是水平文字
            for (int i = 0; i < dbTexts.Count; i++)
            {
                var current = dbTexts[i] as DBText;
                var currentCenterPt = current.GeometricExtents.GetCenter();
                var mt = Matrix3d.Displacement(new Vector3d(currentCenterPt.X * -1.0, 0, 0));
                current.TransformBy(mt);
            }
        }

        private void AdjustLeftAlignment(DBObjectCollection dbTexts)
        {
            // dbTexts 都是水平文字
            for (int i = 0; i < dbTexts.Count; i++)
            {
                var current = dbTexts[i] as DBText;
                var currentMinPt = current.GeometricExtents.MinPoint;
                var mt = Matrix3d.Displacement(new Vector3d(currentMinPt.X * -1.0, 0, 0));
                current.TransformBy(mt);
            }
        }

        private void AdjustRightAlignment(DBObjectCollection dbTexts)
        {
            //// dbTexts 都是水平文字
            for (int i = 0; i < dbTexts.Count; i++)
            {
                var current = dbTexts[i] as DBText;
                var currentMaxPt = current.GeometricExtents.MaxPoint;
                var mt = Matrix3d.Displacement(new Vector3d(currentMaxPt.X * -1.0, 0, 0));
                current.TransformBy(mt);
            }
        }

        private ObjectId CreateBlock(AcadDatabase acadDb , DBObjectCollection objs,string blkName)
        {
            BlockTable bt = acadDb.Element<BlockTable>(acadDb.Database.BlockTableId, true);
            if (!bt.Has(blkName))
            {
                var btr = new BlockTableRecord()
                {
                    Name = blkName,
                    Explodable = false,
                };
                objs.OfType<Entity>().ForEach(o => btr.AppendEntity(o));
                bt.Add(btr);
                acadDb.Database.TransactionManager.AddNewlyCreatedDBObject(btr, true);
            }
            return bt[blkName];
        }

        private void RotateToHorizontalPosition(DBText text)
        {
            var mt = Matrix3d.Rotation(text.Rotation * -1.0, text.Normal, text.Position);
            text.TransformBy(mt);
        }

        private void SetTextCenter(DBText text,Point3d center)
        {
            text.Position = center;
            text.AlignmentPoint = center;
        }

        private void MoveTo(DBText text, Point3d newCenter)
        {
            var oldCenter = text.GeometricExtents.GetCenter();
            var mt = Matrix3d.Displacement(oldCenter.GetVectorTo(newCenter));
            text.TransformBy(mt);
        }

        private void MoveCenterToOrigin(DBObjectCollection texts)
        {
            var extents = new Extents3d();
            texts.OfType<DBText>().ForEach(o =>
            {
                extents.AddExtents(o.GeometricExtents);
            });
            var oldCenter = extents.GetCenter();
            var newCenter = Point3d.Origin;
            var mt = Matrix3d.Displacement(oldCenter.GetVectorTo(newCenter));
            texts.OfType<DBText>().ForEach(o =>
            {
                o.TransformBy(mt);
            });
        }
    }
}
