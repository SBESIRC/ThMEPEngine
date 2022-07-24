﻿using System;
using QuikGraph;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Dreambuild.AutoCAD;
using QuikGraph.Algorithms;
using QuikGraph.Serialization;
using System.Collections.Generic;
using ICSharpCode.SharpZipLib.Zip;
using System.Runtime.Serialization.Formatters.Binary;
using TianHua.Electrical.PDS.Model;
using TianHua.Electrical.PDS.Service;
using TianHua.Electrical.PDS.Project.Module;
using TianHua.Electrical.PDS.Project.Module.ProjectConfigure;
using DwgGraph = QuikGraph.BidirectionalGraph<TianHua.Electrical.PDS.Model.ThPDSCircuitGraphNode, TianHua.Electrical.PDS.Model.ThPDSCircuitGraphEdge<TianHua.Electrical.PDS.Model.ThPDSCircuitGraphNode>>;
using ProjectGraph = QuikGraph.BidirectionalGraph<TianHua.Electrical.PDS.Project.Module.ThPDSProjectGraphNode, TianHua.Electrical.PDS.Project.Module.ThPDSProjectGraphEdge>;
using TianHua.Electrical.PDS.Project.Module.LowVoltageCabinet;

namespace TianHua.Electrical.PDS.Project
{
    public class PDSProjectManagement
    {
        private static PDSProject _project { get { return PDSProject.Instance; } }

        /// <summary>
        /// 推送Data数据
        /// </summary>
        public static void PushGraphData(DwgGraph graph, List<THPDSSubstation> substationList)
        {
            var verticesMap = CreatCircuitVerticesMap(graph.Vertices);
            var projectGraph = BuildProjectGraph(graph, verticesMap);
            _project.graphData = projectGraph.CreatPDSProjectGraph();
            PDSProjectExtend.CalculateProjectInfo();
            _project.substations.Clear();
            _project.substationMap.Clear();
            foreach (var substation in substationList)
            {
                var projectSubstation = new THPDSProjectSubstation();
                projectSubstation.SubstationID = substation.SubstationID;
                foreach (var transformer in substation.Transformers)
                {
                    var projectTransformer = new THPDSProjectTransformer();
                    projectTransformer.TransformerID = transformer.TransformerID;
                    foreach (var lowVoltageCabinet in transformer.LowVoltageCabinets)
                    {
                        var projectLowVoltageCabinet = new FeederCabinet();
                        projectLowVoltageCabinet.LowVoltageCabinetID = lowVoltageCabinet.LowVoltageCabinetID;
                        projectTransformer.LowVoltageCabinets.Add(projectLowVoltageCabinet);
                        foreach (var item in lowVoltageCabinet.Edges)
                        {
                            _project.substationMap.AddMap(projectSubstation, projectTransformer, projectLowVoltageCabinet, verticesMap[item.Target]);
                        }
                        projectTransformer.LowVoltageCabinets.Add(projectLowVoltageCabinet);
                    }
                    projectSubstation.Transformers.Add(projectTransformer);
                }
                _project.substations.Add(projectSubstation);
            }
            _project.BroadcastProjectDataChanged("PushGraphData", "推送Data数据");
        }

        /// <summary>
        /// 二次推送Data数据
        /// </summary>
        public static void SecondaryPushGraphData(DwgGraph graph)
        {
            var verticesMap = CreatCircuitVerticesMap(graph.Vertices);
            var projectGraph = BuildProjectGraph(graph, verticesMap);
            if (!_project.graphData.IsNull() && _project.graphData.Graph.Vertices.Count() > 0)
            {
                _project.graphData.Graph.Vertices.ForEach(node =>
                {
                    node.Load.InstalledCapacity.IsDualPower = node.Details.LoadCalculationInfo.IsDualPower;
                    node.Load.InstalledCapacity.LowPower = node.Load.InstalledCapacity.LowPower > 0 ? node.Details.LoadCalculationInfo.LowPower : 0;
                    node.Load.InstalledCapacity.HighPower = node.Load.InstalledCapacity.HighPower > 0 ? node.Details.LoadCalculationInfo.HighPower : 0;
                });
                ThPDSGraphCompareService compareService = new ThPDSGraphCompareService();
                compareService.Diff(_project.graphData.Graph, projectGraph);
                _project.BroadcastProjectDataChanged("SecondaryPushGraphData", "二次推送Data数据");
            }
            else
            {
                //Project未加载，此时不应该二次抓取数据
                //暂时不报错，跳过处理
            }
        }

