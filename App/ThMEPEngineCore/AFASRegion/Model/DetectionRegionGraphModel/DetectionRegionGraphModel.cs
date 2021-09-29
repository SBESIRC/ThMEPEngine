using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThCADExtension;
using ThMEPEngineCore.AFASRegion.Expand;
using ThMEPEngineCore.AFASRegion.Utls;
using ThMEPEngineCore.CAD;

namespace ThMEPEngineCore.AFASRegion.Model.DetectionRegionGraphModel
{
    public class DetectionRegionGraphModel
    {
        private List<DetectionRegionVertexModel> Vertexs { get; set; }
        private List<DetectionRegionEdgeModel> Edges { get; set; }
        private List<AFASBeamContour> beams { get; set; }
        private AFASDetector DetectorType { get; set; }



        public DetectionRegionGraphModel(List<AFASBeamContour> beam,AFASDetector detector)
        {
            Vertexs = new List<DetectionRegionVertexModel>();
            Edges = new List<DetectionRegionEdgeModel>();
            beams = beam;
            DetectorType = detector;
        }

        /// <summary>
        /// 构建图
        /// </summary>
        public void BuildAFASGraph(List<Entity> space)
        {
            space.ForEach(o => Vertexs.Add(new DetectionRegionVertexModel(o, DetectorType)));
        }

        public List<Entity> GetMergeRegion()
        {
            List<Entity> mergeRegions = new List<Entity>();
            //预处理
            {
                //先剔除大区域，大区域不需要考虑合并
                Vertexs.RemoveAll(o =>
                {
                    if (o.Leval == 1)
                    {
                        mergeRegions.Add(o.Data);
                        return true;
                    }
                    return false;
                });
                //建立连接关系，并断开高梁
                for (int i = 0; i < Vertexs.Count - 1; i++)
                {
                    DetectionRegionVertexModel vertex = Vertexs[i];
                    var othervertexs = Vertexs.Skip(i + 1);
                    foreach (var othervertex in othervertexs)
                    {
                        if (vertex.Boundary.Intersects(othervertex.Boundary))
                        {
                            var intersecLines = vertex.Boundary.GetAllLinesInPolyline().Intersect(othervertex.Boundary.GetAllLinesInPolyline(), new LineCompare()).Where(o => o.Length > 200.0).ToList();
                            if (intersecLines.Count > 0)
                            {
                                BeamType ConnectbeamType = BeamType.HighBeam;
                                double intersecLen = 0.0;
                                intersecLines.ForEach(intersecLine =>
                                {
                                    var beamtype = beams.First(o => intersecLine.StartPoint.IsPointOnLine(o.BeamCenterline) && intersecLine.EndPoint.IsPointOnLine(o.BeamCenterline)).BeamType;
                                    if (beamtype != BeamType.HighBeam)
                                    {
                                        ConnectbeamType = ConnectbeamType == BeamType.LowBeam ? BeamType.LowBeam : beamtype;
                                        intersecLen += intersecLine.Length;
                                    }
                                });
                                if (ConnectbeamType != BeamType.HighBeam)
                                {
                                    vertex.edgs.Add(new DetectionRegionEdgeModel(vertex, othervertex, ConnectbeamType, intersecLen));
                                    othervertex.edgs.Add(new DetectionRegionEdgeModel(othervertex, vertex, ConnectbeamType, intersecLen));
                                }
                            }
                        }
                    }
                }
            }
            //合并低梁
            MergeLowBeamRegion();
            //合并中梁
            mergeRegions.AddRange(MergeMiddleBeamRegion());
            return mergeRegions;
        }

        /// <summary>
        /// 合并低梁
        /// </summary>
        private void MergeLowBeamRegion()
        {
            //合并所有底梁
            while(true)
            {
                var edg = this.Vertexs.SelectMany(o => o.edgs).FirstOrDefault(o => o.beamType == BeamType.LowBeam);
                if (!edg.IsNull())
                {
                    MergeVertexs(edg.StartVertex, edg.EndVertex);
                }
                else
                {
                    break;
                }
            }
        }

