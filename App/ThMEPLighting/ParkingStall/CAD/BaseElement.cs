using Autodesk.AutoCAD.DatabaseServices;
using Dreambuild.AutoCAD;
using Linq2Acad;
using NFox.Cad;
using System;
using System.Collections.Generic;
using System.Linq;
using ThCADCore.NTS;
using ThCADExtension;
using ThMEPEngineCore;
using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore.LaneLine;
using ThMEPTCH.Services;

namespace ThMEPLighting.ParkingStall.CAD
{
    class BaseElement
    {
        string AL_BLOCKNAME = "E-BDB003";
        string AL_LAYERNAME = "E-POWR-EQPM";
        List<BlockReference> _allParkLights;
        List<BlockReference> _allALBlocks;
        ThCADCoreNTSSpatialIndex _lanLineSpatialIndex;
        Database _database;
        ThMEPOriginTransformer _originTransformer;
        public BaseElement(Database database, ThMEPOriginTransformer originTransformer)
        {
            _database = database;
            _originTransformer = originTransformer;
            _allParkLights = new List<BlockReference>();
            _allALBlocks = new List<BlockReference>();
            InitLaneLines();
            InitBlocks();
        }
        void InitLaneLines() 
        {
            var objs = new DBObjectCollection();
            using (AcadDatabase acdb = AcadDatabase.Use(_database))
            {
                var laneLines = acdb.ModelSpace
                .OfType<Curve>()
                .Where(o => o.Layer == ThMEPEngineCoreCommon.LANELINE_LAYER_NAME);
                laneLines.ForEach(x =>
                {
                    var transCurve = x.Clone() as Curve;
                    if (null != _originTransformer)
                        _originTransformer.Transform(transCurve);
                    objs.Add(transCurve);
                });
            }
            _lanLineSpatialIndex = new ThCADCoreNTSSpatialIndex(objs);
        }
        void InitBlocks() 
        {
            //获取相应的块，这里一次获取数据
            using (AcadDatabase acadDatabase = AcadDatabase.Use(_database))
            {
                var items = acadDatabase.ModelSpace
                    .OfType<BlockReference>()
                    .Where(o => o.Layer == ThMEPLightingCommon.EmgLightLayerName || o.Layer == AL_LAYERNAME);
                foreach (BlockReference block in items)
                {
                    if (block.Name == ParkingStallCommon.PARK_LIGHT_BLOCK_NAME)
                    {
                        var copyLight = block.Clone() as BlockReference;
                        if (null != _originTransformer)
                            _originTransformer.Transform(copyLight);
                        _allParkLights.Add(copyLight);
                    }
                    else if (block.Name == AL_BLOCKNAME)
                    {
                        var copyAL = block.Clone() as BlockReference;
                        if (null != _originTransformer)
                            _originTransformer.Transform(copyAL);
                        _allALBlocks.Add(copyAL);
                    }
                }
            }
        }
        public List<BlockReference> GetAreaLights(Polyline outPolyline, List<Polyline> innerPolylines)
        {
            var retList = new List<BlockReference>();
            retList = _allParkLights.Where(o => outPolyline.Contains(o.Position)).ToList();
            if (innerPolylines != null && innerPolylines.Count > 0)
            {
                foreach (var hole in innerPolylines)
                {
                    retList = retList.Where(o => hole.Contains(o.Position) == false).ToList();
                }
            }
            return retList;
        }
        public List<BlockReference> GetAreaDistributionBox(Polyline outPolyline, List<Polyline> innerPolylines) 
        {
            var retList = new List<BlockReference>();
            retList = _allALBlocks.Where(o => outPolyline.Contains(o.Position)).ToList();
            if (innerPolylines != null && innerPolylines.Count > 0)
            {
                foreach (var hole in innerPolylines)
                {
                    retList = retList.Where(o => hole.Contains(o.Position) == false).ToList();
                }
            }
            return retList;
        }
        public List<Line> GetLaneLines(Polyline polyline)
        {
            List<Line> otherLanes = new List<Line>();
            var sprayLines = _lanLineSpatialIndex.SelectCrossingPolygon(polyline).Cast<Curve>().ToList();
            otherLanes.Clear();
            if (sprayLines.Count <= 0)
            {
                return otherLanes;
            }
            sprayLines = sprayLines.SelectMany(x => polyline.Trim(x).OfType<Curve>().ToList()).ToList();

            //处理车道线
            var handleLines = ThMEPLineExtension.LineSimplifier(sprayLines.ToCollection(), 500, 100.0, 2.0, Math.PI / 180.0);
            var parkingLinesService = new ParkingLinesService();
            var parkingLines = parkingLinesService.CreateNodedParkingLines(polyline, handleLines, out List<List<Line>> otherPLines);
            foreach (var item in parkingLines)
            {
                if (null == item || item.Count < 1)
                    continue;
                otherLanes.AddRange(item);
            }
            foreach (var item in otherPLines)
            {
                if (null == item || item.Count < 1)
                    continue;
                otherLanes.AddRange(item);
            }
            return otherLanes;
        }
        public List<Line> GetLayerLines(Polyline polyline, string layerName)
        {
            List<Line> otherLanes = new List<Line>();
            var objs = new DBObjectCollection();
            using (AcadDatabase acdb = AcadDatabase.Active())
            {
                var laneLines = acdb.ModelSpace
                .OfType<Curve>()
                .Where(o => o.Layer == layerName);
                laneLines.ForEach(x =>
                {
                    if(x.GetType().Name.ToLower().Contains("imp"))
                        return;
                    var transCurve = x.Clone() as Curve;
                    if (null != _originTransformer)
                        _originTransformer.Transform(transCurve);
                    objs.Add(transCurve);
                });

                //获取天正桥架
                ThTCHCableTrayOutlineBuilder cableTrayOutlineBuilder = new ThTCHCableTrayOutlineBuilder();
                var cableCurves = cableTrayOutlineBuilder.Build(acdb.Database , new Autodesk.AutoCAD.Geometry.Point3dCollection());
                foreach (var item in cableCurves) 
                {
                    if (item is Curve curve)
                    {
                        var transCurve = curve.Clone() as Curve;
                        if (null != _originTransformer)
                            _originTransformer.Transform(transCurve);
                        objs.Add(transCurve);
                    }
                    else if (item is MPolygon mPolygon) 
                    {
                        var loops  = mPolygon.Loops();
                        foreach (var loop in loops) 
                        {
                            var transCurve = loop.Clone() as Curve;
                            if (null != _originTransformer)
                                _originTransformer.Transform(transCurve);
                            objs.Add(transCurve);
                        }
                    }
                }
            }
            
            ThCADCoreNTSSpatialIndex thCADCoreNTSSpatialIndex = new ThCADCoreNTSSpatialIndex(objs);
            var sprayLines = thCADCoreNTSSpatialIndex.SelectCrossingPolygon(polyline).Cast<Curve>().ToList();
            otherLanes.Clear();
            if (sprayLines.Count <= 0)
            {
                return otherLanes;
            }
            sprayLines = sprayLines.SelectMany(x => polyline.Trim(x).OfType<Curve>().ToList()).ToList();
            otherLanes = ThMEPLineExtension.ExplodeCurves(sprayLines.ToCollection()).Cast<Line>().ToList();
            return otherLanes;
        }
    }
}
