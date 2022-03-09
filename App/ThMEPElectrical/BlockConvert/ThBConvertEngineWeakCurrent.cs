using System;
using Linq2Acad;
using DotNetARX;
using ThCADExtension;
using ThMEPEngineCore.Engine;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPElectrical.BlockConvert
{
    public class ThBConvertEngineWeakCurrent : ThBConvertEngine
    {
        public override ObjectId Insert(string name, Scale3d scale, ThBlockReferenceData srcBlockReference)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                return acadDatabase.ModelSpace.ObjectId.InsertBlockReference(
                    "0",
                    name,
                    Point3d.Origin,
                    scale,
                    0.0,
                    new Dictionary<string, string>(srcBlockReference.Attributes));
            }
        }

        public override void MatchProperties(ObjectId blkRef, ThBlockReferenceData srcBlockReference)
        {
            //
        }

        public override void SetDatabaseProperties(ObjectId blkRef, ThBlockReferenceData srcBlockReference, string layer)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                ThBConvertDbUtils.UpdateLayerSettings(layer);
                var block = acadDatabase.Element<Entity>(blkRef, true);
                block.Layer = layer;
            }
        }

        public override void SetVisibilityState(ObjectId blkRef, ThBlockReferenceData srcBlockReference)
        {
            //
        }

        public override void Displacement(ObjectId blkRef, ThBlockReferenceData srcBlockData, Tuple<ThBConvertInsertMode, string> insertMode)
        {
            if(insertMode.Item1 == ThBConvertInsertMode.OBBCenter)
            {
                TransformByCenter(blkRef, srcBlockData, insertMode.Item2);
            }
            else if(insertMode.Item1 == ThBConvertInsertMode.BottomCenter)
            {
                TransformHYDT(blkRef, srcBlockData);
            }
        }

        /// <summary>
        /// 按几何中心调整位置
        /// </summary>
        /// <param name="blkRef"></param>
        /// <param name="srcBlockData"></param>
        private void TransformByCenter(ObjectId blkRef, ThBlockReferenceData srcBlockData, string geometryLayer)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Use(blkRef.Database))
            {
                var blockReference = acadDatabase.Element<BlockReference>(blkRef, true);
                var targetBlockData = new ThBlockReferenceData(blkRef);
                var targetCentriodPoint = targetBlockData.GetCentroidPoint().TransformBy(targetBlockData.OwnerSpace2WCS);
                var scrCentriodPoint = srcBlockData.GetCentroidPoint(geometryLayer).TransformBy(srcBlockData.OwnerSpace2WCS);
                var offset = targetCentriodPoint.GetVectorTo(scrCentriodPoint);
                blockReference.TransformBy(Matrix3d.Displacement(offset));
            }
        }

        /// <summary>
        /// 按位置点调整位置
        /// </summary>
        /// <param name="blkRef"></param>
        /// <param name="srcBlockData"></param>
        private void TransformByPosition(ObjectId blkRef, ThBlockReferenceData srcBlockData)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Use(blkRef.Database))
            {
                var blockReference = acadDatabase.Element<BlockReference>(blkRef, true);
                var targetBlockData = new ThBlockReferenceData(blkRef);
                var targetMCS2WCS = targetBlockData.BlockTransform.PreMultiplyBy(targetBlockData.OwnerSpace2WCS);
                var targetBlockDataPosition = Point3d.Origin.TransformBy(targetMCS2WCS);
                var targetCentriodPoint = targetBlockData.GetCentroidPoint().TransformBy(targetBlockData.OwnerSpace2WCS);
                var targetPoint = targetCentriodPoint.DistanceTo(targetBlockDataPosition) < 2000
                    ? targetBlockDataPosition : targetCentriodPoint;
                var srcMCS2WCS = srcBlockData.BlockTransform.PreMultiplyBy(srcBlockData.OwnerSpace2WCS);
                var srcBlockDataPosition = Point3d.Origin.TransformBy(srcMCS2WCS);
                var srcCentriodPoint = srcBlockData.GetCentroidPoint().TransformBy(srcBlockData.OwnerSpace2WCS);
                var srcPoint = srcCentriodPoint.DistanceTo(srcBlockDataPosition) < 2000
                    ? srcBlockDataPosition : srcCentriodPoint;
                var offset = targetPoint.GetVectorTo(srcPoint);
                blockReference.TransformBy(Matrix3d.Displacement(offset));
            }
        }

        private void TransformHYDT(ObjectId blkRef, ThBlockReferenceData srcBlockData)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Use(blkRef.Database))
            {
                var blockReference = acadDatabase.Element<BlockReference>(blkRef, true);
                var targetBlockData = new ThBlockReferenceData(blkRef);
                var targetCentriodPoint = targetBlockData.GetBottomCenter().TransformBy(targetBlockData.OwnerSpace2WCS);
                var bottomCenter = srcBlockData.GetBottomCenter() - srcBlockData.Position;
                var bottomCenterTiadl = bottomCenter.X * srcBlockData.OwnerSpace2WCS.CoordinateSystem3d.Xaxis
                    + bottomCenter.Y * srcBlockData.OwnerSpace2WCS.CoordinateSystem3d.Yaxis
                    + bottomCenter.Z * srcBlockData.OwnerSpace2WCS.CoordinateSystem3d.Zaxis;
                var tranMatrix = Matrix3d.Displacement(
                    srcBlockData.BlockTransform.Translation.X * srcBlockData.OwnerSpace2WCS.CoordinateSystem3d.Xaxis
                    + srcBlockData.BlockTransform.Translation.Y * srcBlockData.OwnerSpace2WCS.CoordinateSystem3d.Yaxis
                    + srcBlockData.BlockTransform.Translation.Z * srcBlockData.OwnerSpace2WCS.CoordinateSystem3d.Zaxis);
                //var tranMatrix = Matrix3d.Displacement(srcBlockData.OwnerSpace2WCS.Translation);
                var scrCentriodPoint = Point3d.Origin
                        .TransformBy(tranMatrix.PostMultiplyBy(srcBlockData.OwnerSpace2WCS));
                var offset = targetCentriodPoint.GetVectorTo(scrCentriodPoint) + bottomCenterTiadl;
                blockReference.TransformBy(Matrix3d.Displacement(offset));
            }
        }

        public override void Rotate(ObjectId blkRef, ThBlockReferenceData srcBlockData)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var blockReference = acadDatabase.Element<BlockReference>(blkRef, true);
                var position = srcBlockData.Position;
                var rotation = srcBlockData.Rotation;

                srcBlockData.OwnerSpace2WCS.Decompose(out _, out var rotate, out _, out _);

                if (srcBlockData.Normal == new Vector3d(0, 0, -1))
                {
                    rotation = -rotation;
                }
                if (srcBlockData.EffectiveName.Contains("室内消火栓平面"))
                {
                    blockReference.TransformBy(Matrix3d.Rotation(rotation, Vector3d.ZAxis, position)
                        .PreMultiplyBy(rotate));
                }
                else
                {
                    if (rotation > Math.PI / 2 && rotation - 10 * ThBConvertCommon.radian_tolerance <= Math.PI * 3 / 2)
                    {
                        blockReference.TransformBy(Matrix3d.Rotation(rotation - Math.PI, Vector3d.ZAxis, position)
                            .PostMultiplyBy(rotate));
                    }
                    else
                    {
                        blockReference.TransformBy(Matrix3d.Rotation(rotation, Vector3d.ZAxis, position)
                            .PostMultiplyBy(rotate));
                    }
                }
            }
        }

        public override void Mirror(ObjectId blkRef, ThBlockReferenceData srcBlockData)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var blockReference = acadDatabase.Element<BlockReference>(blkRef, true);
                var targetBlockData = new ThBlockReferenceData(blkRef);
                var targetScale = targetBlockData.ScaleFactors;
                var srcScale = srcBlockData.ScaleFactors;
                var scale = new Scale3d(targetScale.X * srcScale.X, targetScale.Y * srcScale.Y, targetScale.Z * srcScale.Z);
                var mirror = Matrix3d.Identity;
                if (scale.X < 0)
                {
                    if (scale.Y < 0)
                    {
                        if (scale.Z < 0)  //x<0,y<0,z<0
                        {
                            mirror = Matrix3d.Mirroring(Point3d.Origin);
                        }
                        else  //x<0,y<0,z>0
                        {
                            mirror = Matrix3d.Mirroring(new Line3d(Point3d.Origin, new Point3d(0, 0, 1)));
                        }
                    }
                    else
                    {
                        if (scale.Z < 0)  //x<0,y>0,z<0
                        {
                            mirror = Matrix3d.Mirroring(new Line3d(Point3d.Origin, new Point3d(0, 1, 0)));
                        }
                        else  //x<0,y>0,z>0
                        {
                            mirror = Matrix3d.Mirroring(new Plane(Point3d.Origin, new Vector3d(0, 1, 0), new Vector3d(0, 0, 1)));
                        }
                    }
                }
                else
                {
                    if (scale.Y < 0)
                    {
                        if (scale.Z < 0)  //x>0,y<0,z<0
                        {
                            mirror = Matrix3d.Mirroring(new Line3d(Point3d.Origin, new Point3d(1, 0, 0)));
                        }
                        else  //x>0,y<0,z>0
                        {
                            mirror = Matrix3d.Mirroring(new Plane(Point3d.Origin, new Vector3d(1, 0, 0), new Vector3d(0, 0, 1)));
                        }
                    }
                    else
                    {
                        if (scale.Z < 0)  //x>0,y>0,z<0
                        {
                            mirror = Matrix3d.Mirroring(new Plane(Point3d.Origin, new Vector3d(1, 0, 0), new Vector3d(0, 1, 0)));
                        }
                        else  //x>0,y>0,z>0
                        {
                            //
                        }
                    }
                }

                srcBlockData.OwnerSpace2WCS.Decompose(out _, out _, out var UCSMirror, out _);
                blockReference.TransformBy(mirror.PreMultiplyBy(UCSMirror));
            }
        }

        public override void Displacement(ObjectId blkRef, ThBlockReferenceData srcBlockReference, List<ThRawIfcDistributionElementData> list, Scale3d scale)
        {
            //
        }
    }
}
