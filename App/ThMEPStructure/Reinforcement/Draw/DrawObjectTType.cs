using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.AutoCAD.Geometry;
using ThMEPStructure.Reinforcement.Model;
using ThMEPEngineCore.CAD;

namespace ThMEPStructure.Reinforcement.Draw
{
    class DrawObjectTType : DrawObjectBase
    {
        ThTTypeEdgeComponent thTTypeEdgeComponent;
        public override void DrawOutline(string drawingScale)
        {
            int scale = 100 / int.Parse(drawingScale.Substring(2));
            var pts = new Point3dCollection
            {
                TableStartPt + new Vector3d(200, -1500, 0)* scale,
                TableStartPt + new Vector3d(200, -1500 - thTTypeEdgeComponent.Bw, 0) * scale,
                //TableStartPt + new Vector3d(200, -1500 - thTTypeEdgeComponent.Bw, 0),
            }
        }
    }
}
