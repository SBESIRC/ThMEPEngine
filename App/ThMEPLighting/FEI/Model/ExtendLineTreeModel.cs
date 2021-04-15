using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPLighting.FEI.Model
{
    public class ExtendLineTreeModel
    {
        public ExtendLineTreeModel(ExtendLineModel _extendLine, ExtendLineTreeModel _parentExtendLine)
        {
            ExtendLine = _extendLine;
            parentExtendLine = _parentExtendLine;
        }

        public ExtendLineModel ExtendLine { get; set; }

        public ExtendLineTreeModel parentExtendLine { get; set; }
    }
}
