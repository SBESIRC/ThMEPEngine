using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPEngineCore.Algorithm.AStarRoutingEngine.AStarModel;
using ThMEPEngineCore.Algorithm.AStarRoutingEngine.AStarModel.GlobelAStarModel;

namespace ThMEPEngineCore.Algorithm.AStarRoutingEngine.MapService
{
    public class GlobleMapHelper<T> : MapHelper<T>
    {
        public List<GloblePoint> insertPts;
        public GlobleMapHelper(double _step) : base(_step) { insertPts = new List<GloblePoint>(); }

        public GloblePoint SetStartAndEndGlobleInfo(Point3d _startPt, T endInfo)
        {
            SetMapServiceInfo(_startPt, endInfo);

            //插入起点
            Point3d transSP = _startPt.TransformBy(ucsMatrix).TransformBy(moveMatrix.Inverse());
            var sPt = SetGlobleMapPoint(transSP);

            return sPt;
        }

        public override List<int> SetMapServiceInfo(Point3d transSP, T endInfo)
        {
            //设置service信息
            if (typeof(T) == typeof(Line))
            {
                var endLine = endInfo as Line;
                Line cloneLine = endLine.Clone() as Line;
                cloneLine.TransformBy(ucsMatrix);
                cloneLine.TransformBy(moveMatrix.Inverse());
                var sPt = SetGlobleMapPoint(cloneLine.StartPoint);
                var ePt = SetGlobleMapPoint(cloneLine.EndPoint);
                endEntity = new GlobleLine(sPt, ePt);
            }
            else if (typeof(T) == typeof(Point3d))
            {
                var endPt = endInfo as Point3d?;
                Point3d transEP = endPt.Value.TransformBy(ucsMatrix).TransformBy(moveMatrix.Inverse());
                var ePt = SetGlobleMapPoint(transEP);
                endEntity = ePt;
            }

            return new List<int>();
        }

        public override Point3d TransformMapPoint(AStarEntity ent)
        {
            var point = (GloblePoint)ent;
            var pt = new Point3d(point.X * step, point.Y * step, 0);
            return pt.TransformBy(moveMatrix).TransformBy(ucsMatrix.Inverse());
        }

        #region 废弃
        public override AStarEntity SetStartAndEndInfo(Point3d _startPt, T endInfo)
        {
            return null;
        }
        #endregion

        /// <summary>
        /// 将点插入棋盘地图
        /// </summary>
        /// <param name="pt"></param>
        private GloblePoint SetGlobleMapPoint(Point3d pt)
        {
            double sColumn = pt.X / step;
            double sRow = pt.Y / step;
            var globlePt = new GloblePoint(sColumn, sRow);
            insertPts.Add(globlePt);
            return globlePt;
        }
    }
}
