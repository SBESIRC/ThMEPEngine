using System.Collections.Generic;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Runtime;

using ThMEPElectrical.BlockConvert;

namespace ThMEPElectrical
{
    public class ThInsertRevcloud
    {
        private static List<ThRevcloudParameter> PumpRevclouds = new List<ThRevcloudParameter>();

        private static List<ThRevcloudParameter> CompareRevclouds = new List<ThRevcloudParameter>();

        public static void Set(List<ThRevcloudParameter> parameterList)
        {
            CompareRevclouds = parameterList;
        }

        public static void Set(Database database, Polyline obb, short colorIndex, string lineType, double scale)
        {
            PumpRevclouds.Add(new ThRevcloudParameter(database, obb, colorIndex, lineType, scale));
        }

        [CommandMethod("TIANHUACAD", "THPUMPREVCLOUD", CommandFlags.Modal)]
        public void THPUMPREVCLOUD()
        {
            ThBConvertUtils.InsertRevcloud(PumpRevclouds);
            PumpRevclouds.Clear();
        }

        [CommandMethod("TIANHUACAD", "THCOMPAREREVCLOUD", CommandFlags.Modal)]
        public void THCOMPAREREVCLOUD()
        {
            ThBConvertUtils.InsertRevcloud(CompareRevclouds);
            CompareRevclouds.Clear();
        }
    }
}
