using AcHelper;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using GeometryExtensions;
using ThMEPElectrical.Geometry;

namespace ThMEPElectrical.Business
{
    public static class PreWindowSelector
    {
        /// <summary>
        /// 获取选择矩形框线点集
        /// </summary>
        /// <returns></returns>
        public static Point3dCollection GetSelectRectPoints()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Editor ed = doc.Editor;
            PromptPointOptions ppo = new PromptPointOptions("\n请输入结构信息识别范围的第一个角点：");
            ppo.AllowNone = false;
            PromptPointResult ppr = ed.GetPoint(ppo);
            if (ppr.Status != PromptStatus.OK)
                return null;

            Point3d firstPt = ppr.Value;
            PromptCornerOptions pco = new PromptCornerOptions("\n请输入结构信息识别范围的第二个角点:", firstPt);
            ppr = ed.GetCorner(pco);
            if (ppr.Status != PromptStatus.OK)
                return null;
            Point3d secondPt = ppr.Value;

            var ucs2wcs = Active.Editor.UCS2WCS();
            //框线范围
            var points = GeomUtils.CalculateRectangleFromPoints(firstPt.TransformBy(ucs2wcs), secondPt.TransformBy(ucs2wcs));
            return points;
        }
    }
}
