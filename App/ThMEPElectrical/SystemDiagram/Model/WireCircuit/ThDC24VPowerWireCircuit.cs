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

            var BlockPosition2 = ThBlockConfigModel.BlockConfig.First(y => y.BlockName == "E-BFAS520" && y.Index == currentIndex).Position;
            Line Midline2 = new Line(new Point3d(OuterFrameLength * (currentIndex - 1) + BlockPosition2.X, OuterFrameLength * (FloorIndex - 1) + Offset, 0), new Point3d(OuterFrameLength * (currentIndex - 1) + BlockPosition2.X, OuterFrameLength * (FloorIndex - 1) + BlockPosition2.Y + 150, 0));
            result.Add(Midline2);

            return result;
        }

        private List<Entity> DrawSpecialBlock6(int currentIndex)
        {
            List<Entity> result = new List<Entity>();
            if (this.fireDistrict.Data.BlockData.BlockStatistics["E-BFAS030"] > 0)
            {
                var BlockPosition = ThBlockConfigModel.BlockConfig.First(y => y.BlockName == "E-BFAS030" && y.Index == currentIndex).Position;
                Line Midline1 = new Line(new Point3d(OuterFrameLength * (currentIndex - 1) + BlockPosition.X, OuterFrameLength * (FloorIndex - 1) + Offset, 0), new Point3d(OuterFrameLength * (currentIndex - 1) + BlockPosition.X, OuterFrameLength * (FloorIndex - 1) + BlockPosition.Y + 150, 0));
                result.Add(Midline1);
            }
            result.Add(DrawStraightLine(currentIndex));
            return result;
        }

        public override void InitCircuitConnection()
        {
            this.CircuitColorIndex = 1;
            this.CircuitLayer = "E-FAS-WIRE2";
            this.CircuitLinetype = "ByLayer";
            this.CircuitLayerLinetype = "CENTER";
            this.StartIndexBlock = 3;
            this.Offset = 1850;
            this.EndIndexBlock = 21;
            SpecialBlockIndex = new int[] { 5, 6 };
        }
    }
}