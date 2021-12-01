using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Dreambuild.AutoCAD;
using NFox.Cad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThCADExtension;
using ThMEPEngineCore.CAD;
using ThMEPStructure.GirderConnect.SecondaryBeamConnect.Model.Algorithm;
using ThMEPStructure.GirderConnect.SecondaryBeamConnect.Service;

namespace ThMEPStructure.GirderConnect.SecondaryBeamConnect.Model
{
    public class ThBeamTopologyGraph
    {
        public List<ThBeamTopologyNode> Nodes;
        private Dictionary<Vector3d, List<ThBeamTopologyNode>> groupNodes;
        /// <summary>
        /// 构图
        /// </summary>
        /// <param name="space"></param>
        /// <param name="beams"></param>
        /// <param name="assists"></param>
        public void CreatGraph(List<Polyline> space,List<Line> beams,List<Line> assists)
        {
            Nodes = new List<ThBeamTopologyNode>();
            var spacedic = new Dictionary<Polyline, ThBeamTopologyNode>();
            foreach (var beamSpace in space)
            {
                ThBeamTopologyNode node = new ThBeamTopologyNode(beamSpace);
                Nodes.Add(node);
                spacedic.Add(beamSpace, node);
            }
            BuildGraph(spacedic, beams, assists);
            foreach (var node in Nodes)
            {
                node.MappingBeam();
            }
        }

        /// <summary>
        /// 多边形次梁布置（四边形/五边形）
        /// </summary>
        public void PolygonSecondaryBeamLayout()
        {
            //优先布置四边和五边的梁隔区域
            //因为三边的梁隔区域次梁布置需要它的Neighbor状态
            var NotThreeSideNodes = Nodes.Where(o => o.Edges.Count != 3);
            foreach (var node in NotThreeSideNodes)
            {
                node.CalculateSecondaryBeam();
            }
        }

        /// <summary>
        /// 三角形次梁布置
        /// </summary>
        public void TriangleSecondaryBeamLayout()
        {
            //优先布置四边和五边的梁隔区域
            //因为三边的梁隔区域次梁布置需要它的Neighbor状态
            var ThreeSideNodes = Nodes.Where(o => o.Edges.Count == 3);
            foreach (var node in ThreeSideNodes)
            {
                node.CalculateSecondaryBeam();
            }
        }

        /// <summary>
        /// 后调整
        /// </summary>
        public void AdjustmentDirection()
        {
            //分类，划分区隔
            DrawGraph(Matrix3d.Displacement(new Vector3d(200000,0,0)));
            CorrectWrongDir();
            DrawGraph(Matrix3d.Displacement(new Vector3d(400000, 0, 0)));
            GroupBeamNodes();
        }

        /// <summary>
        /// 修正错误次梁布置
        /// </summary>
        private void CorrectWrongDir()
        {
            var nodes = this.Nodes;
            while (nodes.Count > 0)
            {
                var NextNodes = new List<ThBeamTopologyNode>();
                foreach (var node in nodes)
                {
                    //没有可布置的次梁或次梁布置不可调整，直接跳过
                    if (!node.HaveLayoutBackUp || node.LayoutLines.SecondaryBeamLines.Count == 0)
                    {
                        continue;
                    }
                    var JunctionCount = node.LayoutLines.edges.Count(o => node.Neighbor.Any(x => x.Item2.LayoutLines.edges.Any(y => y.BeamSide.Equals(o.BeamSide))));
                    var SpareJunctionCount = node.SpareLayoutLines.edges.Count(o => node.Neighbor.Any(x => x.Item2.LayoutLines.edges.Any(y => y.BeamSide.Equals(o.BeamSide))));
                    if (JunctionCount < SpareJunctionCount)
                    {
                        //Swap
                        node.SwapLayout();
                        NextNodes.AddRange(node.Neighbor.Select(o => o.Item2));
                    }
                }
                nodes = NextNodes.Distinct().ToList();
            }
        }

