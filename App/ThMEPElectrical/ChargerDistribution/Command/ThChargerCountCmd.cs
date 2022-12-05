using System.Linq;
using System.Collections.Generic;

using NFox.Cad;
using AcHelper;
using Linq2Acad;
using Dreambuild.AutoCAD;

using ThMEPEngineCore.Command;
using ThMEPEngineCore.Algorithm;
using ThMEPElectrical.ChargerDistribution.Model;
using ThMEPElectrical.ChargerDistribution.Service;

namespace ThMEPElectrical.ChargerDistribution.Command
{
    public class ThChargerCountCmd : ThMEPBaseCommand
    {
        public override void SubExecute()
        {
            using (var docLock = Active.Document.LockDocument())
            using (var currentDb = AcadDatabase.Active())
            {
                // 获取框线
                // 若选取多个框线，则计算所有框线内目标块数量
                var frames = ThChargerSelector.GetFrames(currentDb);
                if (frames.Count == 0)
                {
                    return;
                }

                var chargerBlocks = ThParkingStallUtils.ChargerRecognize(currentDb).Select(o => new ThChargerData(o)).ToList();
                var geometries = chargerBlocks.Select(o => o.Geometry).ToList();

                // 移动到原点附近
                //var transformer = new ThMEPOriginTransformer(Point3d.Origin);
                var transformer = new ThMEPOriginTransformer(frames[0].GeometricExtents.MinPoint);
                ThParkingStallUtils.Transform(transformer, frames.ToCollection());
                ThParkingStallUtils.Transform(transformer, geometries.ToCollection());

                var dictionary = new Dictionary<string, int>();
                frames.ForEach(frame =>
                {
                    var blocks = ThParkingStallUtils.SelectCrossingPolygon(frame, geometries);
                    blocks.ForEach(o =>
                    {
                        var blockData = chargerBlocks.Where(data => data.Geometry.Equals(o)).FirstOrDefault();
                        if (dictionary.ContainsKey(blockData.Name))
                        {
                            dictionary[blockData.Name]++;
                        }
                        else
                        {
                            dictionary.Add(blockData.Name, 1);
                        }
                    });
                });

                ThParkingStallUtils.Reset(transformer, frames.ToCollection());
                ThParkingStallUtils.Reset(transformer, geometries.ToCollection());

                Active.Editor.WriteLine("区域内充电设备数量如下：");
                dictionary.ForEach(pair =>
                {
                    Active.Editor.WriteLine(pair.Key + ":" + pair.Value);
                });
            }
        }
    }
}
