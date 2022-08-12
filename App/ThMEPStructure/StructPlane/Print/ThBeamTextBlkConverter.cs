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
using ThMEPStructure.StructPlane.Service;

namespace ThMEPStructure.StructPlane.Print
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

        public ObjectIdCollection Convert(Database database,List<DBObjectCollection> beamTextObjs)
        {
            CreateBlock(database, beamTextObjs);
            return InsertBlock(database,beamTextObjs);
        }

        private ObjectIdCollection InsertBlock(Database database, List<DBObjectCollection> beamTextObjs)
        {
            using (var acadDb = AcadDatabase.Use(database))
            {
                var results = new ObjectIdCollection();
                beamTextObjs.Where(o=>o.Count>0.0).ForEach(o =>
                {
                    var blkName = o.GetMultiTextString();
                    var blkId = acadDb.CurrentSpace.ObjectId.InsertBlockReference(
                        ThPrintLayerManager.BeamTextLayerName, blkName, Point3d.Origin, new Scale3d(1.0), 0.0);
                    var blkObj = acadDb.Element<BlockReference>(blkId,true);
                    
                    // 把块中心放置在原点
                    MoveToCenter(blkObj);

                    var blkFrame = GetBlkFrame(blkObj);

                    // 调整块的角度
                    var blkCenter = GetABBCenter(blkObj);
                    var rotation = o.OfType<DBText>().First().Rotation;
                    var rotateMt = Matrix3d.Rotation(rotation, Vector3d.ZAxis, blkCenter);
                    blkObj.TransformBy(rotateMt);
                    blkFrame.TransformBy(rotateMt);
                    var blkFrameCenter = GetFrameCenter(blkFrame);

                    // 让块的OBB 和 o中装的文字的OBB对齐
                    var textGroupFrame = GetParallelTextsFrame(o);
                    var textGroupCenter = GetFrameCenter(textGroupFrame);

                    // 把块移动到文字组的中心
                    
                    var moveMt = Matrix3d.Displacement(blkFrameCenter.GetVectorTo(textGroupCenter));
                    blkObj.TransformBy(moveMt);

                    results.Add(blkId);
                    blkFrame.Dispose();
                    textGroupFrame.Dispose();
                });
                return results;
            }
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

        private void MoveToCenter(BlockReference br)
        {
            var center = GetABBCenter(br);
            var mt = Matrix3d.Displacement(center.GetVectorTo(Point3d.Origin));
            br.TransformBy(mt);
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

        private void CreateBlock(Database db, List<DBObjectCollection> beamTextObjs)
        {
            beamTextObjs.Where(o=>o.Count>0).ForEach(o =>
            {
                // o 中的文字都是平行的
                var clones = o.Clone();
                var blkName = clones.GetMultiTextString();
                if(!string.IsNullOrEmpty(blkName))
                {
                    AdjustPosition(clones);
                    CreateBlock(db, clones, blkName);
                }
            });
        }

        private void AdjustPosition(DBObjectCollection dbTexts)
        {
            // 因为文字旋转后坐标位置会发生变化
            var textGaps = CalculateTextGaps(dbTexts);
            // 把文字旋转到0度,并将文字移动到原点
            dbTexts.OfType<DBText>().ForEach(o => RotateToHorizontalPosition(o));

            // 调整文字间距
            AjustTextGap(dbTexts,textGaps);

            var bottomPos = dbTexts.OfType<DBText>().OrderBy(o => o.Position.Y).First().GeometricExtents.MinPoint;
            var mt = Matrix3d.Displacement(bottomPos.GetVectorTo(Point3d.Origin));
            dbTexts.OfType<DBText>().ForEach(o => o.TransformBy(mt));
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
        }

        private void AjustTextGap(DBObjectCollection horParallelTexts,List<double> textGaps)
        {
            // 保持水平位置的相对性，第一个排在最上面
            if(textGaps.Count==0)
            {
                return;
            }
            var first = horParallelTexts.OfType<DBText>().First();   
            for (int i = 1; i < horParallelTexts.Count; i++)
            {
                var second = horParallelTexts[i] as DBText;
                var oldGap = textGaps[i - 1];
                var newSecondY = first.Position.Y - oldGap;
                var moveVec = new Vector3d(0, newSecondY - second.Position.Y, 0);
                var mt = Matrix3d.Displacement(moveVec);
                second.TransformBy(mt);
                first = second;       
            }
        }

        private List<double> CalculateTextGaps(DBObjectCollection parallelTexts)
        {
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
            var bottomText = dbTexts.OfType<DBText>().OrderBy(o => o.Position.Y).First();
            var bottomCenter = bottomText.GeometricExtents.GetCenter();
            for (int i=0;i<dbTexts.Count;i++)
            {
                var current = dbTexts[i] as DBText;
                var currentCenter = current.GeometricExtents.GetCenter();
                var xMinus = bottomCenter.X - currentCenter.X;
                var mt = Matrix3d.Displacement(new Vector3d(xMinus,0,0));
                current.TransformBy(mt);
            }
        }

        private void AdjustLeftAlignment(DBObjectCollection dbTexts)
        {
            // dbTexts 都是水平文字
            var bottomText = dbTexts.OfType<DBText>().OrderBy(o => o.Position.Y).First();
            var bottomMinPt = bottomText.GeometricExtents.MinPoint;
            for (int i = 0; i < dbTexts.Count; i++)
            {
                var current = dbTexts[i] as DBText;
                var currentMinPt = current.GeometricExtents.MinPoint;
                var xMinus = bottomMinPt.X - currentMinPt.X;
                var mt = Matrix3d.Displacement(new Vector3d(xMinus, 0, 0));
                current.TransformBy(mt);
            }
        }

        private void AdjustRightAlignment(DBObjectCollection dbTexts)
        {
            // dbTexts 都是水平文字
            var bottomText = dbTexts.OfType<DBText>().OrderBy(o => o.Position.Y).First();
            var bottomMaxPt = bottomText.GeometricExtents.MaxPoint;
            for (int i = 0; i < dbTexts.Count; i++)
            {
                var current = dbTexts[i] as DBText;
                var currentMaxPt = current.GeometricExtents.MaxPoint;
                var xMinus = bottomMaxPt.X - currentMaxPt.X;
                var mt = Matrix3d.Displacement(new Vector3d(xMinus, 0, 0));
                current.TransformBy(mt);
            }
        }

        private ObjectId CreateBlock(Database database , DBObjectCollection objs,string blkName)
        {
            using (var acadDb = AcadDatabase.Use(database))
            {
                BlockTable bt = acadDb.Element<BlockTable>(acadDb.Database.BlockTableId, true);
                if (!bt.Has(blkName))
                {
                    var btr = new BlockTableRecord()
                    {
                        Name = blkName,
                        Explodable =false,
                    };
                    objs.OfType<Entity>().ForEach(o => btr.AppendEntity(o));
                    bt.Add(btr);
                    acadDb.Database.TransactionManager.AddNewlyCreatedDBObject(btr, true);
                }                
                return bt[blkName];
            }
        }

        private void RotateToHorizontalPosition(DBText text)
        {
            var mt = Matrix3d.Rotation(text.Rotation * -1.0, text.Normal, text.Position);
            text.TransformBy(mt);
        }
    }
}
