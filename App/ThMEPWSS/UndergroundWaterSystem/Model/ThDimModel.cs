using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Dreambuild.AutoCAD;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPWSS.UndergroundWaterSystem.Model
{
    /// <summary>
    /// 管径标注
    /// </summary>
    public class ThDimModel : ThBaseModel
    {
        //文本
        public string StrText { set; get; }
        public ThDimModel()
        {
            StrText = "";
        }
        public override void Initialization(Entity entity)
        {
            if(entity is DBText dbText)
            {
                StrText = dbText.TextString;
                Position = dbText.Position.ToPoint2D().ToPoint3d();
            }
            else if(entity is BlockReference blk)
            {
                //todo
            }
        }
    }
}
