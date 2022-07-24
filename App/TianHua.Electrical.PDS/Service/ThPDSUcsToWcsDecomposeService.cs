using NFox.Cad;
using Autodesk.AutoCAD.Geometry;

namespace TianHua.Electrical.PDS.Service
{
    public class ThPDSUcsToWcsDecomposeService
    {
        /// <summary>
        /// 获取当前Ucs到Wcs的除平移外的变换矩阵
        /// </summary>
        /// <returns></returns>
        public static Matrix3d Decompose()
        {
            var wcsToUcs = AcHelper.Active.Editor.GetMatrixFromUcsToWcs();
            var array = wcsToUcs.ToArray();
            for (var i = 0; i < 3; i++)
            {
                array[i * 4 + 3] = 0;
            }
            return new Matrix3d(array);
        }
    }
}
