using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using AcHelper;
using NFox.Cad;
using Linq2Acad;
using Dreambuild.AutoCAD;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

using ThCADExtension;
using ThCADCore.NTS;
using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore.GeojsonExtractor;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.LaneLine;
using NetTopologySuite.Geometries;
using ThMEPEngineCore.Diagnostics;
using ThMEPLighting.EmgLight.Model;
using ThMEPLighting.EmgLight.Common;

namespace ThMEPLighting.EmgLight.Service
{
    public class LayoutSingleSideService
    {
        private LayoutService m_layoutService;
        private List<ThStruct> m_layoutList;
        private List<ThStruct> m_singleSidelayout;
        private Dictionary<BlockReference, BlockReference> m_evaList;

        public LayoutSingleSideService(List<ThStruct> layoutList, LayoutService layoutService, Dictionary<BlockReference, BlockReference> evacList)
        {
            m_layoutList = layoutList;
            m_layoutService = layoutService;
            m_singleSidelayout = new List<ThStruct>();
            m_evaList = evacList;
        }

        /// <summary>
        /// 单边没有应急指示灯的逻辑， 大于tolMin小于tolMax最远点依次布置
        /// </summary>
        /// <param name="side"></param>
        /// <param name="laneList"></param>
        /// <param name="tolMin"></param>
        /// <param name="tolMax"></param>
        public void LayoutSingleSide(int side, List<ThLane> laneList, int tolMin, int tolMax)
        {
            if (m_layoutService.UsefulStruct[side].Count == 0)
            {
                return;
            }

            layoutSingleSide(m_layoutService.UsefulStruct[side], laneList, tolMin, tolMax);

            DrawUtils.ShowGeometry(m_singleSidelayout.Select(x => x.geom).ToList(), EmgLightCommon.LayerStructLayout, 210, 50);
            m_layoutList.AddRange(m_singleSidelayout.Distinct().ToList());
        }

        public void LayoutSingleSideByEvac(int side, List<ThLane> LaneList, Dictionary<Point3d , Point3d > evacToLaneDict, int tolMin, int tolMax, int tolLenInEvac)
        {
            //< point of transfered blockreference center, this pt in lane xy coordinate system>
            //var evacToLaneDict = evacInLane.ToDictionary(x => x.Value.Position, x => m_layoutService.thLane.TransformPointToLine(x.Value.Position));

            var evacPtOrder = evacToLaneDict.Select(x => x.Key).OrderBy(x => evacToLaneDict[x].X).ToList();


            //fix pre layout
            var firstPartStructList = m_layoutService.UsefulStruct[side].Where(x => m_layoutService.getCenterInLaneCoor(x).X < evacToLaneDict[evacPtOrder.First()].X).ToList();

            if (firstPartStructList.Count > 0)
            {
                layoutSingleSide(firstPartStructList, LaneList, tolMin, tolMax);
            }

            AddToNonUniformLayoutList(evacOnStruct(m_layoutService.UsefulStruct[side], evacPtOrder.First()), tolMin, LaneList);
            for (int i = 1; i < evacPtOrder.Count(); i++)
            {
                var distEvac = evacPtOrder[i].DistanceTo(evacPtOrder[i - 1]);
                if (distEvac / tolLenInEvac > 2)
                {


                    var midPartStructList = m_layoutService.UsefulStruct[side].Where(x => evacToLaneDict[evacPtOrder[i - 1]].X  < m_layoutService.getCenterInLaneCoor(x).X
                                                                                        && m_layoutService.getCenterInLaneCoor(x).X < evacToLaneDict[evacPtOrder[i]].X ).ToList();

                    if (midPartStructList.Count > 0)
                    {
                        midPartStructList.ForEach(x => DrawUtils.ShowGeometry(x.geom, "l7StructMidPart", 86, 30));
                        layoutSingleSide(midPartStructList, LaneList, tolMin, tolMax);
                    }
                }
                else if (distEvac / tolLenInEvac > 1)
                {
                    //中间补点
                    var centerPt = new Point3d((evacPtOrder[i - 1].X + evacPtOrder[i].X) / 2, (evacPtOrder[i - 1].Y + evacPtOrder[i].Y) / 2, 0);

                    var centStruc = evacOnStruct(m_layoutService.UsefulStruct[side], centerPt);
                    AddToNonUniformLayoutList(centStruc, tolMin, LaneList);

                }

                var curStr = evacOnStruct(m_layoutService.UsefulStruct[side], evacPtOrder[i]);
                AddToNonUniformLayoutList(curStr, tolMin, LaneList);
            }

            var lastPartStructList = m_layoutService.UsefulStruct[side].Where(x => m_layoutService.getCenterInLaneCoor(x).X > evacToLaneDict[evacPtOrder.Last()].X).ToList();

            if (lastPartStructList.Count > 0)
            {
                layoutSingleSide(lastPartStructList, LaneList, tolMin, tolMax);
            }

            DrawUtils.ShowGeometry(m_singleSidelayout.Select(x => x.geom).ToList(), EmgLightCommon.LayerStructLayout, 210, 50);
            m_layoutList.AddRange(m_singleSidelayout.Distinct().ToList());

        }

        private void layoutSingleSide(List<ThStruct> PartUsefulStruct, List<ThLane> laneList, int tolMin, int tolMax)
        {
            AddToNonUniformLayoutList(PartUsefulStruct.First(), tolMin, laneList);
            var layIndex = 0;
            for (int i = 1; i < PartUsefulStruct.Count; i++)
            {
                var bAdd = false;
                var dist = m_layoutService.getCenterInLaneCoor(PartUsefulStruct[i]).X - m_layoutService.getCenterInLaneCoor(PartUsefulStruct[layIndex]).X;

                if (dist >= tolMax)
                {
                    bAdd = true;
                }
                else
                {
                    if (i != PartUsefulStruct.Count - 1)
                    {
                        //看下一个点
                        var distNext = m_layoutService.getCenterInLaneCoor(PartUsefulStruct[i + 1]).X - m_layoutService.getCenterInLaneCoor(PartUsefulStruct[i]).X;
                        if (distNext >= tolMax)
                        {
                            bAdd = true;
                        }
                    }
                    else
                    {
                        //最后一个点，往前不到buff，往后检查到车道线末尾
                        m_layoutService.prjPtToLineEnd(m_singleSidelayout.Last(), out var LastPartLines);
                        if (LastPartLines.Length >= tolMax)
                        {
                            bAdd = true;
                        }
                    }
                }

                if (bAdd == true)
                {
                    layIndex = i;
                    AddToNonUniformLayoutList(PartUsefulStruct[i], tolMin, laneList);
                }
            }
        }

        private ThStruct evacOnStruct(List<ThStruct> UsefulStructSide, Point3d evacPt)
        {
            var thStr = UsefulStructSide.OrderBy(x => x.centerPt.DistanceTo(evacPt)).First();
            return thStr;
        }

        private void AddToNonUniformLayoutList(ThStruct structure, double tol, List<ThLane> laneList)
        {
            //var columnFirst = new List<ThStruct>();
            //columnFirst.AddRange(m_singleSidelayout);
            //columnFirst.AddRange(m_layoutService.UsefulColumns[0]);

            var connectLayout = StructureService.CheckIfInLayout(structure, m_singleSidelayout, tol);

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
                m_singleSidelayout.Add(temp);
            }
        }
    }
}
