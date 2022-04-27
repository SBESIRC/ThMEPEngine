using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using ThMEPWSS.Uitl.ExtensionsNs;
using ThMEPWSS.WaterSupplyPipeSystem.tool;

namespace ThMEPWSS.WaterSupplyPipeSystem.HalfFloorCase
{
    public static class CaseAll
    {
        public static void Init(HalfBranchPipe halfBranchPipe, double pt7OffsetX)
        {
            double curFloorLowerHeight = halfBranchPipe.IndexStartY + (halfBranchPipe.FloorNumber - 1) * halfBranchPipe.FloorHeight;
            var households = halfBranchPipe.Households[halfBranchPipe.AreaIndex];
            var branchPipeX = halfBranchPipe.BranchPipeX;

            var pt1 = new Point3d(halfBranchPipe.PipeOffsetX, curFloorLowerHeight + 100, 0);
            var pt2 = pt1.OffsetX(branchPipeX);
            var pt231 = pt2.OffsetY(-100);

            var pt232 = pt231.OffsetY(-0.7 * halfBranchPipe.BlockSize[0][0]);//减压阀

            var pt3 = pt232.OffsetY(-50);
            halfBranchPipe.TextSite = pt1.OffsetY(-1400);
            var pt374 = BranchPts.Get(pt3, halfBranchPipe);
            var pt7 = pt374.OffsetX(pt7OffsetX);

            var pt11 = new Point3d(pt7.X, curFloorLowerHeight + 790, 0);
            halfBranchPipe.PRValveDetailSite = new Point3d(pt1.X - 5000, curFloorLowerHeight + 200, 0);

            halfBranchPipe.PressureReducingValveSite = pt231;//减压阀位置

            halfBranchPipe.AutoExhaustValveSite = pt1.OffsetY(915);

            halfBranchPipe.BranchPipes.Add(new Line(pt1, pt2));
            halfBranchPipe.BranchPipes.Add(new Line(pt1, pt1.OffsetY(-200)));
            halfBranchPipe.BranchPipes.Add(new Line(pt2, pt231));
            halfBranchPipe.BranchPipes.Add(new Line(pt232, pt3));
            if (halfBranchPipe.Households[halfBranchPipe.AreaIndex] != 0)
            {
                var pt15 = pt11.OffsetX((households - 1) * 200 + 300);
                var pt1501 = pt15.OffsetXY(945, 945);
                halfBranchPipe.FloorPts.Add(pt15.OffsetXY(444, 444));//第一个楼梯敷设点
                var pt1502 = pt1501.OffsetX(275);
                halfBranchPipe.BranchPipes.Add(new Line(pt11, pt15));
                halfBranchPipe.BranchPipes.Add(new Line(pt15, pt1501));
                halfBranchPipe.BranchPipes.Add(new Line(pt1501, pt1502));
                halfBranchPipe.WaterPipeInterrupted.Add(pt1502);//第1个水管截断位置

                halfBranchPipe.BranchPipes.Add(new Line(pt374, pt7));
                halfBranchPipe.BranchPipes.Add(new Line(pt7, pt11));

                for (int i = 1; i < households; i++)
                {
                    var pt4 = pt3.OffsetY(-250);
                    var pt484 = BranchPts.Get(pt4, halfBranchPipe);
                    var pt8 = new Point3d(pt7.X + 200 * i, pt4.Y, 0);
                    var pt12 = new Point3d(pt8.X, pt11.Y - i * 250, 0);
                    var pt16 = pt15.OffsetY(-250 * i);
                    halfBranchPipe.FloorPts.Add(pt16.OffsetXY(444, 444));//第二个楼梯敷设点
                    var pt1601 = pt1501.OffsetY(-250);
                    var pt1602 = pt1502.OffsetY(-250);

                    halfBranchPipe.BranchPipes.Add(new Line(pt484, pt8));
                    halfBranchPipe.BranchPipes.Add(new Line(pt8, pt12));
                    halfBranchPipe.BranchPipes.Add(new Line(pt12, pt16));
                    halfBranchPipe.BranchPipes.Add(new Line(pt16, pt1601));
                    halfBranchPipe.BranchPipes.Add(new Line(pt1601, pt1602));

                    halfBranchPipe.WaterPipeInterrupted.Add(pt1602);//第i个水管截断位置

                    if (i == households - 1)
                    {
                        halfBranchPipe.BranchPipes.Add(new Line(pt3, pt4));
                    }
                }
            }

            if (halfBranchPipe.HasFlushFaucet) //有冲洗龙头
            {
                double pt19Y = pt3.Y - 250 * households;
                var pt19 = new Point3d(pt3.X, pt19Y, 0);
                var pt19204 = BranchPts.Get(pt19, halfBranchPipe);

                var pt20 = new Point3d(pt374.X + 275 + households * 200, pt19.Y, 0);
                halfBranchPipe.BranchPipes.Add(new Line(pt3, pt19));
                halfBranchPipe.BranchPipes.Add(new Line(pt19204, pt20));
                Case1.CreateFlushFaucet(pt20, halfBranchPipe);
            }
        }

    }
}
