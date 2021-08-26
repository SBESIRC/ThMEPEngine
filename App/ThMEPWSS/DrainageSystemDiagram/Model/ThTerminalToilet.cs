using System;
using System.Collections.Generic;

using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

using ThCADExtension;

namespace ThMEPWSS.DrainageSystemDiagram
{
    public class ThTerminalToilet
    {
        public BlockReference blk { get; set; }
        public string Uuid { get; set; }
        public string Type { get; set; }
        public List<Point3d> SupplyCool { get; set; }
        public List<Point3d> SupplyCoolOnWall { get; set; }
        public List<Point3d> SupplyCoolOnBranch { get; set; }

        public Point3d SupplyWarm { get; set; }
        public Point3d SupplyWarmSec { get; set; }
        public Point3d Sewage { get; set; }
        public Point3d SewageSec { get; set; }
        public Polyline Boundary { get; set; }
        public Vector3d Dir { get; set; }
        public string GroupId { get; set; }
        public string AreaId { get; set; }

        public ThTerminalToilet(Entity geometry, string blkName)
        {
            var geom = geometry as BlockReference;
            blk = geom.Clone() as BlockReference;
            Boundary = geom.ToOBB(geom.BlockTransform.PreMultiplyBy(Matrix3d.Identity));

            Type = blkName;
            SupplyCool = new List<Point3d>();
            SupplyCoolOnWall = new List<Point3d>();
            SupplyCoolOnBranch = new List<Point3d>();
            GroupId = "";
            AreaId = "";

            Uuid = Guid.NewGuid().ToString();

            Boundary = turnBoundary(Boundary, Type);
            if (blk.ScaleFactors.X * blk.ScaleFactors.Y < 0)
            {
                Boundary.ReverseCurve();
            }
            Dir = (Boundary.GetPoint3dAt(0) - Boundary.GetPoint3dAt(1)).GetNormal();

            setInfo();

            //Point3d leftBPt = Boundary.GetPoint3dAt(0);
            //Point3d leftPt = Boundary.GetPoint3dAt(1);
            //Point3d rightPt = Boundary.GetPoint3dAt(2);
            //Point3d rightPt2 = Boundary.GetPoint3dAt(3);

            //DrawUtils.ShowGeometry(leftBPt, "0", "l0bounary", 70, 25, 20);
            //DrawUtils.ShowGeometry(leftPt, "1", "l0bounary", 30, 25, 20);
            //DrawUtils.ShowGeometry(rightPt, "2", "l0bounary", 213, 25, 20);
            //DrawUtils.ShowGeometry(rightPt2, "3", "l0bounary", 152, 25, 20);

            //DrawUtils.ShowGeometry(leftBPt, "l0bounary", 70, 25, 20);
            //DrawUtils.ShowGeometry(leftPt, "l0bounary", 30, 25, 20);
            //DrawUtils.ShowGeometry(rightPt, "l0bounary", 213, 25, 20);
            //DrawUtils.ShowGeometry(rightPt2, "l0bounary", 152, 25, 20);

            //SupplyCool.ForEach(x => DrawUtils.ShowGeometry(x, "l0coolPt", 130, 30, 20, "X"));
        }

        public void setInfo()
        {
            SupplyCool.Clear();

            SupplyCool.AddRange(CalculateSupplyCoolPoint());
            SupplyCool.AddRange(CalculateSupplyCoolSecPoint());

            SupplyWarm = CalculateSupplyWarmPoint();
            SupplyWarmSec = CalculateSupplyWarmSecPoint();
            Sewage = CalculateSewagePoint();
            SewageSec = CalculateSewageSecPoint();

        }

        private List<Point3d> CalculateSupplyCoolPoint()
        {
            List<Point3d> returnPt = new List<Point3d>();
            Point3d pt = new Point3d();

            switch (Type)
            {
                case "A-Toilet-1":
                case "A-Toilet-4":
                case "A-Toilet-6":
                case "A-Toilet-8":
                case "A-Kitchen-3":
                case "A-Kitchen-4":
                    pt = getSupplyPtByCenter(ThDrainageSDCommon.supplyCoolDalta75);
                    break;

                case "A-Toilet-2":
                    pt = getSupplyPtDoubleSinkLeft();
                    break;
                case "A-Toilet-3":
                    pt = getSupplyPtByLeftTop(ThDrainageSDCommon.supplyCoolDalta308);
                    break;

                case "小便器":
                case "A-Kitchen-9":
                case "儿童小便器":
                    pt = getSupplyPtByCenter(ThDrainageSDCommon.supplyCoolDalta0);
                    break;

                case "A-Toilet-5":
                    pt = getSupplyPtByCenter(ThDrainageSDCommon.supplyCoolDalta200);
                    break;

                case "儿童坐便器":
                    pt = getSupplyPtByCenter(ThDrainageSDCommon.supplyCoolDalta250);
                    break;

                case "儿童洗脸盆":
                    pt = getSupplyPtByCenter(ThDrainageSDCommon.supplyCoolDalta150);
                    break;

                case "蹲便器":
                    pt = getSupplyPtByCenter(ThDrainageSDCommon.supplyCoolDalta120);
                    break;

                case "A-Toilet-7":
                    pt = getSupplyPtByLeftTop(ThDrainageSDCommon.supplyCoolDalta350);
                    break;

                case "A-Toilet-9":
                    pt = getSupplyPtByLeftTop(ThDrainageSDCommon.supplyCoolDalta150);
                    break;

                default:

                    break;

            }

            if (pt != Point3d.Origin)
            {
                returnPt.Add(pt);
            }

            return returnPt;

        }

