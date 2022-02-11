using AcHelper;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using DotNetARX;
using Linq2Acad;
using System.Collections.Generic;
using ThCADExtension;
using ThMEPHVAC.Command;
using ThMEPHVAC.Common;

namespace ThMEPHVAC
{
    class ThHAVCIndoorFanCmds
    {
        string layerName = "AI-圈注";
        [CommandMethod("TIANHUACAD", "THSNJBZ", CommandFlags.Modal)]
        public void THIndoorFanLayout()
        {
            var ucs = Active.Editor.CurrentUserCoordinateSystem;
            var selectAreas = SelectPolyline();
            var indoorFanLayout = new IndoorFanLayoutCmd(selectAreas, ucs.CoordinateSystem3d.Xaxis, ucs.CoordinateSystem3d.Yaxis,false);
            indoorFanLayout.Execute();
            var cloudLines = indoorFanLayout.ErrorRoomPolylines;
            if (null == cloudLines || cloudLines.Count < 1)
                return;
            ShowErrorRooms(cloudLines);
        }
        [CommandMethod("TIANHUACAD", "THSNJFZ", CommandFlags.Modal)]
        public void THIndoorFanPlace() 
        {
            var placeFan = new IndoorFanPlace();
            placeFan.Execute();
        }
        [CommandMethod("TIANHUACAD", "THSNJJH", CommandFlags.Modal)]
        public void THIndoorFanCheck()
        {
            var selectAreas = SelectPolyline();
            var fanCheck = new IndoorFanCheck(selectAreas);
            fanCheck.Execute();
            var cloudLines = fanCheck.ErrorRoomPolylines;
            if (null == cloudLines || cloudLines.Count < 1)
                return;
            ShowErrorRooms(cloudLines);
        }
        [CommandMethod("TIANHUACAD", "THSNJJHXG", CommandFlags.Modal)]
        public void THIndoorFanChange()
        {
            var selectAreas = SelectPolyline();
            var fanChange = new IndoorFanChange(selectAreas);
            fanChange.Execute();
            var cloudLines = fanChange.ErrorRoomPolylines;
            if (null == cloudLines || cloudLines.Count < 1)
                return;
            ShowErrorRooms(cloudLines);
        }
        [CommandMethod("TIANHUACAD", "THSNJDC", CommandFlags.Modal)]
        public void THIndoorFanExport()
        {
            var fanChange = new ThHvacIndoorFanExportCmd();
            fanChange.Execute();
            var showMsg = fanChange.ShowMsg;
            if (string.IsNullOrEmpty(showMsg))
                return;
            Active.Editor.WriteMessage(showMsg);
        }
        [CommandMethod("TIANHUACAD", "THSNJArea", CommandFlags.Modal)]
        public void THIndoorFanTest()
        {
            var ucs = Active.Editor.CurrentUserCoordinateSystem;
            var selectAreas = SelectPolyline();
            var indoorFanLayout = new IndoorFanLayoutCmd(selectAreas, ucs.CoordinateSystem3d.Xaxis, ucs.CoordinateSystem3d.Yaxis, true);
            indoorFanLayout.Execute();
        }
        private void ShowErrorRooms(List<Polyline> cloudLines) 
        {
            using (var acdb = AcadDatabase.Active())
            {
                var cloudIds = new Dictionary<ObjectId, Color>();

                LayerTableRecord layerRecord = null;
                foreach (var layer in acdb.Layers)
                {
                    if (layer.Name.ToUpper().Equals(layerName))
                    {
                        layerRecord = acdb.Layers.Element(layerName);
                        break;
                    }
                }

                // 创建新的图层
                if (layerRecord == null)
                {
                    layerRecord = acdb.Layers.Create(layerName);
                    layerRecord.Color = Color.FromRgb(255, 0, 0); ;
                    layerRecord.IsPlottable = false;
                }
                foreach (var item in cloudLines)
                {
                    var id = acdb.ModelSpace.Add(item);
                    if (id == null || !id.IsValid)
                        continue;
                    item.Layer = layerName;
                    cloudIds.Add(id,item.Color);
                }
                ShowErrroPolylines(cloudIds);
            }
        }
        private void ShowErrroPolylines(Dictionary<ObjectId, Color> cloudLineIds) 
        {
            if (null == cloudLineIds || cloudLineIds.Count < 1)
                return;
            //revcloud can only print to the current layer.
            //so it changes the active layer to the required layer, then changes back.
            //画云线。 云线只能画在当前图层。所以先转图层画完在转回来。\
            var oriLayer = Active.Database.Clayer;
            using (var acdb = AcadDatabase.Active())
            {
                foreach (var keyValue in cloudLineIds)
                {
                    var id = keyValue.Key;
                    var pline = acdb.ModelSpace.Element(id);
                    if (null == pline)
                        continue;
                    ObjectId revcloud = ObjectId.Null;
                    void handler(object s, ObjectEventArgs e)
                    {
                        if (e.DBObject is Polyline polyline)
                        {
                            revcloud = e.DBObject.ObjectId;
                        }
                    }
                    acdb.Database.ObjectAppended += handler;

#if ACAD_ABOVE_2014
                    Active.Editor.Command("_.REVCLOUD", "_arc", 500, 500, "_Object", id, "_No");
#else
                    ResultBuffer args = new ResultBuffer(
                       new TypedValue((int)LispDataType.Text, "_.REVCLOUD"),
                       new TypedValue((int)LispDataType.Text, "_ARC"),
                       new TypedValue((int)LispDataType.Text, "500"),
                       new TypedValue((int)LispDataType.Text, "500"),
                       new TypedValue((int)LispDataType.Text, "_Object"),
                       new TypedValue((int)LispDataType.ObjectId, id),
                       new TypedValue((int)LispDataType.Text, "_No"));
                    Active.Editor.AcedCmd(args);
#endif
                    acdb.Database.ObjectAppended -= handler;

                    // 设置运行属性
                    var revcloudObj = acdb.Element<Entity>(revcloud, true);
                    revcloudObj.Color = keyValue.Value;
                    revcloudObj.Layer = "AI-圈注";
                }
            }
            Active.Database.Clayer = oriLayer;
        }
        private Dictionary<Polyline, List<Polyline>> SelectPolyline()
        {
            var selectPLines = new Dictionary<Polyline, List<Polyline>>();
            using (AcadDatabase acdb = AcadDatabase.Active())
            {
                // 获取房间框线
                var options = new PromptSelectionOptions()
                {
                    AllowDuplicates = false,
                    MessageForAdding = "选择区域",
                    RejectObjectsOnLockedLayers = true,
                };
                var dxfNames = new string[]
                {
                    RXClass.GetClass(typeof(Polyline)).DxfName,
                };
                var layerNames = new string[]
                {
                    "AI-房间框线",
                };
                var filter = ThSelectionFilterTool.Build(dxfNames, layerNames);
                var result = Active.Editor.GetSelection(options, filter);
                if (result.Status != PromptStatus.OK)
                {
                    return selectPLines;
                }
                List<Curve> frameLst = new List<Curve>();
                foreach (ObjectId obj in result.Value.GetObjectIds())
                {
                    var frame = acdb.Element<Polyline>(obj);
                    frameLst.Add(frame.Clone() as Polyline);
                }
                var pretreatmentPolyline = new PretreatmentPolyline(frameLst);
                selectPLines = pretreatmentPolyline.CalcFrameHoles();
            }
            return selectPLines;
        }
    }
}
