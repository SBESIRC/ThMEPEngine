using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using DotNetARX;
using Dreambuild.AutoCAD;
using Linq2Acad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPHVAC.FanLayout.Model
{
    public class ThFanAirPortMarkModel
    {
        public Point3d FanPosition { set; get; }//风机位置
        public Point3d AirPortMarkPosition { set; get; }//风口位置
        public double FontHeight { set; get; }
        public string AirPortMarkName { set; get; }//风口名称
        public string AirPortMarkSize { set; get; }//尺寸
        public string AirPortMarkCount { set; get; }//风口数量
        public string AirPortMarkVolume { set; get; }//风量
        public void InsertAirPortMark(AcadDatabase acadDatabase)
        {
            Dictionary<string, string> attNameValues = new Dictionary<string, string>();
            attNameValues.Add("风口名称", AirPortMarkName);
            attNameValues.Add("尺寸", AirPortMarkSize);
            attNameValues.Add("数量", AirPortMarkCount);
            attNameValues.Add("风量", AirPortMarkVolume);
            var blkId = acadDatabase.ModelSpace.ObjectId.InsertBlockReference("H-DIMS-DUCT", "风口标注", AirPortMarkPosition, new Scale3d(FontHeight, FontHeight, FontHeight), 0, attNameValues);
            var markLine = new Line(FanPosition, AirPortMarkPosition);
            markLine.LayerId = DbHelper.GetLayerId("W-FRPT-HYDT-PIPE-AI");
            acadDatabase.CurrentSpace.Add(markLine);
        }
    }
}
