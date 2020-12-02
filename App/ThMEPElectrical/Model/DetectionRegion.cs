using Autodesk.AutoCAD.DatabaseServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPElectrical.Model
{
    /// <summary>
    /// 探测区轮廓 + 洞 + 探测区域内的次梁信息
    /// </summary>
    public class DetectionRegion
    {
        public Polyline DetectionProfile = null; //探测区域轮廓
        public List<Polyline> DetectionInnerProfiles = new List<Polyline>(); // 探测区域内洞口， 初步可能有数据，但是后期分割后可能不存在洞口


        public List<SecondBeamProfileInfo> secondBeams = new List<SecondBeamProfileInfo>(); // 每个探测区域中的相关的次梁
        public bool IsHasInBeams = false;
    }
}
