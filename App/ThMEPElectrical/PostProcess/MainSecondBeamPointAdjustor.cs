using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPElectrical.Model;
using ThMEPElectrical.PostProcess.MainSecondBeamAdjustor;

namespace ThMEPElectrical.PostProcess
{
    //主次梁调整类型
    public enum MSPlaceAdjustorType
    {
        SINGLEPLACE = 1, // 一个布置
        MEDIUMPLACE = 2, // 二个布置
        LARGEPLACE = 4, // 四个布置
        REGULARPLACE = 8, // 通用的位置调整逻辑
    }

    /// <summary>
    /// 主次梁 矩形 调整分发器
    /// </summary>
    public class MainSecondBeamPointAdjustor
    {
        private MainSecondBeamRegion m_beamSecondBeamRegion; // 有效的布置区域

        private MSPlaceAdjustorType m_msPlaceAdjustorType; // 主次梁调整类型

        public List<Point3d> PostPoints
        {
            get;
            private set;
        }

        public MainSecondBeamPointAdjustor(MainSecondBeamRegion beamSpanInfo, MSPlaceAdjustorType placeAdjustorType)
        {
            m_beamSecondBeamRegion = beamSpanInfo;
            m_msPlaceAdjustorType = placeAdjustorType;
        }

        /// <summary>
        /// 分发器
        /// </summary>
        public void Distribute()
        {
            switch(m_msPlaceAdjustorType)
            {
                case MSPlaceAdjustorType.SINGLEPLACE:
                    PostPoints = MainSecondBeamSingleAdjustor.MakeSecondBeamSingleAdjustor(m_beamSecondBeamRegion);
                    break;

                case MSPlaceAdjustorType.MEDIUMPLACE:
                    PostPoints = MainSecondBeamMediumAdjustor.MakeMainSecondBeamMediumAdjustor(m_beamSecondBeamRegion);
                    break;

                case MSPlaceAdjustorType.LARGEPLACE:
                    PostPoints = MainSecondBeamLargeAdjustor.MakeMainSecondBeamMediumAdjustor(m_beamSecondBeamRegion);
                    break;

                case MSPlaceAdjustorType.REGULARPLACE:
                    PostPoints = RegularPointMoveAdjustor.MakeRegularPointMoveAdjustor(m_beamSecondBeamRegion);
                    break;

                default:
                    throw new NotSupportedException();
            }
        }

        /// <summary>
        /// 移动梁跨插入点的方法入口
        /// </summary>
        /// <param name="beamSpanInfo"></param>
        /// <param name="pts"></param>
        /// <param name="spanType"></param>
        /// <returns></returns>
        public static List<Point3d> MakeMainBeamPointAdjustor(MainSecondBeamRegion beamSpanInfo, MSPlaceAdjustorType placeAdjustorType)
        {
            if (beamSpanInfo.ValidRegions.Count == 0)
                return new List<Point3d>();

            var mainBeamPointAdjustor = new MainSecondBeamPointAdjustor(beamSpanInfo, placeAdjustorType);
            mainBeamPointAdjustor.Distribute();
            return mainBeamPointAdjustor.PostPoints;
        }
    }
}