        /// <summary>
        /// 项目更新至DWG
        /// </summary>
        public static ProjectGraph ProjectUpdateToDwg(DwgGraph graph)
        {
            var verticesMap = CreatCircuitVerticesMap(graph.Vertices);
            var projectGraph = BuildProjectGraph(graph, verticesMap);
            if (!_project.graphData.IsNull() && _project.graphData.Graph.Vertices.Count() > 0)
            {
                _project.graphData.Graph.Vertices.ForEach(node =>
                {
                    node.Load.InstalledCapacity.IsDualPower = node.Details.LoadCalculationInfo.IsDualPower;
                    node.Load.InstalledCapacity.LowPower = node.Load.InstalledCapacity.LowPower > 0 ? node.Details.LoadCalculationInfo.LowPower : 0;
                    node.Load.InstalledCapacity.HighPower = node.Load.InstalledCapacity.HighPower > 0 ? node.Details.LoadCalculationInfo.HighPower : 0;
                });
                ThPDSGraphCompareService compareService = new ThPDSGraphCompareService();
                compareService.Diff(projectGraph, _project.graphData.Graph);
                return projectGraph;
            }
            else
            {
                //Project未加载，此时不应该更新至DWG
                throw new NotSupportedException();
            }
        }

        private static Dictionary<ThPDSCircuitGraphNode,ThPDSProjectGraphNode> CreatCircuitVerticesMap(IEnumerable<ThPDSCircuitGraphNode> vertices)
        {
            return vertices.ToDictionary(key => key, value => CreatProjectNode(value));
        }

        private static ProjectGraph BuildProjectGraph(DwgGraph graph, Dictionary<ThPDSCircuitGraphNode, ThPDSProjectGraphNode> vertexDir)
        {
            var projectGraph = new ProjectGraph();
            graph.Vertices.ForEach(o => projectGraph.AddVertex(vertexDir[o]));
            foreach (var node in graph.TopologicalSort())
            {
                var edges = graph.OutEdges(node).ToList();
                while (edges.Count > 0)
                {
                    var edge = edges.First();
                    var SameGroupEdges = edges.Where(o => o.Circuit.CircuitUID == edge.Circuit.CircuitUID);
                    if (SameGroupEdges.Count() >= 2)//分支干线
                    {
                        var virtualNode = new ThPDSProjectGraphNode();
                        virtualNode.Type = PDSNodeType.VirtualLoad;
                        virtualNode.Load = new ThPDSLoad();
                        virtualNode.Load.SetLocation(edge.Target.Loads[0].Location);
                        virtualNode.Load.InstalledCapacity = new ThInstalledCapacity();
                        virtualNode.Details.LoadCalculationInfo.LowPower = edges.Sum(o => vertexDir[o.Target].Details.LoadCalculationInfo.LowPower);
                        virtualNode.Details.LoadCalculationInfo.HighPower = edges.Sum(o => vertexDir[o.Target].Details.LoadCalculationInfo.HighPower);
                        virtualNode.Details.LoadCalculationInfo.IsDualPower = edges.Any(o => vertexDir[o.Target].Details.LoadCalculationInfo.IsDualPower);
                        virtualNode.Details.LoadCalculationInfo.LowDemandFactor = edges.GroupBy(o => vertexDir[o.Target].Details.LoadCalculationInfo.LowDemandFactor).OrderByDescending(o => o.Count()).First().Key;
                        virtualNode.Details.LoadCalculationInfo.HighDemandFactor = virtualNode.Details.LoadCalculationInfo.LowDemandFactor;
                        virtualNode.Details.LoadCalculationInfo.PowerFactor = 0.8;
                        projectGraph.AddVertex(virtualNode);
                        projectGraph.AddEdge(new ThPDSProjectGraphEdge(vertexDir[edge.Source], virtualNode) { Circuit = edge.Circuit });
                        SameGroupEdges.ForEach(o => projectGraph.AddEdge(new ThPDSProjectGraphEdge(virtualNode, vertexDir[o.Target]) { Circuit = o.Circuit }));
                        edges.RemoveAll(o => SameGroupEdges.Contains(o));
                    }
                    else
                    {
                        projectGraph.AddEdge(new ThPDSProjectGraphEdge(vertexDir[edge.Source], vertexDir[edge.Target]) { Circuit = edge.Circuit });
                        edges.Remove(edge);
                    }
                }
            }
            return projectGraph;
        }

