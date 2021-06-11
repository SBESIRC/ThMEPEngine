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
    /// 消防应急广播总线
    /// </summary>
    public class ThBroadcastWireCircuit : ThWireCircuit
    {
        //Draw
        public override List<Entity> Draw()
        {
            List<Entity> Result = new List<Entity>();
            int CurrentIndex = this.StartIndexBlock;
            //画起点框
            #region 起点框
            Line Startline1 = new Line(new Point3d( OuterFrameLength * (CurrentIndex-1)+1500, OuterFrameLength * (FloorIndex - 1) + 1050, 0), new Point3d(OuterFrameLength * (CurrentIndex-1)+1600, OuterFrameLength * (FloorIndex - 1) + Offset, 0));
            Result.Add(Startline1);

            Line Startline2 = new Line(new Point3d(OuterFrameLength * (CurrentIndex - 1) + 1600, OuterFrameLength * (FloorIndex - 1) + Offset, 0), new Point3d(OuterFrameLength * (CurrentIndex - 1) + 3000 , OuterFrameLength * (FloorIndex - 1) + Offset, 0));
            Result.Add(Startline2);

            Line Startline3 = new Line(new Point3d(OuterFrameLength * (CurrentIndex - 1) + 1500, OuterFrameLength * (FloorIndex - 1), 0), new Point3d(OuterFrameLength * (CurrentIndex - 1) + 1500, OuterFrameLength * (FloorIndex - 1) + 3000, 0));
            Result.Add(Startline3);

            InsertBlockService.InsertCountBlock(new Point3d(OuterFrameLength * (CurrentIndex - 1) + 2300, OuterFrameLength * (FloorIndex - 1) + 1150, 0), new Scale3d(-100, 100, 100), 0, new Dictionary<string, string>() { { "N", this.fireDistrict.Data.BlockData.BlockStatistics["消防广播火栓强制启动模块"].ToString() } });
            #endregion

            CurrentIndex++;
            while (CurrentIndex < EndIndexBlock)
            {
                if (CurrentIndex == this.SpecialBlockIndex[0])
                {
                    //该模块有挂块
                    Result.AddRange(DrawSpecialBlock5(CurrentIndex));
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
            Line Endline1 = new Line(new Point3d(OuterFrameLength * (CurrentIndex - 1), OuterFrameLength * (FloorIndex - 1) + Offset, 0), new Point3d(OuterFrameLength * (CurrentIndex - 1) + 1500, OuterFrameLength * (FloorIndex - 1) + Offset, 0));
            Result.Add(Endline1);
            Line Endline2 = new Line(new Point3d(OuterFrameLength * (CurrentIndex - 1)+1500, OuterFrameLength * (FloorIndex - 1) + 950, 0), new Point3d(OuterFrameLength * (CurrentIndex - 1) + 1500, OuterFrameLength * (FloorIndex - 1) + Offset, 0));
            Result.Add(Endline2);
            Line Endline3 = new Line(new Point3d(OuterFrameLength * (CurrentIndex - 1)+950, OuterFrameLength * (FloorIndex - 1) + 800, 0), new Point3d(OuterFrameLength * (CurrentIndex - 1) + 1300, OuterFrameLength * (FloorIndex - 1) + 800, 0));
            Result.Add(Endline3);
            Line Endline4 = new Line(new Point3d(OuterFrameLength * (CurrentIndex - 1)+1700, OuterFrameLength * (FloorIndex - 1) + 800, 0), new Point3d(OuterFrameLength * (CurrentIndex - 1) + 2050, OuterFrameLength * (FloorIndex - 1) + 800, 0));
            Result.Add(Endline4);
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
            double BlockPosition = ThBlockConfigModel.BlockConfig.First(y => y.UniqueName== "消防广播火栓强制启动模块").Position.X - 150;
            Line Midline1 = new Line(new Point3d(OuterFrameLength * (currentIndex - 1) + RightmostPosition, OuterFrameLength * (FloorIndex - 1) + Offset, 0), new Point3d(OuterFrameLength * (currentIndex - 1) + BlockPosition, OuterFrameLength * (FloorIndex - 1) + Offset, 0));
            result.Add(Midline1);
            RightmostPosition = BlockPosition + 300;
            Line Midline2 = new Line(new Point3d(OuterFrameLength * (currentIndex - 1) + RightmostPosition, OuterFrameLength * (FloorIndex - 1) + Offset, 0), new Point3d(OuterFrameLength * currentIndex, OuterFrameLength * (FloorIndex - 1) + Offset, 0));
            result.Add(Midline2);
            return result;
        }

        public override void InitCircuitConnection()
        {
            this.CircuitColorIndex = 2;
            this.CircuitLayer = "E-BRST-WIRE";
            this.CircuitLinetype = "ByLayer";
            this.CircuitLayerLinetype = "BORDER";
            this.StartIndexBlock = 1;
            this.Offset = 1150;
            this.EndIndexBlock = 8;
            SpecialBlockIndex = new int[] { 5 };
        }
    }
}
