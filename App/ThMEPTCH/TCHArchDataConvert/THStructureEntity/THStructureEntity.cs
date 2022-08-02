using Autodesk.AutoCAD.DatabaseServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPTCH.TCHArchDataConvert.THStructureEntity
{
    public abstract class THStructureEntity
    {
        public Polyline Outline { get; set; }

        /// <summary>
        /// 拉伸方向长度
        /// </summary>
        public double Height { get; set; }

        /// <summary>
        /// 标高
        /// </summary>
        //public double Elevation { get; set; }
        
        /// <summary>
        /// 顶高
        /// </summary>
        public double TopHeight { get; set; }
        
        /// <summary>
        /// 底高
        /// </summary>
        public double BottomHeight { get; set; }
    }
}
