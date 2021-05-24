using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using DotNetARX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThCADExtension;

namespace ThMEPElectrical.SystemDiagram.Model
{
    /// <summary>
    /// 防火分区
    /// </summary>
    public class ThFireDistrictModel
    {
        public string FireDistrictName { get; set; }

        public DataSummary Data { get; set; }

        /// <summary>
        /// 防火分区PolyLine
        /// </summary>
        public Entity FireDistrictBoundary { get; set; }

        /// <summary>
        /// 防火分区名称坐标
        /// </summary>
        public Point3d TextPoint { get; set; }

        /// <summary>
        /// 是否绘画防火分区名字
        /// </summary>
        public bool DrawFireDistrictNameText { get; set; } = false;

        /// <summary>
        /// 是否绘画该防火分区
        /// </summary>
        public bool DrawFireDistrict { get; set; } = true;

        // <summary>
        /// 初始化防火分区（有防火分区）
        /// </summary>
        /// <param name="storeys"></param>
        public void InitFireDistrict(Entity FireDistrictEntity)
        {
            this.FireDistrictBoundary = FireDistrictEntity;
            if(this.FireDistrictBoundary is Polyline polyline)
            {
                this.TextPoint = polyline.GetMaximumInscribedCircleCenter();
            }
            if(this.FireDistrictBoundary is MPolygon Mpolygon)
            {
                this.TextPoint = Mpolygon.GetMaximumInscribedCircleCenter();
            }
            
        }

        // <summary>
        /// 初始化楼层（无防火分区）
        /// </summary>
        /// <param name="storeys"></param>
        public void InitFireDistrict(BlockReference FireDistrictBlockReference)
        {
            this.FireDistrictBoundary = new Polyline() { Closed = true };
            (this.FireDistrictBoundary as Polyline).CreatePolyline(FireDistrictBlockReference.GeometricExtents.ToRectangle().Vertices());
        }
    }

    /// <summary>
    /// 数据层
    /// </summary>
    public class DataSummary
    {
        public ThBlockNumStatistics BlockData { get; set; }
    }
}
