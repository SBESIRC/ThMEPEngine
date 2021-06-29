using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

using ThCADExtension;
using ThMEPEngineCore.Model;
using ThCADCore.NTS;

namespace ThMEPWSS.DrainageSystemDiagram
{
    public class ThIfcSanitaryTerminalToilate : ThIfcSanitaryTerminal
    {
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
        public Point3d BasePt { get; private set; }

        public string GroupId { get; set; }
        public string AreaId { get; set; }

        public ThIfcSanitaryTerminalToilate(Entity geometry, string blkName)
        {
            Outline = geometry;
            Type = blkName;
            SupplyCool = new List<Point3d>();
            SupplyCoolOnWall = new List<Point3d>();
            SupplyCoolOnBranch = new List<Point3d>();
            GroupId = "";
            AreaId = "";
            setInfo();
        }

        public void setInfo()
        {
            var blk = this.Outline as BlockReference;
            Uuid = Guid.NewGuid().ToString();
            Boundary = blk.ToOBB(blk.BlockTransform.PreMultiplyBy(Matrix3d.Identity));
            Boundary = turnBoundary(Boundary);
            BasePt = Boundary.GetPoint3dAt(1);
            Dir = (Boundary.GetPoint3dAt(0) - BasePt).GetNormal();

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
                    pt = getSupplyPtByCenter(DrainageSDCommon.supplyCoolDalta75);
                    break;

                case "A-Toilet-2":
                    pt = getSupplyPtDoubleSinkLeft();
                    break;
                case "A-Toilet-3":
                    pt = getSupplyPtByLeftTop(DrainageSDCommon.supplyCoolDalta308);
                    break;

                case "小便器":
                case "A-Kitchen-9":
                case "儿童小便器":
                    pt = getSupplyPtByCenter(DrainageSDCommon.supplyCoolDalta0);
                    break;

                case "A-Toilet-5":
                    pt = getSupplyPtByCenter(DrainageSDCommon.supplyCoolDalta200);
                    break;

                case "儿童坐便器":
                    pt = getSupplyPtByCenter(DrainageSDCommon.supplyCoolDalta250);
                    break;

                case "儿童洗脸盆":
                    pt = getSupplyPtByCenter(DrainageSDCommon.supplyCoolDalta150);
                    break;

                case "蹲便器":
                    pt = getSupplyPtByCenter(DrainageSDCommon.supplyCoolDalta120);
                    break;

                case "A-Toilet-7":
                    pt = getSupplyPtByLeftTop(DrainageSDCommon.supplyCoolDalta350);
                    break;

                case "A-Toilet-9":
                    pt = getSupplyPtByLeftTop(DrainageSDCommon.supplyCoolDalta150);
                    break;

                default:

                    break;

            }

            if (pt!= Point3d.Origin )
            {
                returnPt.Add(pt);
            }

            return returnPt;

        }

        private Point3d getSupplyPtByCenter(double dalta)
        {
            Polyline outline = this.Boundary;
            Point3d leftPt = BasePt;
            Point3d rightPt = outline.GetPoint3dAt(2);

            //Point3d rightPt2 = outline.GetPoint3dAt(3);
            //DrawUtils.ShowGeometry(leftPt, "l0leftpt", 30, 25, 20);
            //DrawUtils.ShowGeometry(rightPt, "l0rightpt", 213, 25, 20);
            //DrawUtils.ShowGeometry(rightPt2, "l0rightpt2", 152, 25, 20);

            Point3d cenPt = new Point3d((leftPt.X + rightPt.X) / 2, (leftPt.Y + rightPt.Y) / 2, 0);

            var dir = (rightPt - leftPt).GetNormal();

            double coorX = cenPt.X + (dir * dalta).X;
            double coorY = cenPt.Y + (dir * dalta).Y;

            Point3d supplyPt = new Point3d(coorX, coorY, 0);

            return supplyPt;
        }

        private Point3d getSupplyPtByLeftTop(double dalta)
        {
            Polyline outline = this.Boundary;
            Point3d leftPt = BasePt;
            Point3d rightPt = outline.GetPoint3dAt(2);

            //Point3d rightPt2 = outline.GetPoint3dAt(3);
            //DrawUtils.ShowGeometry(leftPt, "l0leftpt", 30, 25, 20);
            //DrawUtils.ShowGeometry(rightPt, "l0rightpt", 213, 25, 20);
            //DrawUtils.ShowGeometry(rightPt2, "l0rightpt2", 152, 25, 20);

            var dir = (rightPt - leftPt).GetNormal();

            double coorX = leftPt.X + (dir * dalta).X;
            double coorY = leftPt.Y + (dir * dalta).Y;

            Point3d supplyPt = new Point3d(coorX, coorY, 0);

            return supplyPt;
        }

        private Point3d getSupplyPtDoubleSinkLeft()
        {
            Polyline outline = this.Boundary;
            Point3d leftPt = BasePt;
            Point3d rightPt = outline.GetPoint3dAt(2);

            //Point3d rightPt2 = outline.GetPoint3dAt(3);
            //DrawUtils.ShowGeometry(leftPt, "l0leftpt", 30, 25, 20);
            //DrawUtils.ShowGeometry(rightPt, "l0rightpt", 213, 25, 20);
            //DrawUtils.ShowGeometry(rightPt2, "l0rightpt2", 152, 25, 20);

            var dir = (rightPt - leftPt).GetNormal();

            double length = (rightPt - leftPt).Length;
            double dalta = length / DrainageSDCommon.supplyCoolDaltaDoubleSinkLeftParameter + DrainageSDCommon.supplyCoolDalta75;

            double coorX = leftPt.X + (dir * dalta).X;
            double coorY = leftPt.Y + (dir * dalta).Y;

            Point3d supplyPt = new Point3d(coorX, coorY, 0);

            return supplyPt;
        }

        private Point3d getSupplyPtDoubleSinkRight()
        {
            Polyline outline = this.Boundary;
            Point3d leftPt = BasePt;
            Point3d rightPt = outline.GetPoint3dAt(2);

            //Point3d rightPt2 = outline.GetPoint3dAt(3);
            //DrawUtils.ShowGeometry(leftPt, "l0leftpt", 30, 25, 20);
            //DrawUtils.ShowGeometry(rightPt, "l0rightpt", 213, 25, 20);
            //DrawUtils.ShowGeometry(rightPt2, "l0rightpt2", 152, 25, 20);

            var dir = (rightPt - leftPt).GetNormal();

            double length = (rightPt - leftPt).Length;
            double dalta = length / (4.0 / 3.0) + DrainageSDCommon.supplyCoolDalta75;

            double coorX = leftPt.X + (dir * dalta).X;
            double coorY = leftPt.Y + (dir * dalta).Y;

            Point3d supplyPt = new Point3d(coorX, coorY, 0);

            return supplyPt;
        }

        private Polyline turnBoundary(Polyline boundary)
        {
            Polyline boundaryNew = boundary.Clone() as Polyline;
            int turn = 0;
            //Boundary
            switch (Type)
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

            if (pt!= Point3d.Origin )
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
