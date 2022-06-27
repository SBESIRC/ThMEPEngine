using System;
using System.Linq;
using Dreambuild.AutoCAD;
using ThMEPLighting.Common;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThCADExtension;
using Autodesk.AutoCAD.Geometry;

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
                        if(IsExisted(n.Position,n.Number, normalLine.Angle))
                        {
                            var code = CreateText(n.Number, n.Position, normalLine.LineDirection(), normalLine.Angle);
                            numberTexts.Add(code);
                        }
                    }
                });
            });
            return numberTexts;
        }
        private DBText CreateText(string number,Point3d position,Vector3d vec,double angle)
        {
            DBText code = new DBText();
            code.TextString = number;
            var alignPt = position + vec
              .GetPerpendicularVector()
              .GetNormal()
              .MultiplyBy(Height + Gap + TextHeight / 2.0);
            code.Height = TextHeight;
            code.WidthFactor = TextWidthFactor;
            code.Position = alignPt;
            angle = angle / Math.PI * 180.0;
            angle = angle / 180.0 * Math.PI;
            code.Rotation = angle;
            code.HorizontalMode = TextHorizontalMode.TextCenter;
            code.VerticalMode = TextVerticalMode.TextVerticalMid;
            code.AlignmentPoint = code.Position;
            return code;
        }
    }
}
