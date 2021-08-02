using System;
using System.Linq;
using Dreambuild.AutoCAD;
using System.Collections.Generic;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
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

                var objid = InsertBlockService.InsertCountBlock(new Point3d(OuterFrameLength * (CurrentIndex - 1) + 1200, OuterFrameLength * (FloorIndex - 1) + Offset, 0), new Scale3d(100, 100, 100), Math.PI, new Dictionary<string, string>() { { "N", this.fireDistrict.Data.BlockData.BlockStatistics["防排抽烟机"].ToString() } });
                using (Linq2Acad.AcadDatabase acad = Linq2Acad.AcadDatabase.Active())
                {
                    BlockReference br = acad.Element<BlockReference>(objid);
                    Result.Add(br);
                }
                #endregion
                if (FireCompartmentParameter.FixedPartType != 3)
                {
                    InsertBlockService.InsertSmokeExhaust(new Vector3d(0, OuterFrameLength * (FloorIndex - 1), 0));
                }
            }
            //设置线型
            Result.Where(o => !(o is BlockReference)).ForEach(o =>
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
            this.CircuitLayer = "E-CTRL-WIRE";
            this.CircuitLinetype = "ByLayer";
            this.CircuitLayerLinetype = "BORDER2";
            this.StartIndexBlock = 14;
            this.Offset = 2750;
            this.EndIndexBlock = 16;
        }
    }
}
