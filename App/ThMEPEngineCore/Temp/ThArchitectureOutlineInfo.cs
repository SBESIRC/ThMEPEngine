using System;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.Temp
{
    public class ThArchitectureOutlineInfo
    {
        public Polyline Outline { get; set; }
        public string ID { get; set; }
        public string ParentID { get; set; }
        public ThArchitectureOutlineInfo(Polyline outline)
        {
            Outline = outline;
            Parse();
        }
        public ThArchitectureOutlineInfo()
        {
            ParentID = "";
            Outline = new Polyline();
            ID = Guid.NewGuid().ToString();           
        }
        private void Parse()
        {
            if(Outline.Hyperlinks.Count>0)
            {
                string description = Outline.Hyperlinks[0].Description;
                foreach (string item in description.Split('_'))
                {
                    var properties = item.Split(':');
                    if(properties.Length==0)
                    {
                        continue;
                    }
                    if(properties.Length==2)
                    {
                        if (properties[0].Trim().ToUpper() == "ID")
                        {
                            ID = properties[1].Trim();
                        }
                        if (properties[0].Trim().ToUpper() == "ParentID".ToUpper())
                        {
                            ParentID = properties[1].Trim();
                        }
                    }                    
                }
            }
            else
            {
                ID = "";
                ParentID = "";
            }
        }
    } 
}
