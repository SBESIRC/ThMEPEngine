using NFox.Cad;
using System.Linq;
using ThCADExtension;
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
        /// 是否是天正单行文字
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public static bool IsTCHText(this Entity entity)
        {
            return entity.GetRXClass().DxfName == ThCADCommon.DxfName_TCH_Text;
        }

        /// <summary>
        /// 是否是天正阀
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public static bool IsTCHValve(this Entity entity)
        {
            string dxfName = entity.GetRXClass().DxfName.ToUpper();
            return dxfName.StartsWith("TCH") && dxfName.Contains("VALVE");
        }

        /// <summary>
        /// 是否是天正喷头
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public static bool IsTCHSprinkler(this Entity entity)
        {
            string dxfName = entity.GetRXClass().DxfName.ToUpper();
            return dxfName.StartsWith("TCH") && dxfName.Contains("EQUIPMENT");
        }

        /// <summary>
        /// 炸天正单行文字为CAD单行文字
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public static DBObjectCollection ExplodeTCHText(this Entity entity)
        {
            var results = new DBObjectCollection();
            if (IsTCHText(entity))
            {
                entity.Explode(results);
                return results.Cast<Entity>().Where(o => o is DBText).ToCollection();
            }
            return results;
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