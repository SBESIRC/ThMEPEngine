using Autodesk.AutoCAD.DatabaseServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPLighting.DSFEL.Model
{
    public class RoomInfoModel
    {
        /// <summary>
        /// 所属房间
        /// </summary>
        public Polyline room { get; set; }

        /// <summary>
        /// 主要疏散路径
        /// </summary>
        public List<Line> evacuationPaths { get; set; }

        /// <summary>
        /// 出口疏散图块
        /// </summary>
        public List<ExitModel> exitModels { get; set; }
    }
}
