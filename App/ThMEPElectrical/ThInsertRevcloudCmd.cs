using System.Collections.Generic;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Runtime;

using ThMEPElectrical.BlockConvert;

namespace ThMEPElectrical
{
    public class ThInsertRevcloud
    {
        private static List<ThRevcloudParameter> ParameterList = new List<ThRevcloudParameter>();

        public static void Set(List<ThRevcloudParameter> parameterList)
        {
            ParameterList = parameterList;
        }

        public static void Set(Database database, Polyline obb, short colorIndex, string lineType, double scale)
        {
            ParameterList.Add(new ThRevcloudParameter(database, obb, colorIndex, lineType, scale));
        }


        [CommandMethod("TIANHUACAD", "THREVClOUD", CommandFlags.Modal)]
        public void THREVClOUD()
        {
            ThBConvertUtils.InsertRevcloud(ParameterList);
        }
    }
}
