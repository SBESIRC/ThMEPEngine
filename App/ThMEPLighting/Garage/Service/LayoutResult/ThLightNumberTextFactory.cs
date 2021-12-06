using System;
using System.Linq;
using Dreambuild.AutoCAD;
using ThMEPLighting.Common;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThCADExtension;

namespace ThMEPLighting.Garage.Service.LayoutResult
{
    public class ThLightNumberTextFactory : NumberTextFactory
    {        
        public ThLightNumberTextFactory(List<ThLightEdge> lightEdges):base(lightEdges)
        {
        }
        public override DBObjectCollection Build()
        {
            var numberTexts = new DBObjectCollection();
            LightEdges.Where(o => o.IsDX).ForEach(m =>
            {
                var normalLine = m.Edge.NormalizeLaneLine();
                m.LightNodes.ForEach(n =>
                {
                    if (!string.IsNullOrEmpty(n.Number))
                    {
                        DBText code = new DBText();
                        code.TextString = n.Number;
                        var alignPt = n.Position + normalLine.StartPoint.GetVectorTo(normalLine.EndPoint)
                          .GetPerpendicularVector()
                          .GetNormal()
                          .MultiplyBy(Height + Gap + TextHeight / 2.0);
                        code.Height = TextHeight;
                        code.WidthFactor = TextWidthFactor;
                        code.Position = alignPt;
                        double angle = normalLine.Angle / Math.PI * 180.0;
                        //angle = ThGarageLightUtils.LightNumberAngle(angle);
                        angle = angle / 180.0 * Math.PI;
                        code.Rotation = angle;
                        code.HorizontalMode = TextHorizontalMode.TextCenter;
                        code.VerticalMode = TextVerticalMode.TextVerticalMid;
                        code.AlignmentPoint = code.Position;
                        numberTexts.Add(code);
                    }
                });
            });
            return numberTexts;
        }
    }
}
