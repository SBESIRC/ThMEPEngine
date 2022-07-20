using System.IO;
using System.Windows.Controls;
using YamlDotNet.RepresentationModel;

namespace TianHua.Electrical.PDS.UI.Services
{
    public static class ThPDSContextMenuYamlParser
    {
        public static MenuItem LoadAddCircuitMenuItem()
        {
            var yaml = new YamlStream();
            yaml.Load(GetResource());
            return ParseYamlStream(yaml);
        }

        private static MenuItem ParseYamlStream(YamlStream yamls)
        {
            var menuitem = new MenuItem();
            var rootNode = (YamlMappingNode)yamls.Documents[0].RootNode;
            menuitem.Header = rootNode["Header"].ToString();
            foreach (YamlMappingNode node in (YamlSequenceNode)rootNode.Children[new YamlScalarNode("Items")])
            {
                var item = new MenuItem();
                item.Header = node["Header"].ToString();
                foreach (YamlMappingNode child in (YamlSequenceNode)node.Children[new YamlScalarNode("Items")])
                {
                    item.Items.Add(new MenuItem()
                    {
                        Header = child["Header"].ToString(),
                    });
                }
                menuitem.Items.Add(item);
            }
            return menuitem;
        }

        private static StringReader GetResource()
        {
            using (Stream MemoryStream = new MemoryStream(Properties.Resources.AddCircuit))
            {
                using (StreamReader sr = new StreamReader(MemoryStream))
                {
                    return new StringReader(sr.ReadToEnd());
                }
            }
        }
    }
}
