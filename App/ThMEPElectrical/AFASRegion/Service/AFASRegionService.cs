using System;
using NFox.Cad;
using System.Linq;
using ThCADCore.NTS;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Engine;
using ThMEPEngineCore.Extension;
using AcPolygon = Autodesk.AutoCAD.DatabaseServices.Polyline;
using System.Collections.Generic;
using ThCADExtension;

namespace ThMEPElectrical.AFASRegion.Service
{
    public class AFASRegionService
    {
        /// <summary>
        /// 内缩距离
        /// </summary>
        public double BufferDistance { get; set; } = 500;

        /// <summary>
        /// 墙板厚度
        /// </summary>
        private double WallThickness { get; set; } = 100;

        /// <summary>
        /// 最小内缩距离
        /// </summary>
        private double SmallestBufferDistance { get; set; } = 200;

        private ThCADCoreNTSSpatialIndex thcolumnsSpatialIndex { get; set; }
        private ThCADCoreNTSSpatialIndex thhighbeamsSpatialIndex { get; set; }
        private ThCADCoreNTSSpatialIndex thnohighbeamsSpatialIndex { get; set; }
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

            var highbeams = thBeams.Where(o => -(o.DistanceToFloor + WallThickness) > 600).Select(o => o.Outline).Cast<AcPolygon>().ToCollection();
            thhighbeamsSpatialIndex = new ThCADCoreNTSSpatialIndex(highbeams);

            var nohighbeams = thBeams.Where(o => -(o.DistanceToFloor + WallThickness) <= 600).Select(o => o.Outline).Cast<AcPolygon>().ToCollection();
            thnohighbeamsSpatialIndex = new ThCADCoreNTSSpatialIndex(nohighbeams);

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

            //获取高梁
            var highbeams = thhighbeamsSpatialIndex.SelectCrossingPolygon(frame).Cast<AcPolygon>().ToCollection();

            //获取非高梁
            var nohighbeams = thnohighbeamsSpatialIndex.SelectCrossingPolygon(frame).Cast<AcPolygon>().ToCollection();

            //获取剪力墙
            var shearWalls = thshearWallsSpatialIndex.SelectCrossingPolygon(frame).Cast<AcPolygon>().ToCollection();

            //获取建筑墙
            var arcWalls = tharcWallsSpatialIndex.SelectCrossingPolygon(frame).Cast<AcPolygon>().ToCollection();

            DBObjectCollection dBObjects = new DBObjectCollection();
            foreach (AcPolygon beam in highbeams)
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

            DBObjectCollection dBNoHighObjects = new DBObjectCollection();
            foreach (AcPolygon beam in nohighbeams)
            {
                dBNoHighObjects.Add(beam);
            }

            List<Entity> Objs = new List<Entity>();
            foreach (Entity space in DifferenceMP(frame, dBObjects).Cast<Entity>())
            {
                if (space is AcPolygon polygon)
                {
                    var differencespace = polygon.Buffer(-BufferDistance).Cast<AcPolygon>().Where(x => x.Area > 0);
                    if (differencespace.Count() > 0)
                    {
                        Objs.AddRange(Difference(space, dBNoHighObjects).Cast<AcPolygon>().SelectMany(x => x.Buffer(-BufferDistance).Cast<AcPolygon>()).Where(x => x.Area > 0));
                    }
                    else
                    {
                        Objs.AddRange(polygon.Buffer(-SmallestBufferDistance).Cast<AcPolygon>().Where(x => x.Area > 0));
                    }
                }
                else if (space is MPolygon mPolygon)
                {
                    var differencespace = mPolygon.Shell().Buffer(-BufferDistance).Cast<AcPolygon>().Where(x => x.Area > 0);
                    if (differencespace.Count() > 0)
                    {
                        var Diffobjs = Difference(space, dBNoHighObjects).Cast<AcPolygon>();
                        if (Diffobjs.Count() == 1)
                        {
                            Objs.AddRange(Diffobjs.SelectMany(x => x.Buffer(-SmallestBufferDistance).Cast<AcPolygon>()).Where(x => x.Area > 0));
                        }
                        else
                        {
                            Objs.AddRange(Diffobjs.SelectMany(x => x.Buffer(-BufferDistance).Cast<AcPolygon>()).Where(x => x.Area > 0));
                        }
                    }  
                    else
                    {
                        Objs.AddRange(mPolygon.Buffer(-SmallestBufferDistance).Cast<AcPolygon>().Where(x => x.Area > 0));
                    }
                }   
            }
            return Objs.ToCollection();
        }

        private DBObjectCollection Difference(Entity e, DBObjectCollection objs)
        {
            if (e is AcPolygon polygon)
            {
                return polygon.Difference(objs);
            }
            else if (e is MPolygon mPolygon)
            {
                return mPolygon.Difference(objs);
            }
            else
            {
                throw new NotSupportedException();
            }
        }

        private DBObjectCollection DifferenceMP(Entity e, DBObjectCollection objs)
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
