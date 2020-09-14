﻿using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPElectrical.Business.MainBeam;
using ThMEPElectrical.Model;

namespace ThMEPElectrical.Layout.MainSecBeamLayout
{
    //次梁
    public class MainSecondBeamPolygonLayout : MainSecondBeamLayout
    {
        public MainSecondBeamPolygonLayout(PlaceInputProfileData inputProfileDatas, PlaceParameter parameter, Polyline poly)
            : base(inputProfileDatas, parameter)
        {
            PostPoly = poly;
        }

        public override List<Point3d> CalculatePlace()
        {
            if (m_inputProfileData == null || m_parameter == null)
                return new List<Point3d>();

            var layoutData = new LayoutProfileData(m_inputProfileData.MainBeamOuterProfile, PostPoly);
            // 单个布置
            m_placePoints = MultiSegmentPlace.MakeABBPolygonProfilePoints(layoutData, m_parameter);

            return PlacePoints;
        }
    }
}
