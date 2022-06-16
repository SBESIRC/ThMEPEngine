using System;
using QuikGraph;
using System.IO;
using System.Linq;
using Dreambuild.AutoCAD;
using QuikGraph.Serialization;
using TianHua.Electrical.PDS.Model;
using TianHua.Electrical.PDS.Service;
using TianHua.Electrical.PDS.Project.Module;
using TianHua.Electrical.PDS.Project.Module.Configure;
using TianHua.Electrical.PDS.Project.Module.ProjectConfigure;
using DwgGraph = QuikGraph.BidirectionalGraph<TianHua.Electrical.PDS.Model.ThPDSCircuitGraphNode, TianHua.Electrical.PDS.Model.ThPDSCircuitGraphEdge<TianHua.Electrical.PDS.Model.ThPDSCircuitGraphNode>>;
using ProjectGraph = QuikGraph.BidirectionalGraph<TianHua.Electrical.PDS.Project.Module.ThPDSProjectGraphNode, TianHua.Electrical.PDS.Project.Module.ThPDSProjectGraphEdge>;
using Newtonsoft.Json;
using System.Runtime.Serialization.Formatters.Binary;
using ICSharpCode.SharpZipLib.Zip;
using System.Collections.Generic;

namespace TianHua.Electrical.PDS.Project
{
    /// <summary>
    /// PDS项目
    /// </summary>
    [Serializable]
    public class PDSProject
    {
        //==============SINGLETON============
        //fourth version from:
        //http://csharpindepth.com/Articles/General/Singleton.aspx
        private static PDSProject instance = new PDSProject();
        // Explicit static constructor to tell C# compiler
        // not to mark type as beforefieldinit    
        static PDSProject() { }
        internal PDSProject() { }
        public static PDSProject Instance { get { return instance; } }
        //-------------SINGLETON-----------------

        public ThPDSProjectGraph graphData;

        public ProjectGlobalConfiguration projectGlobalConfiguration;

        //public BreakerComponentConfiguration breakerConfig;

        private bool InitializedState;

        public Action DataChanged;

        /// <summary>
        /// 加载项目
        /// </summary>
        /// <param name="url"></param>
        public void Load(string url)
        {
            if (!InitializedState)
            {
                this.LoadGlobalConfig();
                InitializedState = true;
            }
            if (string.IsNullOrEmpty(url))
            {
                //Creat New Project
                this.graphData = new ProjectGraph().CreatPDSProjectGraph();
                PDSProjectExtend.CalculateProjectInfo();
                this.projectGlobalConfiguration = new ProjectGlobalConfiguration();
                instance.DataChanged?.Invoke();
            }
            else
            {
                ImportProject(url);
                instance.DataChanged?.Invoke();
            }
        }

        /// <summary>
        /// 推送Data数据
        /// </summary>
        public void PushGraphData(DwgGraph graph)
        {
            var ProjectGraph = new ProjectGraph();
            var VertexDir = graph.Vertices.ToDictionary(key => key, value => CreatProjectNode(value));
            graph.Vertices.ForEach(o => ProjectGraph.AddVertex(VertexDir[o]));
            graph.Edges.ForEach(o => ProjectGraph.AddEdge(
                new ThPDSProjectGraphEdge(VertexDir[o.Source], VertexDir[o.Target]) { Circuit = o.Circuit }
                ));
            this.graphData = ProjectGraph.CreatPDSProjectGraph();
            PDSProjectExtend.CalculateProjectInfo();
            instance.DataChanged?.Invoke();
        }

