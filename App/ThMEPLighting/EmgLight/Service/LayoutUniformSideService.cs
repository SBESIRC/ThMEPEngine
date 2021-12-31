using System;
using System.Collections.Generic;
using System.Linq;
using ThCADCore.NTS;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Diagnostics;
using ThMEPLighting.EmgLight.Model;

namespace ThMEPLighting.EmgLight.Service
{
    class LayoutUniformSideService
    {
        private LayoutService m_layoutService;
        private List<ThStruct> m_layoutList;
        private List<List<ThStruct>> m_uniformSideStructsList;
        private Dictionary<ThStruct, int> m_uniformSideLayout;//int 为不均匀边布灯的标旗: -1: 头部已经layout的,或尾点对面需要布灯的, -1 标旗不计入均匀边最后的layout 0:和前面隔点, 1:对边
        private List<List<double>> m_distList;

        public LayoutUniformSideService(List<ThStruct> layoutList, LayoutService layoutService, List<List<ThStruct>> uniformSideStructsList, List<List<double>> distList)
        {
            m_layoutList = layoutList;
            m_layoutService = layoutService;
            m_uniformSideStructsList = uniformSideStructsList;
            m_distList = distList;
            m_uniformSideLayout = new Dictionary<ThStruct, int>();

        }

        /// <summary>
        /// 均匀边
        /// </summary>
        /// <param name="usefulStruct"></param>
        /// <param name="uniformSide"></param>
        /// <param name="Lines"></param>
        /// <param name="distList"></param>
        /// <param name="uniformSideLayout"></param>
        /// <param name="layout"></param>
        public void LayoutUniformSide(int uniformSide, List<ThLane> laneList, out Dictionary<ThStruct, int> uniformSideLayout,int tolMin,int tolMax)
        {
            int layoutStatus = 0; //0:非布灯状态,1:布灯状态
            double cumulateDist = 0;

            //找车道线前段的已分布状况,决定第一个点
            int initial = LayoutFirstUniformSide(uniformSide, laneList, tolMax, ref cumulateDist);

            if (initial < m_uniformSideStructsList[uniformSide].Count)
            {
                for (int i = initial; i < m_uniformSideStructsList[uniformSide].Count; i++)
                {
                    if (i < m_uniformSideStructsList[uniformSide].Count - 1)
                    {
                        cumulateDist += m_distList[uniformSide][i];
                        if (cumulateDist > tolMax)
                        {
                            //累计距离到下个柱距离>tol, 本柱需标记
                            if (layoutStatus != 0)
                            {
                                //如果当前状态为布灯状态,将本柱加入
                                AddToUniformLayoutList(m_uniformSideStructsList[uniformSide][i], 0, tolMin, false);
                                layoutStatus = 0;
                            }
                            else
                            {
                                //如果当前状态为非布灯状态,改成布灯状态
                                layoutStatus = 1;
                            }
                            cumulateDist = m_distList[uniformSide][i];
                        }

                        //本柱到下个柱是否很远
                        if (m_distList[uniformSide][i] > tolMax)
                        {
                            //将自己和下个柱加入,累计距离清零
                            AddToUniformLayoutList(m_uniformSideStructsList[uniformSide][i], 1, tolMin, true);
                            AddToUniformLayoutList(m_uniformSideStructsList[uniformSide][i + 1], 1, tolMin, true);
                            layoutStatus = 0;
                            cumulateDist = 0;
                        }
                    }
                    else
                    {
                        //最后一个点特殊处理
                        if (layoutStatus != 0)
                        {
                            AddToUniformLayoutList(m_uniformSideStructsList[uniformSide][i], 0, tolMin, false);
                        }
                        else
                        {
                            //如果末尾还有点,标值-1
                            AddToUniformLayoutList(m_uniformSideStructsList[uniformSide][i], -1, tolMin, false);
                        }
                    }
                }
            }
            else
            {
                AddToUniformLayoutList(m_uniformSideStructsList[uniformSide][initial], 0, tolMin, false);
            }

            DrawUtils.ShowGeometry(m_uniformSideLayout.Where(x => x.Value == 1 || x.Value == 0).Select(y => y.Key.geom).ToList(), EmgLightCommon.LayerStructLayout,  3, 50);
            m_layoutList.AddRange(m_uniformSideLayout.Where(x => x.Value == 1 || x.Value == 0).Select(y => y.Key).ToList());
            uniformSideLayout = m_uniformSideLayout;
        }

