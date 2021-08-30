using System;
using NFox.Cad;
using System.Linq;
using ThCADCore.NTS;
using Autodesk.AutoCAD.DatabaseServices;
using AcPolygon = Autodesk.AutoCAD.DatabaseServices.Polyline;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Engine;
using ThMEPEngineCore.Extension;

namespace ThMEPEngineCore.Algorithm
{
    public class ThMEPRegionService
    {
        /// <summary>
        /// 内缩距离
        /// </summary>
        public double BufferDistance { get; set; } = 500;
        private ThCADCoreNTSSpatialIndex thcolumnsSpatialIndex { get; set; }
        private ThCADCoreNTSSpatialIndex thbeamsSpatialIndex { get; set; }
        private ThCADCoreNTSSpatialIndex thshearWallsSpatialIndex { get; set; }
        private ThCADCoreNTSSpatialIndex tharcWallsSpatialIndex { get; set; }

        public void Initialize(ThBeamConnectRecogitionEngine beamConnectEngine, ThDB3ArchWallRecognitionEngine archWallEngine)
        {
            //获取柱
            var columns = beamConnectEngine.ColumnEngine.Elements.Select(o => o.Outline).Cast<AcPolygon>().ToCollection();
            thcolumnsSpatialIndex = new ThCADCoreNTSSpatialIndex(columns);

            //获取梁
            var thBeams = beamConnectEngine.BeamEngine.Elements.Cast<ThIfcLineBeam>().ToList();
            thBeams.ForEach(x => x.ExtendBoth(20, 20));
            var beams = thBeams.Select(o => o.Outline).Cast<AcPolygon>().ToCollection();
            thbeamsSpatialIndex = new ThCADCoreNTSSpatialIndex(beams);

            //获取剪力墙
            var shearWalls = beamConnectEngine.ShearWallEngine.Elements.Select(o => o.Outline).Cast<AcPolygon>().ToCollection();
            thshearWallsSpatialIndex = new ThCADCoreNTSSpatialIndex(shearWalls);

            //获取建筑墙
            var arcWalls = archWallEngine.Elements.Select(x => x.Outline).Where(x => x is AcPolygon).Cast<AcPolygon>().ToCollection();
            tharcWallsSpatialIndex = new ThCADCoreNTSSpatialIndex(arcWalls);
        }

        /// <summary>
        /// 可布置区域
        /// </summary>
        /// <param name="database"></param>
        /// <param name="frame"></param>
        /// <returns></returns>
        public DBObjectCollection PlacementRegions(Entity frame)
        {
            //获取柱
            var columns = thcolumnsSpatialIndex.SelectCrossingPolygon(frame).Cast<AcPolygon>().ToCollection();

            //获取梁
            var beams = thbeamsSpatialIndex.SelectCrossingPolygon(frame).Cast<AcPolygon>().ToCollection();

            //获取剪力墙
            var shearWalls = thshearWallsSpatialIndex.SelectCrossingPolygon(frame).Cast<AcPolygon>().ToCollection();

            //获取建筑墙
            var arcWalls = tharcWallsSpatialIndex.SelectCrossingPolygon(frame).Cast<AcPolygon>().ToCollection();

            DBObjectCollection dBObjects = new DBObjectCollection();
            foreach (AcPolygon beam in beams)
            {
                dBObjects.Add(beam);
            }

            foreach (AcPolygon cPoly in columns)
            {
                dBObjects.Add(cPoly);
            }

            foreach (AcPolygon wPoly in shearWalls)
            {
                dBObjects.Add(wPoly);
            }

            foreach (AcPolygon wPoly in arcWalls)
            {
                dBObjects.Add(wPoly);
            }

            return Difference(frame, dBObjects).Cast<AcPolygon>()
                .SelectMany(x => x.Buffer(-BufferDistance).Cast<AcPolygon>())
                .Where(x => x.Area > 0)
                .ToCollection();
        }

        private DBObjectCollection Difference(Entity e, DBObjectCollection objs)
        {
            if (e is AcPolygon polygon)
            {
                return polygon.DifferenceMP(objs);
            }
            else if (e is MPolygon mPolygon)
            {
                return mPolygon.DifferenceMP(objs);
            }
            else
            {
                throw new NotSupportedException();
            }
        }
    }
}
