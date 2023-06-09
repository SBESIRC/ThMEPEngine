﻿using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPElectrical.SystemDiagram.Model.WireCircuit
{
    /// <summary>
    /// 总线式消防电话线
    /// </summary>
    public class ThFireTelephoneWireCircuit : ThWireCircuit
    {
        //Draw
        public override List<Entity> Draw()
        {
            List<Entity> Result = new List<Entity>();
            int CurrentIndex = this.StartIndexBlock;
            #region 画竖线
            Line Startline2 = new Line(new Point3d(OuterFrameLength * (CurrentIndex - 1) + 1500, OuterFrameLength * (FloorIndex - 1), 0), new Point3d(OuterFrameLength * (CurrentIndex - 1) + 1500, OuterFrameLength * FloorIndex, 0));
            Result.Add(Startline2);
            #endregion
            bool DrawBFAS212 = true;
            bool DrawBFAS220 = true;
            bool DrawBFAS330 = true;
            //现在逻辑变动，张皓讲这三个块都画上，数量为0也显示
            //画第一条线
            if (DrawBFAS212)
            {
                //重设高度
                this.Offset = 2550;
                CurrentIndex = this.StartIndexBlock;
                //画起点框
                #region 起点框
                Line Startline1 = new Line(new Point3d(OuterFrameLength * (CurrentIndex - 1) + 1500, OuterFrameLength * (FloorIndex - 1) + Offset, 0), new Point3d(OuterFrameLength * CurrentIndex , OuterFrameLength * (FloorIndex - 1) + Offset, 0));
                Result.Add(Startline1);
                Result.Add(DrawFilledCircle(new Point2d(OuterFrameLength * (CurrentIndex - 1) + 1500, OuterFrameLength * (FloorIndex - 1) + Offset)));
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
                Line Endline1 = new Line(new Point3d(OuterFrameLength * (CurrentIndex - 1), OuterFrameLength * (FloorIndex - 1) + Offset, 0), new Point3d(OuterFrameLength * (CurrentIndex - 1) + 1500, OuterFrameLength * (FloorIndex - 1) + Offset, 0));
                Result.Add(Endline1);
                Line Endline2 = new Line(new Point3d(OuterFrameLength * (CurrentIndex - 1)+1500, OuterFrameLength * (FloorIndex - 1) + Offset, 0), new Point3d(OuterFrameLength * (CurrentIndex - 1) + 1500, OuterFrameLength * (FloorIndex - 1) + Offset-900, 0));
                Result.Add(Endline2);
                #endregion
            }
            //画第二条线
            if (DrawBFAS220)
            {
                //重设高度
                this.Offset = 2700;
                CurrentIndex = this.StartIndexBlock;
                //画起点框
                #region 起点框
                Line Startline1 = new Line(new Point3d(OuterFrameLength * (CurrentIndex - 1) + 1500, OuterFrameLength * (FloorIndex - 1) + Offset, 0), new Point3d(OuterFrameLength * CurrentIndex, OuterFrameLength * (FloorIndex - 1) + Offset, 0));
                Result.Add(Startline1);
                Result.Add(DrawFilledCircle(new Point2d(OuterFrameLength * (CurrentIndex - 1) + 1500, OuterFrameLength * (FloorIndex - 1) + Offset)));
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
                Line Endline1 = new Line(new Point3d(OuterFrameLength * (CurrentIndex - 1), OuterFrameLength * (FloorIndex - 1) + Offset, 0), new Point3d(OuterFrameLength * (CurrentIndex - 1) + 2250, OuterFrameLength * (FloorIndex - 1) + Offset, 0));
                Result.Add(Endline1);
                Line Endline2 = new Line(new Point3d(OuterFrameLength * (CurrentIndex - 1) + 2250, OuterFrameLength * (FloorIndex - 1) + Offset, 0), new Point3d(OuterFrameLength * (CurrentIndex - 1) + 2250, OuterFrameLength * (FloorIndex - 1) + Offset - 1750, 0));
                Result.Add(Endline2);
                #endregion
            }
            //画第三条线
            if (DrawBFAS330)
            {
                //重设高度
                this.Offset = 800;
                CurrentIndex = this.StartIndexBlock + 4;
                //画起点框
                #region 起点框
                Line Startline1 = new Line(new Point3d(OuterFrameLength * (CurrentIndex - 1) + 1650, OuterFrameLength * (FloorIndex - 1) + Offset, 0), new Point3d(OuterFrameLength * CurrentIndex, OuterFrameLength * (FloorIndex - 1) + Offset, 0));
                Result.Add(Startline1);
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
                Line Endline1 = new Line(new Point3d(OuterFrameLength * (CurrentIndex - 1), OuterFrameLength * (FloorIndex - 1) + Offset, 0), new Point3d(OuterFrameLength * (CurrentIndex - 1) + 1300, OuterFrameLength * (FloorIndex - 1) + Offset, 0));
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

        // 横线不支持画竖线方法
        public override Dictionary<int, List<Entity>> DrawVertical()
        {
            throw new NotSupportedException();
        }

        public override void InitCircuitConnection()
        {
            this.CircuitColorIndex = (int)ColorIndex.BYLAYER;
            this.CircuitLayer = "E-FAS-WIRE5";
            this.CircuitLinetype = "ByLayer";
            this.CircuitLayerLinetype = "HIDDEN";
            this.StartIndexBlock = 2;
            //this.Offset = 2550; 此回路要画三条线，故在代码里特殊处理了
            this.EndIndexBlock = 7;
        }
    }
}
