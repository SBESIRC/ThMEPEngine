using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPEngineCore.ConnectWiring.Model
{
    /// <summary>
    /// 外包框
    /// </summary>
    public class FrameModel
    {
        /// <summary>
        /// 原始框线
        /// </summary>
        public Polyline OriginFrame { get; set; }

        /// <summary>
        /// ucs分割框线
        /// </summary>
        public List<UcsFrameModel> UcsPolys = new List<UcsFrameModel>();

        /// <summary>
        /// 原始框线方向
        /// </summary>
        public Vector3d dir { get; set; }

        /// <summary>
        /// 电源块
        /// </summary>
        public BlockReference Power { get; set; }

        /// <summary>
        /// 框线内的洞口
        /// </summary>
        public List<Polyline> Holes = new List<Polyline>();
    }

    public class UcsFrameModel
    {
        /// <summary>
        /// ucs框线
        /// </summary>
        public Polyline Frame { get; set; }

        /// <summary>
        /// ucs框线方向
        /// </summary>
        public Vector3d dir { get; set; }
    }
}
