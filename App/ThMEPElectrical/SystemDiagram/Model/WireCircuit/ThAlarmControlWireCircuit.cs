using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPElectrical.SystemDiagram.Model.WireCircuit
{
    /// <summary>
    /// 火灾自动报警、控制总线
    /// </summary>
    public class ThAlarmControlWireCircuit: ThWireCircuit
    {
        //Draw
        public override List<Entity> Draw()
        {
            List<Entity> Result = new List<Entity>();
            int CurrentIndex = this.StartIndexBlock;
            //画起点框
            #region 起点框
            Line Startline1 = new Line(new Point3d(OuterFrameLength * (CurrentIndex - 1) + 1500, OuterFrameLength * (FloorIndex - 1) + 1400, 0), new Point3d(OuterFrameLength * (CurrentIndex - 1) + 1600, OuterFrameLength * (FloorIndex - 1) + Offset, 0));
            Result.Add(Startline1);

            Line Startline2 = new Line(new Point3d(OuterFrameLength * (CurrentIndex - 1) + 1600, OuterFrameLength * (FloorIndex - 1) + Offset, 0), new Point3d(OuterFrameLength * (CurrentIndex - 1) + 3000, OuterFrameLength * (FloorIndex - 1) + Offset, 0));
            Result.Add(Startline2);

            Line Startline3 = new Line(new Point3d(OuterFrameLength * (CurrentIndex - 1) + 1500, OuterFrameLength * (FloorIndex - 1), 0), new Point3d(OuterFrameLength * (CurrentIndex - 1) + 1500, OuterFrameLength * FloorIndex , 0));
            Result.Add(Startline3);
            #endregion

            CurrentIndex++;
            while (CurrentIndex < EndIndexBlock)
            {
                if (CurrentIndex == this.SpecialBlockIndex[0])
                {
                    Result.AddRange(DrawSpecialBlock5(CurrentIndex));
                }
                else if (CurrentIndex == this.SpecialBlockIndex[1])
                {
                    Result.AddRange(DrawSpecialBlock7(CurrentIndex));
                }
                else if (CurrentIndex == this.SpecialBlockIndex[2])
                {
                    Result.AddRange(DrawSpecialBlock9(CurrentIndex));
                }
                else if (CurrentIndex == this.SpecialBlockIndex[3])
                {
                    Result.AddRange(DrawSpecialBlock10(CurrentIndex));
                }
                else if (CurrentIndex == this.SpecialBlockIndex[7])
                {
                    Result.AddRange(DrawSpecialBlock14(CurrentIndex));
                }
                else if (CurrentIndex == this.SpecialBlockIndex[8])
                {
                    Result.AddRange(DrawSpecialBlock16(CurrentIndex));
                }
                else
                {
                    //该模块没有挂块，那就画一条直线
                    Result.Add(DrawStraightLine(CurrentIndex));
                }
                CurrentIndex++;
            }
            //画终点框
            #region 终点框
            Result.Add(DrawStraightLine(CurrentIndex));
            #endregion

            //设置线型
            Result.ForEach(o =>
            {
                o.Linetype = this.CircuitLinetype;
                o.Layer = this.CircuitLayer;
                o.ColorIndex = this.CircuitColorIndex;
            });

            return Result;
        }

        /// <summary>
        /// 画特殊块方法
        /// </summary>
        /// <param name="currentIndex"></param>
        /// <returns></returns>
        private List<Entity> DrawSpecialBlock5(int currentIndex)
        {
            List<Entity> result = new List<Entity>();
            double RightmostPosition = 0;
            double BlockPosition = ThBlockConfigModel.BlockConfig.First(y => y.BlockName == "E-BFAS540" && y.Index == currentIndex).Position.X - 150;
            Line Midline1 = new Line(new Point3d(OuterFrameLength * (currentIndex - 1) + RightmostPosition, OuterFrameLength * (FloorIndex - 1) + Offset, 0), new Point3d(OuterFrameLength * (currentIndex - 1) + BlockPosition, OuterFrameLength * (FloorIndex - 1) + Offset, 0));
            result.Add(Midline1);
            RightmostPosition = BlockPosition + 300;
            Line Midline2 = new Line(new Point3d(OuterFrameLength * (currentIndex - 1) + RightmostPosition, OuterFrameLength * (FloorIndex - 1) + Offset, 0), new Point3d(OuterFrameLength * currentIndex, OuterFrameLength * (FloorIndex - 1) + Offset, 0));
            result.Add(Midline2);
            return result;
        }
        private List<Entity> DrawSpecialBlock7(int currentIndex)
        {
            List<Entity> result = new List<Entity>();
            if (this.fireDistrict.Data.BlockData.BlockStatistics["E-BFAS212"] == 0)
            {
                result.Add(DrawStraightLine(currentIndex));
            }
            else
            {
                double RightmostPosition = 0;
                double BlockPosition = ThBlockConfigModel.BlockConfig.First(y => y.BlockName == "E-BFAS212" && y.Index == currentIndex).Position.X - 150;
                Line Midline1 = new Line(new Point3d(OuterFrameLength * (currentIndex - 1) + RightmostPosition, OuterFrameLength * (FloorIndex - 1) + Offset, 0), new Point3d(OuterFrameLength * (currentIndex - 1) + BlockPosition, OuterFrameLength * (FloorIndex - 1) + Offset, 0));
                result.Add(Midline1);
                RightmostPosition = BlockPosition + 300;
                Line Midline2 = new Line(new Point3d(OuterFrameLength * (currentIndex - 1) + RightmostPosition, OuterFrameLength * (FloorIndex - 1) + Offset, 0), new Point3d(OuterFrameLength * currentIndex, OuterFrameLength * (FloorIndex - 1) + Offset, 0));
                result.Add(Midline2);
            }
            return result;
        }
        private List<Entity> DrawSpecialBlock9(int currentIndex)
        {
            List<Entity> result = new List<Entity>();
            bool HasBFAS110 = this.fireDistrict.Data.BlockData.BlockStatistics["E-BFAS110"]>0;
            bool HasBFAS120 = this.fireDistrict.Data.BlockData.BlockStatistics["E-BFAS120"]>0;
            if (!HasBFAS110 && !HasBFAS120)
            {
                result.Add(DrawStraightLine(currentIndex));
            }
            else
            {
                double RightmostPosition = 0;
                double BlockPosition = 0;
                if (HasBFAS110)
                {
                    BlockPosition = ThBlockConfigModel.BlockConfig.First(y => y.BlockName == "E-BFAS110" && y.Index == currentIndex).Position.X - 150;
                    Line Midline1 = new Line(new Point3d(OuterFrameLength * (currentIndex - 1) + RightmostPosition, OuterFrameLength * (FloorIndex - 1) + Offset, 0), new Point3d(OuterFrameLength * (currentIndex - 1) + BlockPosition, OuterFrameLength * (FloorIndex - 1) + Offset, 0));
                    result.Add(Midline1);
                    RightmostPosition = BlockPosition + 300;
                    if (HasBFAS120)
                    {
                        BlockPosition = ThBlockConfigModel.BlockConfig.First(y => y.BlockName == "E-BFAS120" && y.Index == currentIndex).Position.X - 150;
                        Line Midline2 = new Line(new Point3d(OuterFrameLength * (currentIndex - 1) + RightmostPosition, OuterFrameLength * (FloorIndex - 1) + Offset, 0), new Point3d(OuterFrameLength * (currentIndex - 1) + BlockPosition, OuterFrameLength * (FloorIndex - 1) + Offset, 0));
                        result.Add(Midline2);
                        RightmostPosition = BlockPosition + 300;
                    }
                }
                else
                {
                    BlockPosition = ThBlockConfigModel.BlockConfig.First(y => y.BlockName == "E-BFAS120" && y.Index == currentIndex).Position.X - 150;
                    Line Midline1 = new Line(new Point3d(OuterFrameLength * (currentIndex - 1) + RightmostPosition, OuterFrameLength * (FloorIndex - 1) + Offset, 0), new Point3d(OuterFrameLength * (currentIndex - 1) + BlockPosition, OuterFrameLength * (FloorIndex - 1) + Offset, 0));
                    result.Add(Midline1);
                    RightmostPosition = BlockPosition + 300;
                }
                Line Midline3 = new Line(new Point3d(OuterFrameLength * (currentIndex - 1) + RightmostPosition, OuterFrameLength * (FloorIndex - 1) + Offset, 0), new Point3d(OuterFrameLength * currentIndex, OuterFrameLength * (FloorIndex - 1) + Offset, 0));
                result.Add(Midline3);
            }
            return result;
        }
        private List<Entity> DrawSpecialBlock10(int currentIndex)
        {
            List<Entity> result = new List<Entity>();
            bool HasBFAS112 = this.fireDistrict.Data.BlockData.BlockStatistics["E-BFAS112"] > 0;
            bool HasBFAS113 = this.fireDistrict.Data.BlockData.BlockStatistics["E-BFAS113"] > 0;
            if (!HasBFAS112 && !HasBFAS113)
            {
                result.Add(DrawStraightLine(currentIndex));
            }
            else
            {
                double RightmostPosition = 0;
                //double BlockPosition = 0;
                if (HasBFAS112)
                {
                    Polyline Midpolyline1 = new Polyline(4);
                    Midpolyline1.AddVertexAt(0, new Point2d(OuterFrameLength * (currentIndex - 1) + RightmostPosition, OuterFrameLength * (FloorIndex - 1) + Offset), 0, 0, 0);
                    Midpolyline1.AddVertexAt(1, new Point2d(OuterFrameLength * (currentIndex - 1) + RightmostPosition + 225, OuterFrameLength * (FloorIndex - 1) + Offset), 0, 0, 0);
                    Midpolyline1.AddVertexAt(2, new Point2d(OuterFrameLength * (currentIndex - 1) + RightmostPosition + 225, OuterFrameLength * (FloorIndex - 1) + Offset + 100), 0, 0, 0);
                    Midpolyline1.AddVertexAt(3, new Point2d(OuterFrameLength * (currentIndex - 1) + RightmostPosition + 850, OuterFrameLength * (FloorIndex - 1) + Offset + 100), 0, 0, 0);
                    result.Add(Midpolyline1);
                    RightmostPosition = 1150;
                    if (HasBFAS113)
                    {
                        Line Midline2 = new Line(new Point3d(OuterFrameLength * (currentIndex - 1) + RightmostPosition, OuterFrameLength * (FloorIndex - 1) + Offset + 100, 0), new Point3d(OuterFrameLength * (currentIndex - 1) + RightmostPosition + 700, OuterFrameLength * (FloorIndex - 1) + Offset + 100, 0));
                        result.Add(Midline2);
                        RightmostPosition = 2150;
                    }
                }
                else
                {
                    Polyline Midpolyline1 = new Polyline(4);
                    Midpolyline1.AddVertexAt(0, new Point2d(OuterFrameLength * (currentIndex - 1) + RightmostPosition, OuterFrameLength * (FloorIndex - 1) + Offset), 0, 0, 0);
                    Midpolyline1.AddVertexAt(1, new Point2d(OuterFrameLength * (currentIndex - 1) + RightmostPosition + 1200, OuterFrameLength * (FloorIndex - 1) + Offset), 0, 0, 0);
                    Midpolyline1.AddVertexAt(2, new Point2d(OuterFrameLength * (currentIndex - 1) + RightmostPosition + 1200, OuterFrameLength * (FloorIndex - 1) + Offset + 100), 0, 0, 0);
                    Midpolyline1.AddVertexAt(3, new Point2d(OuterFrameLength * (currentIndex - 1) + RightmostPosition + 1850, OuterFrameLength * (FloorIndex - 1) + Offset + 100), 0, 0, 0);
                    result.Add(Midpolyline1);
                    RightmostPosition = 2150;
                }
                Polyline Midpolyline2 = new Polyline(4);
                Midpolyline2.AddVertexAt(0, new Point2d(OuterFrameLength * (currentIndex - 1) + RightmostPosition, OuterFrameLength * (FloorIndex - 1) + Offset + 100), 0, 0, 0);
                Midpolyline2.AddVertexAt(1, new Point2d(OuterFrameLength * (currentIndex - 1) + RightmostPosition + 625, OuterFrameLength * (FloorIndex - 1) + Offset + 100), 0, 0, 0);
                Midpolyline2.AddVertexAt(2, new Point2d(OuterFrameLength * (currentIndex - 1) + RightmostPosition + 625, OuterFrameLength * (FloorIndex - 1) + Offset), 0, 0, 0);
                Midpolyline2.AddVertexAt(3, new Point2d(OuterFrameLength * currentIndex, OuterFrameLength * (FloorIndex - 1) + Offset), 0, 0, 0);
                result.Add(Midpolyline2);
            }
            return result;
        }
        //穿过块
        private List<Entity> DrawSpecialBlock14(int currentIndex)
        {
            List<Entity> result = new List<Entity>();
            bool HasBFAS711 = this.fireDistrict.Data.BlockData.BlockStatistics["E-BFAS711"] > 0;
            bool HasBFAS712 = this.fireDistrict.Data.BlockData.BlockStatistics["E-BFAS712"] > 0;
            bool HasBFAS713 = this.fireDistrict.Data.BlockData.BlockStatistics["E-BFAS713"] > 0;

            List<double> SplitNodes = new List<double>();
            if (HasBFAS711)
                SplitNodes.Add(ThBlockConfigModel.BlockConfig.First(y => y.BlockName == "E-BFAS711" && y.Index == currentIndex).Position.X - 150);
            if (HasBFAS713)
                SplitNodes.Add(ThBlockConfigModel.BlockConfig.First(y => y.BlockName == "E-BFAS713" && y.Index == currentIndex).Position.X - 150);
            if (HasBFAS712)
                SplitNodes.Add(ThBlockConfigModel.BlockConfig.First(y => y.BlockName == "E-BFAS712" && y.Index == currentIndex).Position.X - 150);
            SplitNodes.Add(OuterFrameLength);
            double RightmostPosition = 0;
            SplitNodes.ForEach(o =>
            {
                Line Midline1 = new Line(new Point3d(OuterFrameLength * (currentIndex - 1) + RightmostPosition, OuterFrameLength * (FloorIndex - 1) + Offset, 0), new Point3d(OuterFrameLength * (currentIndex - 1) + o, OuterFrameLength * (FloorIndex - 1) + Offset, 0));
                result.Add(Midline1);
                RightmostPosition = o + 300;
            });
            return result;
        }
        private List<Entity> DrawSpecialBlock16(int currentIndex)
        {
            List<Entity> result = new List<Entity>();
            if (this.fireDistrict.Data.BlockData.BlockStatistics["E-BFAS522"] == 0)
            {
                result.Add(DrawStraightLine(currentIndex));
            }
            else
            {
                double RightmostPosition = 0;
                double BlockPosition = ThBlockConfigModel.BlockConfig.First(y => y.BlockName == "E-BFAS522" && y.Index == currentIndex).AssociatedBlocks.First(o=>o.BlockName== "E-BFAS011").Position.X - 250;
                Line Midline1 = new Line(new Point3d(OuterFrameLength * (currentIndex - 1) + RightmostPosition, OuterFrameLength * (FloorIndex - 1) + Offset, 0), new Point3d(OuterFrameLength * (currentIndex - 1) + BlockPosition, OuterFrameLength * (FloorIndex - 1) + Offset, 0));
                result.Add(Midline1);
                RightmostPosition = BlockPosition + 500;
                Line Midline2 = new Line(new Point3d(OuterFrameLength * (currentIndex - 1) + RightmostPosition, OuterFrameLength * (FloorIndex - 1) + Offset, 0), new Point3d(OuterFrameLength * currentIndex, OuterFrameLength * (FloorIndex - 1) + Offset, 0));
                result.Add(Midline2);
            }
            return result;
        }

        public override void InitCircuitConnection()
        {
            this.CircuitColorIndex = 4;
            this.CircuitLayer = "E-FAS-WIRE";
            this.CircuitLinetype = "ByLayer";
            this.CircuitLayerLinetype = "CONTINUOUS";
            this.StartIndexBlock = 4;
            this.Offset = 1500;
            this.EndIndexBlock = 21;
            SpecialBlockIndex = new int[] { 5, 7, 9, 10, 11, 12, 13, 14 ,16};
        }
    }
}
