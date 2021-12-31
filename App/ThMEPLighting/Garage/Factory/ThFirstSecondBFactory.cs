using System.Linq;
using System.Collections.Generic;
using NFox.Cad;
using ThCADCore.NTS;
using ThCADExtension;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPLighting.Common;
using ThMEPLighting.Garage.Service;

namespace ThMEPLighting.Garage.Factory
{
    /// <summary>
    /// 利用中线的Normalize方向实现1、2号线逻辑
    /// </summary>
    public class ThFirstSecondBFactory : ThFirstSecondAFactory
    {
        public ThFirstSecondBFactory(List<Line> centerLines,double width)
            :base(centerLines, width)
        {
        }
        private void Init()
        {
            SideLineNumberDict = new Dictionary<Line, EdgePattern>();
            CenterSpatialIndex = new ThCADCoreNTSSpatialIndex(centerLines.ToCollection());
        }
        public override void Produce()
        {
            // 中心线是已经处理过的线
            if (centerLines.Count == 0 || width <= 1.0)
            {
                return;
            }
            // 初始化
            Init();

            // 返回的是中心线和边线的对应关系
            CenterSideDict = ThFindCenterPairService.Find(centerLines, width);
            //Print();

            HandleCenterSideDict(width);
            //Print();

            // 给CenterSideDict赋值
            CenterSideDict.ForEach(o =>
            {
                o.Value.Item1.ForEach(k => AddSideLineNumberDict(k, EdgePattern.Unknown));
                o.Value.Item2.ForEach(k => AddSideLineNumberDict(k, EdgePattern.Unknown));
            });

            // 设置方向
            SetEdgePattern();
        }

        private void SetEdgePattern()
        {
            CenterSideDict.ForEach(o =>
            {
                var normalLine = o.Key.NormalizeLaneLine();
                var firstDir = normalLine.LineDirection().GetPerpendicularVector(); // 1号线的指定方向
                var centerUpDir = o.Key.LineDirection().GetPerpendicularVector(); // 指向CenterSideDict.Value的Item1
                if (firstDir.IsSameDirection(centerUpDir))
                {
                    // 若1号线的指定方向与centerUpDir同向，表示其对应的值的Item1集合为1号线，Item2集合为2号线
                    UpdateSideLineNumberDict(o.Value.Item1, EdgePattern.First);
                    UpdateSideLineNumberDict(o.Value.Item2, EdgePattern.Second);
                }
                else
                {
                    // 若1号线的指定方向与centerUpDir不同向，表示其对应的值的Item1集合为2号线，Item2集合为1号线
                    UpdateSideLineNumberDict(o.Value.Item1, EdgePattern.Second);
                    UpdateSideLineNumberDict(o.Value.Item2, EdgePattern.First);
                }
            });
        }
    }
}
