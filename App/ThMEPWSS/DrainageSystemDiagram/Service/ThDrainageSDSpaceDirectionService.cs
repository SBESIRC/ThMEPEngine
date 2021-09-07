using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using AcHelper;
using NFox.Cad;
using Linq2Acad;
using Dreambuild.AutoCAD;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

using ThCADExtension;
using ThCADCore.NTS;
using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore.GeojsonExtractor;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.LaneLine;
using NetTopologySuite.Geometries;


namespace ThMEPWSS.DrainageSystemDiagram
{
    public class ThDrainageSDSpaceDirectionService
    {
        public static double findSpaceDirection(Polyline roomBoundary)
        {
            double finalAnalge = 0;
            var b = roomBoundary.CalObb();

            DrawUtils.ShowGeometry(b, "l0boundary", 140);
            DrawUtils.ShowGeometry(b.GetPoint3dAt(0), "l0boundary", 140);
            DrawUtils.ShowGeometry(b.GetPoint3dAt(1), "l0boundary", 240);
            DrawUtils.ShowGeometry(b.GetPoint3dAt(2), "l0boundary", 72);


            var dir = (b.GetPoint3dAt(1) - b.GetPoint3dAt(0)).GetNormal();
            Vector3d toX = Vector3d.XAxis;
            Vector3d toY = Vector3d.YAxis;

            if (Math.Abs(dir.X) >= 0.99 || Math.Abs(dir.Y) >= 0.99)
            {
                finalAnalge = 0;
            }
            if (dir.X > 0 && dir.Y > 0)
            {

            }
            if (dir.X < 0 && dir.Y > 0)
            {
                toX = -toX;
            }
            if (dir.X < 0 && dir.Y < 0)
            {
                toX = -toX;
                toY = -toY;
            }
            if (dir.X > 0 && dir.Y < 0)
            {
                toY = -toY;
            }
            var angleX = dir.GetAngleTo(toX);
            var angleY = dir.GetAngleTo(toY);

            finalAnalge = angleX > angleY ? angleY : angleX;

            return finalAnalge;


        }


        public static Matrix3d getMatrix(Vector3d xVector, Point3d basePt)
        {
            var turnAngel = xVector.GetAngleTo(Vector3d.XAxis, -Vector3d.ZAxis);

            if (90 * Math.PI / 180 < turnAngel && turnAngel < 270 * Math.PI / 180)
            {
                turnAngel = (-xVector).GetAngleTo(Vector3d.XAxis, -Vector3d.ZAxis);
            }


            var turnMatrix = Matrix3d.Rotation(turnAngel, Vector3d.ZAxis, basePt);
            return turnMatrix;
        }

    }
}
