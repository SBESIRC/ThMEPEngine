using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPElectrical.Model;
using Linq2Acad;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using ThMEPElectrical.Assistant;

namespace ThMEPElectrical.Business.Procedure
{
    public class UcsInfoCalculator
    {
        public List<UcsInfo> UcsInfos = new List<UcsInfo>();
        private string m_layerName;

        public UcsInfoCalculator(string blockLayerName)
        {
            m_layerName = blockLayerName;
        }

        public static List<UcsInfo> MakeUcsInfos(string blockLayerName)
        {
            var ucsInfoCalculator = new UcsInfoCalculator(blockLayerName);
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
                    var blockSystem = block.BlockTransform.CoordinateSystem3d;
                    var transBlock = Matrix3d.AlignCoordinateSystem(Point3d.Origin, blockSystem.Xaxis, blockSystem.Yaxis, 
                        blockSystem.Zaxis, Point3d.Origin, Vector3d.XAxis, Vector3d.YAxis, Vector3d.ZAxis);
                    UcsInfos.Add(new UcsInfo(block.Position, transBlock, block.Rotation, blockSystem.Xaxis, block.BlockTransform));
                    //DrawUtils.DrawProfile(GeometryTrans.MatrixSystemCurves(block.BlockTransform, 100), "drawMatrix");
                }
            }
        }
    }
}
