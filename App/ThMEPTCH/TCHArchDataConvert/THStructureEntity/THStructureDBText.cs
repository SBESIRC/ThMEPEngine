using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPTCH.TCHArchDataConvert.THStructureEntity
{
    public class THStructureDBText : THStructureEntity
    {
        public DBTextType TextType { get; set; }

        /// <summary>
        /// 内容
        /// </summary>
        public string Content { get; set; }

        /// <summary>
        /// 文字方向
        /// </summary>
        public Vector3d Vector { get; set; }

        public Point3d Point { get; set; }
    }

    public enum DBTextType
    {
        BeamText,
    }
}
