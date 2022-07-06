using System;
using System.Linq;
using System.Collections.Generic;
using ThMEPStructure.Reinforcement.Model;

namespace ThMEPStructure.Reinforcement.Service
{
    public class ThDataGroupService
    {
        private bool ConsiderWall { get; set; }
        /// <summary>
        /// 配筋率阶差
        /// </summary>
        private double ReinforceRatioStep { get; set; }
        /// <summary>
        /// 配箍率阶差
        /// </summary>
        private double StirrupRatioStep { get; set; }
        public ThDataGroupService(
            bool considerWall,
            double stirrupRatioStep,
            double reinforceRatioStep)
        {
            ConsiderWall = considerWall;
            StirrupRatioStep = stirrupRatioStep;
            ReinforceRatioStep = reinforceRatioStep;
        }
        public List<List<EdgeComponentExtractInfo>> Group(List<EdgeComponentExtractInfo> infos)
        {
            var results = new List<List<EdgeComponentExtractInfo>>();
            //results.AddRange(GroupNonStandards(infos.Where(o => !o.IsStandard).ToList()));
            results.AddRange(GroupStandards(infos.Where(o => o.IsStandard && !o.IsCalculation).ToList()));
            results.AddRange(GroupStandardCals(infos.Where(o => o.IsStandard && o.IsCalculation).ToList()));
            return results;
        }

        private List<List<EdgeComponentExtractInfo>> GroupNonStandards(List<EdgeComponentExtractInfo> infos)
        {
            return GroupStandards(infos);
        }

        private List<List<EdgeComponentExtractInfo>> GroupStandards(List<EdgeComponentExtractInfo> infos)
        {
            //分组规则
            //1、外形相同 2、尺寸相同，3、YBZ、GBZ 4、考虑墙体位置
            var results = new List<List<EdgeComponentExtractInfo>>();
            if(ConsiderWall)
            {
                var groups = infos.GroupBy(o => o.StandardType + o.Spec +
                            o.ShapeCode + o.ComponentType.ToString() + o.TypeCode+o.LinkWallPos);
                foreach (var group in groups)
                {
                    results.Add(group.ToList());
                }
            }
            else
            {
                var groups = infos.GroupBy(o => o.StandardType + o.Spec +
                            o.ShapeCode + o.ComponentType.ToString() + o.TypeCode);
                foreach (var group in groups)
                {
                    results.Add(group.ToList());
                }
            }
            return results;
        }
        private List<List<EdgeComponentExtractInfo>> GroupStandardCals(List<EdgeComponentExtractInfo> infos)
        {
            /*
             * 分组规则：
             * 1、外形相同 2、尺寸相同，3、YBZ、GBZ 4、考虑墙体位置
             * 上述分组完后，再对组内的元素先按配筋率再按配箍率的阶梯进行组划分
             * 组划分原则为：以同类的最小值作为基数，逐级向上划分
             * 例如：｛200,220,240,250,260,255,300｝一组数，按归并阶差50，
             * 归并结果为｛250,250,250,250,300,300,300｝
            */
            var results = new List<List<EdgeComponentExtractInfo>>();
            var g1Groups = GroupStandards(infos);
            g1Groups.ForEach(g1 =>
            {
                var g2Groups = GroupByReinforceRatio(g1);
                g2Groups.ForEach(g2 =>
                {
                    var g3Groups = GroupByStirrupRatioStep(g2);
                    g3Groups.ForEach(g =>
                    {
                        results.Add(g);
                    });
                });
            });
            return results;
        }
        private List<List<EdgeComponentExtractInfo>> GroupByReinforceRatio(List<EdgeComponentExtractInfo> infos)
        {
            var results = new List<List<EdgeComponentExtractInfo>>();
            if(infos.Count==0)
            {
                return results;
            }
            var minimum = infos.Select(o => o.ReinforceRatio).OrderBy(o => o).First();
            var groupDict = new Dictionary<Tuple<double, double>, List<EdgeComponentExtractInfo>>();
            infos.ForEach(o =>
            {
                bool isFind = false;
                foreach(var item in groupDict)
                {
                    var start = item.Key.Item1;
                    var end = item.Key.Item2;
                    if(o.ReinforceRatio >= start && o.ReinforceRatio <= end)
                    {
                        isFind = true;
                        item.Value.Add(o);
                        break;
                    }
                }
                if(!isFind)
                {
                    var start = minimum;
                    int i = 0;
                    while (true)
                    {
                        var end = start + ReinforceRatioStep;
                        if (o.ReinforceRatio >= start && o.ReinforceRatio <= end)
                        {
                            groupDict.Add(Tuple.Create(start, end),new List<EdgeComponentExtractInfo> {o});
                            break;
                        }
                        start = end;
                        if(i++>10000)
                        {
                            break;
                        }
                    }
                }
            });
            foreach(var item in groupDict)
            {
                results.Add(item.Value);
            }
            return results;
        }
        private List<List<EdgeComponentExtractInfo>> GroupByStirrupRatioStep(List<EdgeComponentExtractInfo> infos)
        {
            var results = new List<List<EdgeComponentExtractInfo>>();
            if (infos.Count == 0)
            {
                return results;
            }
            var minimum = infos.Select(o => o.StirrupRatio).OrderBy(o => o).First();
            var groupDict = new Dictionary<Tuple<double, double>, List<EdgeComponentExtractInfo>>();
            infos.ForEach(o =>
            {
                bool isFind = false;
                foreach (var item in groupDict)
                {
                    if (o.StirrupRatio >= item.Key.Item1 && o.StirrupRatio <= item.Key.Item2)
                    {
                        isFind = true;
                        item.Value.Add(o);
                        break;
                    }
                }
                if (!isFind)
                {
                    var start = minimum;
                    int i = 0;
                    while (true)
                    {
                        var end = start + StirrupRatioStep;
                        if (o.StirrupRatio >= start && o.StirrupRatio <= end)
                        {
                            groupDict.Add(Tuple.Create(start, end), new List<EdgeComponentExtractInfo> { o });
                            break;
                        }
                        start = end;
                        if (i++ > 10000)
                        {
                            break;
                        }
                    }
                }
            });
            foreach (var item in groupDict)
            {
                results.Add(item.Value);
            }
            return results;
        }
    }
}
