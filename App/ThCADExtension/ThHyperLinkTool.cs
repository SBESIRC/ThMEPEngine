using Autodesk.AutoCAD.DatabaseServices;

namespace ThCADExtension
{
    public static class ThHyperLinkTool
    {
        public static void Add(Entity entity, string description, string name)
        {
            if(!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(description))
            {
                var hyperLink = new HyperLink
                {
                    Name = name,
                    Description = description
                };
                var linkCollection = entity.Hyperlinks;
                if (linkCollection.Count == 0)
                {
                    linkCollection.Add(hyperLink);
                }
                else
                {
                    bool isExisted = false;
                    foreach(HyperLink link in linkCollection)
                    {
                        if(link.Name== name)
                        {
                            link.Description = description;
                            isExisted = true;
                            break;
                        }
                    }
                    if(!isExisted)
                    {
                        linkCollection.Add(hyperLink);
                    }
                }
            }
        }
    }
}
