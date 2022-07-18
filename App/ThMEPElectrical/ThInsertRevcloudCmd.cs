using System.Collections.Generic;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Runtime;

using ThMEPElectrical.BlockConvert;
using ThMEPElectrical.Model;
using ThMEPElectrical.ViewModel;

namespace ThMEPElectrical
{
    public class ThInsertRevcloud
    {
        private static List<ThRevcloudParameter> ParameterList { get; set; }

        public static void Set(List<ThRevcloudParameter> parameterList)
        {
            ParameterList = parameterList;
        }

        public static void Set(Database database, Polyline obb, short colorIndex, string lineType, double scale)
        {
            ParameterList = new List<ThRevcloudParameter>();
            ParameterList.Add(new ThRevcloudParameter(database, obb, colorIndex, lineType, scale));
        }


        [CommandMethod("TIANHUACAD", "THREVClOUD", CommandFlags.Modal)]
        public void THREVClOUD()
        {
            ThBConvertUtils.InsertRevcloud(ParameterList);
        }
    }

    public class ThBConvertZoom
    {
        public static BlockConvertInfo Info { get; set; }
        public static ThBlockConvertVM blockConvertVM { get; set; }

        public static void Set(BlockConvertInfo info, ThBlockConvertVM _blockConvertVM)
        {
            Info = info;
            blockConvertVM = _blockConvertVM;
        }

        [CommandMethod("TIANHUACAD", "THBCONVERTZOOM", CommandFlags.Modal)]
        public void THBCONVERTZOOM()
        {
            blockConvertVM.Zoom(Info);
        }
    }
}
