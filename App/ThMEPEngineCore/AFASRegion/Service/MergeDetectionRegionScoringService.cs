using Autodesk.AutoCAD.DatabaseServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThCADExtension;
using ThMEPEngineCore.AFASRegion.Utls;

namespace ThMEPEngineCore.AFASRegion.Model.DetectionRegionGraphModel
{
    /// <summary>
    /// 区间合并 评分模型
    /// 
    /// 目前共受到多个参数影响
    /// ①体量是否相等
    /// ②被合并的Polyline 边数量
    /// ③每个边平均 权重
    /// ④合并后影响 两个的连接子集
    /// ⑤低梁，中梁，高粱
    /// ⑥合并后的图像 标准性
    /// ⑦共边的权重比
    /// ⑧
    /// </summary>
    public class MergeDetectionRegionScoringService
    {
        /// <summary>
        /// 评分
        /// </summary>
        /// <param name="source"></param>
        /// <param name="target"></param>
        /// <returns></returns>
        public static ScoringModel ImpressionScore(DetectionRegionVertexModel source, DetectionRegionVertexModel target)
        {
            ScoringModel model = new ScoringModel();
            if (Math.Min(source.Leval, target.Leval) < source.FusionNum + target.FusionNum || source.edgs.Count(o => o.EndVertex == target) != 1)
            {
                //如果两个图形不能合并 或 两个图形不相交
                model.IsLegalRegion = false;
            }
            else
            {
                AreaScore(source, target, 1, ref model);//优先判断面积，先确定是否是面积过大区域
                CoedgeWeightScore(source, target, 3, ref model);
                GlobalLocationScore(source, target, 2, ref model);
                IntersectionScore(source, target, 3, ref model);
                UnionRegionScore(source, target, 3, ref model);
            }
            return model;
        }

        
        public static bool CanMergeRegion(List<DetectionRegionVertexModel> vertexModels)
        {
            NetTopologySuite.Geometries.Geometry polygon = vertexModels[0].Data.ToNTSPolygonalGeometry();
            for (int i = 1; i < vertexModels.Count; i++)
            {
                polygon = polygon.Union(vertexModels[i].Data.ToNTSPolygonalGeometry());
            }
            Entity mergeRegion = polygon.ToDbCollection()[0] as Entity;
            Polyline boundary = new Polyline();
            if (mergeRegion is Polyline polyline)
            {
                boundary = polyline;
            }
            if (mergeRegion is MPolygon mPolygon)
            {
                boundary = mPolygon.Shell();
            }
            return boundary.IsSimilar(boundary.OBB(), 0.75);//认为75%近似长方形，就鼓励合并，否则不鼓励合并
        }

        /// <summary>
        /// 共边权重得分
        /// </summary>
        /// <param name="source"></param>
        /// <param name="target"></param>
        /// <returns></returns>
        private static void CoedgeWeightScore(DetectionRegionVertexModel source, DetectionRegionVertexModel target, int ce, ref ScoringModel model)
        {
            double intersecLen = source.edgs.First(o => o.EndVertex == target).IntersecLen;
            var sourcePercentage = Math.Round(intersecLen / source.Boundary.Length, 3);
            var targetPercentage = Math.Round(intersecLen / target.Boundary.Length, 3);
            if ((sourcePercentage < 0.1 || targetPercentage < 0.1) && !model.IsAreaGap)
            {
                //共边占比小于1/10，认为共边过短，不应该合并
                model.IsCoedge = false;
            }
            model.Score += (10 * sourcePercentage + 10 * targetPercentage) * ce;
        }

        /// <summary>
        /// 面积得分
        /// </summary>
        /// <param name="source"></param>
        /// <param name="target"></param>
        /// <returns></returns>
        private static void AreaScore(DetectionRegionVertexModel source, DetectionRegionVertexModel target, int ce, ref ScoringModel model)
        {
            var maxArea = Math.Max(source.Boundary.Area, target.Boundary.Area);
            var minArea = Math.Min(source.Boundary.Area, target.Boundary.Area);
            var coefficient = Math.Round(maxArea / minArea, 1);
            double score;
            if (coefficient > 4)
            {
                //面积之比大于4，认为两个空间差距过大，应该吸附 5-6分
                score = 4 + Math.Round((coefficient - 4) / coefficient, 1);
                model.IsAreaGap = true;
            }
            else if (coefficient < 1.25)
            {
                //面积之比小于5/4,认为两个空间基本处于一个量级，应该合并 8-9分
                score = 10 - coefficient;
            }
            else
            {
                //面积之比处于中间级，认为可能 较小面积更应该被别的面合并 2分
                score = 2.0;
            }
            model.Score += score * ce;
        }

        /// <summary>
        /// 全局位置得分
        /// </summary>
        /// <param name="source"></param>
        /// <param name="target"></param>
        /// <returns></returns>
        private static void GlobalLocationScore(DetectionRegionVertexModel source, DetectionRegionVertexModel target, int ce, ref ScoringModel model)
        {
            //target的连接点越多，它的位置越偏向于局部中心，应优先合并其他target
            model.Score += (10 - target.edgs.Count * 2) * ce;
        }

        /// <summary>
        /// 公共子集得分
        /// </summary>
        /// <param name="source"></param>
        /// <param name="target"></param>
        /// <returns></returns>
        private static void IntersectionScore(DetectionRegionVertexModel source, DetectionRegionVertexModel target, int ce, ref ScoringModel model)
        {
            var sourceedgs = source.edgs.Select(o => o.EndVertex);
            var targetedgs = target.edgs.Select(o => o.EndVertex);
            var count = sourceedgs.Intersect(targetedgs).Count();
            //公共子集越多，说明两个图形越“小”，且被公共子集“包围”着，越应该合并
            model.Score += count * 3 * ce;
        }

        private static void UnionRegionScore(DetectionRegionVertexModel source, DetectionRegionVertexModel target, int ce, ref ScoringModel model)
        {
            Entity mergeRegion = source.Data.ToNTSPolygonalGeometry().Union(target.Data.ToNTSPolygonalGeometry()).ToDbCollection()[0] as Entity;
            Polyline boundary = new Polyline();
            if (mergeRegion is Polyline polyline)
            {
                boundary = polyline;
            }
            if (mergeRegion is MPolygon mPolygon)
            {
                boundary = mPolygon.Shell();
            }
            double measure = boundary.SimilarityMeasure(boundary.OBB());
            double score = measure * 10  * ce;
            if (measure > 0.85)
            {
                //近似完美图形，格外加分，引导算法往完美图形合并
                score += 20.0;
                model.IsPerfectUnionRegion = true;
            }
            else if (measure < 0.55 && measure > 0.45)
            {
                //粗略认为近似三角形，认为也可以合并，额外加分
                score += 8.0;
            }
            else if(measure <= 0.45)
            {
                //认为完全不应该合并，格外减分
                score -= 20.0;
            }
            model.Score += score;
        }
    }
}
