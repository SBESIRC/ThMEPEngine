using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Linq2Acad;
using System.Collections.Generic;
using System.Linq;
using ThMEPElectrical.Model;
using ThMEPEngineCore.Algorithm;

namespace ThMEPElectrical.Business.Procedure
{
    public class UcsInfoCalculator
    {
        public List<UcsInfo> UcsInfos = new List<UcsInfo>();
        private string m_layerName;
        ThMEPOriginTransformer m_originTransformer;

        public UcsInfoCalculator(string blockLayerName, ThMEPOriginTransformer originTransformer)
        {
            m_originTransformer = originTransformer;
            m_layerName = blockLayerName;
        }

        public static List<UcsInfo> MakeUcsInfos(string blockLayerName, ThMEPOriginTransformer originTransformer)
        {
            var ucsInfoCalculator = new UcsInfoCalculator(blockLayerName, originTransformer);
            ucsInfoCalculator.Do();
            return ucsInfoCalculator.UcsInfos;
        }

        public void Do()
        {
            using (var db = AcadDatabase.Active())
            {
                var blockRefs = db.ModelSpace.OfType<BlockReference>()
                    .Where(p => p.Layer.ToUpper().Contains(m_layerName.ToUpper()))
                    .ToList();

                foreach (var block in blockRefs)
                {
                    var copyBlock = (BlockReference)block.Clone();
                    if (null != m_originTransformer)
                        m_originTransformer.Transform(copyBlock);
                    var blockSystem = copyBlock.BlockTransform.CoordinateSystem3d;
                    var transBlock = Matrix3d.AlignCoordinateSystem(Point3d.Origin, blockSystem.Xaxis, blockSystem.Yaxis, 
                        blockSystem.Zaxis, Point3d.Origin, Vector3d.XAxis, Vector3d.YAxis, Vector3d.ZAxis);
                    UcsInfos.Add(new UcsInfo(copyBlock.Position, transBlock, copyBlock.Rotation, blockSystem.Xaxis, copyBlock.BlockTransform));
                    //DrawUtils.DrawProfile(GeometryTrans.MatrixSystemCurves(block.BlockTransform, 100), "drawMatrix");
                }
            }
        }
    }
}