        /// <summary>
        /// 二次推送Data数据
        /// </summary>
        public void SecondaryPushGraphData(DwgGraph graph)
        {
            var ProjectGraph = new ProjectGraph();
            var VertexDir = graph.Vertices.ToDictionary(key => key, value => CreatProjectNode(value));
            graph.Vertices.ForEach(o => ProjectGraph.AddVertex(VertexDir[o]));
            graph.Edges.ForEach(o => ProjectGraph.AddEdge(
                new ThPDSProjectGraphEdge(VertexDir[o.Source], VertexDir[o.Target]) { Circuit = o.Circuit }
                ));
            if (!this.graphData.IsNull() && this.graphData.Graph.Vertices.Count() > 0)
            {
                this.graphData.Graph.Vertices.ForEach(node =>
                {
                    node.Load.InstalledCapacity.IsDualPower = node.Details.IsDualPower;
                    node.Load.InstalledCapacity.LowPower = node.Load.InstalledCapacity.LowPower > 0 ? node.Details.LowPower : 0;
                    node.Load.InstalledCapacity.HighPower = node.Load.InstalledCapacity.HighPower > 0 ? node.Details.HighPower : 0;
                });
                ThPDSGraphCompareService compareService = new ThPDSGraphCompareService();
                compareService.Diff(this.graphData.Graph, ProjectGraph);
                instance.DataChanged?.Invoke();
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
        public ProjectGraph ProjectUpdateToDwg(DwgGraph graph)
        {
            var ProjectGraph = new ProjectGraph();
            var VertexDir = graph.Vertices.ToDictionary(key => key, value => CreatProjectNode(value));
            graph.Vertices.ForEach(o => ProjectGraph.AddVertex(VertexDir[o]));
            graph.Edges.ForEach(o => ProjectGraph.AddEdge(
                new ThPDSProjectGraphEdge(VertexDir[o.Source], VertexDir[o.Target]) { Circuit = o.Circuit }
                ));
            if (!this.graphData.IsNull() && this.graphData.Graph.Vertices.Count() > 0)
            {
                this.graphData.Graph.Vertices.ForEach(node =>
                {
                    node.Load.InstalledCapacity.IsDualPower = node.Details.IsDualPower;
                    node.Load.InstalledCapacity.LowPower = node.Load.InstalledCapacity.LowPower > 0 ? node.Details.LowPower : 0;
                    node.Load.InstalledCapacity.HighPower = node.Load.InstalledCapacity.HighPower > 0 ? node.Details.HighPower : 0;
                });
                ThPDSGraphCompareService compareService = new ThPDSGraphCompareService();
                compareService.Diff(ProjectGraph, this.graphData.Graph);
                return ProjectGraph;
            }
            else
            {
                //Project未加载，此时不应该更新至DWG
                throw new NotSupportedException();
            }
        }

        public ThPDSProjectGraphNode CreatProjectNode(ThPDSCircuitGraphNode node)
        {
            var newNode = new ThPDSProjectGraphNode();
            newNode.Type = node.NodeType;
            newNode.Load = node.Loads.Count == 0 ? new ThPDSLoad() : node.Loads[0];
            if (node.Loads.Count > 1)
            {
                //多负载必定单功率
                newNode.Load.InstalledCapacity.HighPower = node.Loads.Sum(o => o.InstalledCapacity.IsNull() ? 0 : o.InstalledCapacity.HighPower);
                newNode.Details.HighPower = newNode.Load.InstalledCapacity.HighPower;
                newNode.Load.InstalledCapacity.IsDualPower = false;
                newNode.Details.IsDualPower = false;
            }
            else
            {
                var load = node.Loads[0];
                newNode.Load.InstalledCapacity = load.InstalledCapacity;
                newNode.Details.LowPower = load.InstalledCapacity.LowPower;
                newNode.Details.HighPower = load.InstalledCapacity.HighPower;
                newNode.Details.IsDualPower = load.InstalledCapacity.IsDualPower;
            }
            return newNode;
        }

        public void ExportProject(string filePath, string fileName)
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

        public void ImportProject(string filePath)
        {
            try
            {
                var files = UnZip(filePath, "PDSProjectKey");
                var GraphFileBuffer = files.First(o => o.Key.Equals("Graph.Config")).Value;
                var GlobalConfigurationFileBuffer = files.First(o => o.Key.Equals("GlobalConfiguration.Config")).Value;
                GraphFileBuffer.Seek(0, SeekOrigin.Begin);
                //1.直接序列化会报<无法找到程序集的错误，本质原因是"提示找不到程序集，原因是序列化时把序列化类的命名空间等信息保存了，但应用程序和类库的命名空间可能是不一样的,所以提示找不到程序集">
                //graphData.Graph = GraphFileBuffer.DeserializeFromBinary<ThPDSProjectGraphNode, ThPDSProjectGraphEdge, ProjectGraph>();

                //2.重写SerializationBinder，可以解决上述问题，但是会有另外一个问题无法解决，就是反序列化时GetType无法找到对应的Type
                var uBinder = new PDSProjectUBinder();
                this.graphData = new ThPDSProjectGraph(GraphFileBuffer.DeserializeFromBinary<ThPDSProjectGraphNode, ThPDSProjectGraphEdge, ProjectGraph>(uBinder));

                GlobalConfigurationFileBuffer.Seek(0, SeekOrigin.Begin);
                BinaryFormatter bf = new BinaryFormatter();
                string data = bf.Deserialize(GlobalConfigurationFileBuffer).ToString();
                this.projectGlobalConfiguration = JsonConvert.DeserializeObject<ProjectGlobalConfiguration>(data);
            }
            catch (Exception ex)
            {
            }
        }

        public void ImportGlobalConfiguration(string filePath)
        {
            try
            {
                FileStream fs = new FileStream(filePath, FileMode.Open);
                try
                {
                    BinaryFormatter bf = new BinaryFormatter();
                    string data = bf.Deserialize(fs).ToString();
                    fs.Close();
                    projectGlobalConfiguration = JsonConvert.DeserializeObject<ProjectGlobalConfiguration>(data);
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

        private string ExportGraph(string filePath, string fileName = "Graph.Config")
        {
            var path = Path.Combine(filePath, fileName);
            using (var stream = File.Open(path, FileMode.Create))
            {
                graphData.Graph.SerializeToBinary(stream);
            }
            return path;
        }

        public string ExportGlobalConfiguration(string filePath, string fileName = "GlobalConfiguration.Config")
        {
            var path = Path.Combine(filePath, fileName);
            var data = JsonConvert.SerializeObject(projectGlobalConfiguration, Formatting.Indented);
            //var dataModel = JsonConvert.DeserializeObject<ProjectGlobalConfiguration>(data);
            //File.WriteAllText(path, data);
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

        public void Zip(string[] files, ZipOutputStream outStream, string pwd)
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

        public Dictionary<string, MemoryStream> UnZip(string zipFile, string pwd)
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