        /// <summary>
        /// 找车道线前段的已分布状况,决定第一个点
        /// </summary>
        /// <param name="layout"></param>
        /// <param name="usefulStruct"></param>
        /// <param name="uniformSide"></param>
        /// <param name="layoutServer"></param>
        /// <param name="lanes"></param>
        /// <param name="uniformSideLayout"></param>
        /// <param name="LastHasNoLightColumn"></param>
        /// <param name="sum"></param>
        /// <returns></returns>
        private int LayoutFirstUniformSide(int uniformSide, List<ThLane> laneList,int tolMax, ref double sum)
        {
            //   A|   |B    |E       [uniform side]
            //-----s[-----------lane----------]e
            //   C|   |D
            //

            int nStart = 0;
            int otherSide = uniformSide == 0 ? 1 : 0;
            bool initialSet = false;

            //  bool added = false;
            if (m_layoutList.Count > 0)
            {
                ////车道线往前做框buffer,选出车线头部的已布情况
                var importLayout = m_layoutService.BuildHeadLayout(m_layoutList, tolMax, EmgLightCommon.TolLane);

                //情况A:
                var uniformSideHeadLayout = importLayout[uniformSide].Where(x => m_layoutService.getCenterInLaneCoor(x).X < 0).ToList();

                //情况B:

                var uniformSideStartLayout = importLayout[uniformSide].Where(x => m_layoutService.getCenterInLaneCoor(x).X <= m_layoutService.getCenterInLaneCoor(m_uniformSideStructsList[uniformSide][0]).X && m_layoutService.getCenterInLaneCoor(x).X >= 0).ToList();

                //情况D:
                var otherSideStartLayout = importLayout[otherSide].Where(x => m_layoutService.getCenterInLaneCoor(x).X >= 0).ToList();


                if (initialSet == false && uniformSideStartLayout.Count > 0)
                {
                    //情况B:
                    m_uniformSideLayout.Add(uniformSideStartLayout.Last(), 0);
                    sum = 0;
                    nStart = 0;
                    initialSet = true;

                }

                if (initialSet == false && otherSideStartLayout.Count > 0)
                {
                    //情况D:
                    //找D开始距离8.5内最远的作为开始
                    if (m_uniformSideStructsList[uniformSide].Count > 0)
                    {
                        for (int i = 0; i < m_uniformSideStructsList[uniformSide].Count; i++)
                        {
                            var dist = m_layoutService.getCenterInLaneCoor(m_uniformSideStructsList[uniformSide][i]).X - m_layoutService.getCenterInLaneCoor(otherSideStartLayout.Last()).X;
                            if (Math.Abs(dist) > tolMax)
                            {
                                if (i > 0)
                                {
                                    if (StructureService.CheckIfInLaneHead(m_uniformSideStructsList[uniformSide][i - 1], laneList) == false)
                                    {
                                        m_uniformSideLayout.Add(m_uniformSideStructsList[uniformSide][i - 1], 0);
                                        sum = 0;
                                        nStart = i - 1;
                                        initialSet = true;
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }

                if (initialSet == false && uniformSideHeadLayout.Count > 0)
                {
                    //情况A:
                    if (m_uniformSideStructsList[uniformSide].Count > 0)
                    {
                        m_uniformSideLayout.Add(importLayout[uniformSide].Last(), -1);
                        sum = importLayout[uniformSide].Last().geom.Distance(m_uniformSideStructsList[uniformSide][0].geom);
                        nStart = 0;
                    }
                    initialSet = true;
                }
            }

            if (initialSet == false)
            {
                //没有已布, 情况C:插入均匀边第一个点
                if (m_uniformSideStructsList[uniformSide].Count > 0)
                {
                    for (int i = 0; i < m_uniformSideStructsList[uniformSide].Count; i++)
                    {
                        if (StructureService.CheckIfInLaneHead(m_uniformSideStructsList[uniformSide][i], laneList) == false)
                        {
                            m_uniformSideLayout.Add(m_uniformSideStructsList[uniformSide][i], 0);
                            sum = 0;
                            nStart = 0;
                            break;
                        }
                    }
                }
            }

            return nStart;
        }

        /// <summary>
        /// 构建加入均匀边
        /// </summary>
        /// <param name="layout"></param>
        /// <param name="structure"></param>
        /// <param name="index"></param>
        /// <param name="tol"></param>
        /// <param name="cover"></param>
        /// <param name="uniformSideLayout"></param>
        private void AddToUniformLayoutList(ThStruct structure, int index, double tol, bool cover)
        {
            var connectLayout =StructureService . CheckIfInLayout(structure,m_layoutList , tol);
            ThStruct temp = null;

            if (connectLayout != null)
            {
                temp = connectLayout;
            }
            else
            {
                temp = structure;
            }

            if (m_uniformSideLayout.ContainsKey(temp) == false)
            {
                m_uniformSideLayout.Add(temp, index);
            }
            else
            {
                if (cover == true)
                {
                    m_uniformSideLayout[temp] = index;
                }
            }
        }
    }
}
