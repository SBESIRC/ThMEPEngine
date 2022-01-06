using Autodesk.AutoCAD.DatabaseServices;
using System;
using System.Collections.Generic;
using ThMEPEngineCore.Command;
using ThMEPEngineCore.Service.Hvac;
using ThMEPHVAC.Algorithm;
using ThMEPHVAC.Model;

namespace TianHua.Hvac.UI.Command
{
    public class ThCutCurveTestCmd : ThMEPBaseCommand, IDisposable
    {
        public ThCutCurveTestCmd()
        {
        }
        public void Dispose()
        {
            //
        }
        public override void SubExecute()
        {
            var curves = new List<Curve>();
            var lines = new DBObjectCollection();
            var centerlines = ThDuctPortsReadComponent.GetCenterlineByLayer(ThHvacCommon.AI_DUCT_ROUTINE);
            foreach (var a in centerlines)
            {
                if (a is Line)
                    lines.Add(a as Line);
                else if (a is Curve)
                    curves.Add(a as Curve);
            }
            var cutter = new ThPolygonlizerCurveLine(curves, lines);
            cutter.SplitArcs();
            cutter.Draw();
        }
    }
}
