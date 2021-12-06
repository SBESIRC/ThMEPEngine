using System.Linq;
using ThMEPLighting.Common;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPLighting.Garage.Service.Number;

namespace ThMEPLighting.Garage.Service
{
    public class ThFindStartIndexService
    {
        /// <summary>
        /// 查找到的索引编号
        /// </summary>
        public int FindIndex { get; set; }
        public int StartIndex { get; set; }
        protected bool? IsPreFind { get; set; }
        private ThLightGraphService LightGraph { get; set; }
        private ThLinkPath SinglePath { get; set; }
        private int LoopNumber { get; set; }
        private int DefaultNumber { get; set; }
        private bool IsSingleRowNumber { get; set; }
        private ThFindStartIndexService(ThLightGraphService lightGraph, ThLinkPath singlePath,
            int loopNumber,bool isSingleRowNumber,int defaultNumber)
        {
            LightGraph = lightGraph;
            SinglePath = singlePath;
            StartIndex = -1;
            FindIndex = -1;
            IsPreFind = null;
            LoopNumber = loopNumber;
            DefaultNumber = defaultNumber;
            IsSingleRowNumber = isSingleRowNumber;
        }
        public bool IsFind
        {
            get
            {
                return StartIndex != -1;
            }
        }
        public static ThFindStartIndexService Find(ThLightGraphService lightGraph, 
            ThLinkPath singlePath, int loopNumber, bool isSingleRowNumber,int defaultNumber)
        {
            var instance = new ThFindStartIndexService(lightGraph, singlePath, loopNumber, isSingleRowNumber, defaultNumber);
            instance.Find();
            return instance;
        }
        private void Find()
        {
            if(SinglePath.IsMain)
            {
                StartIndex = DefaultNumber;
            }
            else
            {
                FindPromPreLinkPath();                
            }
        }        
        private void FindPromPreLinkPath()
        {
            var results = LightGraph.Links.Where(m =>
            {
                return m.Edges.Where(n => n.Id == SinglePath.PreEdge.Id).Any();
            });
            if (results.Count() > 0)
            {
                //暂时不支持往灯线的后续查找,只能向前查找,根据需求再说
                FindPreLinkPath(results.First(), SinglePath.PreEdge, SinglePath.Start);
            }
        }
        private void FindPreLinkPath(ThLinkPath preLinkPath, ThLightEdge preEdge,Point3d originPt)
        {
            int i = preLinkPath.Edges.IndexOf(preEdge);          
            Point3d endPt = originPt;
            while (i>=0)
            {
                ThLightEdge currentEdge = preLinkPath.Edges[i--];
                int preIndex = FindPreIndex(currentEdge, endPt);
                if (preIndex != -1)
                {
                    FindIndex = preIndex;
                    IsPreFind = true;
                    if (IsSingleRowNumber)
                    {
                        StartIndex = ThSingleRowLightNumber.NextIndex(LoopNumber, preIndex, DefaultNumber);
                    }
                    else
                    {
                        StartIndex = ThDoubleRowLightNumber.NextIndex(LoopNumber, preIndex, DefaultNumber);
                    }
                    return;
                }
                else
                {
                    endPt = currentEdge.GetDirectionPts().Item1;
                }
            }         
        }
        private void FindNextLinkPath(ThLinkPath preLinkPath, ThLightEdge preEdge, Point3d originPt)
        {
            int index = preLinkPath.Edges.IndexOf(preEdge);
            int j = index + 1;
            Point3d startPt = originPt;
            while (j < preLinkPath.Edges.Count)
            {
                ThLightEdge currentEdge = preLinkPath.Edges[j++];
                int nextIndex = FindNextIndex(currentEdge, startPt);
                if (nextIndex != -1)
                {
                    FindIndex = nextIndex;
                    IsPreFind = false;
                    if (currentEdge.IsDX)
                    {
                        if (IsSingleRowNumber)
                        {
                            StartIndex = ThSingleRowLightNumber.PreIndex(LoopNumber, nextIndex, StartIndex);
                        }
                        else
                        {
                            StartIndex = ThDoubleRowLightNumber.PreIndex(LoopNumber, nextIndex, StartIndex);
                        }
                    }
                    else
                    {
                        StartIndex = nextIndex;
                    }
                    return;
                }
                else
                {
                    startPt = currentEdge.GetDirectionPts().Item2;
                }
            }
        }
        private int FindPreIndex(ThLightEdge lightEdge,Point3d pt)
        {
            int preIndex = -1;
            if(lightEdge.LightNodes.Count==0)
            {
                return preIndex;
            }
            //表是此边是往前找的
            if (lightEdge.IsDX)
            {
                var preNode = lightEdge.FindPreLightNode(pt);
                if (!string.IsNullOrEmpty(preNode.Number))
                {
                    preIndex = preNode.GetIndex();
                }
            }
            else
            {
                //如果是非灯线，
                if (lightEdge.LightNodes.Count > 0 &&
                    !string.IsNullOrEmpty(lightEdge.LightNodes[0].Number))
                {
                    preIndex = lightEdge.LightNodes[0].GetIndex();
                }
            }
            return preIndex;
        }
        private int FindNextIndex(ThLightEdge lightEdge, Point3d pt)
        {
            int nextIndex = -1;
            if (lightEdge.IsDX)
            {
                var nextNode = lightEdge.FindNextLightNode(pt);
                if (!string.IsNullOrEmpty(nextNode.Number))
                {
                    nextIndex = nextNode.GetIndex();                    
                }
            }
            else
            {
                //如果是非灯线，
                if (lightEdge.LightNodes.Count > 0 &&
                    !string.IsNullOrEmpty(lightEdge.LightNodes[0].Number))
                {
                    nextIndex = lightEdge.LightNodes[0].GetIndex();
                }
            }
            return nextIndex;
        }
    }
}
