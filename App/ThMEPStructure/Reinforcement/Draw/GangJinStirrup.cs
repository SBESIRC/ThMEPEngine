using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.ApplicationServices;
using ThMEPStructure.Reinforcement.Model;

namespace ThMEPStructure.Reinforcement.Draw
{
    class GangJinStirrup:GangJinBase
    {
        public List<Polyline> stirrups = new List<Polyline>();
        public Polyline Outline;
        public double scale;

        public void CalPositionR(ThRectangleEdgeComponent thRectangleEdgeComponent)
        {
            Point3d pt = Outline.GetPoint3dAt(0);
            double offset = (thRectangleEdgeComponent.C + 5) * scale + thRectangleEdgeComponent.PointReinforceLineWeight + thRectangleEdgeComponent.StirrupLineWeight;
            pt += new Vector3d(offset, -offset, 0);
            double r = thRectangleEdgeComponent.PointReinforceLineWeight + thRectangleEdgeComponent.StirrupLineWeight / 2;
            double width = thRectangleEdgeComponent.Hc * scale - 2 * offset;
            double height = thRectangleEdgeComponent.Bw * scale - 2 * offset;
            Polyline polyline = DrawStirrup(width, height, pt, r, thRectangleEdgeComponent.StirrupLineWeight);
            stirrups.Add(polyline);
        }

        public void CalPositionL(ThLTypeEdgeComponent thLTypeEdgeComponent)
        {
            Point3d pt = Outline.GetPoint3dAt(0);
            double offset = (thLTypeEdgeComponent.C + 5) * scale + thLTypeEdgeComponent.PointReinforceLineWeight + thLTypeEdgeComponent.StirrupLineWeight;
            pt += new Vector3d(offset, -offset, 0);
            double r = thLTypeEdgeComponent.PointReinforceLineWeight + thLTypeEdgeComponent.StirrupLineWeight / 2;
            double width = thLTypeEdgeComponent.Bf * scale - 2 * offset;
            double height = (thLTypeEdgeComponent.Hc1 + thLTypeEdgeComponent.Bw) * scale - 2 * offset;
            Polyline polyline = DrawStirrup(width, height, pt, r, thLTypeEdgeComponent.StirrupLineWeight);
            stirrups.Add(polyline);

            pt = Outline.GetPoint3dAt(1);
            pt += new Vector3d(offset, -offset, 0);
            width = (thLTypeEdgeComponent.Bf + thLTypeEdgeComponent.Hc2) * scale - 2 * offset;
            height = thLTypeEdgeComponent.Bw * scale - 2 * offset;
            polyline = DrawStirrup(width, height, pt, r, thLTypeEdgeComponent.StirrupLineWeight);
            stirrups.Add(polyline);
        }

        public void CalPositionT(ThTTypeEdgeComponent thTTypeEdgeComponent)
        {
            Point3d pt = Outline.GetPoint3dAt(0);
            double offset = (thTTypeEdgeComponent.C + 5) * scale + thTTypeEdgeComponent.PointReinforceLineWeight + thTTypeEdgeComponent.StirrupLineWeight;
            pt += new Vector3d(offset, -offset, 0);
            double r = thTTypeEdgeComponent.PointReinforceLineWeight + thTTypeEdgeComponent.StirrupLineWeight / 2;
            double width = thTTypeEdgeComponent.Bf * scale - 2 * offset;
            double height = (thTTypeEdgeComponent.Bw + thTTypeEdgeComponent.Hc1) * scale - 2 * offset;
            Polyline polyline = DrawStirrup(width, height, pt, r, thTTypeEdgeComponent.StirrupLineWeight);
            stirrups.Add(polyline);

            pt = Outline.GetPoint3dAt(2);
            pt += new Vector3d(offset, -offset, 0);
            width = (thTTypeEdgeComponent.Hc2s + thTTypeEdgeComponent.Bf + thTTypeEdgeComponent .Hc2l) * scale - 2 * offset;
            height = thTTypeEdgeComponent.Bw * scale - 2 * offset;
            polyline = DrawStirrup(width, height, pt, r, thTTypeEdgeComponent.StirrupLineWeight);
            stirrups.Add(polyline);
        }

        /// <summary>
        /// 生成箍筋
        /// </summary>
        /// <param name="width">内部横向点筋距离</param>    
        /// <param name="height">内部纵向点筋距离</param>
        /// <param name="startPt">左上角点筋位置</param>
        /// <param name="r">点筋到箍筋距离</param>
        /// <param name="lineWeight">线宽</param>
        /// <returns>箍筋</returns>
        protected Polyline DrawStirrup(double width, double height, Point3d startPt, double r, double lineWeight)
        {
            Polyline res = new Polyline();
            Point2d pt = new Point2d(startPt.X, startPt.Y);
            pt += new Vector2d(1, 1).GetNormal() * r + new Vector2d(1, -1).GetNormal() * 150 * scale / 4;
            res.AddVertexAt(0, new Point2d(pt.X, pt.Y), 0, lineWeight, lineWeight);
            pt += new Vector2d(-1, 1).GetNormal() * 150 * scale / 4;
            res.AddVertexAt(1, new Point2d(pt.X, pt.Y), Math.Tan(3 * Math.PI / 16), lineWeight, lineWeight);
            pt += new Vector2d(-1, -1).GetNormal() * r + new Vector2d(-1, 0) * r;
            res.AddVertexAt(2, new Point2d(pt.X, pt.Y), 0, lineWeight, lineWeight);
            pt += new Vector2d(0, -1) * height;
            res.AddVertexAt(3, new Point2d(pt.X, pt.Y), Math.Tan(Math.PI / 8), lineWeight, lineWeight);
            pt += new Vector2d(1, -1) * r;
            res.AddVertexAt(4, new Point2d(pt.X, pt.Y), 0, lineWeight, lineWeight);
            pt += new Vector2d(1, 0) * width;
            res.AddVertexAt(5, new Point2d(pt.X, pt.Y), Math.Tan(Math.PI / 8), lineWeight, lineWeight);
            pt += new Vector2d(1, 1) * r;
            res.AddVertexAt(6, new Point2d(pt.X, pt.Y), 0, lineWeight, lineWeight);
            pt += new Vector2d(0, 1) * height;
            res.AddVertexAt(7, new Point2d(pt.X, pt.Y), Math.Tan(Math.PI / 8), lineWeight, lineWeight);
            pt += new Vector2d(-1, 1) * r;
            res.AddVertexAt(8, new Point2d(pt.X, pt.Y), 0, lineWeight, lineWeight);
            pt += new Vector2d(-1, 0) * width;
            res.AddVertexAt(9, new Point2d(pt.X, pt.Y), Math.Tan(3 * Math.PI / 16), width, width);
            pt += new Vector2d(0, -1) * r + new Vector2d(-1, -1).GetNormal() * r;
            res.AddVertexAt(10, new Point2d(pt.X, pt.Y), 0, lineWeight, lineWeight);
            pt += new Vector2d(1, -1).GetNormal() * 150 * scale / 4;
            res.AddVertexAt(11, new Point2d(pt.X, pt.Y), 0, lineWeight, lineWeight);
            res.ConstantWidth = lineWeight;
            return res;
        }

        protected void DrawExplode()
        {

        }

        public override void Draw()
        {
            Document doc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            var documentlock = doc.LockDocument();
            Autodesk.AutoCAD.EditorInput.Editor ed = doc.Editor;
            ed.WriteMessage("我是箍筋");
            DrawExplode();
        }
    }
}
