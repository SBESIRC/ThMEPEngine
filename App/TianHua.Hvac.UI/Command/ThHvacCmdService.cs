using AcHelper;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using ThMEPEngineCore.Service.Hvac;
using ThMEPHVAC.Model;

namespace TianHua.Hvac.UI.Command
{
    public class ThHvacCmdService
    {
        public static DBObjectCollection Get_walls()
        {
            var wallobjects = new DBObjectCollection();
            var objIds = ThHvacCmdService.Get_from_prompt("请选择内侧墙线", false);
            if (objIds.Count == 0)
                return new DBObjectCollection();
            foreach (ObjectId oid in objIds)
            {
                var obj = oid.GetDBObject();
                if (obj is Curve curveobj)
                {
                    wallobjects.Add(curveobj);
                }
            }
            return ThMEPHVACLineProc.Pre_proc(wallobjects);
        }
        public static DBObjectCollection Get_fan_and_centerline(out ObjectId fan_id)
        {
            fan_id = ObjectId.Null;
            var objIds = Get_from_prompt("请选择风机和中心线", false);
            if (objIds.Count == 0)
                return new DBObjectCollection();
            fan_id = Classify_fan(objIds, out DBObjectCollection center_lines);
            return ThMEPHVACLineProc.Pre_proc(center_lines);
        }
        public static ObjectIdCollection Get_from_prompt(string prompt, bool only_able)
        {
            PromptSelectionOptions options = new PromptSelectionOptions()
            {
                AllowDuplicates = false,
                MessageForAdding = prompt,
                RejectObjectsOnLockedLayers = true,
                SingleOnly = only_able
            };
            var result = Active.Editor.GetSelection(options);
            if (result.Status == PromptStatus.OK)
            {
                return result.Value.GetObjectIds().ToObjectIdCollection();
            }
            else
            {
                return new ObjectIdCollection();
            }
        }
        private static ObjectId Classify_fan(ObjectIdCollection selections, out DBObjectCollection center_lines)
        {
            ObjectId fan_id = ObjectId.Null;
            center_lines = new DBObjectCollection();
            foreach (ObjectId oid in selections)
            {
                var obj = oid.GetDBObject();
                if (obj.IsRawModel())
                {
                    fan_id = oid;
                }
                else if (obj is Curve curve)
                {
                    center_lines.Add(curve.Clone() as Curve);
                }
            }
            return fan_id;
        }
    }
}
