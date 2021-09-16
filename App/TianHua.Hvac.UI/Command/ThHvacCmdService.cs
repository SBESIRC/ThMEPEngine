using System.Collections.Generic;
using Linq2Acad;
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
            using (var db = AcadDatabase.Active())
            {
                var wallobjects = new DBObjectCollection();
                var objIds = Get_from_prompt("请选择内侧墙线", false);
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
        }
        public static DBObjectCollection Get_fan_and_centerline(out ObjectId fan_id, out List<ObjectId> line_ids)
        {
            using (var db = AcadDatabase.Active())
            {
                fan_id = ObjectId.Null;
                line_ids = new List<ObjectId>();
                var objIds = Get_from_prompt("请选择风机和中心线", false);
                if (objIds.Count == 0)
                    return new DBObjectCollection();
                fan_id = Classify_fan(objIds, out line_ids, out DBObjectCollection center_lines);
                return ThMEPHVACLineProc.Pre_proc(center_lines);
            }  
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
        private static ObjectId Classify_fan(ObjectIdCollection selections, 
                                             out List<ObjectId> line_ids,
                                             out DBObjectCollection center_lines)
        {
            ObjectId fan_id = ObjectId.Null;
            line_ids = new List<ObjectId>();
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
                    line_ids.Add(oid);
                }
            }
            return fan_id;
        }
    }
}
