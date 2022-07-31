using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using ProtoBuf;
using System.Collections.Generic;

namespace ThMEPTCH.Model
{
    [ProtoContract]
    public class ThTCHSlab : ThTCHElement
    {
        /// <summary>
        /// 降板信息
        /// </summary>
        [ProtoMember(21)]
        public List<ThTCHSlabDescendingData> Descendings { get; set; }
        private ThTCHSlab()
        {

        }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="polygon"></param>
        /// <param name="thickness"></param>
        /// <param name="extVector"></param>
        public ThTCHSlab(MPolygon polygon, double thickness, Vector3d extVector)
        {
            Outline = polygon;
            Height = thickness;
            ExtrudedDirection = extVector;
            Descendings = new List<ThTCHSlabDescendingData>();
        }

        public ThTCHSlab(Polyline pline, double thickness, Vector3d extVector)
        {
            Outline = pline;
            Height = thickness;
            ExtrudedDirection = extVector;
            Descendings = new List<ThTCHSlabDescendingData>();
        }
    }
}
