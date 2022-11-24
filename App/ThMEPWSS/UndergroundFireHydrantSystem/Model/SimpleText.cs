using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPWSS.UndergroundFireHydrantSystem.Model
{
    public struct SimpleText
    {
        public Polyline Rect { get; set; }
        public string Content { get; set; }
        public SimpleText(Polyline rect, string content)
        {
            Rect = rect;
            Content = content;
        }
    }
}
