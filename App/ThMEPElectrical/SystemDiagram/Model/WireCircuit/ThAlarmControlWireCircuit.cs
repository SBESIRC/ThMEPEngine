using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPElectrical.SystemDiagram.Service;

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
            while (CurrentIndex <= EndIndexBlock)
            {
                if (CurrentIndex == this.SpecialBlockIndex[0])
                {
                    Result.AddRange(DrawSpecialBlock6(CurrentIndex));
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
                else if (CurrentIndex == this.SpecialBlockIndex[4])
                {
                    Result.AddRange(DrawSpecialBlock11(CurrentIndex));
                }
                else if (CurrentIndex == this.SpecialBlockIndex[5])
                {
                    Result.AddRange(DrawSpecialBlock12(CurrentIndex));
                }
                else if (CurrentIndex == this.SpecialBlockIndex[6])
                {
                    Result.AddRange(DrawSpecialBlock13(CurrentIndex));
                }
                else if (CurrentIndex == this.SpecialBlockIndex[7])
                {
                    Result.AddRange(DrawSpecialBlock14(CurrentIndex));
                }
                else if (CurrentIndex == this.SpecialBlockIndex[8])
                {
                    Result.AddRange(DrawSpecialBlock15(CurrentIndex));
                }
                else if (CurrentIndex == this.SpecialBlockIndex[9])
                {
                    Result.AddRange(DrawSpecialBlock16(CurrentIndex));
                }
                else if (CurrentIndex == this.SpecialBlockIndex[10])
                {
                    Result.AddRange(DrawSpecialBlock17(CurrentIndex));
                }
                else if (CurrentIndex == this.SpecialBlockIndex[11])
                {
                    Result.AddRange(DrawSpecialBlock18(CurrentIndex));
                }
                else if (CurrentIndex == this.SpecialBlockIndex[12])
                {
                    Result.AddRange(DrawSpecialBlock19(CurrentIndex));
                }
                else if (CurrentIndex == this.SpecialBlockIndex[13])
                {
                    Result.AddRange(DrawSpecialBlock20(CurrentIndex));
                }
                else if (CurrentIndex == this.SpecialBlockIndex[14])
                {
                    Result.AddRange(DrawSpecialBlock21(CurrentIndex));
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
            //Result.Add(DrawStraightLine(EndIndexBlock));
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

        // 横线不支持画竖线方法
        public override Dictionary<int, List<Entity>> DrawVertical()
        {
            throw new NotSupportedException();
        }

        #region 画特殊块方法
        private List<Entity> DrawSpecialBlock6(int currentIndex)
        {
            List<Entity> result = new List<Entity>();
            double RightmostPosition = 0;
            double BlockPosition = ThBlockConfigModel.BlockConfig.First(y => y.BlockName == "E-BFAS540" && y.Index == currentIndex).Position.X - 150;
            Line Midline1 = new Line(new Point3d(OuterFrameLength * (currentIndex - 1) + RightmostPosition, OuterFrameLength * (FloorIndex - 1) + Offset, 0), new Point3d(OuterFrameLength * (currentIndex - 1) + BlockPosition, OuterFrameLength * (FloorIndex - 1) + Offset, 0));
            result.Add(Midline1);
            RightmostPosition = BlockPosition + 300;
            Line Midline2 = new Line(new Point3d(OuterFrameLength * (currentIndex - 1) + RightmostPosition, OuterFrameLength * (FloorIndex - 1) + Offset, 0), new Point3d(OuterFrameLength * currentIndex, OuterFrameLength * (FloorIndex - 1) + Offset, 0));
            result.Add(Midline2);

            var PointPosition = ThBlockConfigModel.BlockConfig.First(y => y.UniqueName == "分区声光报警器").Position;
            result.Add(DrawFilledCircle(new Point2d(OuterFrameLength * (currentIndex - 1) + PointPosition.X, OuterFrameLength * (FloorIndex - 1) + Offset)));
            Line Addline1 = new Line(new Point3d(OuterFrameLength * (currentIndex - 1) + PointPosition.X, OuterFrameLength * (FloorIndex - 1) + Offset, 0), new Point3d(OuterFrameLength * (currentIndex - 1) + PointPosition.X, OuterFrameLength * (FloorIndex - 1) + PointPosition.Y + 150, 0));
            result.Add(Addline1);

            PointPosition = ThBlockConfigModel.BlockConfig.First(y => y.UniqueName == "消防广播火栓强制启动模块").Position;
            result.Add(DrawFilledCircle(new Point2d(OuterFrameLength * (currentIndex - 1) + PointPosition.X, OuterFrameLength * (FloorIndex - 1) + Offset)));
            Line Addline2 = new Line(new Point3d(OuterFrameLength * (currentIndex - 1) + PointPosition.X, OuterFrameLength * (FloorIndex - 1) + Offset, 0), new Point3d(OuterFrameLength * (currentIndex - 1) + PointPosition.X, OuterFrameLength * (FloorIndex - 1) + PointPosition.Y + 150, 0));
            result.Add(Addline2);

            return result;
        }
        private List<Entity> DrawSpecialBlock7(int currentIndex)
        {
            List<Entity> result = new List<Entity>();
            double RightmostPosition = 0;
            double BlockPosition = ThBlockConfigModel.BlockConfig.First(y => y.BlockName == "E-BFAS212" && y.Index == currentIndex).Position.X - 150;
            Line Midline1 = new Line(new Point3d(OuterFrameLength * (currentIndex - 1) + RightmostPosition, OuterFrameLength * (FloorIndex - 1) + Offset, 0), new Point3d(OuterFrameLength * (currentIndex - 1) + BlockPosition, OuterFrameLength * (FloorIndex - 1) + Offset, 0));
            result.Add(Midline1);
            RightmostPosition = BlockPosition + 300;
            Line Midline2 = new Line(new Point3d(OuterFrameLength * (currentIndex - 1) + RightmostPosition, OuterFrameLength * (FloorIndex - 1) + Offset, 0), new Point3d(OuterFrameLength * currentIndex, OuterFrameLength * (FloorIndex - 1) + Offset, 0));
            result.Add(Midline2);
            return result;
        }
        private List<Entity> DrawSpecialBlock9(int currentIndex)
        {
            List<Entity> result = new List<Entity>();
            bool HasBFAS110 = this.fireDistrict.Data.BlockData.BlockStatistics["感烟火灾探测器"] >0;
            bool HasBFAS120 = this.fireDistrict.Data.BlockData.BlockStatistics["感温火灾探测器"] >0;
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
            bool HasBFAS112 = this.fireDistrict.Data.BlockData.BlockStatistics["红外光束感烟火灾探测器发射器"] > 0;
            bool HasBFAS113 = this.fireDistrict.Data.BlockData.BlockStatistics["红外光束感烟火灾探测器接收器"] > 0;
            if (!HasBFAS112 && !HasBFAS113)
            {
                //要关注到[吸气式感烟火灾探测器],[家用感烟火灾探测报警器],[家用感温火灾探测报警器]因为这些块和其他的块长的都不一样
                int DetectorCount = ThAutoFireAlarmSystemCommon.Detectors.Count(o => this.fireDistrict.Data.BlockData.BlockStatistics[o] > 0);
                //没有其他探测器
                if (DetectorCount == 0)
                {
                    result.Add(DrawStraightLine(currentIndex));
                }
                //一个探测器
                else if (DetectorCount == 1)
                {
                    double spacing = 150.0;
                    bool HasBFAS160 = this.fireDistrict.Data.BlockData.BlockStatistics["吸气式感烟火灾探测器"] > 0;
                    if(HasBFAS160)
                    {
                        spacing = 250.0;
                    }
                    else
                    {
                        bool HasBFAS114 = this.fireDistrict.Data.BlockData.BlockStatistics["家用感烟火灾探测报警器"] > 0;
                        bool HasBFAS124 = this.fireDistrict.Data.BlockData.BlockStatistics["家用感温火灾探测报警器"] > 0;
                        spacing = 200.0;
                    }
                    double RightmostPosition = 0;
                    double BlockPosition = 1500 - spacing;
                    Line Midline1 = new Line(new Point3d(OuterFrameLength * (currentIndex - 1) + RightmostPosition, OuterFrameLength * (FloorIndex - 1) + Offset, 0), new Point3d(OuterFrameLength * (currentIndex - 1) + BlockPosition, OuterFrameLength * (FloorIndex - 1) + Offset, 0));
                    result.Add(Midline1);
                    RightmostPosition = 1500 + spacing;
                    Line Midline2 = new Line(new Point3d(OuterFrameLength * (currentIndex - 1) + RightmostPosition, OuterFrameLength * (FloorIndex - 1) + Offset, 0), new Point3d(OuterFrameLength * currentIndex, OuterFrameLength * (FloorIndex - 1) + Offset, 0));
                    result.Add(Midline2);
                }
                //两个探测器
                else if(DetectorCount == 2)
                {
                    double spacing1 = 150.0;
                    double spacing2 = 150.0;
                    bool HasBFAS160 = this.fireDistrict.Data.BlockData.BlockStatistics["吸气式感烟火灾探测器"] > 0;
                    bool HasBFAS114 = this.fireDistrict.Data.BlockData.BlockStatistics["家用感烟火灾探测报警器"] > 0;
                    bool HasBFAS124 = this.fireDistrict.Data.BlockData.BlockStatistics["家用感温火灾探测报警器"] > 0;
                    if (HasBFAS160)
                    {
                        spacing1 = 250.0;
                        if (HasBFAS114 | HasBFAS124)
                        {
                            spacing2 = 200.0;
                        }
                    }
                    else
                    {
                        if (HasBFAS114 & HasBFAS124)
                        {
                            spacing1 = 200.0;
                            spacing2 = 200.0;
                        }
                        else if (HasBFAS114 | HasBFAS124)
                        {
                            spacing1 = 200.0;
                        }
                    }
                    double RightmostPosition = 0;
                    double BlockPosition = 1000 - spacing1;
                    Line Midline1 = new Line(new Point3d(OuterFrameLength * (currentIndex - 1) + RightmostPosition, OuterFrameLength * (FloorIndex - 1) + Offset, 0), new Point3d(OuterFrameLength * (currentIndex - 1) + BlockPosition, OuterFrameLength * (FloorIndex - 1) + Offset, 0));
                    result.Add(Midline1);
                    RightmostPosition = 1000 + spacing1;
                    Line Midline2 = new Line(new Point3d(OuterFrameLength * (currentIndex - 1) + RightmostPosition, OuterFrameLength * (FloorIndex - 1) + Offset, 0), new Point3d(OuterFrameLength * (currentIndex - 1) + 2000 - spacing2, OuterFrameLength * (FloorIndex - 1) + Offset, 0));
                    result.Add(Midline2);
                    RightmostPosition = 2000 + spacing2;
                    Line Midline3 = new Line(new Point3d(OuterFrameLength * (currentIndex - 1) + RightmostPosition, OuterFrameLength * (FloorIndex - 1) + Offset, 0), new Point3d(OuterFrameLength * currentIndex, OuterFrameLength * (FloorIndex - 1) + Offset, 0));
                    result.Add(Midline3);
                }
                //三个及以上探测器
                else
                {
                    double spacing1 = 150.0;
                    double spacing2 = 150.0;
                    double spacing3 = 150.0;
                    bool HasBFAS160 = this.fireDistrict.Data.BlockData.BlockStatistics["吸气式感烟火灾探测器"] > 0;
                    bool HasBFAS114 = this.fireDistrict.Data.BlockData.BlockStatistics["家用感烟火灾探测报警器"] > 0;
                    bool HasBFAS124 = this.fireDistrict.Data.BlockData.BlockStatistics["家用感温火灾探测报警器"] > 0;
                    if (HasBFAS160)
                    {
                        spacing1 = 250.0;
                        if (HasBFAS114 & HasBFAS124)
                        {
                            spacing2 = 200.0;
                            spacing3 = 200.0;
                        }
                        else if (HasBFAS114 | HasBFAS124)
                        {
                            spacing2 = 200.0;
                        }
                    }
                    else
                    {
                        if (HasBFAS114 & HasBFAS124)
                        {
                            spacing1 = 200.0;
                            spacing2 = 200.0;
                        }
                        else if (HasBFAS114 | HasBFAS124)
                        {
                            spacing1 = 200.0;
                        }
                    }
                    double RightmostPosition = 0;
                    double BlockPosition = 750 - spacing1;
                    Line Midline1 = new Line(new Point3d(OuterFrameLength * (currentIndex - 1) + RightmostPosition, OuterFrameLength * (FloorIndex - 1) + Offset, 0), new Point3d(OuterFrameLength * (currentIndex - 1) + BlockPosition, OuterFrameLength * (FloorIndex - 1) + Offset, 0));
                    result.Add(Midline1);
                    RightmostPosition = 750 + spacing1;
                    Line Midline2 = new Line(new Point3d(OuterFrameLength * (currentIndex - 1) + RightmostPosition, OuterFrameLength * (FloorIndex - 1) + Offset, 0), new Point3d(OuterFrameLength * (currentIndex - 1) + 1500 - spacing2, OuterFrameLength * (FloorIndex - 1) + Offset, 0));
                    result.Add(Midline2);
                    RightmostPosition = 1500 + spacing2;
                    Line Midline3 = new Line(new Point3d(OuterFrameLength * (currentIndex - 1) + RightmostPosition, OuterFrameLength * (FloorIndex - 1) + Offset, 0), new Point3d(OuterFrameLength * (currentIndex - 1) + 2250 - spacing3, OuterFrameLength * (FloorIndex - 1) + Offset, 0));
                    result.Add(Midline3);
                    RightmostPosition = 2250 + spacing3;
                    Line Midline4 = new Line(new Point3d(OuterFrameLength * (currentIndex - 1) + RightmostPosition, OuterFrameLength * (FloorIndex - 1) + Offset, 0), new Point3d(OuterFrameLength * currentIndex, OuterFrameLength * (FloorIndex - 1) + Offset, 0));
                    result.Add(Midline4);
                }
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
        private List<Entity> DrawSpecialBlock11(int currentIndex)
        {
            List<Entity> result = new List<Entity>();
            if (this.fireDistrict.Data.BlockData.BlockStatistics["强电间总线控制模块"] == 0)
            {
                result.Add(DrawStraightLine(currentIndex));
            }
            else
            {
                double RightmostPosition = 0;
                double BlockPosition = ThBlockConfigModel.BlockConfig.First(y => y.UniqueName== "强电间总线控制模块").Position.X - 150;
                Line Midline1 = new Line(new Point3d(OuterFrameLength * (currentIndex - 1) + RightmostPosition, OuterFrameLength * (FloorIndex - 1) + Offset, 0), new Point3d(OuterFrameLength * (currentIndex - 1) + BlockPosition, OuterFrameLength * (FloorIndex - 1) + Offset, 0));
                result.Add(Midline1);
                RightmostPosition = BlockPosition + 300;
                Line Midline2 = new Line(new Point3d(OuterFrameLength * (currentIndex - 1) + RightmostPosition, OuterFrameLength * (FloorIndex - 1) + Offset, 0), new Point3d(OuterFrameLength * currentIndex, OuterFrameLength * (FloorIndex - 1) + Offset, 0));
                result.Add(Midline2);
            }
            return result;
        }
        private List<Entity> DrawSpecialBlock12(int currentIndex)
        {
            List<Entity> result = new List<Entity>();
            if (this.fireDistrict.Data.BlockData.BlockStatistics["弱电间总线控制模块"] == 0)
            {
                result.Add(DrawStraightLine(currentIndex));
            }
            else
            {
                double RightmostPosition = 0;
                double BlockPosition = ThBlockConfigModel.BlockConfig.First(y => y.UniqueName == "弱电间总线控制模块").Position.X - 150;
                Line Midline1 = new Line(new Point3d(OuterFrameLength * (currentIndex - 1) + RightmostPosition, OuterFrameLength * (FloorIndex - 1) + Offset, 0), new Point3d(OuterFrameLength * (currentIndex - 1) + BlockPosition, OuterFrameLength * (FloorIndex - 1) + Offset, 0));
                result.Add(Midline1);
                RightmostPosition = BlockPosition + 300;
                Line Midline2 = new Line(new Point3d(OuterFrameLength * (currentIndex - 1) + RightmostPosition, OuterFrameLength * (FloorIndex - 1) + Offset, 0), new Point3d(OuterFrameLength * currentIndex, OuterFrameLength * (FloorIndex - 1) + Offset, 0));
                result.Add(Midline2);
            }
            return result;
        }
        private List<Entity> DrawSpecialBlock13(int currentIndex)
        {
            List<Entity> result = new List<Entity>();
            bool HasFireproofShutter = this.fireDistrict.Data.BlockData.BlockStatistics["防火卷帘模块"] > 0;
            bool HasElevator = this.fireDistrict.Data.BlockData.BlockStatistics["电梯模块"] > 0;
            if (!HasFireproofShutter && !HasElevator)
            {
                result.Add(DrawStraightLine(currentIndex));
            }
            else
            {
                double RightmostPosition = 0;
                double BlockPosition = 0;
                if (HasFireproofShutter)
                {
                    BlockPosition = ThBlockConfigModel.BlockConfig.First(y => y.UniqueName== "防火卷帘模块").Position.X - 250;
                    Line Midline1 = new Line(new Point3d(OuterFrameLength * (currentIndex - 1) + RightmostPosition, OuterFrameLength * (FloorIndex - 1) + Offset, 0), new Point3d(OuterFrameLength * (currentIndex - 1) + BlockPosition, OuterFrameLength * (FloorIndex - 1) + Offset, 0));
                    result.Add(Midline1);
                    RightmostPosition = BlockPosition + 500;
                    if (HasElevator)
                    {
                        BlockPosition = ThBlockConfigModel.BlockConfig.First(y => y.UniqueName== "电梯模块").Position.X - 250;
                        Line Midline2 = new Line(new Point3d(OuterFrameLength * (currentIndex - 1) + RightmostPosition, OuterFrameLength * (FloorIndex - 1) + Offset, 0), new Point3d(OuterFrameLength * (currentIndex - 1) + BlockPosition, OuterFrameLength * (FloorIndex - 1) + Offset, 0));
                        result.Add(Midline2);
                        RightmostPosition = BlockPosition + 500;
                    }
                }
                else
                {
                    BlockPosition = ThBlockConfigModel.BlockConfig.First(y => y.UniqueName == "电梯模块").Position.X - 250;
                    Line Midline1 = new Line(new Point3d(OuterFrameLength * (currentIndex - 1) + RightmostPosition, OuterFrameLength * (FloorIndex - 1) + Offset, 0), new Point3d(OuterFrameLength * (currentIndex - 1) + BlockPosition, OuterFrameLength * (FloorIndex - 1) + Offset, 0));
                    result.Add(Midline1);
                    RightmostPosition = BlockPosition + 500;
                }
                Line Midline3 = new Line(new Point3d(OuterFrameLength * (currentIndex - 1) + RightmostPosition, OuterFrameLength * (FloorIndex - 1) + Offset, 0), new Point3d(OuterFrameLength * currentIndex, OuterFrameLength * (FloorIndex - 1) + Offset, 0));
                result.Add(Midline3);
            }
            return result;
        }
        private List<Entity> DrawSpecialBlock14(int currentIndex)
        {
            List<Entity> result = new List<Entity>();
            bool HasBFAS711 = this.fireDistrict.Data.BlockData.BlockStatistics["70℃防火阀+输入模块"] > 0;
            bool HasBFAS712 = this.fireDistrict.Data.BlockData.BlockStatistics["280℃防火阀+输入模块"] > 0;
            bool HasBFAS713 = this.fireDistrict.Data.BlockData.BlockStatistics["150℃防火阀+输入模块"] > 0;

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
        private List<Entity> DrawSpecialBlock15(int currentIndex)
        {
            List<Entity> result = new List<Entity>();
            if (this.fireDistrict.Data.BlockData.BlockStatistics["电动防火阀"] == 0)
            {
                result.Add(DrawStraightLine(currentIndex));
            }
            else
            {
                double RightmostPosition = 0;
                double BlockPosition = ThBlockConfigModel.BlockConfig.First(y => y.UniqueName == "电动防火阀").AssociatedBlocks[0].Position.X - 150;
                Line Midline1 = new Line(new Point3d(OuterFrameLength * (currentIndex - 1) + RightmostPosition, OuterFrameLength * (FloorIndex - 1) + Offset, 0), new Point3d(OuterFrameLength * (currentIndex - 1) + BlockPosition, OuterFrameLength * (FloorIndex - 1) + Offset, 0));
                result.Add(Midline1);
                RightmostPosition = BlockPosition + 300;
                Line Midline2 = new Line(new Point3d(OuterFrameLength * (currentIndex - 1) + RightmostPosition, OuterFrameLength * (FloorIndex - 1) + Offset, 0), new Point3d(OuterFrameLength * currentIndex, OuterFrameLength * (FloorIndex - 1) + Offset, 0));
                result.Add(Midline2);
            }
            return result;
        }
        private List<Entity> DrawSpecialBlock16(int currentIndex)
        {
            List<Entity> result = new List<Entity>();
            bool HasSmokeMachine = this.fireDistrict.Data.BlockData.BlockStatistics["防排抽烟机"] > 0;
            bool HasBypassValve = this.fireDistrict.Data.BlockData.BlockStatistics["旁通阀"] > 0;
            if (!HasSmokeMachine && !HasBypassValve)
            {
                result.Add(DrawStraightLine(currentIndex));
            }
            else
            {
                double RightmostPosition = 0;
                double BlockPosition = 0;
                if (HasSmokeMachine)
                {
                    BlockPosition = ThBlockConfigModel.BlockConfig.First(y => y.UniqueName == "防排抽烟机").AssociatedBlocks[1].Position.X - 250;
                    Line Midline1 = new Line(new Point3d(OuterFrameLength * (currentIndex - 1) + RightmostPosition, OuterFrameLength * (FloorIndex - 1) + Offset, 0), new Point3d(OuterFrameLength * (currentIndex - 1) + BlockPosition, OuterFrameLength * (FloorIndex - 1) + Offset, 0));
                    result.Add(Midline1);
                    RightmostPosition = BlockPosition + 500;
                    if (HasBypassValve)
                    {
                        BlockPosition = ThBlockConfigModel.BlockConfig.First(y => y.UniqueName == "旁通阀").AssociatedBlocks[0].Position.X - 150;
                        Line Midline2 = new Line(new Point3d(OuterFrameLength * (currentIndex - 1) + RightmostPosition, OuterFrameLength * (FloorIndex - 1) + Offset, 0), new Point3d(OuterFrameLength * (currentIndex - 1) + BlockPosition, OuterFrameLength * (FloorIndex - 1) + Offset, 0));
                        result.Add(Midline2);
                        RightmostPosition = BlockPosition + 300;
                    }
                }
                else
                {
                    BlockPosition = ThBlockConfigModel.BlockConfig.First(y => y.UniqueName == "旁通阀").AssociatedBlocks[0].Position.X - 150;
                    Line Midline1 = new Line(new Point3d(OuterFrameLength * (currentIndex - 1) + RightmostPosition, OuterFrameLength * (FloorIndex - 1) + Offset, 0), new Point3d(OuterFrameLength * (currentIndex - 1) + BlockPosition, OuterFrameLength * (FloorIndex - 1) + Offset, 0));
                    result.Add(Midline1);
                    RightmostPosition = BlockPosition + 300;
                }
                Line Midline3 = new Line(new Point3d(OuterFrameLength * (currentIndex - 1) + RightmostPosition, OuterFrameLength * (FloorIndex - 1) + Offset, 0), new Point3d(OuterFrameLength * currentIndex, OuterFrameLength * (FloorIndex - 1) + Offset, 0));
                result.Add(Midline3);
            }
            return result;
        }
        private List<Entity> DrawSpecialBlock17(int currentIndex)
        {
            List<Entity> result = new List<Entity>();
            bool HasFireHydrant = this.fireDistrict.Data.BlockData.BlockStatistics["消火栓按钮"] > 0;
            bool HasFireExtinguishingSystem = this.fireDistrict.Data.BlockData.BlockStatistics["灭火系统流量开关"] > 0;
            if (!HasFireHydrant && !HasFireExtinguishingSystem)
            {
                result.Add(DrawStraightLine(currentIndex));
            }
            else
            {
                double RightmostPosition = 0;
                double BlockPosition = 0;
                if (HasFireHydrant)
                {
                    BlockPosition = ThBlockConfigModel.BlockConfig.First(y => y.UniqueName == "消火栓按钮").Position.X - 150;
                    Line Midline1 = new Line(new Point3d(OuterFrameLength * (currentIndex - 1) + RightmostPosition, OuterFrameLength * (FloorIndex - 1) + Offset, 0), new Point3d(OuterFrameLength * (currentIndex - 1) + BlockPosition, OuterFrameLength * (FloorIndex - 1) + Offset, 0));
                    result.Add(Midline1);
                    RightmostPosition = BlockPosition + 300;
                    if (HasFireExtinguishingSystem)
                    {
                        BlockPosition = ThBlockConfigModel.BlockConfig.First(y => y.UniqueName == "灭火系统流量开关").Position.X - 150;
                        Line Midline2 = new Line(new Point3d(OuterFrameLength * (currentIndex - 1) + RightmostPosition, OuterFrameLength * (FloorIndex - 1) + Offset, 0), new Point3d(OuterFrameLength * (currentIndex - 1) + BlockPosition, OuterFrameLength * (FloorIndex - 1) + Offset, 0));
                        result.Add(Midline2);
                        RightmostPosition = BlockPosition + 300;
                    }
                }
                else
                {
                    BlockPosition = ThBlockConfigModel.BlockConfig.First(y => y.UniqueName == "灭火系统流量开关").Position.X - 150;
                    Line Midline1 = new Line(new Point3d(OuterFrameLength * (currentIndex - 1) + RightmostPosition, OuterFrameLength * (FloorIndex - 1) + Offset, 0), new Point3d(OuterFrameLength * (currentIndex - 1) + BlockPosition, OuterFrameLength * (FloorIndex - 1) + Offset, 0));
                    result.Add(Midline1);
                    RightmostPosition = BlockPosition + 300;
                }
                Line Midline3 = new Line(new Point3d(OuterFrameLength * (currentIndex - 1) + RightmostPosition, OuterFrameLength * (FloorIndex - 1) + Offset, 0), new Point3d(OuterFrameLength * currentIndex, OuterFrameLength * (FloorIndex - 1) + Offset, 0));
                result.Add(Midline3);
            }
            return result;
        }
        private List<Entity> DrawSpecialBlock18(int currentIndex)
        {
            List<Entity> result = new List<Entity>();
            bool HasFlowIndicator = this.fireDistrict.Data.BlockData.BlockStatistics["水流指示器"] > 0;
            bool HasPressureSwitch = this.fireDistrict.Data.BlockData.BlockStatistics["灭火系统压力开关"] > 0;
            bool HasFireWaterTank = this.fireDistrict.Data.BlockData.BlockStatistics["消防水箱"] > 0;
            if (!HasFlowIndicator && !HasPressureSwitch && !HasFireWaterTank)
            {
                result.Add(DrawStraightLine(currentIndex));
            }
            else
            {
                double RightmostPosition = 0;
                double BlockPosition = 0;
                if (HasFlowIndicator)
                {
                    BlockPosition = ThBlockConfigModel.BlockConfig.First(y => y.UniqueName == "水流指示器").Position.X - 450;
                    Line Midline1 = new Line(new Point3d(OuterFrameLength * (currentIndex - 1) + RightmostPosition, OuterFrameLength * (FloorIndex - 1) + Offset, 0), new Point3d(OuterFrameLength * (currentIndex - 1) + BlockPosition, OuterFrameLength * (FloorIndex - 1) + Offset, 0));
                    result.Add(Midline1);
                    RightmostPosition = BlockPosition + 600;
                    if (HasFireWaterTank)
                    {
                        BlockPosition = ThBlockConfigModel.BlockConfig.First(y => y.UniqueName == "消防水箱").Position.X - 150;
                        Line Midline2 = new Line(new Point3d(OuterFrameLength * (currentIndex - 1) + RightmostPosition, OuterFrameLength * (FloorIndex - 1) + Offset, 0), new Point3d(OuterFrameLength * (currentIndex - 1) + BlockPosition, OuterFrameLength * (FloorIndex - 1) + Offset, 0));
                        result.Add(Midline2);
                        RightmostPosition = BlockPosition + 300;
                    }
                    if (HasPressureSwitch)
                    {
                        BlockPosition = ThBlockConfigModel.BlockConfig.First(y => y.UniqueName == "灭火系统压力开关").Position.X - 450;
                        Line Midline3 = new Line(new Point3d(OuterFrameLength * (currentIndex - 1) + RightmostPosition, OuterFrameLength * (FloorIndex - 1) + Offset, 0), new Point3d(OuterFrameLength * (currentIndex - 1) + BlockPosition, OuterFrameLength * (FloorIndex - 1) + Offset, 0));
                        result.Add(Midline3);
                        RightmostPosition = BlockPosition + 600;
                    }
                }
                else
                {
                    if (HasFireWaterTank)
                    {
                        BlockPosition = ThBlockConfigModel.BlockConfig.First(y => y.UniqueName == "消防水箱").Position.X - 150;
                        Line Midline2 = new Line(new Point3d(OuterFrameLength * (currentIndex - 1) + RightmostPosition, OuterFrameLength * (FloorIndex - 1) + Offset, 0), new Point3d(OuterFrameLength * (currentIndex - 1) + BlockPosition, OuterFrameLength * (FloorIndex - 1) + Offset, 0));
                        result.Add(Midline2);
                        RightmostPosition = BlockPosition + 300;

                        if (HasPressureSwitch)
                        {
                            BlockPosition = ThBlockConfigModel.BlockConfig.First(y => y.UniqueName == "灭火系统压力开关").Position.X - 450;
                            Line Midline3 = new Line(new Point3d(OuterFrameLength * (currentIndex - 1) + RightmostPosition, OuterFrameLength * (FloorIndex - 1) + Offset, 0), new Point3d(OuterFrameLength * (currentIndex - 1) + BlockPosition, OuterFrameLength * (FloorIndex - 1) + Offset, 0));
                            result.Add(Midline3);
                            RightmostPosition = BlockPosition + 600;
                        }
                    }
                    else
                    {
                        BlockPosition = ThBlockConfigModel.BlockConfig.First(y => y.UniqueName == "灭火系统压力开关").Position.X - 450;
                        Line Midline3 = new Line(new Point3d(OuterFrameLength * (currentIndex - 1) + RightmostPosition, OuterFrameLength * (FloorIndex - 1) + Offset, 0), new Point3d(OuterFrameLength * (currentIndex - 1) + BlockPosition, OuterFrameLength * (FloorIndex - 1) + Offset, 0));
                        result.Add(Midline3);
                        RightmostPosition = BlockPosition + 600;
                    }
                }
                Line Midline4 = new Line(new Point3d(OuterFrameLength * (currentIndex - 1) + RightmostPosition, OuterFrameLength * (FloorIndex - 1) + Offset, 0), new Point3d(OuterFrameLength * currentIndex, OuterFrameLength * (FloorIndex - 1) + Offset, 0));
                result.Add(Midline4);
            }
            return result;
        }
        private List<Entity> DrawSpecialBlock19(int currentIndex)
        {
            List<Entity> result = new List<Entity>();
            if (this.fireDistrict.Data.BlockData.BlockStatistics["消火栓泵"] == 0)
            {
                result.Add(DrawStraightLine(currentIndex));
            }
            else
            {
                double RightmostPosition = 0;
                double BlockPosition = ThBlockConfigModel.BlockConfig.First(y => y.UniqueName == "消火栓泵").AssociatedBlocks[1].Position.X - 250;
                Line Midline1 = new Line(new Point3d(OuterFrameLength * (currentIndex - 1) + RightmostPosition, OuterFrameLength * (FloorIndex - 1) + Offset, 0), new Point3d(OuterFrameLength * (currentIndex - 1) + BlockPosition, OuterFrameLength * (FloorIndex - 1) + Offset, 0));
                result.Add(Midline1);
                RightmostPosition = BlockPosition + 500;
                Line Midline2 = new Line(new Point3d(OuterFrameLength * (currentIndex - 1) + RightmostPosition, OuterFrameLength * (FloorIndex - 1) + Offset, 0), new Point3d(OuterFrameLength * currentIndex, OuterFrameLength * (FloorIndex - 1) + Offset, 0));
                result.Add(Midline2);
                Line Midline3 = new Line(new Point3d(OuterFrameLength * (currentIndex - 1) + 650, OuterFrameLength * (FloorIndex - 1) + 1100, 0), new Point3d(OuterFrameLength * (currentIndex - 1) + 750, OuterFrameLength * (FloorIndex - 1) + 1200, 0));
                result.Add(Midline3);
                Line Midline4 = new Line(new Point3d(OuterFrameLength * (currentIndex - 1) + 750, OuterFrameLength * (FloorIndex - 1) + 1200, 0), new Point3d(OuterFrameLength * (currentIndex - 1) + 950, OuterFrameLength * (FloorIndex - 1) + 1200, 0));
                result.Add(Midline4);
            }
            return result;
        }
        private List<Entity> DrawSpecialBlock20(int currentIndex)
        {
            List<Entity> result = new List<Entity>();
            if (this.fireDistrict.Data.BlockData.BlockStatistics["喷淋泵"] == 0)
            {
                result.Add(DrawStraightLine(currentIndex));
            }
            else
            {
                double RightmostPosition = 0;
                double BlockPosition = ThBlockConfigModel.BlockConfig.First(y => y.UniqueName == "消火栓泵").AssociatedBlocks[1].Position.X - 250;
                Line Midline1 = new Line(new Point3d(OuterFrameLength * (currentIndex - 1) + RightmostPosition, OuterFrameLength * (FloorIndex - 1) + Offset, 0), new Point3d(OuterFrameLength * (currentIndex - 1) + BlockPosition, OuterFrameLength * (FloorIndex - 1) + Offset, 0));
                result.Add(Midline1);
                RightmostPosition = BlockPosition + 500;
                Line Midline2 = new Line(new Point3d(OuterFrameLength * (currentIndex - 1) + RightmostPosition, OuterFrameLength * (FloorIndex - 1) + Offset, 0), new Point3d(OuterFrameLength * currentIndex, OuterFrameLength * (FloorIndex - 1) + Offset, 0));
                result.Add(Midline2);
                Line Midline3 = new Line(new Point3d(OuterFrameLength * (currentIndex - 1) + 650, OuterFrameLength * (FloorIndex - 1) + 1100, 0), new Point3d(OuterFrameLength * (currentIndex - 1) + 750, OuterFrameLength * (FloorIndex - 1) + 1200, 0));
                result.Add(Midline3);
                Line Midline4 = new Line(new Point3d(OuterFrameLength * (currentIndex - 1) + 750, OuterFrameLength * (FloorIndex - 1) + 1200, 0), new Point3d(OuterFrameLength * (currentIndex - 1) + 950, OuterFrameLength * (FloorIndex - 1) + 1200, 0));
                result.Add(Midline4);
            }
            return result;
        }
        private List<Entity> DrawSpecialBlock21(int currentIndex)
        {
            List<Entity> result = new List<Entity>();
            if (this.fireDistrict.Data.BlockData.BlockStatistics["消防水池"] == 0)
            {
                result.Add(DrawStraightLine(currentIndex));
            }
            else
            {
                double RightmostPosition = 0;
                double BlockPosition = ThBlockConfigModel.BlockConfig.First(y => y.UniqueName == "消防水池").Position.X - 150;
                Line Midline1 = new Line(new Point3d(OuterFrameLength * (currentIndex - 1) + RightmostPosition, OuterFrameLength * (FloorIndex - 1) + Offset, 0), new Point3d(OuterFrameLength * (currentIndex - 1) + BlockPosition, OuterFrameLength * (FloorIndex - 1) + Offset, 0));
                result.Add(Midline1);
                RightmostPosition = BlockPosition + 300;
                Line Midline2 = new Line(new Point3d(OuterFrameLength * (currentIndex - 1) + RightmostPosition, OuterFrameLength * (FloorIndex - 1) + Offset, 0), new Point3d(OuterFrameLength * currentIndex, OuterFrameLength * (FloorIndex - 1) + Offset, 0));
                result.Add(Midline2);
            }
            return result;
        }
        #endregion

        public override void InitCircuitConnection()
        {
            this.CircuitColorIndex = (int)ColorIndex.BYLAYER;
            this.CircuitLayer = "E-FAS-WIRE";
            this.CircuitLinetype = "ByLayer";
            this.CircuitLayerLinetype = "CONTINUOUS";
            this.StartIndexBlock = 4;
            this.Offset = 1500;
            this.EndIndexBlock = 21;
            SpecialBlockIndex = new int[] { 6, 7, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21 };
        }
    }
}
