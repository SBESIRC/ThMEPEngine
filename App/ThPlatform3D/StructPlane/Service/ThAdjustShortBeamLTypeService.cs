using System.Linq;
using System.Collections.Generic;
using NFox.Cad;
using ThCADCore.NTS;
using ThCADExtension;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Model;

namespace ThPlatform3D.StructPlane.Service
{
    /// <summary>
    /// 有些很短的梁线线型和邻居不一样，需要调整其线型
    /// </summary>
    internal class ThAdjustShortBeamLTypeService
    {
        private double shortBeamUpperValue;
        private double PointTolerance = 1.0;
        private List<ThGeometry> BeamGeos { get; set; }
        private ThCADCoreNTSSpatialIndex SpatialIndex { get; set; }
        private DBObjectCollection ShortBeamLines { get; set; } =new DBObjectCollection();
        public ThAdjustShortBeamLTypeService(List<ThGeometry> beamGeos,double shortBeamUpperValue=600.0)
        {
            if(shortBeamUpperValue>0.0)
            {
                this.shortBeamUpperValue = shortBeamUpperValue;
            }
            BeamGeos = beamGeos;
            var lines = GetLines(beamGeos);            
            SpatialIndex = new  ThCADCoreNTSSpatialIndex(lines);
            // 获取短的梁线
            ShortBeamLines = lines
                .OfType<Line>()
                .Where(o => o.Length <= shortBeamUpperValue)
                .ToCollection();
            PointTolerance = ThStructurePlaneCommon.PointTolerance;
        }

        public void Adjust()
        {
            // 查找短线两端连着与自己线型不同的线
            // 查找短线一端连着与自己线型不同的线
            ShortBeamLines.OfType<Line>().ForEach(l =>
            {
                var spObjs = FilterCollinear(Query(l.StartPoint),l.StartPoint,l.EndPoint);
                var epObjs = FilterCollinear(Query(l.EndPoint), l.StartPoint, l.EndPoint);
                epObjs = epObjs.Difference(spObjs);
                spObjs.Remove(l);
                epObjs.Remove(l);
                var currentGeo = FindGeometry(l);
                var currentLineType = GetLineType(currentGeo);
                if (spObjs.Count==1 && epObjs.Count==1)
                {
                    var spLine = spObjs.OfType<Line>().First();
                    var epLine = epObjs.OfType<Line>().First();
                    var spGeo = FindGeometry(spLine);
                    var epGeo = FindGeometry(epLine);
                    var spLineType = GetLineType(spGeo);
                    var epLineType = GetLineType(epGeo);
                    if (spLineType == epLineType && spLineType!= currentLineType &&
                    (spLine.Length> shortBeamUpperValue || epLine.Length> shortBeamUpperValue))
                    {
                        currentGeo.Properties.UpdateLineType(spLineType);
                    }
                }
                else if(spObjs.Count==1 && epObjs.Count==0)
                {
                    var spLine = spObjs.OfType<Line>().First();
                    var spGeo = FindGeometry(spLine);
                    var spLineType = GetLineType(spGeo);
                    if (spLineType != currentLineType && spLine.Length > shortBeamUpperValue)
                    {
                        currentGeo.Properties.UpdateLineType(spLineType);
                    }
                }
                else if(spObjs.Count ==0  && epObjs.Count==1)
                {
                    var epLine = epObjs.OfType<Line>().First();
                    var epGeo = FindGeometry(epLine);
                    var epLineType = GetLineType(epGeo);
                    if (epLineType != currentLineType && epLine.Length > shortBeamUpperValue)
                    {
                        currentGeo.Properties.UpdateLineType(epLineType);
                    }
                }
            });
        }

        private DBObjectCollection FilterCollinear(DBObjectCollection lines,Point3d sp,Point3d ep)
        {
            return lines
                .OfType<Line>()
                .Where(o => ThGeometryTool.IsCollinearEx(sp, ep, o.StartPoint, o.EndPoint))
                .ToCollection();
        }

        private DBObjectCollection Query(Point3d pt)
        {
            var envelop = pt.CreateSquare(PointTolerance*2);            
            var results = Query(envelop);
            envelop.Dispose();
            return results;
        }
        private DBObjectCollection Query(Polyline outline)
        {
            return SpatialIndex.SelectCrossingPolygon(outline);
        }

        private ThGeometry FindGeometry(Curve curve)
        {
            var index = BeamGeos.Select(o => o.Boundary).ToCollection().IndexOf(curve);
            return index >= 0 ? BeamGeos[index] : null;
        }
        private string GetLineType(ThGeometry geo)
        {
            return geo == null ? "" : geo.Properties.GetLineType();
        }       
        private DBObjectCollection GetLines(List<ThGeometry> beamGeos)
        {
            return beamGeos
                .Where(o => o.Boundary is Line)
                .Select(o => o.Boundary)
                .ToCollection();
        }
    }
}
