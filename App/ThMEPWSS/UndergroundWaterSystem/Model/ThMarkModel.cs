using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPWSS.UndergroundWaterSystem.Model
{
    public class ThMarkModel
    {
        public Point3d Poistion { set; get; }//标注指示点
        public string MarkText { set; get; }//标注文字
        public ThMarkModel()
        {
            MarkText = "";
        }
    }
}
