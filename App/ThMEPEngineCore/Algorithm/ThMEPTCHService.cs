using NFox.Cad;
using System.Linq;
using ThCADExtension;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.Algorithm
{
    public static class ThMEPTCHService
    {
        /// <summary>
        /// DXF（水管）
        /// </summary>
        public const string DXF_PIPE = "TCH_PIPE";

        /// <summary>
        /// DXF（风管）
        /// </summary>
        public const string DXF_HVACDUCT = "TCH_DBHVACDUCT";

        /// <summary>
        /// DXF (墙）
        /// </summary>
        public const string DXF_WALL = "TCH_WALL";

        /// <summary>
        /// DXF (开洞）
        /// </summary>
        public const string DXF_OPENING = "TCH_OPENING";

        /// <summary>
        /// 天正桥架
        /// </summary>
        public const string DXF_CABLETRY = "TCH_CABLETRY";

        /// <summary>
        /// 天正弯通
        /// </summary>
        public const string DXF_EFITTING = "TCH_EFITTING";

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
            var dxfName = entity.GetRXClass().DxfName.ToUpper();
            return dxfName.StartsWith("TCH") && dxfName.Contains("VALVE");
        }

        /// <summary>
        /// 是否是天正风管
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public static bool IsTCHDuct(this Entity entity)
        {
            var dxfName = entity.GetRXClass().DxfName.ToUpper();
            return dxfName.StartsWith("TCH") && dxfName.Contains("DBHVACDUCT");
        }

        /// <summary>
        /// 是否是天正水管
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public static bool IsTCHPipe(this Entity entity)
        {
            var dxfName = entity.GetRXClass().DxfName.ToUpper();
            return dxfName.StartsWith("TCH") && dxfName.Contains("PIPE");
        }

        /// <summary>
        /// 是否是天正水管文字
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public static bool IsTCHPipeText(this Entity entity)
        {
            var dxfName = entity.GetRXClass().DxfName.ToUpper();
            return dxfName.StartsWith("TCH") && dxfName.Contains("PIPETEXT");
        }

        /// <summary>
        /// 是否是天正配件
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public static bool IsTCHFitting(this Entity entity)
        {
            var dxfName = entity.GetRXClass().DxfName.ToUpper();
            return dxfName.StartsWith("TCH") && dxfName.Contains("DBHFITTING");
        }

        /// <summary>
        /// 是否为天正喷头
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public static bool IsTCHSprinkler(this Entity entity)
        {
            var dxfName = entity.GetRXClass().DxfName.ToUpper();
            return dxfName.StartsWith("TCH") && dxfName.Contains("EQUIPMENT");
        }

        /// <summary>
        /// 是否为天正水流指示器
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public static bool IsTCHPipeValue(this Entity entity)
        {
            var dxfName = entity.GetRXClass().DxfName.ToUpper();
            return dxfName.StartsWith("TCH") && dxfName.Contains("PIPEVALVE");
        }

        /// <summary>
        /// 是否为天正设备
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public static bool IsTCHEquipment(this Entity entity)
        {
            var dxfName = entity.GetRXClass().DxfName.ToUpper();
            return dxfName.StartsWith("TCH") && dxfName.Contains("EQUIPMENT");
        }

        /// <summary>
        /// 是否为天正回路标注
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public static bool IsTCHWireDim2(this Entity entity)
        {
            var dxfName = entity.GetRXClass().DxfName.ToUpper();
            return dxfName.StartsWith("TCH") && dxfName.Contains("WIREDIM2");
        }

        /// <summary>
        /// 是否为天正引出标注
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public static bool IsTCHMULTILEADER(this Entity entity)
        {
            var dxfName = entity.GetRXClass().DxfName.ToUpper();
            return dxfName.StartsWith("TCH") && dxfName.Contains("MULTILEADER");
        }

        /// <summary>
        /// 天正墙
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public static bool IsTCHWall(this Entity entity)
        {
            var dxfName = entity.GetRXClass().DxfName.ToUpper();
            return dxfName.StartsWith("TCH") && dxfName.Contains("WALL");
        }

        /// <summary>
        /// 天正窗
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        public static bool IsTCHWindow(this Entity e)
        {
            return e.IsTCHOpening() && Kind(e).Contains("窗");
        }

        /// <summary>
        /// 天正门
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        public static bool IsTCHDoor(this Entity e)
        {
            return e.IsTCHOpening() && Kind(e).Contains("门");
        }

        private static bool IsTCHOpening(this Entity entity)
        {
            var dxfName = entity.GetRXClass().DxfName.ToUpper();
            return dxfName.StartsWith("TCH") && dxfName.Contains("OPENING");
        }

        private static string Kind(this Entity e)
        {
            dynamic obj = e.AcadObject;
            return obj.GetKind as string;
        }

        /// <summary>
        /// 天正桥架
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public static bool IsTCHCableCarrierSegment(this Entity entity)
        {
            var dxfName = entity.GetRXClass().DxfName.ToUpper();
            return dxfName.StartsWith("TCH") && dxfName.Contains("CABLETRY");
        }

        /// <summary>
        /// 天正弯通（弯通，三通）
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public static bool IsTCHCableCarrierFitting(this Entity entity)
        {
            var dxfName = entity.GetRXClass().DxfName.ToUpper();
            return dxfName.StartsWith("TCH") && dxfName.Contains("EFITTING");
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