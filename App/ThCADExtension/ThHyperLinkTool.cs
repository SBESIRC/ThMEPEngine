using Autodesk.AutoCAD.DatabaseServices;

namespace ThCADExtension
{
    public static class ThHyperLinkTool
    {
        public static void Add(Entity entity, string description, string name="")
        {
            var linkCollection = entity.Hyperlinks;
            var hyperLink = new HyperLink();
            hyperLink.Name = name;
            hyperLink.Description = description;
            linkCollection.Add(hyperLink);
        }

        public static void Clear(Entity entity)
        {
            entity.Hyperlinks.Clear();
        }
    }
}
