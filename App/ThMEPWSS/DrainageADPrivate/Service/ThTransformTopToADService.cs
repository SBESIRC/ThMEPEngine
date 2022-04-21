using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;

using AcHelper;
using NFox.Cad;
using Linq2Acad;
using Dreambuild.AutoCAD;

using ThCADExtension;
using ThCADCore.NTS;
using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore.Diagnostics;
using ThMEPEngineCore.GeojsonExtractor;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.LaneLine;
using NetTopologySuite.Geometries;
using ThMEPWSS.DrainageADPrivate.Model;

namespace ThMEPWSS.DrainageADPrivate.Service
{
    internal class ThTransformTopToADService
    {
        Matrix<double> TransMatrix;
        Matrix<double> ProjectMatrix;
        // Matrix<double> ScaleZMatrix;
        Matrix<double> EnlargeMatrix;


        public ThTransformTopToADService()
        {
            TransMatrix = IdentityTransformMatrix();
            ProjectMatrix = IdentityProjectMatrix();
            //ScaleZMatrix = IdentityScaleZMatrix();
            EnlargeMatrix = IdentityEnlargeMatrix();
        }
        private Matrix<double> IdentityTransformMatrix()
        {
            var shearX = Math.Sqrt(0.5);
            var shearArray = new double[,]
                {
                    { 1,shearX , 0, 0 },
                    { 0, 1, 0, 0 },
                    { 0, 0, 1, 0 },
                    { 0.0, 0.0, 0.0, 1.0}
                };
            var angle = Math.PI / 4;
            var rotateXArray = new double[,]
            {
                {1,0,0,0 },
                {0,Math.Cos (angle),-Math.Sin (angle),0 },
                {0,Math.Sin (angle),Math.Cos(angle),0 },
                {0,0,0,1 }
            };


            var shearMatrix = DenseMatrix.OfArray(shearArray);
            var rotateMatrix = DenseMatrix.OfArray(rotateXArray);

            var transMatrix = rotateMatrix.Inverse() * shearMatrix;

            return transMatrix;
        }

        private Matrix<double> IdentityProjectMatrix()
        {
            var projectArray = new double[,]
                                {
                                    {1,0,0,0 },
                                    {0,1,0,0 },
                                    {0,0,0,0 },
                                    {0,0,0,1 }
                                };
            var projectMatrix = DenseMatrix.OfArray(projectArray);

            return projectMatrix;
        }
        private Matrix<double> IdentityEnlargeMatrix()
        {
            var enlargeScale = 1.5;
            var enlargeArray = new double[,]
                               {
                                        {enlargeScale,0,0,0 },
                                        {0,enlargeScale,0,0 },
                                        {0,0,enlargeScale,0 },
                                        {0,0,0,1 }
                               };


            var enlargeMatrix = DenseMatrix.OfArray(enlargeArray);
            return enlargeMatrix;
        }

        private Matrix<double> IdentityScaleZMatrix()
        {
            double scaleZ = 1 / 3.0;

            var shortZArray = new double[,]
            {
                {1,0,0,0 },
                {0,1,0,0 },
                {0,0,scaleZ,0 },
                {0,0,0,1 }
            };

            var salceZMatrix = DenseMatrix.OfArray(shortZArray);

            return salceZMatrix;
        }

        public void TransformTree(ThDrainageTreeNode node)
        {
            node.TransPt = TransformPt(node.TransPt);
            node.Child.ForEach(x => TransformTree(x));
        }

        public Point3d TransformPt(Point3d pt)
        {

            var ptArray = new double[] { pt.X, pt.Y, pt.Z, 1 };
            var ptVector = DenseVector.OfArray(ptArray);

            //var ptVectorT = TransMatrix * (ScaleZMatrix * ptVector) * ProjectMatrix;
            var ptVectorT = TransMatrix * ptVector * EnlargeMatrix * ProjectMatrix;

            var pointNew = new Point3d(ptVectorT[0], ptVectorT[1], ptVectorT[2]);

            return pointNew;
        }


        public static void TransPtNormalZ(ThDrainageTreeNode node)
        {
            TransPtNormalZNode(node);
            node.Child.ForEach(x => TransPtNormalZ(x));
        }
        private static void TransPtNormalZNode(ThDrainageTreeNode node)
        {
            if (node.Parent != null)
            {
                node.TransPt = node.Pt;
                double deltaZ = node.Parent.Pt.Z - node.Pt.Z;
                if (Math.Abs(deltaZ) >= 10)//立管
                {
                    double newZ = node.Parent.TransPt.Z - deltaZ / Math.Abs(deltaZ) * 1000;
                    node.TransPt = new Point3d(node.TransPt.X, node.TransPt.Y, newZ);
                }
                else
                {
                    //检查水平管，前面的管z值有变化
                    double deltaTransZ = node.Parent.TransPt.Z - node.TransPt.Z;
                    if (Math.Abs(deltaTransZ) >= 0.1)
                    {
                        node.TransPt = new Point3d(node.TransPt.X, node.TransPt.Y, node.Parent.TransPt.Z);
                    }
                }
            }
        }

    }
}
