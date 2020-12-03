using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.AutoCAD.Geometry;
using QuickGraph;

namespace ThMEPHVAC.Duct
{
    public class ThDuctDesignEngine
    {
        private AdjacencyGraph<ThDuctVertex, ThDuctEdge<ThDuctVertex>> Graph { get; set; }
        public ThDuctVertex StartVertex { get; set; }

        public ThDuctDesignEngine(AdjacencyGraph<ThDuctVertex, ThDuctEdge<ThDuctVertex>> graph, ThDuctVertex startvertex)
        {
            Graph = graph;
            StartVertex = startvertex;
            DuctVolumeCalculation(Graph.OutEdges(startvertex).First());
        }

        private void DuctVolumeCalculation(ThDuctEdge<ThDuctVertex> searchedge)
        {
            if (Graph.OutDegree(searchedge.Target) == 0)
            {
                if (searchedge.DraughtCount == 0)
                {
                    searchedge.AirVolume = 0;
                }
                else
                {
                    searchedge.AirVolume = GetVolumeInDraughtEdge(searchedge);
                    searchedge.TotalVolumeInEdgeChain = searchedge.AirVolume;
                }
                return;
            }
            else
            {
                if (Graph.OutDegree(searchedge.Target) == 1)
                {
                    if (searchedge.DraughtCount == 0)
                    {
                        DuctVolumeCalculation(Graph.OutEdges(searchedge.Target).First());
                        searchedge.AirVolume = Graph.OutEdges(searchedge.Target).First().AirVolume;
                        searchedge.TotalVolumeInEdgeChain += Graph.OutEdges(searchedge.Target).First().TotalVolumeInEdgeChain;
                    }
                    else
                    {
                        searchedge.AirVolume = GetVolumeInDraughtEdge(searchedge);
                        DuctVolumeCalculation(Graph.OutEdges(searchedge.Target).First());
                        searchedge.TotalVolumeInEdgeChain = searchedge.AirVolume + Graph.OutEdges(searchedge.Target).First().TotalVolumeInEdgeChain;
                    }
                }
                else
                {
                    var outedgesintarget = Graph.OutEdges(searchedge.Target);
                    foreach (var edge in outedgesintarget)
                    {
                        DuctVolumeCalculation(edge);
                    }
                    searchedge.AirVolume = outedgesintarget.Sum(e=>e.TotalVolumeInEdgeChain);
                    searchedge.TotalVolumeInEdgeChain = searchedge.AirVolume;
                }
            }
        }

        private double GetVolumeInDraughtEdge(ThDuctEdge<ThDuctVertex> draughtedge)
        {
            return draughtedge.DraughtCount * draughtedge.DraughtInfomation.First().Parameters.DraughtVolume;
        }
    }
}
