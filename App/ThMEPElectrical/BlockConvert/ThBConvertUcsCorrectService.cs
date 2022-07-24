using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

using ThCADExtension;

namespace ThMEPElectrical.BlockConvert
{
    public static class ThBConvertUcsCorrectService
    {
        public static void Transform(this ThBlockReferenceData blockData, BlockReference block, Matrix3d ucsToWcs)
        {
            var displacement = Matrix3d.Displacement(blockData.Position - Point3d.Origin);
            var roration = Matrix3d.Rotation(blockData.Rotation, Vector3d.ZAxis, Point3d.Origin);
            var transform = displacement.PostMultiplyBy(ucsToWcs)
                .PostMultiplyBy(roration.Inverse())
                .PostMultiplyBy(displacement.Inverse());
            block.TransformBy(transform);
        }
    }
}
