using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using ThCADExtension;
using ThMEPEngineCore.Algorithm.AStarAlgorithm;

namespace ThMEPLighting.ParkingStall.Worker.LightConnect
{
    class LightConnectLight
    {
        List<Polyline> _allWalls;
        List<Polyline> _allColumns;
        Polyline _outPolyline;
        List<Polyline> _innerPolylines;
        List<Polyline> _obstacles;
        List<Line> _outLines;
        List<Line> _innerLines;
        List<Line> _columnLines;
        public LightConnectLight(Polyline outPolyline, List<Polyline> innerPolylines,List<Polyline> allWalls,List<Polyline> allColumns) 
        {
            this._allWalls = new List<Polyline>();
            this._columnLines = new List<Line>();
            this._outLines = new List<Line>();
            this._outPolyline = outPolyline;
            this._innerLines = new List<Line>();
            this._innerPolylines = new List<Polyline>();
            this._obstacles = new List<Polyline>();
            this._allColumns = new List<Polyline>();
            if (null != innerPolylines && innerPolylines.Count > 0) 
            {
                foreach (var pline in innerPolylines)
                {
                    if (null == pline || pline.Area < 100)
                        continue;
                    _innerPolylines.Add(pline);
                }
            }
            if (null != allWalls && allWalls.Count > 0)
            {
                foreach (var pline in allWalls)
                {
                    if (null == pline || pline.Area < 100)
                        continue;
                    _allWalls.Add(pline);
                }
            }
            if (null != allColumns && allColumns.Count > 0)
            {
                foreach (var pline in allColumns)
                {
                    if (null == pline || pline.Area < 100)
                        continue;
                    _allColumns.Add(pline);
                }
            }
            InitObstacles();
        }
        void InitObstacles()
        {
            this._obstacles.Clear();
            //_obstacles.Add(_outPolyline);
            _outLines.AddRange(LightConnectUtil.TransPolylineToLine(_outPolyline));
            if (null != _allColumns && _allColumns.Count > 0) 
            {
                foreach (var pline in _allColumns) 
                {
                    _columnLines.AddRange(LightConnectUtil.TransPolylineToLine(pline));
                    _obstacles.Add(pline);
                }
            }
            //if(false)
            //    _allWalls.ForEach(c => this._obstacles.Add(c));
            if (null != _innerPolylines && _innerPolylines.Count > 0) 
            {
                foreach (var pline in _innerPolylines)
                {
                    _innerLines.AddRange(LightConnectUtil.TransPolylineToLine(pline));
                    _obstacles.Add(pline);
                }
            }
        }
        public List<Line> PointConnectLines(Point3d startPoint,Point3d endPoint,Vector3d xDir) 
        {
            List<Line> lines = new List<Line>();
            Line line = new Line(startPoint,endPoint);
            if (IsCrossOutPolyline(line) || IsCrossInnerPolyline(line) || IsCrossColumnLine(line) || IsCrossWallLine(line))
            {
                //走A*寻路，躲避障碍物
                lines = AStarFindPath(startPoint, endPoint, xDir);
            }
            else 
            {
                lines.Add(line);
            }
            return lines;
        }
        public bool CrossObstacleLine(Point3d lineStartPoint, Point3d lineEndPoint)
        {
            var line = new Line(lineStartPoint, lineEndPoint);
            return CrossObstacleLine(line);
        }
        public bool CrossObstacleLine(Line line) 
        {
            if (IsCrossOutPolyline(line) || IsCrossInnerPolyline(line) || IsCrossColumnLine(line) || IsCrossWallLine(line))
            {
                return true;
            }
            return false;
        }
        public bool IsCrossOutPolyline(Line line) 
        {
            return IsCrossLine(line,_outLines);
        }
        public bool IsCrossInnerPolyline(Line line) 
        {
            return IsCrossLine(line, _innerLines);
        }
        public bool IsCrossColumnLine(Line line) 
        {
            return IsCrossLine(line, _columnLines);
        }
        public bool IsCrossWallLine(Line line) 
        {
            return false;
        }
        bool IsCrossLine(Line line, List<Line> checkLines) 
        {
            if (null == checkLines || checkLines.Count < 1)
                return false;
            foreach (var li in checkLines) 
            {
                if (null == li)
                    continue;
                if (line.LineIsIntersection(li))
                    return true;
            }
            return false;
        }
        
        List<Line> AStarFindPath(Point3d startPint,Point3d endPoint,Vector3d xDir) 
        {
            List<Line> lines = new List<Line>();
            var aStarRoute = new AStarRoutePlanner<Point3d>(_outPolyline, xDir, endPoint, 400, 100, 100);
            if (null != _obstacles && _obstacles.Count > 0)
                aStarRoute.SetObstacle(_obstacles);
            else
                aStarRoute.SetObstacle(new List<Polyline>());
            var path = aStarRoute.Plan(startPint);

            if (path != null)
            {
                var lastPoint = path.GetPoint3dAt(path.NumberOfVertices - 1);
                if (lastPoint.DistanceTo(endPoint) > 10)
                {
                    //无法到达，直连
                    lines.Add(new Line(startPint, endPoint));
                }
                else
                {
                    lines.AddRange(LightConnectUtil.TransPolylineToLine(path));
                }
            }
            return lines;
        }
        
    }
}
