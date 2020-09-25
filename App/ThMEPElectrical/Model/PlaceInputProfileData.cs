using Autodesk.AutoCAD.DatabaseServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPElectrical.Model
{
    /// <summary>
    /// 主次梁的结构数据
    /// </summary>
    public class PlaceInputProfileData
    {
        // 主梁构成的轮廓区域(次梁高于600递归后的主梁）
        public Polyline MainBeamOuterProfile { get; private set; }

        // 包含次梁构成的轮廓，可能没有次梁轮廓
        public List<Polyline> SecondBeamProfiles { get; private set; }

        public PlaceInputProfileData(Polyline poly)
        {
            MainBeamOuterProfile = poly;
            SecondBeamProfiles = new List<Polyline>();
        }

        public PlaceInputProfileData(Polyline poly, List<Polyline> srcPlaceProfiles)
        {
            MainBeamOuterProfile = poly;
            SecondBeamProfiles = srcPlaceProfiles;
        }
    }
}
