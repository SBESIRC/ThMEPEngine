using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;

namespace ThMEPLighting.FEI.ThEmgPilotLamp
{
    class EmgLaneLineMark
    {
        double _firstAndLastLineMinLength = 3000;
        double _innerBufferLineLength = 1000;
        double _firstOrLastLineMinLenght = 1100;
        double _outLineDistance = 1000;//灯可能偏离线，如果偏离基本是800；
        List<Curve> _laneLineCurves;
        List<Curve> _hostLineCurves;
        List<Line> _laneLines;
        List<Line> _hostLines;
        public EmgLaneLineMark(List<Curve> laneLines, List<Curve> hostLines)
        {
            _laneLineCurves = new List<Curve>();
            _hostLineCurves = new List<Curve>();
            _laneLines = new List<Line>();
            _hostLines = new List<Line>();
            if (null != laneLines && laneLines.Count > 0)
            {
                foreach (var line in laneLines)
                {
                    if (line == null)
                        continue;
                    this._laneLineCurves.Add(line);
                }
            }
            if (null != hostLines && hostLines.Count > 0)
            {
                foreach (var line in hostLines)
                {
                    if (line == null)
                        continue;
                    this._hostLineCurves.Add(line);
                }
            }
            InitCurveToLines();
        }
        void InitCurveToLines() 
        {
            var objs = new DBObjectCollection();
            _laneLineCurves.ForEach(x => objs.Add(x));
            _laneLines = ThMEPEngineCore.Algorithm.ThMEPLineExtension.LineSimplifier(objs, 10, 20.0, 20, Math.PI / 180.0).Cast<Line>().ToList();

            objs.Clear();
            _hostLineCurves.ForEach(x => objs.Add(x));
            _hostLines = ThMEPEngineCore.Algorithm.ThMEPLineExtension.LineSimplifier(objs, 10, 20.0, 20, Math.PI*5 / 180.0).Cast<Line>().ToList();

            
        }
        public List<Polyline> MarkLines(double bufferDis,List<Point3d> hostLightPoints)
        {
            var pLines = new List<Polyline>();
            var objs = new DBObjectCollection();
            _laneLines.ForEach(x => objs.Add(x));
            _hostLines.ForEach(x => objs.Add(x));
            var nodeGeo = objs.ToNTSNodedLineStrings();
            var allLines = new List<Line>();
            if (nodeGeo != null)
            {
                allLines = nodeGeo.ToDbObjects()
                .SelectMany(x =>
                {
                    DBObjectCollection entitySet = new DBObjectCollection();
                    (x as Polyline).Explode(entitySet);
                    return entitySet.Cast<Line>().ToList();
                })
                .Where(x => x.Length > 5)
                .ToList();
            }
            double dis = 100;
            var breakHostLines = new List<Line>();
            var laneLines = new List<Line>();
            foreach (var line in allLines)
            {
                if (EmgPilotLampUtil.LineIsCollinear(line.StartPoint, line.EndPoint, _hostLines))
                    breakHostLines.Add(line);
                else
                    laneLines.Add(line);
            }
            
            List<Line> tempLines = new List<Line>();
            while (breakHostLines.Count > 0)
            {
                tempLines.Clear();
                Line startLine = null;
                Line rmLine = null;
                foreach (var line in breakHostLines)
                {
                    if (startLine != null)
                        break;
                    var sp = line.StartPoint;
                    var ep = line.EndPoint;
                    if (EmgPilotLampUtil.PointInLines(sp, laneLines,5, dis))
                    {
                        rmLine = line;
                        startLine = line;
                    }
                    else if (EmgPilotLampUtil.PointInLines(ep, laneLines, 5, dis))
                    {
                        rmLine = line;
                        startLine = new Line(ep, sp);
                    }
                }
                if (startLine == null)
                    break;
                breakHostLines.Remove(rmLine);
                tempLines.Add(new Line(startLine.StartPoint,startLine.EndPoint));
                var endPoint = startLine.EndPoint;
                var currentLine = startLine;
                while(!EmgPilotLampUtil.PointInLines(endPoint, laneLines, 5, dis)) 
                {
                    rmLine = null;
                    foreach (var line in breakHostLines)
                    {
                        if (null != rmLine)
                            break;
                        var sp = line.StartPoint;
                        var ep = line.EndPoint;
                        if (sp.DistanceTo(endPoint)<10)
                        {
                            rmLine = line;
                            currentLine = line;
                        }
                        else if (ep.DistanceTo(endPoint)<10)
                        {
                            rmLine = line;
                            currentLine = new Line(ep, sp);
                        }
                    }
                    if (rmLine == null)
                        break;
                    breakHostLines.Remove(rmLine);
                    tempLines.Add(new Line(currentLine.StartPoint, currentLine.EndPoint));
                    endPoint = currentLine.EndPoint;
                }
                if (EmgPilotLampUtil.PointInLines(endPoint, laneLines, 5, dis)) 
                {
                    bool haveLight = CheckLinesHaveHostingLight(tempLines, hostLightPoints);
                    if (!haveLight)
                        continue;
                    var pline = LinesToPolyline(tempLines,bufferDis);
                    if (null == pline)
                        continue;
                    pLines.Add(pline);
                }
            }
            return pLines;
        }

