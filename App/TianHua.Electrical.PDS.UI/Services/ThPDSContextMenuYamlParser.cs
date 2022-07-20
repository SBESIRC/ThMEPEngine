using System.IO;
using System.Windows.Controls;
using YamlDotNet.RepresentationModel;

namespace TianHua.Electrical.PDS.UI.Services
{
    public class ThPDSContextMenuYamlParser
    {
        private YamlStream Yaml { get; set; }
        private YamlMappingNode Root => (YamlMappingNode)Yaml.Documents[0].RootNode;
        public  bool IsCentralizedPowerCircuit { get; set; }
        public ThPDSContextMenuYamlParser()
        {
            Yaml = new YamlStream();
            IsCentralizedPowerCircuit = false;
        }

        public MenuItem Parse()
        {
            Yaml.Load(GetResource());
            return ParseYamlStream();
        }

        private MenuItem ParseYamlStream()
        {
            var menuitem = new MenuItem()
            {
                Header = Header(Root),
            };
            foreach (YamlMappingNode node in Items(Root))
            {
                if (IsVisible(node))
                {
                    var item = new MenuItem()
                    {
                        Header = Header(node),
                    };
                    foreach (YamlMappingNode child in Items(node))
                    {
                        if (IsVisible(node))
                        {
                            item.Items.Add(new MenuItem()
                            {
                                Header = Header(child),
                            });
                        }
                    }
                    menuitem.Items.Add(item);
                }
            }
            return menuitem;
        }

        private YamlSequenceNode Items(YamlMappingNode node)
        {
            return node.Children[new YamlScalarNode("Items")] as YamlSequenceNode;
        }

        private string Header(YamlMappingNode node)
        {
            var header = node.Children[new YamlScalarNode("Header")];
            return header.ToString();
        }

        private bool IsVisible(YamlMappingNode node)
        {
            var attributes = (YamlMappingNode)node.Children[new YamlScalarNode("attributes")];
            var isVisible = attributes.Children[new YamlScalarNode("IsVisible")];
            return bool.Parse(isVisible.ToString());
        }

        private byte[] ResourceBuffer()
        {
            return IsCentralizedPowerCircuit ? Properties.Resources.AddCircuit_CentralizedPower : Properties.Resources.AddCircuit;
        }

        private StringReader GetResource()
        {
            using (Stream ms = new MemoryStream(ResourceBuffer()))
            {
                using (StreamReader sr = new StreamReader(ms))
                {
                    return new StringReader(sr.ReadToEnd());
                }
            }
        }
    }
}
