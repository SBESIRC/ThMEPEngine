using Autodesk.AutoCAD.DatabaseServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPStructure.GirderConnect.SecondaryBeamConnect.Service;

namespace ThMEPStructure.GirderConnect.SecondaryBeamConnect.Model
{
    public class BeamEdge
    {
        public BeamEdge(Line line)
        {
            this.TrueSide = line;
        }
        //真实边
        public Line TrueSide { get; set; }
        //对应主梁
        public Line BeamSide { get; set; }
        //梁类型
        public BeamType BeamType { get; set; }
    }
}
