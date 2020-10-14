using Autodesk.AutoCAD.DatabaseServices;
using DotNetARX;
using Linq2Acad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThMEPWSS.Model;
using ThMEPWSS.Utils;
using ThWSS;

namespace ThMEPWSS.Bussiness
{
    public class SprayBlindService
    {
        /// <summary>
        /// 获取盲区
        /// </summary>
        /// <param name="sprays"></param>
        /// <param name="polyline"></param>
        /// <returns></returns>
        public List<Polyline> GetBlindArea(List<SprayLayoutData> sprays, Polyline polyline)
        {
            var sprayArea = SprayLayoutDataUtils.Radii(sprays);
            return polyline.Difference(sprayArea).Cast<Polyline>().ToList();
        }

        /// <summary>
        /// 打印盲区
        /// </summary>
        /// <param name="blindArea"></param>
        public void InsertBlindArea(List<Polyline> blindArea)
        {
            using (var db = AcadDatabase.Active())
            {
                var layerId = LayerTools.AddLayer(db.Database, ThWSSCommon.BlindArea_LayerName);

                foreach (var area in blindArea.Where(x => x.Area > 1))
                {
                    area.Layer = ThWSSCommon.BlindArea_LayerName;
                    area.ColorIndex = 10;
                    db.ModelSpace.Add(area);
                    
                    // 外圈轮廓
                    ObjectIdCollection objIdColl = new ObjectIdCollection();
                    objIdColl.Add(area.Id);

                    // 填充面积框线
                    Hatch hatch = new Hatch();
                    hatch.LayerId = layerId;
                    db.ModelSpace.Add(hatch);
                    hatch.ColorIndex = 10;
                    hatch.SetHatchPattern(HatchPatternType.PreDefined, "Solid");
                    hatch.Associative = true;
                    hatch.AppendLoop(HatchLoopTypes.Outermost, objIdColl);

                    // 重新生成Hatch纹理
                    hatch.EvaluateHatch(true);
                }
            }
        }
    }
}
