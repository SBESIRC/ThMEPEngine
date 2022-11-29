using AcHelper;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Linq2Acad;
using System.Linq;
namespace ThMEPElectrical
{
    public class PickEntityCommand
    {
        Entity _selectEntity;
        bool _isExternal = false;
        public PickEntityCommand() { }
        public string GetEntityLayerName()
        {
            if (null == _selectEntity || _selectEntity.IsErased)
                return "";
            string layerName = _selectEntity.Layer;
            if (_isExternal)
                layerName = layerName.Split(new char[] { '|', '$' }).Last().Trim();
            return layerName;
        }
        public string GetBlockName()
        {
            if (null == _selectEntity || _selectEntity.IsErased)
                return "";
            bool isBlock = _selectEntity is BlockReference;
            if (!isBlock)
                return "";
            string retBlockName = "";
            if (_isExternal)
            {
                retBlockName = (_selectEntity as BlockReference).GetEffectiveName();
                retBlockName = ThMEPEngineCore.Algorithm.ThMEPXRefService.OriginalFromXref(retBlockName);
                if (retBlockName.Contains("*"))
                    return "";
            }
            else
            {

                retBlockName = (_selectEntity as BlockReference).GetEffectiveName();
                if (retBlockName.Contains("*"))
                    return "";
            }
            return retBlockName;
        }
        public Entity GetSelectEntity()
        {
            return _selectEntity;
        }
        public bool PickExternalBlock(string msg)
        {
            _selectEntity = null;
            _isExternal = true;
            FocusToCAD();
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                PromptNestedEntityOptions nestedEntOpt = new PromptNestedEntityOptions(string.Format("\n{0}", msg));
                Document dwg = Autodesk.AutoCAD.ApplicationServices.Core.Application.DocumentManager.MdiActiveDocument;
                Editor ed = dwg.Editor;
                PromptNestedEntityResult nestedEntRes = ed.GetNestedEntity(nestedEntOpt);
                if (nestedEntRes.Status != PromptStatus.OK)
                    return false;
                //这里选择的是块中的元素，需要获取块
                ObjectId[] ctnIds = nestedEntRes.GetContainers();
                if (ctnIds.Length == 0)
                    return false;
                _selectEntity = acadDatabase.Element<BlockReference>(ctnIds[0]);
                return true;
            }
        }
        public bool PickModelSpaceBlock(string msg)
        {
            _selectEntity = null;
            _isExternal = false;
            FocusToCAD();
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var entOpt = new PromptEntityOptions(string.Format("\n{0}", msg));
                var entityResult = Active.Editor.GetEntity(entOpt);
                if (entityResult.Status != PromptStatus.OK)
                    return false;
                var entId = entityResult.ObjectId;
                var dbObj = acadDatabase.Element<Entity>(entId);
                bool isBlock = dbObj is BlockReference;
                if (!isBlock)
                    return false;
                _selectEntity = dbObj;
                return true;
            }
        }
        void FocusToCAD()
        {
            //  https://adndevblog.typepad.com/autocad/2013/03/use-of-windowfocus-in-autocad-2014.html
#if ACAD2012
                    Autodesk.AutoCAD.Internal.Utils.SetFocusToDwgView();
#else
            Active.Document.Window.Focus();
#endif
        }
    }
}
