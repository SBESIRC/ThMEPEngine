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
        public int GangjinType;
    }
}
