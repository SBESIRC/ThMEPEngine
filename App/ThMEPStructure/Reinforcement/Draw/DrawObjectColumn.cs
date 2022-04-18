using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using ThMEPStructure.Reinforcement.Model;
using ThMEPEngineCore.CAD;
using Dreambuild.AutoCAD;
namespace ThMEPStructure.Reinforcement.Draw
{
    class DrawObjectColumn
    {
        public DrawObjectBase ObjectBase;
        public DBObjectCollection objectCollection;
        public ThRectangleEdgeComponent thRectangleEdgeComponent;
        void TransformFromColumn(ThColumnComponent component)
        {
            if(component is ThRectangleColumnComponent)
            {
                ThRectangleColumnComponent thRectangleColumnComponent = component as ThRectangleColumnComponent;
                thRectangleEdgeComponent.Bw = thRectangleColumnComponent.Bw;
                thRectangleEdgeComponent.C = thRectangleColumnComponent.C;
                thRectangleEdgeComponent.Hc = thRectangleColumnComponent.Hc;
                thRectangleEdgeComponent.Link2 = thRectangleColumnComponent.Link2;
                thRectangleEdgeComponent.Link3 = thRectangleColumnComponent.Link3;
                thRectangleEdgeComponent.Number = thRectangleColumnComponent.Number;
                thRectangleEdgeComponent.PointReinforceLineWeight = thRectangleColumnComponent.PointReinforceLineWeight;
                thRectangleEdgeComponent.Reinforce = thRectangleColumnComponent.Reinforce;
                thRectangleEdgeComponent.Stirrup = thRectangleColumnComponent.Stirrup;
                thRectangleEdgeComponent.StirrupLineWeight = thRectangleColumnComponent.StirrupLineWeight;
            }
            
        }
        public void CalAndDrawGangJin(double H, double W, ThColumnComponent component, Point3d point)
        {
            ObjectBase.CalAndDrawGangJin(H, W, thRectangleEdgeComponent, point);
        }

        public void init(ThColumnComponent component, string elevation, double tblRowHeight, double scale, Point3d position)
        {
            if (component is ThRectangleColumnComponent)
            {
                ObjectBase = new DrawObjectColumnRectangle();
            }
                
            thRectangleEdgeComponent = new ThRectangleEdgeComponent();
            TransformFromColumn(component);
            ObjectBase.init(thRectangleEdgeComponent, elevation, tblRowHeight, scale, position);

        }

        public void GetTableFirstRowHW(out double firstRowHeight, out double firstRowWidth)
        {
            ObjectBase.GetTableFirstRowHW(out firstRowHeight,out firstRowWidth);
        }

    }
}
