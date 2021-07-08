using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPWSS.DrainageSystemDiagram
{
    public class ThDrainageSDPositionDimEngine
    {
        public static void positionDimTry(ThDrainageSDTreeNode root)

        {
            var p1 = root.Node;
            var p2 = root.Child.First().Child.First().Child.First().Node;
            var c = new Point3d((p1.X + p2.X) * 0.5, (p1.Y + p2.Y) * 0.5, 0);
            var c2 = new Point3d(p2.X, p1.Y, 0);

            //Dreambuild.AutoCAD.DbHelper.GetDimstyleId(style, adb.Database);

            var dim = new AlignedDimension();
            dim.XLine1Point = p1;
            dim.XLine2Point = p2;
            dim.DimLinePoint = c;
            dim.HorizontalRotation = Vector3d.XAxis.GetAngleTo(Vector3d.YAxis);
            double dist = p1.DistanceTo(p2);
            dim.DimensionText = dist.ToString();

            DrawUtils.ShowGeometry(dim, "l0dimTest", 3);
            DrawUtils.ShowGeometry(c, "l0dimTest", 3, 25, 20);


            var dim2 = new RotatedDimension();
            dim2.XLine1Point = p1;
            dim2.XLine2Point = p2;
            dim2.DimLinePoint = c;
            DrawUtils.ShowGeometry(dim2, "l0dimTest", 3);
            DrawUtils.ShowGeometry(c2, "l0dimTest", 3, 25, 20);



        }

        public static List<RotatedDimension > getPositionDim(ThDrainageSDDataExchange dataset)
        {
            var positionDim = new List<RotatedDimension>();

            var groupList = dataset.GroupList;








            return positionDim;
        }

    }
}
