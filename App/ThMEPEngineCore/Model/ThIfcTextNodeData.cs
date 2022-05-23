using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.Model
{
    public class ThIfcTextNodeData : ThIfcAnnotation
    {
        public string Text { get; set; } = "";
        public Polyline Geometry { get; set; }
        public Entity Data { get; set; }

        public ThIfcTextNodeData(string text, Polyline geometry, Entity data)
        {
            Text = text;
            Geometry = geometry;
            Data = data;
        }

    }
}
