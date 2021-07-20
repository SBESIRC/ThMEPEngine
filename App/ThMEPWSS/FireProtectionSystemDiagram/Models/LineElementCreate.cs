using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPWSS.FireProtectionSystemDiagram.Models
{
    class LineElementCreate
    {
        public CreateBlockInfo createBlock { get; }
        public CreateDBTextElement createDBText { get; }
        public EnumElementType enumElement { get; }
        public EnumPosition enumPosition { get; }
        public double width { get; }
        public double marginPrevious { get; }
        public LineElementCreate(CreateDBTextElement createDBText, double marginPrevious)
        {
            this.enumElement = EnumElementType.Text;
            this.enumPosition = EnumPosition.LeftBottom;
            this.createDBText = createDBText;
            this.marginPrevious = marginPrevious;
        }
        public LineElementCreate(CreateBlockInfo createBlock, double width, double marginPrevious, EnumPosition enumPosition)
        {
            this.createBlock = createBlock;
            this.enumElement = EnumElementType.Block;
            this.enumPosition = enumPosition;
            this.width = width;
            this.marginPrevious = marginPrevious;
        }
    }
    enum EnumElementType
    {
        Text = 1,
        Block = 2,
    }
    enum EnumPosition
    {
        Center = 1,
        LeftCenter = 2,
        LeftBottom = 3,
    }
}