        private Point3d getSupplyPtByCenter(double dalta)
        {
            Point3d leftPt = Boundary.GetPoint3dAt(1);
            Point3d rightPt = Boundary.GetPoint3dAt(2);

            Point3d cenPt = new Point3d((leftPt.X + rightPt.X) / 2, (leftPt.Y + rightPt.Y) / 2, 0);

            var dir = (rightPt - leftPt).GetNormal();

            double coorX = cenPt.X + (dir * dalta).X;
            double coorY = cenPt.Y + (dir * dalta).Y;

            Point3d supplyPt = new Point3d(coorX, coorY, 0);

            return supplyPt;
        }

        private Point3d getSupplyPtByLeftTop(double dalta)
        {
            Point3d leftPt = Boundary.GetPoint3dAt(1);
            Point3d rightPt = Boundary.GetPoint3dAt(2);

            var dir = (rightPt - leftPt).GetNormal();

            double coorX = leftPt.X + (dir * dalta).X;
            double coorY = leftPt.Y + (dir * dalta).Y;

            Point3d supplyPt = new Point3d(coorX, coorY, 0);

            return supplyPt;
        }

        private Point3d getSupplyPtDoubleSinkLeft()
        {
            Point3d leftPt = Boundary.GetPoint3dAt(1);
            Point3d rightPt = Boundary.GetPoint3dAt(2);

            var dir = (rightPt - leftPt).GetNormal();

            double length = (rightPt - leftPt).Length;
            double dalta = length / ThDrainageSDCommon.supplyCoolDaltaDoubleSinkLeftParameter + ThDrainageSDCommon.supplyCoolDalta75;

            double coorX = leftPt.X + (dir * dalta).X;
            double coorY = leftPt.Y + (dir * dalta).Y;

            Point3d supplyPt = new Point3d(coorX, coorY, 0);

            return supplyPt;
        }

        private Point3d getSupplyPtDoubleSinkRight()
        {
            Point3d leftPt = Boundary.GetPoint3dAt(1);
            Point3d rightPt = Boundary.GetPoint3dAt(2);

            var dir = (rightPt - leftPt).GetNormal();

            double length = (rightPt - leftPt).Length;
            double dalta = length / (4.0 / 3.0) + ThDrainageSDCommon.supplyCoolDalta75;

            double coorX = leftPt.X + (dir * dalta).X;
            double coorY = leftPt.Y + (dir * dalta).Y;

            Point3d supplyPt = new Point3d(coorX, coorY, 0);

            return supplyPt;
        }

        private static Polyline turnBoundary(Polyline boundary, string type)
        {
            int turn = 0;
            //Boundary
            switch (type)
            {
                case "A-Toilet-5":
                case "A-Toilet-9":
                case "蹲便器":
                    turn = 2;
                    break;

                case "A-Toilet-6":
                case "A-Toilet-8":
                    turn = 3;
                    break;
                default:
                    break;
            }

            Polyline boundaryNew = turnBoundary(boundary, turn);

            return boundaryNew;

        }

        public static Polyline turnBoundary(Polyline boundary, int turn)
        {
            //
            Polyline boundaryNew = boundary.Clone() as Polyline;
            if (turn != 0)
            {
                for (int i = 0; i < boundary.NumberOfVertices; i++)
                {
                    boundaryNew.SetPointAt(i, boundary.GetPoint3dAt((i + turn) % boundary.NumberOfVertices).ToPoint2D());
                }
            }
            return boundaryNew;
        }

        private Point3d CalculateSupplyWarmPoint()
        {
            Point3d pt = new Point3d();
            // "给水角阀平面"
            return pt;

        }

        private List<Point3d> CalculateSupplyCoolSecPoint()
        {
            List<Point3d> returnPt = new List<Point3d>();
            Point3d pt = new Point3d();

            switch (Type)
            {
                case "A-Toilet-2":
                    pt = getSupplyPtDoubleSinkRight();

                    break;
                default:

                    break;
            }

            if (pt != Point3d.Origin)
            {
                returnPt.Add(pt);
            }

            return returnPt;

        }

        private Point3d CalculateSupplyWarmSecPoint()
        {
            Point3d pt = new Point3d();

            //case "A-Toilet-2":
            //     break;

            return pt;

        }
        private Point3d CalculateSewagePoint()
        {
            Point3d pt = new Point3d();

            return pt;

        }
        private Point3d CalculateSewageSecPoint()
        {
            Point3d pt = new Point3d();

            return pt;

        }

    }
}
