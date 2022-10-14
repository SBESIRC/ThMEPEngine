using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using AcHelper;
using NFox.Cad;
using Linq2Acad;
using Dreambuild.AutoCAD;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

using ThCADExtension;
using ThCADCore.NTS;
using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore.Diagnostics;
using ThMEPEngineCore.GeojsonExtractor;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.LaneLine;
using NetTopologySuite.Geometries;

using ThMEPWSS.DrainageADPrivate.Model;

namespace ThMEPWSS.DrainageADPrivate.Service
{
    internal class ThCreateLineService
    {
        //public static List<Line> TurnNodeToTransLine(List<ThDrainageTreeNode> allNode)
        //{
        //    var allLine = new List<Line>();

        //    foreach (var node in allNode)
        //    {
        //        if (node.Parent != null)
        //        {
        //            var line = new Line(node.Parent.TransPt, node.TransPt);
        //            allLine.Add(line);
        //        }
        //    }
        //    return allLine;
        //}

        public static Dictionary<Line, ThDrainageTreeNode> TurnNodeToTransLineDict(List<ThDrainageTreeNode> allNode)
        {
            var allLineNodeDict = new Dictionary<Line, ThDrainageTreeNode>();

            foreach (var node in allNode)
            {
                if (node.Parent != null)
                {
                    var line = new Line(node.Parent.TransPt, node.TransPt);
                    allLineNodeDict.Add(line, node);
                }
            }
            return allLineNodeDict;
        }
    }
}
