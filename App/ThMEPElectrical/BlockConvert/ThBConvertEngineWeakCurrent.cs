﻿using System;
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
        public override ObjectId Insert(string name, Scale3d scale, ThBlockReferenceData srcBlockData)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                return acadDatabase.ModelSpace.ObjectId.InsertBlockReference(
                    "0",
                    name,
                    Point3d.Origin,
                    scale,
                    0.0,
                    new Dictionary<string, string>(srcBlockData.Attributes));
            }
        }

        public override void MatchProperties(ThBlockReferenceData targetBlockData, ThBlockReferenceData srcBlockData)
        {
            //
        }

        public override void SetDatabaseProperties(ThBlockReferenceData targetBlockData, string layer)
        {
            using (var acadDatabase = AcadDatabase.Use(targetBlockData.Database))
            {
                ThBConvertDbUtils.UpdateLayerSettings(layer);
                var block = acadDatabase.Element<Entity>(targetBlockData.ObjId, true);
                block.Layer = layer;
            }
        }

        public override void SetVisibilityState(ThBlockReferenceData targetBlockData, ThBlockReferenceData srcBlockData)
        {
            //
        }

        /// <summary>
        /// 分别计算源块与目标块的插入点，并移动
        /// </summary>
        /// <param name="targetBlockData"></param>
        /// <param name="srcBlockData"></param>
        public override void Displacement(ThBlockReferenceData targetBlockData, ThBlockReferenceData srcBlockData)
        {
            using (var acadDatabase = AcadDatabase.Use(targetBlockData.Database))
            {
                var blockReference = acadDatabase.Element<BlockReference>(targetBlockData.ObjId, true);
                var targetBlockDataPosition = targetBlockData.GetNewBasePoint();
                var srcBlockDataPosition = srcBlockData.GetNewBasePoint().TransformBy(srcBlockData.OwnerSpace2WCS);
                var offset = targetBlockDataPosition.GetVectorTo(srcBlockDataPosition);
                blockReference.TransformBy(Matrix3d.Displacement(offset));
            }
        }

        public override void Rotate(ThBlockReferenceData targetBlockData, ThBlockReferenceData srcBlockData)
        {
            using (var acadDatabase = AcadDatabase.Use(targetBlockData.Database))
            {
                var blockReference = acadDatabase.Element<BlockReference>(targetBlockData.ObjId, true);
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

        public override void Mirror(ThBlockReferenceData targetBlockData, ThBlockReferenceData srcBlockData)
        {
            using (var acadDatabase = AcadDatabase.Use(targetBlockData.Database))
            {
                var blockReference = acadDatabase.Element<BlockReference>(targetBlockData.ObjId, true);
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

        public override void Displacement(ThBlockReferenceData targetBlockData, ThBlockReferenceData srcBlockData, List<ThRawIfcDistributionElementData> list, Scale3d scale)
        {
            //
        }

        public override void SpecialTreatment(ThBlockReferenceData targetBlockData, ThBlockReferenceData srcBlockData)
        {
            //
        }
    }
}
