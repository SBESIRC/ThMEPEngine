using Autodesk.AutoCAD.DatabaseServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPElectrical.Model
{
    public class DetectionRegion
    {
        public Polyline DetectionProfile = null; //探测区域轮廓
        public List<BeamProfile> secondBeams = new List<BeamProfile>(); // 每个探测区域中的相关的次梁
        public bool IsHasInBeams = false;
    }
}
