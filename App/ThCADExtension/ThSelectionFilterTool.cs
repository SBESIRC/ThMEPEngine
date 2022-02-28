using NFox.Cad;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThCADExtension
{
    public static class ThSelectionFilterTool
    {
        public static SelectionFilter Build(string[] dxfNames)
        {
            return OpFilter.Bulid(o =>
                o.Dxf((int)DxfCode.Start) == string.Join(",", dxfNames));
        }

        public static SelectionFilter Build(string[] dxfNames, string[] layerNames)
        {
            if (layerNames.Length > 0)
            {
                return OpFilter.Bulid(o =>
                o.Dxf((int)DxfCode.Start) == string.Join(",", dxfNames) &
                o.Dxf((int)DxfCode.LayerName) == string.Join(",", layerNames));
            }
            else
            {
                return Build(dxfNames);
            }
        }

        /// <summary>
        /// 根据块名选择对象
        /// </summary>
        /// <param name="blkNames"></param>
        /// <returns></returns>
        public static SelectionFilter BuildBlockFilter(string[] blkNames)
        {
            return OpFilter.Bulid(o =>
                o.Dxf((int)DxfCode.BlockName) == string.Join(",", blkNames));
        }
    }
}
