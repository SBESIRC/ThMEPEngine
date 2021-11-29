using Autodesk.AutoCAD.DatabaseServices;
using Dreambuild.AutoCAD;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThMEPEngineCore.Service;

namespace ThMEPStructure.GirderConnect.ConnectMainBeam.Test
{
    public class ThHandleOverlap
    {
        private List<Polyline> Columns { get; set; }
        private double Bufferlength { get; set; }
        public ThHandleOverlap(List<Polyline> columns, double bufferlength = 40)
        {
            Columns = columns;
            Bufferlength = bufferlength;
        }
        public List<Polyline> Handle()
        {
            var columnsGroup = CreatGroup();
            var results = Merge(columnsGroup);

            return results;
        }
        private Dictionary<Polyline, List<Polyline>> CreatGroup()
        {
            var columnGroup = new Dictionary<Polyline, List<Polyline>>();
            var garbage = new DBObjectCollection();
            Columns.Sort((x, y) => -x.Area.CompareTo(y.Area));
            var objs = new DBObjectCollection();
            Columns.ForEach(p => objs.Add(p));
            var spatialIndex = new ThCADCoreNTSSpatialIndex(objs);
            var bufferservice = new ThNTSBufferService();
            foreach (Polyline column in Columns)
            {
                if (garbage.Contains(column))
                {
                    continue;
                }
                columnGroup.Add(column, new List<Polyline>());
                var newColumn = bufferservice.Buffer(column, Bufferlength);
                var querys = spatialIndex.SelectWindowPolygon(newColumn);
                querys.Remove(column);
                querys.Cast<Entity>().ForEach(o => garbage.Add(o));
                querys.Cast<Polyline>().ForEach(o => columnGroup[column].Add(o));
            }

            return columnGroup;
        }
        private List<Polyline> Merge(Dictionary<Polyline, List<Polyline>> columnsGroup)
        {
            var results = new List<Polyline>();
            var bufferservice = new ThNTSBufferService();
            foreach (var item in columnsGroup)
            {
                var newColumn = bufferservice.Buffer(item.Key, Bufferlength);
                DBObjectCollection mergeCollection = new DBObjectCollection();
                mergeCollection.Add(newColumn);
                item.Value.ForEach(o => mergeCollection.Add(o));
                var Union = ThCADCoreNTSDbObjCollectionExtension.UnionPolygons(mergeCollection);
                Union.Cast<Polyline>().ForEach(o => results.Add(bufferservice.Buffer(o, -Bufferlength) as Polyline));
            }

            return results;
        }
    }
}
