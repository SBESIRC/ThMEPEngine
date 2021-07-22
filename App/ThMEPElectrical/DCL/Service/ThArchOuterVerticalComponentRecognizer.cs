using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThMEPElectrical.DCL.Data;

namespace ThMEPElectrical.DCL.Service
{
    public class ThArchOuterVerticalComponentRecognizer : ThOuterVerticalComponentRecognizer
    {        
        private ThArchOuterVertialComponentData InputData { get; set; }
        private const double OuterArchOutlineOffsetLength = 2000.0;
        private const double HoleArchOutlineOffsetLength = 2000.0;
        public ThArchOuterVerticalComponentRecognizer(ThOuterVertialComponentData inputData)
        {
            if (inputData is ThArchOuterVertialComponentData data)
                InputData = data;
            OuterOutlineBufferDic = Buffer(InputData.OuterOutlines, -OuterArchOutlineOffsetLength);
            InnerOutlineBufferDic = Buffer(InputData.InnerOutlines, HoleArchOutlineOffsetLength);
        }
        public override void Recognize()
        {
            //创建柱和剪力墙的索引
            var ColumnSpatialIndex = new ThCADCoreNTSSpatialIndex(InputData.Columns);
            var ShearWallSpatialIndex = new ThCADCoreNTSSpatialIndex(InputData.Shearwalls);
            //获取外圈构建
            OuterLineHandleColumn(ColumnSpatialIndex, OuterOutlineBufferDic);
            OuterLineHandleShearWall(ShearWallSpatialIndex, OuterOutlineBufferDic);
            //获取洞口外圈构件
            InnerLineHandleColumn(ColumnSpatialIndex, InnerOutlineBufferDic);
            InnerLineHandleShearWall(ShearWallSpatialIndex, InnerOutlineBufferDic);
            //获取其他构件
            OtherColumns = DBObjectCollectionSubtraction(InputData.Columns, OuterColumns);
            OtherShearwalls = DBObjectCollectionSubtraction(InputData.Shearwalls, OuterShearwalls);
        }
    }
}