        /// <summary>
        /// 合并中粱
        /// </summary>
        /// <returns></returns>
        private List<Entity> MergeMiddleBeamRegion()
        {
            List<Entity> mergeRegions = new List<Entity>();
            var MiddleRegions = this.Vertexs.Where(o => o.Leval == 2).ToList();//拿到二级区域
            var LowRegions = this.Vertexs.Where(o => o.Leval > 2).ToList();//拿到三四五级区域

            //Step1 合并小区域
            var LowRegionChain = GetChain(LowRegions);
            LowRegionChain.ForEach(chain =>
            {
                if(chain.Count==1)
                {
                    //小区域就只有本身自己，周围都是二级区域
                    //do not
                }
                else if(chain.Min(o=>o.Leval)<chain.Count)
                {
                    //链表组成部分超过探测器个数限制，不能合并
                    //do not
                }
                else
                {
                    //符合条件的区域，但是也不能随意合并，要检查合并后的规范性
                    if (MergeDetectionRegionScoringService.CanMergeRegion(chain))
                    {
                        var vertex = MergeVertexs(chain);
                        mergeRegions.Add(vertex.Data);
                        DeleteVertex(vertex);
                    }
                }
            });

            //Step2 如果小区域剩余的还足够多，再次筛选合并更小区域
            LowRegions = this.Vertexs.Where(o => o.Leval > 2).ToList();//拿到剩余三四五级区域
            if (LowRegions.Count > 3)
            {
                double SmallArea = Math.Round(LowRegions.Average(o => o.Boundary.Area) * 0.75, 2);
                LowRegions.RemoveAll(o => o.Boundary.Area > SmallArea);
                LowRegionChain = GetChain(LowRegions);
                LowRegionChain.ForEach(chain =>
                {
                    if (chain.Count == 1)
                    {
                        //小区域就只有本身自己，周围都是二级区域
                        //do not
                    }
                    else if (chain.Min(o => o.Leval) < chain.Count)
                    {
                        //链表组成部分超过探测器个数限制，不能合并
                        //do not
                    }
                    else
                    {
                        //符合条件的区域，但是也不能随意合并，要检查合并后的规范性
                        if (MergeDetectionRegionScoringService.CanMergeRegion(chain))
                        {
                            var newVertex = MergeVertexs(chain);
                            if (!newVertex.edgs.Any(o => { var service = MergeDetectionRegionScoringService.ImpressionScore(newVertex, o.EndVertex); return service.IsLegalRegion & service.IsCoedge; }))
                            {
                                //没有可以合并的邻居，无法再次合并
                                mergeRegions.Add(newVertex.Data);
                                DeleteVertex(newVertex);
                            }
                        }
                    }
                });
            }

            //Step3 走到这里，认为都是较大的区域了
            mergeRegions.AddRange(MergeMiddleAreaRegion());

            return mergeRegions;
        }

        /// <summary>
        /// 合并梁间区域
        /// </summary>
        /// <returns></returns>
        private List<Entity> MergeMiddleAreaRegion()
        {
            List<Entity> mergeRegions = new List<Entity>();
            while (Vertexs.Count>0)
            {
                var vertex = this.Vertexs.OrderBy(o => o.edgs.Count).ThenBy(o => o.Boundary.Area).First();
                if (vertex.edgs.Count > 0)
                {
                    var regionScoreDic = vertex.edgs.ToDictionary(
                        x => x.EndVertex,
                        y => MergeDetectionRegionScoringService.ImpressionScore(vertex, y.EndVertex))
                        .Where(x => x.Value.IsLegalRegion)//剔除不合法图形
                        .Where(x => x.Value.IsCoedge)//剔除不应合并图形
                        .OrderByDescending(x => x.Value.Score);
                    if (regionScoreDic.Count() > 0)
                    {
                        var newVertex = MergeVertexs(vertex, regionScoreDic.First().Key);
                        if (!newVertex.edgs.Any(o => { var service = MergeDetectionRegionScoringService.ImpressionScore(newVertex, o.EndVertex); return service.IsLegalRegion & service.IsCoedge; }))
                        {
                            //没有可以合并的邻居，无法再次合并
                            mergeRegions.Add(newVertex.Data);
                            DeleteVertex(newVertex);
                        }
                    }
                    else
                    {
                        //没有应该合并的邻居，无法合并
                        mergeRegions.Add(vertex.Data);
                        DeleteVertex(vertex);
                    }
                }
                else
                {
                    //没有邻居，无法合并
                    mergeRegions.Add(vertex.Data);
                    DeleteVertex(vertex);
                }
            }
            return mergeRegions;
        }

        private List<List<DetectionRegionVertexModel>> GetChain(List<DetectionRegionVertexModel> lowRegions)
        {
            List<List<DetectionRegionVertexModel>> chains = new List<List<DetectionRegionVertexModel>>();
            while (lowRegions.Count > 0)
            {
                List<DetectionRegionVertexModel> chain = new List<DetectionRegionVertexModel>();
                DetectionRegionVertexModel vertex = lowRegions.First();
                chain.Add(vertex); lowRegions.Remove(vertex);
                ExtensionChain(vertex,ref chain, ref lowRegions);
                chains.Add(chain);
            }
            return chains;
        }

        private void ExtensionChain(DetectionRegionVertexModel vertex, ref List<DetectionRegionVertexModel> chain,ref List<DetectionRegionVertexModel> lowRegions)
        {
            foreach (var edg in vertex.edgs)
            {
                if(lowRegions.Contains(edg.EndVertex))
                {
                    chain.Add(edg.EndVertex);
                    lowRegions.Remove(edg.EndVertex);
                    ExtensionChain(edg.EndVertex, ref chain, ref lowRegions);
                }
            }
        }

        /// <summary>
        /// 删除节点
        /// </summary>
        /// <param name="vertex"></param>
        private void DeleteVertex(DetectionRegionVertexModel vertex)
        {
            //切断所有邻居对自己的连接
            vertex.edgs.Select(o => o.EndVertex).ToList().ForEach(o => o.edgs.RemoveAll(x => x.EndVertex == vertex));
            //删除自己
            Vertexs.Remove(vertex);
        }

