using ThMEPEngineCore.Model;

namespace ThMEPTCH.Model
{
    public class ThTCHProject : ThIfcProject
    {
        public string ProjectName { get; set; }
        public ThTCHSite Site { get; set; }
    }
}