        public static ThPDSProjectGraphNode CreatProjectNode(ThPDSCircuitGraphNode node)
        {
            var newNode = new ThPDSProjectGraphNode();
            newNode.Type = node.NodeType;
            newNode.Load = node.Loads.Count == 0 ? new ThPDSLoad() : node.Loads[0];
            var load = node.Loads[0];
            if (node.Loads.Count > 1)
            {
                //多负载必定单功率
                newNode.Load.InstalledCapacity.HighPower = node.Loads.Sum(o => o.InstalledCapacity.IsNull() ? 0 : o.InstalledCapacity.HighPower);
                newNode.Details.LoadCalculationInfo.HighPower = newNode.Load.InstalledCapacity.HighPower;
                newNode.Load.InstalledCapacity.IsDualPower = false;
                newNode.Details.LoadCalculationInfo.IsDualPower = false;
            }
            else
            {
                newNode.Load.InstalledCapacity = load.InstalledCapacity;
                newNode.Details.LoadCalculationInfo.LowPower = load.InstalledCapacity.LowPower;
                newNode.Details.LoadCalculationInfo.HighPower = load.InstalledCapacity.HighPower;
                newNode.Details.LoadCalculationInfo.IsDualPower = load.InstalledCapacity.IsDualPower;
            }
            if(load.LoadTypeCat_2 == ThPDSLoadTypeCat_2.FireResistantShutter && newNode.Details.LoadCalculationInfo.HighPower == 0)
            {
                newNode.Details.LoadCalculationInfo.HighPower = _project.projectGlobalConfiguration.FireproofShutterPower;
            }
            newNode.Details.LoadCalculationInfo.LowDemandFactor = load.DemandFactor;
            newNode.Details.LoadCalculationInfo.HighDemandFactor = load.DemandFactor;
            newNode.Details.LoadCalculationInfo.PowerFactor = load.PowerFactor;
            newNode.Details.LoadCalculationInfo.LoadCalculationGrade = load.Phase == ThPDSPhase.三相 ? LoadCalculationGrade.一级 : LoadCalculationGrade.三级;
            return newNode;
        }

        public static void ExportProject(string filePath, string fileName)
        {
            var path = Path.Combine(filePath, fileName);
            try
            {
                string[] ConfigFiles = new string[2];
                var GraphFile = ExportGraph(filePath);
                var GlobalConfigurationFile = ExportGlobalConfiguration(filePath);
                ConfigFiles[0] = GraphFile;
                ConfigFiles[1] = GlobalConfigurationFile;
                using (ZipOutputStream outStream = new ZipOutputStream(File.Create(path)))
                {
                    Zip(ConfigFiles, outStream, "PDSProjectKey");
                }
            }
            catch (Exception ex)
            {

            }
        }

