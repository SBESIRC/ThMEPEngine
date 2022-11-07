using AcHelper;
using Autodesk.AutoCAD.EditorInput;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using ThMEPArchitecture.PartitionLayout;
using Accord.Math;
using ThParkingStall.Core.Tools;

namespace ThMEPArchitecture.ParkingStallArrangement.General
{
    internal class Utils
    {
        public static bool SetLayoutMainDirection()
        {
            var options = new PromptKeywordOptions("\n优先方向：");

            options.Keywords.Add("纵向", "V", "纵向(V)");
            options.Keywords.Add("横向", "H", "横向(H)");
            options.Keywords.Add("长度", "L", "长度(L)");

            options.Keywords.Default = "纵向";
            var rstDirection = Active.Editor.GetKeywords(options);
            if (rstDirection.Status != PromptStatus.OK)
            {
                return false;
            }

            if (rstDirection.StringResult.Equals("纵向"))
            {
                ThMEPArchitecture.PartitionLayout.ParkingPartitionPro.LayoutMode = ((int)LayoutDirection.VERTICAL);
            }
            else if (rstDirection.StringResult.Equals("横向"))
            {
                ThMEPArchitecture.PartitionLayout.ParkingPartitionPro.LayoutMode = ((int)LayoutDirection.HORIZONTAL);
            }
            else
            {
                ThMEPArchitecture.PartitionLayout.ParkingPartitionPro.LayoutMode = ((int)LayoutDirection.LENGTH);
            }

            return true;
        }
    }
}