        /// <summary>
        /// 合并节点
        /// </summary>
        /// <param name="v1"></param>
        /// <param name="v2"></param>
        /// <returns></returns>
        private DetectionRegionVertexModel MergeVertexs(DetectionRegionVertexModel v1, DetectionRegionVertexModel v2)
        {
            DetectionRegionVertexModel newVertex = new DetectionRegionVertexModel(v1, v2);
            DeleteVertex(v1); DeleteVertex(v2);
            var ConnectEdgs = v1.edgs.Select(o => new DetectionRegionEdgeModel(newVertex, o.EndVertex, o.beamType, o.IntersecLen)).ToList();
            ConnectEdgs.RemoveAll(o => o.EndVertex == v2);
            v2.edgs.ForEach(o =>
            {
                var edg = ConnectEdgs.FirstOrDefault(x => x.EndVertex == o.EndVertex);
                if(!edg.IsNull())
                {
                    edg.IntersecLen += o.IntersecLen;
                    edg.beamType = edg.beamType == BeamType.LowBeam ? BeamType.LowBeam : o.beamType;
                }
                else if(o.EndVertex != v1)
                {
                    ConnectEdgs.Add(new DetectionRegionEdgeModel(newVertex, o.EndVertex, o.beamType, o.IntersecLen));
                }
            });
            newVertex.edgs = ConnectEdgs;
            newVertex.edgs.ForEach(o =>
            {
                o.EndVertex.edgs.Add(new DetectionRegionEdgeModel(o.EndVertex, o.StartVertex, o.beamType, o.IntersecLen));
            });
            this.Vertexs.Add(newVertex);
            return newVertex;
        }

        /// <summary>
        /// 合并节点
        /// </summary>
        /// <param name="parm"></param>
        /// <returns></returns>
        private DetectionRegionVertexModel MergeVertexs(List<DetectionRegionVertexModel> parm)
        {
            var vertex = parm[0];
            for (int i = 1; i < parm.Count; i++)
            {
                vertex = MergeVertexs(vertex, parm[i]);
            }
            return vertex;
        }

        /// <summary>
        /// 画图当前状态，测试用，后期要删掉
        /// </summary>
        public void DrawGroup()
        {
            using (Linq2Acad.AcadDatabase acad = Linq2Acad.AcadDatabase.Active())
            {
                int colorindex = 1;
                Vertexs.ForEach(o =>
                {
                    o.Data.ColorIndex = colorindex;
                    acad.ModelSpace.Add(o.Data);
                    DBText dBText = new DBText() { Height = 200, WidthFactor = 0.7, HorizontalMode = TextHorizontalMode.TextMid, TextString = o.edgs.Count.ToString(), Position = o.Boundary.GetRectangleCenterPt(), AlignmentPoint = o.Boundary.GetRectangleCenterPt() };
                    dBText.ColorIndex = colorindex;
                    acad.ModelSpace.Add(dBText);
                    colorindex++;
                });
            }
        }
    }

    public class DetectionRegionVertexModel
    {
        public List<DetectionRegionEdgeModel> edgs { get; set; }
        public Entity Data { get; set; }
        public Polyline Boundary { get; set; }
        public bool IsVisited { get; set; } = false;
        public int Leval { get; set; }

        public int FusionNum { get; set; } = 1;

        public DetectionRegionVertexModel(Entity Vertexdata, AFASDetector detector)
        {
            edgs = new List<DetectionRegionEdgeModel>();
            if (Vertexdata is MPolygon mPolygon)
            {
                Boundary = mPolygon.Shell();
                Data = mPolygon.Buffer(1)[0] as Entity;
                Leval = detector.RegionLevel(mPolygon.Area);
            }
            else
            {
                Boundary = Vertexdata as Polyline;
                Data = Boundary.Buffer(1)[0] as Polyline;
                Leval = detector.RegionLevel(Boundary.Area);
            }
        }

        public DetectionRegionVertexModel(DetectionRegionVertexModel v1, DetectionRegionVertexModel v2)
        {
            edgs = new List<DetectionRegionEdgeModel>();
            this.Data = v1.Data.ToNTSPolygon().Union(v2.Data.ToNTSPolygon()).ToDbCollection()[0] as Entity;
            Leval = Math.Min(v1.Leval, v2.Leval);
            FusionNum = v1.FusionNum + v2.FusionNum;
            if (Data is MPolygon mPolygon)
            {
                Boundary = mPolygon.Shell();
            }
            else
            {
                Boundary = Data as Polyline;
            }
        }
    }

    public class DetectionRegionEdgeModel
    {
        public DetectionRegionVertexModel StartVertex { get; set; }
        public DetectionRegionVertexModel EndVertex { get; set; }

        public double IntersecLen { get; set; }
        public BeamType beamType { get; set; }
        public DetectionRegionEdgeModel(DetectionRegionVertexModel Vertex1, DetectionRegionVertexModel Vertex2, BeamType beamtype, double intersecLen)
        {
            StartVertex = Vertex1;
            EndVertex = Vertex2;
            beamType = beamtype;
            IntersecLen = intersecLen;
        }
    }
}
