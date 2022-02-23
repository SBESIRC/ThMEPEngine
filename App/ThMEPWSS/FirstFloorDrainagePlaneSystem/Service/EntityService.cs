using Autodesk.AutoCAD.DatabaseServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPEngineCore.Algorithm;

namespace ThMEPWSS.FirstFloorDrainagePlaneSystem.Service
{
    public static class EntityService
    {
        public static List<Entity> GetBasicEntity(Entity entity)
        {
            var baseEnts = new List<Entity>();
            if (entity is BlockReference || entity.IsTCHElement())
            {
                var objs = new DBObjectCollection();
                entity.Explode(objs);
                baseEnts.AddRange(GetBasicEntity(objs.Cast<Entity>().ToList()));
            }
            else
            {
                baseEnts.Add(entity);
            }
            return baseEnts;
        }

        public static List<Entity> GetBasicEntity(List<Entity> entities)
        {
            var baseEnts = new List<Entity>();
            foreach (var entity in entities)
            {
                if (entity is BlockReference || entity.IsTCHElement())
                {
                    var objs = new DBObjectCollection();
                    entity.Explode(objs);
                    baseEnts.AddRange(GetBasicEntity(objs.Cast<Entity>().ToList()));
                }
                else
                {
                    baseEnts.Add(entity);
                }
            }
            return baseEnts;
        }

        public static Dictionary<Entity, List<Entity>> GetBasicEntityDic(List<Entity> entities)
        {
            Dictionary<Entity, List<Entity>> entDic = new Dictionary<Entity, List<Entity>>();
            foreach (var ent in entities)
            {
                var entLst = GetBasicEntity(ent);
                entDic.Add(ent, entLst);
            }
            return entDic;
        }
    }
}
