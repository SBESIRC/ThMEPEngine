using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.Algorithm
{
    public static class ThMEPTCHService
    {
        /// <summary>
        /// 是否是天正元素
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public static bool IsTCHElement(this Entity entity)
        {
            return entity.GetRXClass().DxfName.ToUpper().StartsWith("TCH");
        }

        /// <summary>
        /// 炸天正元素
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public static DBObjectCollection ExplodeTCHElement(this Entity entity)
        {
            var results = new DBObjectCollection();
            var entitySet = new DBObjectCollection();
            entity.Explode(entitySet);
            foreach (Entity ent in entitySet)
            {
                if (ent.IsTCHElement())
                {
                    var nestObjs = ent.ExplodeTCHElement();
                    foreach (Entity nestEnt in nestObjs)
                    {
                        results.Add(nestEnt);
                    }
                }
                else if (ent is BlockReference br)
                {
                    var nestObjs = br.ExplodeTCHElement();
                    foreach (Entity nestEnt in nestObjs)
                    {
                        results.Add(nestEnt);
                    }
                }
                else
                {
                    results.Add(ent);
                }
            }
            return results;
        }
    }
}