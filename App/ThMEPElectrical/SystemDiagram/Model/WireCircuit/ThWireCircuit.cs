using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPElectrical.SystemDiagram.Model.WireCircuit
{
    public abstract class ThWireCircuit
    {
        /// <summary>
        /// 正方形边框长款
        /// </summary>
        protected const int OuterFrameLength = 3000;

        /// <summary>
        /// 起点块
        /// </summary>
        protected int StartIndexBlock { get; set; } = 0;

        /// <summary>
        /// 终点块
        /// </summary>
        protected int EndIndexBlock { get; set; }

        /// <summary>
        /// 记录楼层或者列信息
        /// </summary>
        protected int FloorIndex { get; set; }

        /// <summary>
        /// 偏移量
        /// </summary>
        protected double Offset { get; set; }

        /// <summary>
        /// 特殊块索引
        /// </summary>
        protected int[] SpecialBlockIndex { get; set; }

        /// <summary>
        /// 线型信息
        /// </summary>
        protected string CircuitLinetype { get; set; }

        /// <summary>
        /// 图层线型信息
        /// </summary>
        public string CircuitLayerLinetype { get; set; }

        /// <summary>
        /// 图层颜色
        /// </summary>
        public string CircuitLayer { get; set; }

        /// <summary>
        /// 线型颜色
        /// </summary>
        protected int CircuitColorIndex { get; set; }

        /// <summary>
        /// 数据集合
        /// </summary>
        protected ThDrawModel fireDistrict { get; set; }

        /// <summary>
        /// 数据集合
        /// </summary>
        protected List<ThDrawModel> AllFireDistrictData { get; set; }

        /// <summary>
        /// 连接的块的信息
        /// </summary>
        //public List<ThConnectInfoModel> connectionModels = new List<ThConnectInfoModel>();

        /// <summary>
        /// 绘制方法
        /// </summary>
        public abstract List<Entity> Draw();

        /// <summary>
        /// 此方法进行回路的初始化
        /// </summary>
        public abstract void InitCircuitConnection();

        /// <summary>
        /// 设置楼层信息
        /// </summary>
        /// <param name="FloorIndex">系统图层数</param>
        /// <param name="fireDistrict">该防火分区数据</param>
        public void SetFloorIndex(int FloorIndex, ThDrawModel fireDistrict)
        {
            this.FloorIndex = FloorIndex;
            this.fireDistrict = fireDistrict;
        }

        /// <summary>
        /// 设置楼层信息
        /// </summary>
        /// <param name="FloorIndex">系统图层数</param>
        /// <param name="fireDistrict">该防火分区数据</param>
        public void SetFloorIndex(int FloorIndex, List<ThDrawModel> fireDistrict)
        {
            this.FloorIndex = FloorIndex;
            this.AllFireDistrictData = fireDistrict;
        }

        /// <summary>
        /// 无挂块，画直线
        /// </summary>
        /// <param name="CurrentAddress">当前地址索引</param>
        /// <param name="IsHorizontalLine">线的方向:True水平线,False竖直线</param>
        /// <returns></returns>
        protected Entity DrawStraightLine(int CurrentAddress, bool IsHorizontalLine = true)
        {
            Line line = new Line();
            //水平线
            if (IsHorizontalLine)
            {
                line.StartPoint = new Point3d(OuterFrameLength * (CurrentAddress - 1), OuterFrameLength * (FloorIndex - 1) + Offset, 0);
                line.EndPoint = new Point3d(OuterFrameLength * CurrentAddress, OuterFrameLength * (FloorIndex - 1) + Offset, 0);
            }
            //竖直线
            else
            {
                line.StartPoint = new Point3d(OuterFrameLength * (FloorIndex - 1) + Offset, OuterFrameLength * (CurrentAddress - 1), 0);
                line.EndPoint = new Point3d(OuterFrameLength * (FloorIndex - 1) + Offset, OuterFrameLength * (CurrentAddress), 0);
            }
            return line;
        }

        /// <summary>
        /// 画挂块的实心圆点
        /// </summary>
        /// <param name="CurrentAddress">当前地址索引</param>
        /// <param name="IsHorizontalLine">线的方向:True水平线,False竖直线</param>
        /// <returns></returns>
        protected Entity DrawFilledCircle(Point2d CircleCenter)
        {
            Polyline polyline = new Polyline()
            {
                Closed = true
            };
            var sp = CircleCenter.Add(new Vector2d(-25, 0));
            var ep = CircleCenter.Add(new Vector2d(25, 0));
            polyline.AddVertexAt(0, sp, Math.Tan(Math.PI / 4.0), 300, 300);
            polyline.AddVertexAt(1, ep, Math.Tan(Math.PI / 4.0), 300, 300);
            return polyline;
        }
    }
}
