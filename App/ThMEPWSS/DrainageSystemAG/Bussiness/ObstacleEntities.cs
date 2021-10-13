using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using ThCADCore.NTS;
using ThCADExtension;

namespace ThMEPWSS.DrainageSystemAG.Bussiness
{
    public class ObstacleEntities
    {
        List<Polyline> _obstacleEntities;
        List<Line> _obstacleLines;
        List<Polyline> _obstacleCreateEntities;
        ThCADCoreNTSSpatialIndex _obstacleSpatialIndex;
        public ObstacleEntities() 
        {
            this._obstacleCreateEntities = new List<Polyline>();
            this._obstacleEntities = new List<Polyline>();
            this._obstacleLines = new List<Line>();
            var addDBColl = new DBObjectCollection();
            _obstacleSpatialIndex = new ThCADCoreNTSSpatialIndex(addDBColl);
        }
        public void AddMainLine(Line line) 
        {
            if (null == line)
                return;
            _obstacleLines.Add(line);
        }
        public void AddObstacleEntity(Polyline pline,bool isCreate)
        {
            if (null != pline && pline.Area < 10)
                return;
            _obstacleEntities.Clear();
            _obstacleEntities.Add(pline);
            if(isCreate)
                _obstacleCreateEntities.Add(pline);
            UpdataObstacleEntitys();
        }
        public void AddObstacleEntitys(List<Entity> entitys)
        {
            if (null == entitys || entitys.Count < 1)
                return;
            _obstacleEntities.Clear();
            foreach (var entity in entitys)
                AddObstacle(entity);
            UpdataObstacleEntitys();
        }
        void AddObstacle(Entity entity)
        {
            Polyline polyline = null;
            try
            {
                if (entity is BlockReference)
                {
                    var block = entity as BlockReference;
                    var ntsPLine = entity.GeometricExtents.ToNTSPolygon();
                    polyline = ntsPLine.ToDbPolylines().FirstOrDefault();
                }
                else if (entity is Polyline pLine)
                {
                    if (pLine.Area < 0.001)
                        return;
                    polyline = pLine;
                }
                else if (entity is Circle || entity is Arc)
                {
                    polyline = entity.GeometricExtents.ToNTSPolygon().ToDbPolylines().FirstOrDefault();
                }
                else if (entity is Line)
                {
                    polyline = (entity as Line).Buffer(10);
                }
                else if (entity is DBText || entity is MText)
                {
                    polyline = entity.GeometricExtents.ToNTSPolygon().ToDbPolylines().FirstOrDefault();
                }
            }
            catch (Exception ex)
            {
                polyline = null;
            }
            if (null != polyline)
            {
                _obstacleEntities.Add(polyline);
            }
        }
        void UpdataObstacleEntitys() 
        {
            var addDBColl = new DBObjectCollection();
            _obstacleEntities.ForEach(c =>
            {
                if (null != c && c.Area > 10)
                    addDBColl.Add(c);
            });
            _obstacleSpatialIndex.Update(addDBColl, new DBObjectCollection());
            _obstacleEntities.Clear();
        }
        public bool CheckBySpaceIndex(Polyline checkPLine) 
        {
            bool isIntersect = false;
            if (null == _obstacleSpatialIndex || checkPLine == null || checkPLine.Area < 10)
                return isIntersect;
            var crossPLines = _obstacleSpatialIndex.SelectCrossingPolygon(checkPLine).Cast<Entity>();
            isIntersect = crossPLines != null && crossPLines.Count() > 0;
            var textGeo = checkPLine.ToNTSPolygon();
            foreach (var item in _obstacleCreateEntities)
            {
                if (isIntersect)
                    break;
                if (item == null || item.Area < 10)
                    continue;
                var itemGeo = item.ToNTSPolygon();
                isIntersect = textGeo.Intersects(itemGeo) || textGeo.Crosses(itemGeo);
            }
            return isIntersect;
        }
        public bool CheckMainLineObstacle(Line mainLine) 
        {
            if (_obstacleLines == null || _obstacleLines.Count < 1)
                return false;
            var mainDir = (mainLine.EndPoint - mainLine.StartPoint).GetNormal();
            bool isColl = false;
            foreach (var line in _obstacleLines)
            {
                if (isColl)
                    break;
                var lineDir = (line.EndPoint - line.StartPoint).GetNormal();
                //先判断方向
                var dotDir = lineDir.DotProduct(mainDir);
                if (Math.Abs(dotDir) < 0.95)
                    continue;
                //再判断是否共线，将线投影到mainLine上，再进行精确判断
                var prjSp = line.StartPoint.PointToLine(mainLine);
                var prjEp = line.EndPoint.PointToLine(mainLine);
                if (prjSp.DistanceTo(line.StartPoint) > 10)
                    continue;
                var listPoints = new List<Point3d>() { prjSp, prjEp };
                listPoints = ThPointVectorUtil.PointsOrderByDirection(listPoints, mainDir, false).ToList();
                var spVector = listPoints.First() - mainLine.StartPoint;
                var epVector = listPoints.Last() - mainLine.EndPoint;
                var spDot = spVector.DotProduct(mainDir);
                if (spDot > -0.0001)
                {
                    if (spVector.Length < line.Length - 1)
                    {
                        isColl = true;
                        break;
                    }
                }
                else
                {
                    var spDotEp = spVector.DotProduct(epVector);
                    if (spDotEp < 0)
                    {
                        isColl = true;
                        break;
                    }
                }
            }
            return isColl;
        }
    }
}
