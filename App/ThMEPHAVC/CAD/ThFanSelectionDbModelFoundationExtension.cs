﻿using Autodesk.AutoCAD.DatabaseServices;
using TianHua.FanSelection;

namespace ThMEPHAVC.CAD
{
    public static class ThFanSelectionDbModelFoundationExtension
    {
        public static bool IsModelFoundation(this Entity entity)
        {
            return entity.GetXDataForApplication(ThFanSelectionCommon.RegAppName_Model_Foundation) != null;
        }

        public static bool IsModelFoundationLayer(this Entity entity)
        {
            return entity.Layer == ThFanSelectionCommon.FOUNDATION_LAYER;
        }
    }
}
