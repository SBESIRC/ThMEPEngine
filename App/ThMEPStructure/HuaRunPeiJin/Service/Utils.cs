using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Dreambuild.AutoCAD;
using Linq2Acad;
using NFox.Cad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThCADExtension;
using ThMEPEngineCore.Service;

namespace ThMEPStructure.HuaRunPeiJin.Service
{
    public static class Utils
    {
        public static DBObjectCollection GetEntitiesFromMS(this Database db,List<string> layers)
        {
            using (var acadDb = AcadDatabase.Use(db))
            {
                return acadDb.ModelSpace
                    .OfType<Entity>()
                    .Where(o => layers.Contains(o.Layer))
                    .ToCollection();
            }            
        }
        public static DBObjectCollection Clean(this DBObjectCollection lines)
        {
            if (lines.Count == 0)
            {
                return new DBObjectCollection();
            }
            else
            {
                var cleanInstance = new ThLaneLineCleanService();
                return cleanInstance.CleanNoding(lines);
            }
        }

        public static DBObjectCollection Extend(this DBObjectCollection lines,double length)
        {
            return lines.OfType<Line>().Select(o => o.ExtendLine(length)).ToCollection();
        }

        public static DBObjectCollection SelectCrossPolygon(this DBObjectCollection objs,Point3dCollection pts)
        {
            var spatialIndex = new ThCADCore.NTS.ThCADCoreNTSSpatialIndex(objs);
            return spatialIndex.SelectCrossingPolygon(pts);
        }
        /// <summary>
        /// 扩展的Dispose
        /// </summary>
        /// <param name="objs"></param>
        public static void DisposeEx(this DBObjectCollection objs)
        {
            objs.OfType<Entity>().ForEach(e =>
            {
                if(!e.IsDisposed)
                {
                    e.Dispose();
                }
            });
        }
        /// <summary>
        /// 扩展的Clone
        /// </summary>
        /// <param name="objs"></param>
        /// <returns></returns>
        public static DBObjectCollection CloneEx(this DBObjectCollection objs)
        {
            return objs.OfType<Entity>().Select(e => e.Clone() as Entity).ToCollection();
        }
    }
}
