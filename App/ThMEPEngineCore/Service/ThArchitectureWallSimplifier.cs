﻿using System;
using NFox.Cad;
using System.Linq;
using ThCADCore.NTS;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.DatabaseServices;
using AcPolygon = Autodesk.AutoCAD.DatabaseServices.Polyline;

namespace ThMEPEngineCore.Service
{
    public class ThArchitectureWallSimplifier
    {
        private const double AREA_TOLERANCE = 1.0;
        private const double OFFSET_DISTANCE = 20.0;
        private const double DISTANCE_TOLERANCE = 1.0;

        public static DBObjectCollection Simplify(DBObjectCollection walls)
        {
            var objs = new DBObjectCollection();
            walls.Cast<AcPolygon>().ForEach(o =>
            {
                // 由于投影误差，DB3切出来的墙线中有非常短的线段（长度<1mm)
                // 这里使用简化算法，剔除掉这些非常短的线段
                objs.Add(o.DPSimplify(DISTANCE_TOLERANCE));
            });
            return objs;
        }

        public static DBObjectCollection MakeValid(DBObjectCollection walls)
        {
            var objs = new DBObjectCollection();
            walls.Cast<AcPolygon>().ForEach(o =>
            {
                var results = o.MakeValid().Cast<AcPolygon>();
                if (results.Any())
                {
                    objs.Add(results.OrderByDescending(p => p.Area).First());
                }
            });
            return objs;
        }

        public static DBObjectCollection Normalize(DBObjectCollection walls)
        {
            var objs = new DBObjectCollection();            
            foreach (AcPolygon wall in walls)
            {
                wall.Buffer(-OFFSET_DISTANCE)
                    .Cast<AcPolygon>()
                    .ForEach(o =>
                    {
                        o.Buffer(OFFSET_DISTANCE)
                        .Cast<AcPolygon>()
                        .ForEach(e => objs.Add(e));
                    });
            }
            return objs;
        }

        public static DBObjectCollection BuildArea(DBObjectCollection walls)
        {
            walls = Buffer(walls, -DISTANCE_TOLERANCE);
            walls = walls.BuildArea();
            walls = Buffer(walls, DISTANCE_TOLERANCE);
            return walls;
        }

        public static DBObjectCollection Filter(DBObjectCollection walls)
        {
            return walls.Cast<Entity>().Where(o =>
            {
                if (o is AcPolygon polygon)
                {
                    return polygon.Area > AREA_TOLERANCE;
                }
                else if (o is MPolygon mPolygon)
                {
                    return mPolygon.Area > AREA_TOLERANCE;
                }
                else
                {
                    throw new NotSupportedException();
                }
            }).ToCollection();
        }
        private static DBObjectCollection Buffer(DBObjectCollection objs, double disttance)
        {
            DBObjectCollection result = new DBObjectCollection();
            var bufferService = new ThNTSBufferService();
            foreach (Entity obj in objs)
            {
                var entity = bufferService.Buffer(obj, disttance);
                if (entity != null)
                {
                    result.Add(entity);
                }
            }
            return result;
        }
    }
}
