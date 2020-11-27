using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Linq2Acad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPElectrical.Assistant;
using ThMEPElectrical.Model;

namespace ThMEPElectrical.Business.BlindAreaReminder
{
    public class HatchCreater
    {
        private List<PolygonInfo> m_polygonInfos;

        public static void MakeHatchCreater(List<PolygonInfo> polygonInfos)
        {
            var hatchCreater = new HatchCreater(polygonInfos);
            hatchCreater.DoCreateHatch();
        }

        public HatchCreater(List<PolygonInfo> polygonInfos)
        {
            m_polygonInfos = polygonInfos;
        }

        public void DoCreateHatch()
        {
            using (var acadDatabase = AcadDatabase.Active())
            {
                foreach (var polygon in m_polygonInfos)
                {
                    // 填充面积框线
                    Hatch hatch = new Hatch();
                    hatch.LayerId = DrawUtils.CreateLayer(ThMEPCommon.BLINDAREA_HATCH_LAYER_NAME, Color.FromRgb(255, 0, 0));
                    acadDatabase.ModelSpace.Add(hatch);
                    hatch.Associative = true;

                    // 外圈轮廓
                    ObjectIdCollection objIdColl = new ObjectIdCollection();
                    var externalId = acadDatabase.ModelSpace.Add(polygon.ExternalProfile);
                    objIdColl.Add(externalId);
                    hatch.AppendLoop(HatchLoopTypes.Outermost, objIdColl);

                    // 重新生成Hatch纹理
                    hatch.EvaluateHatch(true);

                    // 孤岛
                    foreach (var item in polygon.InnerProfiles)
                    {
                        objIdColl.Clear();
                        var innerId = acadDatabase.ModelSpace.Add(item);
                        objIdColl.Add(innerId);
                        hatch.AppendLoop(HatchLoopTypes.Default, objIdColl);

                        // 重新生成Hatch纹理
                        hatch.EvaluateHatch(true);
                    }

                    // 需要重新设置Pattern属性后Pattern才能被正确的应用
                    //hatch.SetHatchPattern(hatch.PatternType, hatch.PatternName);
                    hatch.EvaluateHatch(true);
                }
            }
        }
    }
}
