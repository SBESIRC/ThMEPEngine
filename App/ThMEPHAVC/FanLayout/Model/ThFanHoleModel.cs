using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using DotNetARX;
using Linq2Acad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPHVAC.FanLayout.Model
{
    public class ThFanHoleModel
    {
        public Point3d FanHolePosition { set; get; }//洞口位置
        public double FontHeight { set; get; }//文字高度
        public double FanHoleWidth { set; get; }//洞口宽度
        public double FanHoleAngle { set; get; }//洞口角度
        public string FanHoleSize { set; get; }//洞口尺寸
        public string FanHoleMark { set; get; }//标高

        public void InsertFanHole(AcadDatabase acadDatabase)
        {
            Dictionary<string, string> attNameValues = new Dictionary<string, string>();
            attNameValues.Add("标高", FanHoleMark );
            attNameValues.Add("洞口尺寸", FanHoleSize);
            var blkId = acadDatabase.ModelSpace.ObjectId.InsertBlockReference("H-HOLE", "AI-洞口", FanHolePosition, new Scale3d(1, 1, 1), 0, attNameValues);
            var blk = acadDatabase.Element<BlockReference>(blkId);
            if (blk.IsDynamicBlock)
            {
                foreach (DynamicBlockReferenceProperty property in blk.DynamicBlockReferencePropertyCollection)
                {
                    if (property.PropertyName == "洞口宽度")
                    {
                        property.Value = FanHoleWidth;
                    }
                    if (property.PropertyName == "洞口角度")
                    {
                        property.Value = FanHoleAngle;
                    }
                    if (property.PropertyName == "文字高度")
                    {
                        property.Value = FontHeight;
                    }
                }
            }
        }
    }
}
