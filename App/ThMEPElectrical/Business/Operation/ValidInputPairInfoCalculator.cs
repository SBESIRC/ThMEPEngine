using Autodesk.AutoCAD.DatabaseServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThMEPElectrical.Model;

namespace ThMEPElectrical.Business.Operation
{
    /// <summary>
    /// 有效组数据筛选器
    /// </summary>
    public class ValidInputPairInfoCalculator
    {
        private List<PlaceInputProfileData> m_inputPlaceProfileDatas;
        private PolygonInfo m_polygonInfo;
        private List<PlaceInputProfileData> m_validPlaceProfileDatas = new List<PlaceInputProfileData>();

        public List<PlaceInputProfileData> ValidProfileDatas
        {
            get { return m_validPlaceProfileDatas; }
        }

        public static List<PlaceInputProfileData> MakeValidInputPairInfoCalculator(List<PlaceInputProfileData> inputProfileDatas, PolygonInfo polygonInfo)
        {
            if (polygonInfo.InnerProfiles.Count == 0)
                return inputProfileDatas;

            var validProfileDataSelector = new ValidInputPairInfoCalculator(inputProfileDatas, polygonInfo);
            validProfileDataSelector.DoValidSelect();

            return validProfileDataSelector.ValidProfileDatas;
        }

        public ValidInputPairInfoCalculator(List<PlaceInputProfileData> inputPlaceProfileDatas, PolygonInfo polygonInfo)
        {
            m_inputPlaceProfileDatas = inputPlaceProfileDatas;
            m_polygonInfo = polygonInfo;
        }

        public void DoValidSelect()
        {
            // 内部洞口
            var innerHoles = m_polygonInfo.InnerProfiles;
            for (int i = 0; i < m_inputPlaceProfileDatas.Count; i++)
            {
                var curPlaceProfileData = m_inputPlaceProfileDatas[i];
                var curRegionProfile = curPlaceProfileData.MainBeamOuterProfile;
                var secondBeamProfiles = curPlaceProfileData.SecondBeamProfiles;
                var validPolys = SubtractRegion(curRegionProfile, innerHoles);

                if (validPolys.Count > 0)
                {
                    foreach (Polyline resPoly in validPolys)
                        m_validPlaceProfileDatas.Add(new PlaceInputProfileData(resPoly, secondBeamProfiles));
                }
            }
        }

        private List<Polyline> SubtractRegion(Polyline externalProfile, List<Polyline> innerHoles)
        {
            var dbLst = new DBObjectCollection();
            foreach (Polyline poly in innerHoles)
            {
                dbLst.Add(poly);
            }

            var resPolys = new List<Polyline>();
            foreach (Polyline item in externalProfile.Difference(dbLst))
            {
                resPolys.Add(item);
            }

            return resPolys;
        }
    }
}
