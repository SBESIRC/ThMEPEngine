using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPLighting.EmgLight.Assistant;
using ThMEPLighting.EmgLight.Model;
using ThMEPLighting.EmgLight.Common;


namespace ThMEPLighting.EmgLight.Service
{
    class LayoutNonUniformSideService
    {
        private LayoutService m_layoutService;
        private List<ThStruct> m_layoutList;
        private Dictionary<ThStruct, int> m_uniformSideLayout;//int 为不均匀边布灯的标旗: -1: 头部已经layout的,或尾点对面需要布灯的, -1 标旗不计入均匀边最后的layout 0:和前面隔点, 1:对边
        private List<ThStruct> m_nonUniformSideLayout;

        public LayoutNonUniformSideService(List<ThStruct> layoutList, LayoutService layoutService, Dictionary<ThStruct, int> uniformSideLayout)
        {
            m_layoutList = layoutList;
            m_layoutService = layoutService;
            m_uniformSideLayout = uniformSideLayout;
            m_nonUniformSideLayout = new List<ThStruct>();
        }


        /// <summary>
        /// 均匀对边
        /// </summary>
        /// <param name="usefulColumns"></param>
        /// <param name="usefulWalls"></param>
        /// <param name="uniformSide"></param>
        /// <param name="lane"></param>
        /// <param name="columnDistList"></param>
        /// <param name="uniformSideLayout"></param>
        /// <param name="layout"></param>
        public void LayoutOppositeSide(int uniformSide, List<ThLane> laneList)
        {
            int nonUniformSide = uniformSide == 0 ? 1 : 0;

            //List<ThStruct> nonUniformSideLayout;
            Point3d CloestPt = new Point3d();
            double distToNext = 0;
            Polyline ExtendPoly;

            if (m_layoutService.UsefulStruct[nonUniformSide].Count == 0)
            {
                return;
            }

            if (m_uniformSideLayout.Count > 0)
            {
                if (m_uniformSideLayout.Count > 1)
                {
                    //第一个点是头点,不做任何事
                    if (m_uniformSideLayout.First().Value == -1)
                    {
                    }
                    else if (m_uniformSideLayout.First().Value == 1)
                    {
                        //均匀边每个分布,第一个点对边找点
                        m_layoutService.prjPtToLine(m_uniformSideLayout.First().Key, out CloestPt);

                        ExtendPoly = GeomUtils.CreateExtendPoly(CloestPt, m_layoutService.thLane.dir, EmgLightCommon.TolLightRangeMin, EmgLightCommon.TolLane);
                        StructureService.FindClosestStructToPt(m_layoutService.UsefulStruct[nonUniformSide], CloestPt, ExtendPoly, out var closestStruct);

                        AddToNonUniformLayoutList(closestStruct, EmgLightCommon.TolLightRangeMin, laneList);

                    }

                    //从第二个点开始处理
                    for (int i = 1; i < m_uniformSideLayout.Count; i++)
                    {
                        if (m_uniformSideLayout.ElementAt(i).Value == 1)
                        {
                            //均匀边每个分布
                            m_layoutService.prjPtToLine(m_uniformSideLayout.ElementAt(i).Key, out CloestPt);
                            ExtendPoly = GeomUtils.CreateExtendPoly(CloestPt, m_layoutService.thLane.dir, EmgLightCommon.TolLightRangeMin, EmgLightCommon.TolLane);
                            StructureService.FindClosestStructToPt(m_layoutService.UsefulStruct[nonUniformSide], CloestPt, ExtendPoly, out var closestStruct);

                            AddToNonUniformLayoutList(closestStruct, EmgLightCommon.TolLightRangeMin, laneList);
                        }
                        else if (m_uniformSideLayout.ElementAt(i).Value == 0)
                        {
                            //均匀边隔柱分布
                            m_layoutService.findMidPointOnLine(m_uniformSideLayout.ElementAt(i - 1).Key.centerPt, m_uniformSideLayout.ElementAt(i).Key.centerPt, out var midPt);
                            ExtendPoly = GeomUtils.CreateExtendPoly(midPt, m_layoutService.thLane.dir, EmgLightCommon.TolLightRangeMin, EmgLightCommon.TolLane);
                            StructureService.FindClosestStructToPt(m_layoutService.UsefulStruct[nonUniformSide], midPt, ExtendPoly, out var closestStruct);

                            AddToNonUniformLayoutList(closestStruct, EmgLightCommon.TolLightRangeMin, laneList);
                        }
                    }
                }

                //处理最后一个点. 
                ExtendPoly = null;

                if (m_uniformSideLayout.Last().Value == -1)
                {
                    //对边最后一个点是非布点且对边大于1个点, 此点到前一点距离大于4000,在对面找布点
                    if (m_uniformSideLayout.Count > 1)
                    {
                        distToNext = m_uniformSideLayout.Last().Key.centerPt.DistanceTo(m_uniformSideLayout.ElementAt(m_uniformSideLayout.Count - 2).Key.centerPt);
                        if (distToNext > EmgLightCommon.TolLightRangeMin)
                        {
                            m_layoutService.prjPtToLine(m_uniformSideLayout.Last().Key, out CloestPt);
                            ExtendPoly = GeomUtils.CreateExtendPoly(CloestPt, m_layoutService.thLane.dir, EmgLightCommon.TolLightRangeMin, EmgLightCommon.TolLane);
                        }
                    }
                }
                else if (m_uniformSideLayout.Last().Value == 0)
                {
                    //对边最后一个点是已布点,检查车道线后面长度是否需要对面补点
                    m_layoutService.prjPtToLineEnd(m_uniformSideLayout.Last().Key, out var LastPartLines);
                    if (LastPartLines.Length > EmgLightCommon.TolLightRangeMax)
                    {
                        CloestPt = LastPartLines.GetPointAtDist(EmgLightCommon.TolLightRangeMax);
                        ExtendPoly = GeomUtils.CreateExtendPoly(CloestPt, m_layoutService.thLane.dir, EmgLightCommon.TolLightRangeMin, EmgLightCommon.TolLane);
                    }
                }
                if (ExtendPoly != null)
                {
                    StructureService.FindClosestStructToPt(m_layoutService.UsefulStruct[nonUniformSide], CloestPt, ExtendPoly, out var closestStruct);
                    AddToNonUniformLayoutList(closestStruct, EmgLightCommon.TolLightRangeMin, laneList);
                }
            }
            else
            {
                //如果均匀边没有布点
                if (m_layoutService.UsefulStruct[uniformSide].Count > 0)
                {
                    //均匀边有构建, 加入[均匀边]最后点
                    AddToNonUniformLayoutList(m_layoutService.UsefulStruct[uniformSide].Last(), EmgLightCommon.TolLightRangeMin, laneList);
                }
                else
                {
                    //均匀边没有构建,在非均匀边每隔8500布置一个点
                    AddToNonUniformLayoutList(m_layoutService.UsefulStruct[nonUniformSide].First(), EmgLightCommon.TolLightRangeMin, laneList);
                    var layIndex = 0;
                    for (int i = 1; i < m_layoutService.UsefulStruct[nonUniformSide].Count; i++)
                    {
                        var dist = m_layoutService.getCenterInLaneCoor(m_layoutService.UsefulStruct[nonUniformSide][i]).X - m_layoutService.getCenterInLaneCoor(m_layoutService.UsefulStruct[nonUniformSide][layIndex]).X;
                        if (dist > EmgLightCommon.TolLightRangeMax)
                        {
                            layIndex = i;
                            AddToNonUniformLayoutList(m_layoutService.UsefulStruct[nonUniformSide][i], EmgLightCommon.TolLightRangeMin, laneList);
                        }
                    }
                }
            }

            DrawUtils.ShowGeometry(m_nonUniformSideLayout.Select(x => x.geom).ToList(), EmgLightCommon.LayerStructLayout, Color.FromColorIndex(ColorMethod.ByColor, 210), LineWeight.LineWeight050);
            m_layoutList.AddRange(m_nonUniformSideLayout.Distinct().ToList());
        }

        /// <summary>
        /// 构建加入非均匀边布置
        /// </summary>
        /// <param name="layout"></param>
        /// <param name="structure"></param>
        /// <param name="tol"></param>
        /// <param name="lanes"></param>
        /// <param name="nonUniformSideLayout"></param>
        private void AddToNonUniformLayoutList(ThStruct structure, double tol, List<ThLane> laneList)
        {
            var connectLayout = StructureService.CheckIfInLayout(structure, m_layoutList, tol);

            ThStruct temp = null;
            if (connectLayout != null)
            {
                temp = connectLayout;
            }
            else
            {
                temp = structure;
            }

            var bAdd = StructureService.CheckIfInLaneHead(temp, laneList);
            if (bAdd == false && temp != null)
            {
                m_nonUniformSideLayout.Add(temp);
            }
        }


    }
}