        /// <summary>
        /// 为次梁布置分组
        /// </summary>
        private void GroupBeamNodes()
        {
            RegionGrowAlgorithm algorithm = new RegionGrowAlgorithm();
            algorithm.RegionGrow(this.Nodes.ToArray().ToList());
            //algorithm.CreatRandomSeed();

            {
                int index = 1;
                using (Linq2Acad.AcadDatabase acad = Linq2Acad.AcadDatabase.Active())
                {
                    foreach (var item in algorithm.Aggregatespace)
                    {
                        foreach (var a in item)
                        {
                            var entity = a.Boundary.Clone() as Polyline;
                            entity = entity.Buffer(-500)[0] as Polyline;
                            entity.ColorIndex = index;
                            acad.ModelSpace.Add(entity);
                        }
                        index=index%3 +1;
                    }

                    //foreach (var item in algorithm.Adjustmentspace)
                    //{
                    //    foreach (var a in item)
                    //    {
                    //        var entity = a.Boundary.Clone() as Polyline;
                    //        try
                    //        {
                    //            entity = entity.Buffer(-2000)[0] as Polyline;
                    //        }
                    //        catch (Exception ex)
                    //        {
                    //            entity = entity.Buffer(-1000)[0] as Polyline;
                    //        }
                    //        entity.ColorIndex = index + 4;
                    //        acad.ModelSpace.Add(entity);
                    //    }
                    //    index=index%3 +1;
                    //}
                }
            }

            //GeneticAlgorithm genetic = new GeneticAlgorithm(algorithm.Aggregatespace, algorithm.Adjustmentspace[3]);
            //genetic.Run();

            foreach (var space in algorithm.Adjustmentspace)
            {
                var neighber = algorithm.Aggregatespace.Where(o => o.IsNeighbor(space)).OrderByDescending(o=>o.Count).ToList();
                BeamGameTreeAlgorithm gameTree = new BeamGameTreeAlgorithm(neighber, space);
                gameTree.Start();
                var Result = gameTree.ChessGameResult;
                if (!Result.IsNull())
                {
                    for (int i = 0; i < Result.Length; i++)
                    {
                        var node = space[i];
                        if(!node.CheckCurrentPixel(neighber[Result[i] - 1].First()))
                        {
                            node.SwapLayout();
                        }
                    }
                }
                else
                {
                    //throw new NotImplementedException();
                }
            }

        }

        public void DrawGraph(Matrix3d matrix,bool drawBoundary = true)
        {
            using (Linq2Acad.AcadDatabase acad = Linq2Acad.AcadDatabase.Active())
            {
                foreach (var node in this.Nodes)
                {
                    if (drawBoundary)
                    {
                        var polyline = node.Boundary.Clone() as Polyline;
                        polyline.ColorIndex =130;
                        polyline.TransformBy(matrix);
                        acad.ModelSpace.Add(polyline);
                    }
                    foreach (var beamline in node.LayoutLines.SecondaryBeamLines)
                    {
                        var line = beamline.Clone() as Line;
                        line.TransformBy(matrix);
                        acad.ModelSpace.Add(line);
                    }
                }
            }
        }

        private void BuildGraph(Dictionary<Polyline, ThBeamTopologyNode> spaceDic, List<Line> beams, List<Line> assists)
        {
            ThCADCoreNTSSpatialIndex spatialIndex = new ThCADCoreNTSSpatialIndex(spaceDic.Keys.ToCollection());
            foreach (var beam in beams)
            {
                Point3d center = beam.GetCenterPt();
                var square = center.CreateSquare(25);
                var fence = spatialIndex.SelectFence(square).Cast<Polyline>().ToList();
                if (fence.Count > 2)
                {
                    throw new NotImplementedException();
                }
                else if (fence.Count == 2)
                {
                    ThBeamTopologyNode node1 = spaceDic[fence[0]];
                    ThBeamTopologyNode node2 = spaceDic[fence[1]];
                    node1.UseBeams.Add(beam);
                    node2.UseBeams.Add(beam);
                    node1.Neighbor.Add(new Tuple<Line, ThBeamTopologyNode>(beam, node2));
                    node2.Neighbor.Add(new Tuple<Line, ThBeamTopologyNode>(beam, node1));
                }
                else if (fence.Count == 1)
                {
                    ThBeamTopologyNode node1 = spaceDic[fence[0]];
                    node1.UseBeams.Add(beam);
                }
                else
                {

                }
            }
            foreach (var assist in assists)
            {
                Point3d center = assist.GetCenterPt();
                var square = center.CreateSquare(25);
                var fence = spatialIndex.SelectFence(square).Cast<Polyline>().ToList();
                if (fence.Count > 2)
                {
                    throw new NotImplementedException();
                }
                else if (fence.Count == 2)
                {
                    ThBeamTopologyNode node1 = spaceDic[fence[0]];
                    ThBeamTopologyNode node2 = spaceDic[fence[1]];
                    node1.Assists.Add(assist);
                    node2.Assists.Add(assist);
                    node1.Neighbor.Add(new Tuple<Line, ThBeamTopologyNode>(assist, node2));
                    node2.Neighbor.Add(new Tuple<Line, ThBeamTopologyNode>(assist, node1));
                }
                else if (fence.Count == 1)
                {
                    ThBeamTopologyNode node1 = spaceDic[fence[0]];
                    node1.Assists.Add(assist);
                }
            }
        }


        
    }
}
