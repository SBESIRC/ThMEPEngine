using AcHelper;
using GeometryExtensions;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.EditorInput;

namespace TianHua.Electrical.PDS.Service
{
    public static class ThPDSSelectPointService
    {
        public static bool TrySelectPoint(out Point3d basePt, string note)
        {
            var basePtOptions = new PromptPointOptions(note);
            var result = Active.Editor.GetPoint(basePtOptions);
            if (result.Status != PromptStatus.OK)
            {
                basePt = default;
                return false;
            }
            basePt = result.Value.TransformBy(Active.Editor.UCS2WCS());
            return true;
        }

        public static bool TryInputScale(out Scale3d scale, string note)
        {
            var result = Active.Editor.GetDouble(note);
            scale = new Scale3d();
            if (result.Status != PromptStatus.OK)
            {
                return false;
            }
            else
            {
                scale = new Scale3d(result.Value * 0.01);
            }
            return true;
        }
    }
}
