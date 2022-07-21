using System.Linq;
using System.Collections.Generic;

using Linq2Acad;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

using ThCADCore.NTS;
using ThCADExtension;

namespace ThMEPElectrical.BlockConvert
{
    public class ThBConvertDamperAvoidService
    {
        /// <summary>
        /// 转换结果
        /// </summary>
        public List<ThBConvertEntityInfos> EntityInfos { get; set; }

        /// <summary>
        /// 图纸比例
        /// </summary>
        public double Scale { get; set; }

        public ThBConvertDamperAvoidService()
        {
            EntityInfos = new List<ThBConvertEntityInfos>();
        }

        public void Avoid()
        {
            using (var currentDb = AcadDatabase.Active())
            {
                var targetEntites = EntityInfos.Where(o => o.EquimentType.Contains(ThBConvertCommon.FIRE_DAMPER))
                    .Select(o => currentDb.Element<BlockReference>(o.ObjectId, true)).ToList();
                var cacheIndex = new List<int>();
                for (var i = 0; i < targetEntites.Count; i++)
                {
                    if (cacheIndex.Contains(i))
                    {
                        continue;
                    }
                    cacheIndex.Add(i);

                    var frame = new Circle(targetEntites[i].Position, Vector3d.ZAxis, 3 * Scale).Tessellate(100.0);
                    for (var j = i + 1; j < targetEntites.Count; j++)
                    {
                        if (cacheIndex.Contains(j))
                        {
                            continue;
                        }

                        if (frame.Contains(targetEntites[j].Position))
                        {
                            var direction = targetEntites[j].Position - targetEntites[i].Position;
                            var normalDirection = direction.GetNormal();
                            targetEntites[i].TransformBy(Matrix3d.Displacement(-(3 * Scale - direction.Length) / 2 * normalDirection));
                            targetEntites[j].TransformBy(Matrix3d.Displacement((3 * Scale - direction.Length) / 2 * normalDirection));

                            cacheIndex.Add(j);
                        }
                    }
                }
            }
        }
    }
}
