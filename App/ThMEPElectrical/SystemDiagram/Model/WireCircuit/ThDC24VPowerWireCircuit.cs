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
    /// DC24V附加电源总线
    /// </summary>
    public class ThDC24VPowerWireCircuit : ThWireCircuit
    {
        //Draw
        public override List<Entity> Draw()
        {
            List<Entity> Result = new List<Entity>();
            int CurrentIndex = this.StartIndexBlock;

            //画起点框
            #region 起点框
            Line Startline1 = new Line(new Point3d(OuterFrameLength * (CurrentIndex - 1) + 1500, OuterFrameLength * (FloorIndex - 1) + Offset, 0), new Point3d(OuterFrameLength * CurrentIndex, OuterFrameLength * (FloorIndex - 1) + Offset, 0));
            Result.Add(Startline1);
            Line Startline2 = new Line(new Point3d(OuterFrameLength * (CurrentIndex - 1) + 1500, OuterFrameLength * (FloorIndex - 1), 0), new Point3d(OuterFrameLength * (CurrentIndex - 1) + 1500, OuterFrameLength * FloorIndex, 0));
            Result.Add(Startline2);
            Result.Add(DrawFilledCircle(new Point2d(OuterFrameLength * (CurrentIndex - 1) + 1500, OuterFrameLength * (FloorIndex - 1) + Offset)));
            #endregion

            CurrentIndex++;
            while (CurrentIndex < EndIndexBlock)
            {
                if (CurrentIndex == this.SpecialBlockIndex[0])
                {
                    //该模块有挂块
                    Result.AddRange(DrawSpecialBlock5(CurrentIndex));
                }
                else if (CurrentIndex == this.SpecialBlockIndex[1])
                {
                    //该模块有挂块
                    Result.AddRange(DrawSpecialBlock6(CurrentIndex));
                }
                else if (CurrentIndex == this.SpecialBlockIndex[2])
                {
                    //该模块有挂块
                    Result.AddRange(DrawSpecialBlock11(CurrentIndex));
                }
                else if (CurrentIndex == this.SpecialBlockIndex[3])
                {
                    //该模块有挂块
                    Result.AddRange(DrawSpecialBlock12(CurrentIndex));
                }
                else if (CurrentIndex == this.SpecialBlockIndex[4])
                {
                    //该模块有挂块
                    Result.AddRange(DrawSpecialBlock13(CurrentIndex));
                }
                else if (CurrentIndex == this.SpecialBlockIndex[5])
                {
                    //该模块有挂块
                    Result.AddRange(DrawSpecialBlock15(CurrentIndex));
                }
                else if (CurrentIndex == this.SpecialBlockIndex[6])
                {
                    //该模块有挂块
                    Result.AddRange(DrawSpecialBlock16(CurrentIndex));
                }
                else if (CurrentIndex == this.SpecialBlockIndex[7])
                {
                    //该模块有挂块
                    Result.AddRange(DrawSpecialBlock19(CurrentIndex));
                }
                else if (CurrentIndex == this.SpecialBlockIndex[8])
                {
                    //该模块有挂块
                    Result.AddRange(DrawSpecialBlock20(CurrentIndex));
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

        // 横线不支持画竖线方法
        public override Dictionary<int, List<Entity>> DrawVertical()
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// 画特殊块方法
        /// </summary>
        /// <param name="currentIndex"></param>
        /// <returns></returns>
        private List<Entity> DrawSpecialBlock6(int currentIndex)
        {
            List<Entity> result = new List<Entity>();
            Polyline Midpolyline1 = new Polyline(3);
            Midpolyline1.AddVertexAt(0, new Point2d(OuterFrameLength * (currentIndex - 1), OuterFrameLength * (FloorIndex - 1) + Offset), 0, 0, 0);
            Midpolyline1.AddVertexAt(1, new Point2d(OuterFrameLength * (currentIndex - 1) + 275, OuterFrameLength * (FloorIndex - 1) + Offset), 0, 0, 0);
            Midpolyline1.AddVertexAt(2, new Point2d(OuterFrameLength * (currentIndex - 1) + 550, OuterFrameLength * (FloorIndex - 1) + Offset - 275), 0, 0, 0);
            result.Add(Midpolyline1);

            Polyline Midpolyline2 = new Polyline(3);
            Midpolyline2.AddVertexAt(0, new Point2d(OuterFrameLength * (currentIndex - 1) + 850, OuterFrameLength * (FloorIndex - 1) + Offset - 275), 0, 0, 0);
            Midpolyline2.AddVertexAt(1, new Point2d(OuterFrameLength * (currentIndex - 1) + 1125, OuterFrameLength * (FloorIndex - 1) + Offset), 0, 0, 0);
            Midpolyline2.AddVertexAt(2, new Point2d(OuterFrameLength * currentIndex, OuterFrameLength * (FloorIndex - 1) + Offset), 0, 0, 0);
            result.Add(Midpolyline2);

            //var BlockPosition = ThBlockConfigModel.BlockConfig.First(y => y.BlockName == "E-BFAS030" && y.Index == currentIndex).Position;
            //Line Midline1 = new Line(new Point3d(OuterFrameLength * (currentIndex - 1) + BlockPosition.X, OuterFrameLength * (FloorIndex - 1) + Offset, 0), new Point3d(OuterFrameLength * (currentIndex - 1) + BlockPosition.X, OuterFrameLength * (FloorIndex - 1) + BlockPosition.Y+150, 0));
            //result.Add(Midline1);

            var BlockPosition2 = ThBlockConfigModel.BlockConfig.First(y => y.UniqueName == "分区声光报警器" && y.Index == currentIndex).Position.Add(new Vector3d(100,0,0));
            result.Add(DrawFilledCircle(new Point2d(OuterFrameLength * (currentIndex - 1) + BlockPosition2.X, OuterFrameLength * (FloorIndex - 1) + Offset)));
            Line Midline2 = new Line(new Point3d(OuterFrameLength * (currentIndex - 1) + BlockPosition2.X, OuterFrameLength * (FloorIndex - 1) + Offset, 0), new Point3d(OuterFrameLength * (currentIndex - 1) + BlockPosition2.X, OuterFrameLength * (FloorIndex - 1) + BlockPosition2.Y + 150, 0));
            result.Add(Midline2);

            var BlockPosition3 = ThBlockConfigModel.BlockConfig.First(y => y.UniqueName == "消防广播火栓强制启动模块" && y.Index == currentIndex).Position.Add(new Vector3d(100, 0, 0));
            result.Add(DrawFilledCircle(new Point2d(OuterFrameLength * (currentIndex - 1) + BlockPosition3.X, OuterFrameLength * (FloorIndex - 1) + Offset)));
            Line Midline3 = new Line(new Point3d(OuterFrameLength * (currentIndex - 1) + BlockPosition3.X, OuterFrameLength * (FloorIndex - 1) + Offset, 0), new Point3d(OuterFrameLength * (currentIndex - 1) + BlockPosition3.X, OuterFrameLength * (FloorIndex - 1) + BlockPosition3.Y + 150, 0));
            result.Add(Midline3);

            return result;
        }

        private List<Entity> DrawSpecialBlock5(int currentIndex)
        {
            List<Entity> result = new List<Entity>();
            if (this.fireDistrict.Data.BlockData.BlockStatistics["区域显示器/火灾显示盘"] > 0 || this.fireDistrict.Data.BlockData.BlockStatistics["楼层或回路重复显示屏"] > 0)
            {
                var BlockPosition = ThBlockConfigModel.BlockConfig.First(y => y.BlockName == "E-BFAS030" && y.Index == currentIndex).Position;
                result.Add(DrawFilledCircle(new Point2d(OuterFrameLength * (currentIndex - 1) + BlockPosition.X, OuterFrameLength * (FloorIndex - 1) + Offset)));
                Line Midline1 = new Line(new Point3d(OuterFrameLength * (currentIndex - 1) + BlockPosition.X, OuterFrameLength * (FloorIndex - 1) + Offset, 0), new Point3d(OuterFrameLength * (currentIndex - 1) + BlockPosition.X, OuterFrameLength * (FloorIndex - 1) + BlockPosition.Y + 150, 0));
                result.Add(Midline1);
            }
            result.Add(DrawStraightLine(currentIndex));
            return result;
        }

        private List<Entity> DrawSpecialBlock11(int currentIndex)
        {
            List<Entity> result = new List<Entity>();
            if (this.fireDistrict.Data.BlockData.BlockStatistics["强电间总线控制模块"] > 0)
            {
                var BlockPosition = ThBlockConfigModel.BlockConfig.First(y => y.UniqueName == "强电间总线控制模块").Position;
                result.Add(DrawFilledCircle(new Point2d(OuterFrameLength * (currentIndex - 1) + BlockPosition.X, OuterFrameLength * (FloorIndex - 1) + Offset)));
                Line Midline1 = new Line(new Point3d(OuterFrameLength * (currentIndex - 1) + BlockPosition.X, OuterFrameLength * (FloorIndex - 1) + Offset, 0), new Point3d(OuterFrameLength * (currentIndex - 1) + BlockPosition.X, OuterFrameLength * (FloorIndex - 1) + BlockPosition.Y + 150, 0));
                result.Add(Midline1);
            }
            result.Add(DrawStraightLine(currentIndex));
            return result;
        }

        private List<Entity> DrawSpecialBlock12(int currentIndex)
        {
            List<Entity> result = new List<Entity>();
            if (this.fireDistrict.Data.BlockData.BlockStatistics["弱电间总线控制模块"] > 0)
            {
                var BlockPosition = ThBlockConfigModel.BlockConfig.First(y => y.UniqueName == "弱电间总线控制模块").Position;
                result.Add(DrawFilledCircle(new Point2d(OuterFrameLength * (currentIndex - 1) + BlockPosition.X, OuterFrameLength * (FloorIndex - 1) + Offset)));
                Line Midline1 = new Line(new Point3d(OuterFrameLength * (currentIndex - 1) + BlockPosition.X, OuterFrameLength * (FloorIndex - 1) + Offset, 0), new Point3d(OuterFrameLength * (currentIndex - 1) + BlockPosition.X, OuterFrameLength * (FloorIndex - 1) + BlockPosition.Y + 150, 0));
                result.Add(Midline1);
            }
            result.Add(DrawStraightLine(currentIndex));
            return result;
        }

        private List<Entity> DrawSpecialBlock13(int currentIndex)
        {
            List<Entity> result = new List<Entity>();
            if (this.fireDistrict.Data.BlockData.BlockStatistics["防火卷帘模块"] > 0)
            {
                var BlockPosition = ThBlockConfigModel.BlockConfig.First(y => y.UniqueName == "防火卷帘模块").Position;
                result.Add(DrawFilledCircle(new Point2d(OuterFrameLength * (currentIndex - 1) + BlockPosition.X, OuterFrameLength * (FloorIndex - 1) + Offset)));
                Line Midline1 = new Line(new Point3d(OuterFrameLength * (currentIndex - 1) + BlockPosition.X, OuterFrameLength * (FloorIndex - 1) + Offset, 0), new Point3d(OuterFrameLength * (currentIndex - 1) + BlockPosition.X, OuterFrameLength * (FloorIndex - 1) + BlockPosition.Y + 150, 0));
                result.Add(Midline1);
            }
            if (this.fireDistrict.Data.BlockData.BlockStatistics["电梯模块"] > 0)
            {
                var BlockPosition = ThBlockConfigModel.BlockConfig.First(y => y.UniqueName == "电梯模块").Position;
                result.Add(DrawFilledCircle(new Point2d(OuterFrameLength * (currentIndex - 1) + BlockPosition.X, OuterFrameLength * (FloorIndex - 1) + Offset)));
                Line Midline1 = new Line(new Point3d(OuterFrameLength * (currentIndex - 1) + BlockPosition.X, OuterFrameLength * (FloorIndex - 1) + Offset, 0), new Point3d(OuterFrameLength * (currentIndex - 1) + BlockPosition.X, OuterFrameLength * (FloorIndex - 1) + BlockPosition.Y + 150, 0));
                result.Add(Midline1);
            }
            result.Add(DrawStraightLine(currentIndex));
            return result;
        }

        private List<Entity> DrawSpecialBlock15(int currentIndex)
        {
            List<Entity> result = new List<Entity>();
            
            if (this.fireDistrict.Data.BlockData.BlockStatistics["电动防火阀"] > 0)
            {
                var BlockPosition = ThBlockConfigModel.BlockConfig.First(y => y.UniqueName == "电动防火阀").AssociatedBlocks[0].Position;
                result.Add(DrawFilledCircle(new Point2d(OuterFrameLength * (currentIndex - 1) + BlockPosition.X, OuterFrameLength * (FloorIndex - 1) + Offset)));
                Line Midline1 = new Line(new Point3d(OuterFrameLength * (currentIndex - 1) + BlockPosition.X, OuterFrameLength * (FloorIndex - 1) + Offset, 0), new Point3d(OuterFrameLength * (currentIndex - 1) + BlockPosition.X, OuterFrameLength * (FloorIndex - 1) + BlockPosition.Y + 150, 0));
                result.Add(Midline1);
            }
            result.Add(DrawStraightLine(currentIndex));

            if (this.fireDistrict.Data.BlockData.BlockStatistics["70度电动防火阀"] > 0)
            {
                var BlockPosition = ThBlockConfigModel.BlockConfig.First(y => y.UniqueName == "70度电动防火阀").AssociatedBlocks[0].Position;
                result.Add(DrawFilledCircle(new Point2d(OuterFrameLength * (currentIndex - 1) + BlockPosition.X, OuterFrameLength * (FloorIndex - 1) + Offset)));
                Line Midline1 = new Line(new Point3d(OuterFrameLength * (currentIndex - 1) + BlockPosition.X, OuterFrameLength * (FloorIndex - 1) + Offset, 0), new Point3d(OuterFrameLength * (currentIndex - 1) + BlockPosition.X, OuterFrameLength * (FloorIndex - 1) + BlockPosition.Y + 150, 0));
                result.Add(Midline1);
            }
            result.Add(DrawStraightLine(currentIndex));
            return result;
        }

        private List<Entity> DrawSpecialBlock16(int currentIndex)
        {
            List<Entity> result = new List<Entity>();
            if (this.fireDistrict.Data.BlockData.BlockStatistics["防排抽烟机"] > 0)
            {
                var BlockPosition = ThBlockConfigModel.BlockConfig.First(y => y.UniqueName == "防排抽烟机").AssociatedBlocks[1].Position;
                result.Add(DrawFilledCircle(new Point2d(OuterFrameLength * (currentIndex - 1) + BlockPosition.X, OuterFrameLength * (FloorIndex - 1) + Offset)));
                Line Midline1 = new Line(new Point3d(OuterFrameLength * (currentIndex - 1) + BlockPosition.X, OuterFrameLength * (FloorIndex - 1) + Offset, 0), new Point3d(OuterFrameLength * (currentIndex - 1) + BlockPosition.X, OuterFrameLength * (FloorIndex - 1) + BlockPosition.Y + 150, 0));
                result.Add(Midline1);
            }
            result.Add(DrawStraightLine(currentIndex));
            return result;
        }

        private List<Entity> DrawSpecialBlock19(int currentIndex)
        {
            List<Entity> result = new List<Entity>();
            if (this.fireDistrict.Data.BlockData.BlockStatistics["消火栓泵"] > 0)
            {
                var BlockPosition = ThBlockConfigModel.BlockConfig.First(y => y.UniqueName == "消火栓泵").AssociatedBlocks[1].Position;
                result.Add(DrawFilledCircle(new Point2d(OuterFrameLength * (currentIndex - 1) + BlockPosition.X, OuterFrameLength * (FloorIndex - 1) + Offset)));
                Line Midline1 = new Line(new Point3d(OuterFrameLength * (currentIndex - 1) + BlockPosition.X, OuterFrameLength * (FloorIndex - 1) + Offset, 0), new Point3d(OuterFrameLength * (currentIndex - 1) + BlockPosition.X, OuterFrameLength * (FloorIndex - 1) + BlockPosition.Y + 150, 0));
                result.Add(Midline1);
            }
            result.Add(DrawStraightLine(currentIndex));
            return result;
        }

        private List<Entity> DrawSpecialBlock20(int currentIndex)
        {
            List<Entity> result = new List<Entity>();
            if (this.fireDistrict.Data.BlockData.BlockStatistics["喷淋泵"] > 0)
            {
                var BlockPosition = ThBlockConfigModel.BlockConfig.First(y => y.UniqueName == "喷淋泵").AssociatedBlocks[1].Position;
                result.Add(DrawFilledCircle(new Point2d(OuterFrameLength * (currentIndex - 1) + BlockPosition.X, OuterFrameLength * (FloorIndex - 1) + Offset)));
                Line Midline1 = new Line(new Point3d(OuterFrameLength * (currentIndex - 1) + BlockPosition.X, OuterFrameLength * (FloorIndex - 1) + Offset, 0), new Point3d(OuterFrameLength * (currentIndex - 1) + BlockPosition.X, OuterFrameLength * (FloorIndex - 1) + BlockPosition.Y + 150, 0));
                result.Add(Midline1);
            }
            result.Add(DrawStraightLine(currentIndex));
            return result;
        }

        public override void InitCircuitConnection()
        {
            this.CircuitColorIndex = (int)ColorIndex.BYLAYER;
            this.CircuitLayer = "E-FAS-WIRE2";
            this.CircuitLinetype = "ByLayer";
            this.CircuitLayerLinetype = "CENTER";
            this.StartIndexBlock = 3;
            this.Offset = 1850;
            this.EndIndexBlock = 21;
            SpecialBlockIndex = new int[] { 5, 6, 11, 12, 13, 15, 16 ,19 ,20 };
        }
    }
}