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
    /// 排烟风机入口处280℃防火阀直接联动排烟风机关闭信号线
    /// </summary>
    public class ThConnect280FireDamperWireCircuit : ThWireCircuit
    {
        public override List<Entity> Draw()
        {
            List<Entity> Result = new List<Entity>();
            int CurrentIndex = this.StartIndexBlock;
            bool CanDraw = true;
            //判断末尾是否有连接块,如没有，则省略整条线都不画了
            if (this.fireDistrict.Data.BlockData.BlockStatistics["280℃防火阀+输入模块"] == 0 || this.fireDistrict.Data.BlockData.BlockStatistics["防排抽烟机"] == 0)
            {
                CanDraw = false;
            }
            if (CanDraw)
            {
                //画起点框
                #region 起点框
                Line Startline1 = new Line(new Point3d(OuterFrameLength * (CurrentIndex - 1) + 2250, OuterFrameLength * (FloorIndex - 1) + 1650, 0), new Point3d(OuterFrameLength * (CurrentIndex - 1) + 2250, OuterFrameLength * (FloorIndex - 1) + Offset, 0));
                Result.Add(Startline1);

                Line Startline2 = new Line(new Point3d(OuterFrameLength * (CurrentIndex - 1) + 2250, OuterFrameLength * (FloorIndex - 1) + Offset, 0), new Point3d(OuterFrameLength * CurrentIndex, OuterFrameLength * (FloorIndex - 1) + Offset, 0));
                Result.Add(Startline2);
                #endregion

                CurrentIndex++;
                while (CurrentIndex < EndIndexBlock)
                {
                    Result.Add(DrawStraightLine(CurrentIndex));
                    CurrentIndex++;
                }
                //画终点框
                #region 终点框
                Line Endline1 = new Line(new Point3d(OuterFrameLength * (CurrentIndex - 1), OuterFrameLength * (FloorIndex - 1) + Offset, 0), new Point3d(OuterFrameLength * (CurrentIndex - 1) + 1925, OuterFrameLength * (FloorIndex - 1) + Offset, 0));
                Result.Add(Endline1);

                Line Endline2 = new Line(new Point3d(OuterFrameLength * (CurrentIndex - 1) + 1925, OuterFrameLength * (FloorIndex - 1) + Offset, 0), new Point3d(OuterFrameLength * (CurrentIndex - 1) + 1925, OuterFrameLength * (FloorIndex - 1) + 1200, 0));
                Result.Add(Endline2);

                Line Endline3 = new Line(new Point3d(OuterFrameLength * (CurrentIndex - 1) + 1750, OuterFrameLength * (FloorIndex - 1) + 1200, 0), new Point3d(OuterFrameLength * (CurrentIndex - 1) + 1925, OuterFrameLength * (FloorIndex - 1) + 1200, 0));
                Result.Add(Endline3);

                InsertBlockService.InsertCountBlock(new Point3d(OuterFrameLength * (CurrentIndex - 1) + 1200, OuterFrameLength * (FloorIndex - 1) + Offset, 0), new Scale3d(100, 100, 100), Math.PI, new Dictionary<string, string>() { { "N", this.fireDistrict.Data.BlockData.BlockStatistics["防排抽烟机"].ToString() } });
                #endregion

                InsertBlockService.InsertSmokeExhaust(new Vector3d(0, OuterFrameLength * (FloorIndex - 1), 0));
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
            this.CircuitColorIndex = 3;
            this.CircuitLayer = "E-CTRL-WIRE";
            this.CircuitLinetype = "ByLayer";
            this.CircuitLayerLinetype = "BORDER2";
            this.StartIndexBlock = 14;
            this.Offset = 2750;
            this.EndIndexBlock = 16;
        }
    }
}
