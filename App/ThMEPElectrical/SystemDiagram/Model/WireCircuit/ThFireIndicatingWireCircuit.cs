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
    /// 火灾显示盘总线
    /// </summary>
    public class ThFireIndicatingWireCircuit : ThWireCircuit
    {
        //Draw
        public override List<Entity> Draw()
        {
            List<Entity> Result = new List<Entity>();
            int CurrentIndex = this.StartIndexBlock;
            #region 画竖线
            Line Startline2 = new Line(new Point3d(OuterFrameLength * (CurrentIndex - 1) + 2200, OuterFrameLength * (FloorIndex - 1), 0), new Point3d(OuterFrameLength * (CurrentIndex - 1) + 2200, OuterFrameLength * (FloorIndex - 1) + 3000, 0));
            Result.Add(Startline2);
            #endregion
            //判断末尾是否有连接块,如没有，则省略整条线都不画了
            if (this.fireDistrict.Data.BlockData.BlockStatistics["区域显示器/火灾显示盘"] != 0 || this.fireDistrict.Data.BlockData.BlockStatistics["楼层或回路重复显示屏"] != 0)
            {
                //画起点框
                #region 起点框
                Line Startline1 = new Line(new Point3d(OuterFrameLength * (CurrentIndex - 1) + 2200, OuterFrameLength * (FloorIndex - 1) + Offset, 0), new Point3d(OuterFrameLength * (CurrentIndex - 1) + 3000, OuterFrameLength * (FloorIndex - 1) + Offset, 0));
                Result.Add(Startline1);
                Result.Add(DrawFilledCircle(new Point2d(OuterFrameLength * (CurrentIndex - 1) +2200, OuterFrameLength * (FloorIndex - 1) + Offset)));
                #endregion

                CurrentIndex++;
                while (CurrentIndex < EndIndexBlock)
                {
                    //该模块没有挂块，那就画一条直线
                    Result.Add(DrawStraightLine(CurrentIndex));
                    CurrentIndex++;
                }
                //画终点框
                #region 终点框
                Line Endline1 = new Line(new Point3d(OuterFrameLength * (CurrentIndex - 1), OuterFrameLength * (FloorIndex - 1) + Offset, 0), new Point3d(OuterFrameLength * (CurrentIndex - 1) + 1250, OuterFrameLength * (FloorIndex - 1) + Offset, 0));
                Result.Add(Endline1);
                #endregion
            }
            //设置线型
            Result.ForEach(o =>
            {
                o.Linetype = this.CircuitLinetype;
                o.Layer = this.CircuitLayer;
                o.ColorIndex = this.CircuitColorIndex;
            });

            return Result;
        }

        public override void InitCircuitConnection()
        {
            this.CircuitColorIndex = 4;
            this.CircuitLayer = "E-FAS-WIRE";
            this.CircuitLinetype = "ByLayer";
            this.CircuitLayerLinetype = "CONTINUOUS";
            this.StartIndexBlock = 2;
            this.Offset = 450;
            this.EndIndexBlock = 5;
        }
    }
}
