﻿using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThCADExtension
{
    public static class ThBlockReferenceTool
    {
        /// <summary>
        /// 获取块引用的变换矩阵
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public static Matrix3d GetBlockTransform(this ObjectId id)
        {
            BlockReference bref = id.GetObject(OpenMode.ForRead) as BlockReference;
            if (bref != null)//如果是块参照
                return bref.BlockTransform;
            else
                return Matrix3d.Identity;
        }

        /// <summary>
        /// 获取块引用的旋转角度
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public static double GetBlockRotation(this ObjectId id)
        {
            BlockReference bref = id.GetObject(OpenMode.ForRead) as BlockReference;
            if (bref != null)//如果是块参照
                return bref.Rotation;
            else
                return 0.0;
        }

        /// <summary>
        /// 获取块引用的正方向
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public static Vector3d GetBlockNormal(this ObjectId id)
        {
            BlockReference bref = id.GetObject(OpenMode.ForRead) as BlockReference;
            if (bref != null)//如果是块参照
                return bref.Normal;
            else
                return new Vector3d(0, 0, 1);
        }

        public static Point3d GetBlockPosition(this ObjectId id)
        {
            BlockReference bref = id.GetObject(OpenMode.ForRead) as BlockReference;
            if (bref != null)//如果是块参照
                return bref.Position;
            else
                return Point3d.Origin;
        }

        public static Scale3d GetScaleFactors(this ObjectId id)
        {
            BlockReference bref = id.GetObject(OpenMode.ForRead) as BlockReference;
            if (bref != null)//如果是块参照
                return bref.ScaleFactors;
            else
                return new Scale3d();
        }

        public static string GetBlockLayer(this ObjectId id)
        {
            BlockReference bref = id.GetObject(OpenMode.ForRead) as BlockReference;
            if (bref != null)//如果是块参照
                return bref.Layer;
            else
                return "0";
        }

        public static Extents3d GetBlockGeometryExtents(this ObjectId id)
        {
            try
            {
                var bref = id.GetObject(OpenMode.ForRead) as BlockReference;
                return bref.GeometryExtentsBestFit();
            }
            catch
            {
                return new Extents3d();
            }
        }

        public static Extents3d GetBlockGeometryExtents(this ObjectId id, Matrix3d parentTransform)
        {
            try
            {
                var bref = id.GetObject(OpenMode.ForRead) as BlockReference;
                return bref.GeometryExtentsBestFit(parentTransform);
            }
            catch
            {
                return new Extents3d();
            }
        }
    }
}
