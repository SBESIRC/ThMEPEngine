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

namespace ThMEPWSS.DrainageADPrivate.Service
{
    internal class TransformTopToADService
    {
        Matrix<double> TransMatrix;
        Matrix<double> ProjectMatrix;
        Matrix<double> ScaleZMatrix;

        public TransformTopToADService()
        {
            TransMatrix = IdentityTransformMatrix();
            ProjectMatrix = IdentityProjectMatrix();
            ScaleZMatrix = IdentityScaleZMatrix();
        }

        public Line TransformLine(Line line)
        {
            var transPts = TransformPt(line.StartPoint);
            var transPte = TransformPt(line.EndPoint);

            var newLine = new Line(transPts, transPte);

            return newLine;
        }

        private Point3d TransformPt(Point3d pt)
        {

            var ptArray = new double[] { pt.X, pt.Y, pt.Z, 1 };
            var ptVector = DenseVector.OfArray(ptArray);

            var ptVectorT = TransMatrix * (ScaleZMatrix * ptVector) * ProjectMatrix;

            var pointNew = new Point3d(ptVectorT[0], ptVectorT[1], ptVectorT[2]);

            return pointNew;
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


    }
}
