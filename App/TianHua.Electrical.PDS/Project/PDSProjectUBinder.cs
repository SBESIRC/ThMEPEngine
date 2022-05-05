using System;
using QuikGraph;
using System.Reflection;
using System.Runtime.Serialization;
using TianHua.Electrical.PDS.Project.Module;

namespace TianHua.Electrical.PDS.Project
{
    public class PDSProjectUBinder : SerializationBinder
    {
        public override Type BindToType(string assemblyName, string typeName)
        {
            /*
             * 这个地方暂时还没有找到很“完美”的解决方案
             * 目前崩溃的主要原因是BidirectionalGraph<ThPDSProjectGraphNode, ThPDSProjectGraphEdge>这个Type，仅通过程序集"QuikGraph.DLL"是反射不回来的，因为ThPDSProjectGraphNode和ThPDSProjectGraphEdge不在"QuikGraph.DLL"里，但是我们通过typeof(BidirectionalGraph<ThPDSProjectGraphNode, ThPDSProjectGraphEdge>)这个Type，发现他的程序集Assembly，确实是"QuikGraph.DLL"，这个是比较费解的
             * 目前的理解是GetType不太支持跨DLL的泛型对象？？？（不确定）
             */
            
            try
            {
                Type type = Type.GetType(typeName);
                //Type a = typeof(BidirectionalGraph<ThPDSProjectGraphNode, ThPDSProjectGraphEdge>);
                //Assembly ass = Assembly.LoadFrom("file:///D:/Code/App/TianHua.Electrical.PDS.UI/bin/Debug-NET45/QuikGraph.DLL");
                //var assTyle = ass.GetType(typeName,true,true);
                if (type.IsNull())
                {
                    //通过重写反序列化的SerializationBinder，把有关QuikGraph的对象，"绕过去"
                    if (typeName.Contains("BidirectionalGraph"))
                        return typeof(BidirectionalGraph<ThPDSProjectGraphNode, ThPDSProjectGraphEdge>);
                    else if (typeName.Contains("VertexEdgeDictionary"))
                        return typeof(QuikGraph.Collections.VertexEdgeDictionary<ThPDSProjectGraphNode, ThPDSProjectGraphEdge>);
                    else if (typeName.Contains("EdgeList"))
                        return typeof(QuikGraph.Collections.EdgeList<ThPDSProjectGraphNode, ThPDSProjectGraphEdge>);
                    throw new Exception("无法找到Type");
                }
                return type;
            }
            catch
            {
                return Assembly.Load(assemblyName).GetType(typeName);
            }
        }

    }
}
