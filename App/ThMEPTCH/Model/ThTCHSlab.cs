using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using ThMEPEngineCore.Model;

namespace ThMEPTCH.Model
{
    public class ThTCHSlab: ThIfcSlab
    {
        /// <summary>
        /// 降板信息
        /// </summary>
        public List<ThTCHSlabDescendingData> Descendings { get; set; }

        /// <summary>
        /// 拉伸方向
        /// </summary>
        public Vector3d ExtrudedDirection { get; private set; }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="polygon"></param>
        /// <param name="thickness"></param>
        /// <param name="extVector"></param>
        public ThTCHSlab(MPolygon polygon, double thickness, Vector3d extVector)
        {
            Outline = polygon;
            Thickness = thickness;
            ExtrudedDirection = extVector;
        }

        public ThTCHSlab(Polyline pline, double thickness, Vector3d extVector)
        {
            Outline = pline;
            Thickness = thickness;
            ExtrudedDirection = extVector;
        }
    }
}
