using System;
using NFox.Cad;
using ThCADCore.NTS;
using ThCADExtension;
using Dreambuild.AutoCAD;
using System.Linq;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.LaneLine
{
    public class ThLaneLineJoinEngine
    {
        public static DBObjectCollection Join(DBObjectCollection curves)
        {
            var objs = new DBObjectCollection();
            var kdTree = new ThCADCoreNTSKdTree(20.0);
            curves.Cast<Line>().ForEach(o => kdTree.InsertLine(o));
            curves.Cast<Line>().ForEach(o =>
            {
                objs.Add(new Line(kdTree.Query(o.StartPoint), kdTree.Query(o.EndPoint)));
            });
            return objs;
        }
    }
}
