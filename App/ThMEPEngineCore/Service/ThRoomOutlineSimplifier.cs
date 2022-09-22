﻿using System.Linq;
using System.Collections.Generic;
using ThCADCore.NTS;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.Service
{
    public class ThRoomOutlineSimplifier:ThPolygonalElementSimplifier
    {
        public ThRoomOutlineSimplifier()
        {
            OFFSETDISTANCE = 20.0;
            DISTANCETOLERANCE = 1.0;
            TESSELLATEARCLENGTH = 100.0;
            ClOSED_DISTANC_TOLERANCE = 1000.0; // 待定
            AREATOLERANCE = 100.0; //过滤房间面积
            SIMILARITYMEASURETOLERANCE = 0.99;
        }
        public DBObjectCollection OverKill(DBObjectCollection polygons)
        {
            var results = new DBObjectCollection();

            // 用NTS的GeometryEquality去重
            var firstObjs = ThCADCoreNTSGeometryFilter.GeometryEquality(polygons);

            // 用NTS的相似度去重
            var spatialIndex = new ThCADCoreNTSSpatialIndex(firstObjs);
            var garbages = new HashSet<DBObject>();
            var bufferService = new ThNTSBufferService();
            firstObjs
                .OfType<Entity>()
                .Where(o => o is Polyline || o is MPolygon)
                .ForEach(outerPolygon =>
                {
                    if (!garbages.Contains(outerPolygon))
                    {
                        var bufferPolygon = bufferService.Buffer(outerPolygon, 1.0);
                        var objs = spatialIndex.SelectWindowPolygon(bufferPolygon)
                        .OfType<DBObject>().ToHashSet();
                        results.Add(outerPolygon);
                        objs.Remove(outerPolygon);
                        if (objs.Count > 0)
                        {
                            foreach (Entity innerPolygon in objs)
                            {
                                if (IsSimilar(outerPolygon, innerPolygon))
                                {
                                    garbages.Add(innerPolygon);
                                }
                            }
                        }                        
                    }                    
                });

            return results;
        }
        /// <summary>
        /// 过滤secondPolygons中某些元素的几何信息和firstPolygons中的某些元素一致
        /// </summary>
        /// <param name="firstPolygons"></param>
        /// <param name="secondPolygons"></param>
        /// <returns>secondPolygons中重复的对象</returns>
        public DBObjectCollection OverKill(DBObjectCollection firstPolygons, DBObjectCollection secondPolygons)
        {
            // 用NTS的相似度去重
            // 用firstPolygons中的元素，在secondPolygons查找相似对象
            var results = new DBObjectCollection();
            var spatialIndex = new ThCADCoreNTSSpatialIndex(secondPolygons);
            var bufferService = new ThNTSBufferService();
            firstPolygons
                .OfType<Entity>()
                .Where(o => o is Polyline || o is MPolygon)
                .ForEach(o =>
                {
                    var bufferPolygon = bufferService.Buffer(o, 1.0);
                    if(bufferPolygon!=null)
                    {
                        foreach (Entity innerPolygon in spatialIndex.SelectWindowPolygon(bufferPolygon))
                        {
                            if (IsSimilar(o, innerPolygon))
                            {
                                results.Add(innerPolygon);
                            }
                        }
                        bufferPolygon.Dispose();
                    }
                });
            return results;
        }
        private bool IsSimilar(Entity first,Entity second)
        {
            if(first is Polyline firstPolyline)
            {
                if(second is Polyline secondPolyline)
                {
                    return firstPolyline.IsSimilar(secondPolyline, SIMILARITYMEASURETOLERANCE);
                }
                else if(second is MPolygon secondPolygon)
                {
                    return firstPolyline.IsSimilar(secondPolygon, SIMILARITYMEASURETOLERANCE);
                }
                else
                {
                    return false;
                }
            }
            else if(first is MPolygon firstPolygon)
            {
                if (second is Polyline secondPolyline)
                {
                    return firstPolygon.IsSimilar(secondPolyline, SIMILARITYMEASURETOLERANCE);
                }
                else if (second is MPolygon secondPolygon)
                {
                    return firstPolygon.IsSimilar(secondPolygon, SIMILARITYMEASURETOLERANCE);
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }
    }
}
