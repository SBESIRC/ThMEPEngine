using System;
using NFox.Cad;
using System.Linq;
using ThCADCore.NTS;
using ThCADExtension;
using Dreambuild.AutoCAD;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Engine;
using ThMEPEngineCore.Extension;
using AcPolygon = Autodesk.AutoCAD.DatabaseServices.Polyline;

namespace ThMEPEngineCore.AFASRegion.Service
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
        private ThCADCoreNTSSpatialIndex thWallsSpatialIndex { get; set; }
        private ThCADCoreNTSSpatialIndex thHolesSpatialIndex { get; set; }

        public void Initialize(List<ThIfcBuildingElement> columns, List<ThIfcBuildingElement> beams, List<ThIfcBuildingElement> walls, List<AcPolygon> holes)
        {
            //获取柱
            var thcolumns = columns.Select(o => o.Outline).Cast<AcPolygon>().ToCollection();
            thcolumnsSpatialIndex = new ThCADCoreNTSSpatialIndex(thcolumns);

            //获取梁
            var thBeams = beams.Cast<ThIfcLineBeam>().ToList();
            thBeams.ForEach(x => x.ExtendBoth(20, 20));

            var highbeams = thBeams.Where(o => -(o.DistanceToFloor + WallThickness) > 600).Select(o => o.Outline).Cast<AcPolygon>().ToCollection();
            thhighbeamsSpatialIndex = new ThCADCoreNTSSpatialIndex(highbeams);

            var nohighbeams = thBeams.Where(o => -(o.DistanceToFloor + WallThickness) <= 600).Select(o => o.Outline).Cast<AcPolygon>().ToCollection();
            thnohighbeamsSpatialIndex = new ThCADCoreNTSSpatialIndex(nohighbeams);

            //获取墙
            var thWalls= walls.Select(o => o.Outline).Cast<AcPolygon>().ToCollection();
            thWallsSpatialIndex = new ThCADCoreNTSSpatialIndex(thWalls);

            //获取洞
            var thholes = holes.ToCollection();
            thHolesSpatialIndex = new ThCADCoreNTSSpatialIndex(thholes);
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

            //获取墙
            var Walls = thWallsSpatialIndex.SelectCrossingPolygon(frame).Cast<AcPolygon>().ToCollection();

            //获取洞
            var Holes = thHolesSpatialIndex.SelectCrossingPolygon(frame).Cast<AcPolygon>().ToCollection();

            DBObjectCollection dBObjects = new DBObjectCollection();
            foreach (AcPolygon beam in highbeams)
            {
                dBObjects.Add(beam);
            }

            foreach (AcPolygon cPoly in columns)
            {
                dBObjects.Add(cPoly);
            }

            foreach (AcPolygon wPoly in Walls)
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
                        var buffers = Difference(space, dBNoHighObjects).Cast<AcPolygon>().SelectMany(x => x.Buffer(-BufferDistance).Cast<AcPolygon>()).Where(x => x.Area > 0);
                        DBObjectCollection objs = Holes;
                        foreach (var polylinespace in buffers)
                        {
                            var Truediffobjs = DifferenceMP(polylinespace, objs).Cast<Entity>();
                            Objs.AddRange(Truediffobjs);
                        }
                    }
                    else
                    {
                        var buffers = polygon.Buffer(-SmallestBufferDistance).Cast<AcPolygon>().Where(x => x.Area > 0);
                        DBObjectCollection objs = Holes;
                        foreach (var polylinespace in buffers)
                        {
                            var Truediffobjs = DifferenceMP(polylinespace, objs).Cast<Entity>();
                            Objs.AddRange(Truediffobjs);
                        }
                    }
                }
                else if (space is MPolygon mPolygon)
                {
                    var differencespace = mPolygon.Shell().Buffer(-BufferDistance).Cast<AcPolygon>().Where(x => x.Area > 0);
                    if (differencespace.Count() > 0)
                    {
                        var Diffobjsm = DifferenceMP(space, dBNoHighObjects).Cast<Entity>();
                        foreach (Entity mpolyline in Diffobjsm)
                        {
                            if(mpolyline is Polyline polyline)
                            {
                                var buffers = polyline.Buffer(-BufferDistance).Cast<AcPolygon>().Where(x => x.Area > 0);
                                DBObjectCollection objs = Holes;
                                foreach (var polylinespace in buffers)
                                {
                                    var Truediffobjs = DifferenceMP(polylinespace, objs).Cast<Entity>();
                                    Objs.AddRange(Truediffobjs);
                                }
                            }
                            else if (mpolyline is MPolygon mpolygon)
                            {
                                var shellbuffers = mpolygon.Shell().Buffer(-BufferDistance).Cast<AcPolygon>().Where(x => x.Area > 0);
                                DBObjectCollection objs = mpolygon.Holes().SelectMany(y => y.Buffer(BufferDistance).Cast<AcPolygon>().Where(x => x.Area > 0)).ToCollection();
                                objs = objs.Union(Holes);
                                foreach (var shellbuffer in shellbuffers)
                                {
                                    var Truediffobjs = DifferenceMP(shellbuffer, objs).Cast<Entity>();
                                    Objs.AddRange(Truediffobjs);
                                }
                            }
                        }

                        //if(Diffobjsm.Any(o=>o is MPolygon))
                        //{
                        //    using (Linq2Acad.AcadDatabase acad=Linq2Acad.AcadDatabase.Active())
                        //    {
                        //        var a =Diffobjsm.Where(o => o is MPolygon m && m.Holes().Count>0).Cast<MPolygon>();
                        //        foreach (var item in a)
                        //        {
                        //            item.ColorIndex = 5;
                        //            acad.ModelSpace.Add(item);
                        //        }
                        //    }
                        //}
                        //var Diffobjs = Difference(space, dBNoHighObjects).Cast<AcPolygon>();
                        //if (Diffobjs.Count() == 1)
                        //{
                        //    Objs.AddRange(Diffobjs.SelectMany(x => x.Buffer(-SmallestBufferDistance).Cast<AcPolygon>()).Where(x => x.Area > 0));
                        //}
                        //else
                        //{
                        //    Objs.AddRange(Diffobjs.SelectMany(x => x.Buffer(-BufferDistance).Cast<AcPolygon>()).Where(x => x.Area > 0));
                        //}
                    }  
                    else
                    {
                        Objs.AddRange(mPolygon.Buffer(-SmallestBufferDistance).Cast<AcPolygon>().Where(x => x.Area > 0));
                    }
                }   
            }
            //新的需求，如果可布置区域内缩500一个区域都缩不出来，则选择最大的那个区域，进行内缩200
            //这样做的意义是，保证至少有一个可布置区域
            if(Objs.Count==0)
            {
                var MaxAreaSpace = Difference(frame, dBObjects.Union(dBNoHighObjects)).Cast<AcPolygon>().OrderByDescending(o =>o.Area).First();
                Objs.AddRange(MaxAreaSpace.Buffer(-SmallestBufferDistance).Cast<AcPolygon>().Where(x => x.Area > 0));
            }
            return Objs.ToCollection();
        }

        

        private DBObjectCollection Difference(Entity e, DBObjectCollection objs)
        {
            if (objs.Count == 0)
            {
                return new DBObjectCollection() { e };
            }
            else if (e is AcPolygon polygon)
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
            if(objs.Count ==0)
            {
                return new DBObjectCollection() { e };
            }
            else if (e is AcPolygon polygon)
            {
                return StandardEntityCollection(polygon.DifferenceMP(objs));
            }
            else if (e is MPolygon mPolygon)
            {
                return StandardEntityCollection(mPolygon.DifferenceMP(objs));
            }
            else
            {
                throw new NotSupportedException();
            }
        }

        private DBObjectCollection StandardEntityCollection(DBObjectCollection objs)
        {
            DBObjectCollection reobjs = new DBObjectCollection();
            objs.Cast<Entity>().ForEach(o =>
            {
                if (o is Polyline)
                    reobjs.Add(o);
                else if(o is MPolygon mPolygon)
                {
                    if (mPolygon.Holes().Count > 0)
                        reobjs.Add(mPolygon);
                    else
                        reobjs.Add(mPolygon.Shell());
                }
            });
            return reobjs;
        }
    }
}
