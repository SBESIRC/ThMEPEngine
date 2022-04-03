using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
namespace ThMEPStructure.Reinforcement.Draw
{
    abstract class GangJinBase
    {
        public abstract void Draw();

        /// <summary>
        /// 钢筋类型，0为纵筋，1为箍筋，2为拉筋
        /// </summary>
        public int GangjinType;
    }
}
