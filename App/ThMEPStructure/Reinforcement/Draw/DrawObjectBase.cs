using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using ThMEPStructure.Reinforcement.Model;
using ThMEPEngineCore.CAD;

namespace ThMEPStructure.Reinforcement.Draw
{
    abstract class DrawObjectBase
    {
        public void DrawGangJin()
        {
            foreach (var gangJin in GangJinBases)
            {
                gangJin.Draw();
            }
        }

        /// <summary>
        /// 绘制表格
        /// </summary>
        public void DrawTable()
        {

        }

        /// <summary>
        /// 绘制轮廓
        /// </summary>
        /// <param name="drawingScale"></param>
        public abstract void DrawOutline(string drawingScale);

        /// <summary>
        /// 绘制轮廓尺寸
        /// </summary>
        /// <param name="drawingScale"></param>
        public abstract void DrawDim(string drawingScale);
        /// <summary>
        /// 绘制连接墙体
        /// </summary>
        /// <param name="drawingScale"></param>
        public abstract void DrawWall(string drawingScale);

        public Point3d TableStartPt;
        public Polyline Outline;
        public List<Curve> LinkedWallLines;
        public List<RotatedDimension> rotatedDimensions;
        public List<GangJinBase> GangJinBases;


        //初始化钢筋列表
        public void Init(ThLTypeEdgeComponent thLTypeEdgeComponent)
        {
            GangJinBases = new List<GangJinBase>();
            GangJinStirrup gangJinStirrup = new GangJinStirrup();
            GangJinBases.Add(gangJinStirrup);
            GangJinReinforce gangJinReinforce = new GangJinReinforce();
            GangJinBases.Add(gangJinReinforce);
        }

        public void Init(ThRectangleEdgeComponent thRectangleEdgeComponent)
        {
            GangJinBases = new List<GangJinBase>();
            GangJinStirrup gangJinStirrup = new GangJinStirrup();
            GangJinBases.Add(gangJinStirrup);
            GangJinReinforce gangJinReinforce = new GangJinReinforce();
            GangJinBases.Add(gangJinReinforce);
        }

        public void Init(ThTTypeEdgeComponent thTTypeEdgeComponent)
        {
            GangJinBases = new List<GangJinBase>();
            GangJinStirrup gangJinStirrup = new GangJinStirrup();
            GangJinBases.Add(gangJinStirrup);
            GangJinReinforce gangJinReinforce = new GangJinReinforce();
            GangJinBases.Add(gangJinReinforce);
        }

        public Polyline GenPouDuan(Point3d pt1, Point3d pt2, Point3d linePt, out Line line1, out Line line2)
        {
            double length = pt1.DistanceTo(pt2);
            Vector3d direction = linePt - (pt1 + (pt2 - pt1).GetNormal().DotProduct(linePt - pt1) * (pt2 - pt1).GetNormal());
            line1 = new Line
            {
                StartPoint = pt1,
                EndPoint = pt1 + direction.GetNormal() * length * 5 / 8.0
            };
            line2 = new Line
            {
                StartPoint = pt2,
                EndPoint = pt2 + direction.GetNormal() * length * 5 / 8.0
            };
            var pts = new Point3dCollection
            {
                pt1 + direction.GetNormal() * length * 5 / 8.0 - (pt2 - pt1) * 3 / 8.0,
                pt1 + direction.GetNormal() * length * 5 / 8.0 + (pt2 - pt1) * 3 / 8.0,
                pt1 + direction.GetNormal() * length * 7 / 8.0 + (pt2 - pt1) * 7 / 16.0,
                pt1 + direction.GetNormal() * length * 3 / 8.0 + (pt2 - pt1) * 9 / 16.0,
                pt1 + direction.GetNormal() * length * 5 / 8.0 + (pt2 - pt1) * 5 / 8.0,
                pt1 + direction.GetNormal() * length * 5 / 8.0 + (pt2 - pt1) * 11 / 8.0
            };
            return pts.CreatePolyline();
        }
    }
}