        Polyline LinesToPolyline(List<Line> lines, double bufferDis)
        {
            //这里线是按照顺序来的,并且是首尾相接的
            Polyline polyline = new Polyline();
            polyline.Closed = false;
            for (int i = 0; i < lines.Count; i++)
            {
                var line = lines[i];
                var sp = line.StartPoint;
                var ep = line.EndPoint;

                var ep2d = new Point2d(ep.X, ep.Y);
                if (i == 0)
                {
                    var sp2d = new Point2d(sp.X, sp.Y);
                    polyline.AddVertexAt(0, sp2d, 0, 0, 0);
                }
                polyline.AddVertexAt(0, ep2d, 0, 0, 0);
            }
            var objs = new DBObjectCollection();
            objs.Add(polyline);
            var pline = objs.Buffer(bufferDis).ToNTSMultiPolygon().ToDbPolylines().FirstOrDefault();
            return pline;
        }

        bool CheckLinesHaveHostingLight(List<Line> lines,List<Point3d> hostLightPoints) 
        {
            if (null == hostLightPoints || hostLightPoints.Count < 1)
                return false;
            bool haveHostingLight = false;
            for (int i = 0; i < lines.Count; i++) 
            {
                if (haveHostingLight)
                    break;
                var tempLine = lines[i];
                var lineSp = tempLine.StartPoint;
                var lineEp = tempLine.EndPoint;
                var lineDir = (lineEp - lineSp).GetNormal();
                if (i == 0 && i == lines.Count - 1)
                {
                    if (tempLine.Length < _firstAndLastLineMinLength)
                        continue;
                    lineSp = lineSp + lineDir.MultiplyBy(_innerBufferLineLength);
                    lineEp = lineEp - lineDir.MultiplyBy(_innerBufferLineLength);
                }
                else if (i == 0)
                {
                    if (tempLine.Length < _firstOrLastLineMinLenght)
                        continue;
                    lineSp = lineSp + lineDir.MultiplyBy(_innerBufferLineLength);
                }
                else if (i == lines.Count - 1)
                {
                    if (tempLine.Length < _firstOrLastLineMinLenght)
                        continue;
                    lineEp = lineEp - lineDir.MultiplyBy(_innerBufferLineLength);
                }
                haveHostingLight = CheckLineHaveLight(new Line(lineSp, lineEp), hostLightPoints, _outLineDistance);
            }
            return haveHostingLight;
        }
        bool CheckLineHaveLight(Line line, List<Point3d> hostLightPoints,double outLineDis) 
        {
            if (null == hostLightPoints || hostLightPoints.Count < 1)
                return false;
            bool haveLight = false;
            foreach (var point in hostLightPoints) 
            {
                if (haveLight)
                    break;
                haveLight = EmgPilotLampUtil.PointInLine(point, line, 5, outLineDis);
            }
            return haveLight;
        }
    }
}
