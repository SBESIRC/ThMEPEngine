using NFox.Cad;
using System.Linq;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThCADCore.NTS;
using ThMEPEngineCore.Service;

namespace ThMEPStructure.GirderConnect.ConnectMainBeam.Utils
{
    class ThGroupService
    {
        private const double BufferLength = 500.0;
        private DBObjectCollection Objs { get; set; }
        private List<Entity> Outlines = new List<Entity>();
        private ThCADCoreNTSSpatialIndex SpatialIndex { get; set; }
        public Dictionary<Entity, HashSet<Entity>> Groups { get; private set; }

        public ThGroupService(List<Entity> outlines, DBObjectCollection objs)
        {
            Objs = objs;
            Outlines = outlines;
            Groups = new Dictionary<Entity, HashSet<Entity>>();
            SpatialIndex = new ThCADCoreNTSSpatialIndex(objs);
            Group();
        }
        public DBObjectCollection Group(Entity outline, ThCADCoreNTSSpatialIndex spatialIndex)
        {
            var newOutline = Buffer(outline, BufferLength);
            return spatialIndex.SelectWindowPolygon(newOutline);
        }

        private void Group()
        {
            Groups = new Dictionary<Entity, HashSet<Entity>>();
            Outlines.ForEach(o =>
            {
                var querys = Group(o, SpatialIndex);
                Groups.Add(o, querys.OfType<Entity>().ToHashSet());
            });
        }

        private Entity Buffer(Entity outline, double length)
        {
            var bufferService = new ThNTSBufferService();
            return bufferService.Buffer(outline, length);
        }

        /// <summary>
        /// 不在框里面的东西
        /// </summary>
        public List<Entity> OutsideObjs
        {
            get
            {
                return QueryOutsideObjs();
            }
        }
        private List<Entity> QueryOutsideObjs()
        {
            var inners = Groups.SelectMany(o => o.Value).ToCollection();
            return Objs.OfType<Entity>().Where(o => !inners.Contains(o)).ToList();
        }
    }
}