        public static void ImportProject(string filePath)
        {
            var files = UnZip(filePath, "PDSProjectKey");
            var GraphFileBuffer = files.First(o => o.Key.Equals("Graph.Config")).Value;
            var GlobalConfigurationFileBuffer = files.First(o => o.Key.Equals("GlobalConfiguration.Config")).Value;
            GraphFileBuffer.Seek(0, SeekOrigin.Begin);
            //1.直接序列化会报<无法找到程序集的错误，本质原因是"提示找不到程序集，原因是序列化时把序列化类的命名空间等信息保存了，但应用程序和类库的命名空间可能是不一样的,所以提示找不到程序集">
            //graphData.Graph = GraphFileBuffer.DeserializeFromBinary<ThPDSProjectGraphNode, ThPDSProjectGraphEdge, ProjectGraph>();

            //2.重写SerializationBinder，可以解决上述问题，但是会有另外一个问题无法解决，就是反序列化时GetType无法找到对应的Type
            var uBinder = new PDSProjectUBinder();
            _project.graphData = new ThPDSProjectGraph(GraphFileBuffer.DeserializeFromBinary<ThPDSProjectGraphNode, ThPDSProjectGraphEdge, ProjectGraph>(uBinder));

            GlobalConfigurationFileBuffer.Seek(0, SeekOrigin.Begin);
            BinaryFormatter bf = new BinaryFormatter();
            string data = bf.Deserialize(GlobalConfigurationFileBuffer).ToString();
            _project.projectGlobalConfiguration = JsonConvert.DeserializeObject<ProjectGlobalConfiguration>(data);
        }

        public static void ImportGlobalConfiguration(string filePath)
        {
            try
            {
                FileStream fs = new FileStream(filePath, FileMode.Open);
                try
                {
                    BinaryFormatter bf = new BinaryFormatter();
                    string data = bf.Deserialize(fs).ToString();
                    fs.Close();
                    _project.projectGlobalConfiguration = JsonConvert.DeserializeObject<ProjectGlobalConfiguration>(data);
                }
                catch (Exception ex)
                {
                    fs.Close();
                }
            }
            catch (Exception ex)
            {
            }
        }

        private static string ExportGraph(string filePath, string fileName = "Graph.Config")
        {
            var path = Path.Combine(filePath, fileName);
            using (var stream = File.Open(path, FileMode.Create))
            {
                _project.graphData.Graph.SerializeToBinary(stream);
            }
            return path;
        }

        public static string ExportGlobalConfiguration(string filePath, string fileName = "GlobalConfiguration.Config")
        {
            var path = Path.Combine(filePath, fileName);
            var data = JsonConvert.SerializeObject(_project.projectGlobalConfiguration, Formatting.Indented);
            FileInfo fileInfo = new FileInfo(path);
            if (!Directory.Exists(fileInfo.DirectoryName))
            {
                Directory.CreateDirectory(fileInfo.DirectoryName);
            }
            FileStream fs = new FileStream(path, FileMode.Create);
            BinaryFormatter bf = new BinaryFormatter();
            bf.Serialize(fs, data);
            fs.Close();
            return path;
        }

        public static void Zip(string[] files, ZipOutputStream outStream, string pwd)
        {
            try
            {
                for (int i = 0; i < files.Length; i++)
                {
                    if (!File.Exists(files[i]))
                    {
                        throw new Exception("文件不存在");
                    }
                    using (FileStream fs = File.OpenRead(files[i]))
                    {
                        byte[] buffer = new byte[fs.Length];
                        fs.Read(buffer, 0, buffer.Length);
                        if (!string.IsNullOrWhiteSpace(pwd))
                        {
                            outStream.Password = pwd;
                        }
                        ZipEntry ZipEntry = new ZipEntry(Path.GetFileName(files[i]));
                        outStream.PutNextEntry(ZipEntry);
                        outStream.Write(buffer, 0, buffer.Length);
                    }
                    File.Delete(files[i]);
                }
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public static Dictionary<string, MemoryStream> UnZip(string zipFile, string pwd)
        {
            Dictionary<string, MemoryStream> result = new Dictionary<string, MemoryStream>();
            try
            {
                using (ZipInputStream zipInputStream = new ZipInputStream(File.OpenRead(zipFile)))
                {
                    if (!string.IsNullOrWhiteSpace(pwd))
                    {
                        zipInputStream.Password = pwd;
                    }
                    ZipEntry theEntry;
                    while ((theEntry = zipInputStream.GetNextEntry()) != null)
                    {
                        byte[] data = new byte[1024 * 1024];
                        int dataLength = 0;
                        MemoryStream stream = new MemoryStream();
                        while ((dataLength = zipInputStream.Read(data, 0, data.Length)) > 0)
                        {
                            stream.Write(data, 0, dataLength);
                        }
                        result.Add(theEntry.Name, stream);
                    }
                }
            }
            catch (Exception ex)
            {
            }
            return result;
        }
    }
}
