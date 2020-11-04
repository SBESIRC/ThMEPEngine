using System;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using Autodesk.Windows;
using YamlDotNet.RepresentationModel;

namespace TianHua.AutoCAD.ThCui
{
    public class ThCuiProfileYamlParser
    {
        private YamlStream Yaml { get; set; }

        public ThCuiProfileYamlParser(Profile profile)
        {
            Yaml = new YamlStream();
            string config = null;
            switch (profile)
            {
                case Profile.ARCHITECTURE:
                    config = GetResourceFileText("TianHua.AutoCAD.ThCui.Resources.profile_ribbon_architecture.yml");
                    break;
                case Profile.CONSTRUCTION:
                    config = GetResourceFileText("TianHua.AutoCAD.ThCui.Resources.profile_ribbon_construction.yml");
                    break;
                case Profile.STRUCTURE:
                    config = GetResourceFileText("TianHua.AutoCAD.ThCui.Resources.profile_ribbon_structure.yml");
                    break;
                case Profile.HAVC:
                    config = GetResourceFileText("TianHua.AutoCAD.ThCui.Resources.profile_ribbon_havc.yml");
                    break;
                case Profile.ELECTRICAL:
                    config = GetResourceFileText("TianHua.AutoCAD.ThCui.Resources.profile_ribbon_electrical.yml");
                    break;
                case Profile.WSS:
                    config = GetResourceFileText("TianHua.AutoCAD.ThCui.Resources.profile_ribbon_wss.yml");
                    break;
                default:
                    throw new NotSupportedException();
            }
            Yaml.Load(new StringReader(config));
        }

        private string GetResourceFileText(string resource)
        {
            string result = string.Empty;
            Assembly asm = Assembly.GetExecutingAssembly();
            using (Stream stream = asm.GetManifestResourceStream(resource))
            {
                using (StreamReader sr = new StreamReader(stream))
                {
                    result = sr.ReadToEnd();
                }
            }
            return result;
        }

        private void UpdateIsVisible(RibbonItem item, YamlMappingNode node)
        {
            foreach (YamlMappingNode itemNode in (YamlSequenceNode)node.Children[new YamlScalarNode("items")])
            {
                var text = itemNode.Children[new YamlScalarNode("text")];
                if (item.Text == Regex.Unescape(text.ToString()))
                {
                    var attributes = (YamlMappingNode)itemNode.Children[new YamlScalarNode("attributes")];
                    var isVisible = attributes.Children[new YamlScalarNode("IsVisible")];
                    item.IsVisible = bool.Parse(isVisible.ToString());
                }
            }
        }

        public void UpdateIsVisible(RibbonPanel panel)
        {
            var mapping = (YamlMappingNode)Yaml.Documents[0].RootNode;
            var nodes = (YamlSequenceNode)mapping.Children[new YamlScalarNode("panels")];
            foreach (YamlMappingNode node in nodes)
            {
                if (panel.UID == node.Children[new YamlScalarNode("UID")].ToString())
                {
                    var attributes = (YamlMappingNode)node.Children[new YamlScalarNode("attributes")];
                    var isVisible = attributes.Children[new YamlScalarNode("IsVisible")];
                    panel.IsVisible = bool.Parse(isVisible.ToString());
                    if (!panel.IsVisible)
                    {
                        return;
                    }

                    foreach (RibbonItem item in panel.Source.Items)
                    {
                        if (item is RibbonRowPanel rowPanel)
                        {
                            foreach (RibbonItem subItem in rowPanel.Items)
                            {
                                foreach (YamlMappingNode rowPanelNode in (YamlSequenceNode)node.Children[new YamlScalarNode("rowPanels")])
                                {
                                    UpdateIsVisible(subItem, rowPanelNode);
                                }
                            }
                        }
                        else if (!string.IsNullOrEmpty(item.Text))
                        {
                            UpdateIsVisible(item, node);
                        }
                    }
                }
            }
        }
    }
}
