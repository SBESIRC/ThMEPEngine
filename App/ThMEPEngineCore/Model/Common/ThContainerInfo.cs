using System.Collections.Generic;
using ThMEPEngineCore.Algorithm;

namespace ThMEPEngineCore.Model.Common
{
    public class ThContainerInfo
    {
        private string _name = "";
        public string Name
        {
            get => _name;
            set
            {
                _name = value;
            }
        }
        private string _layer = "";
        public string Layer
        {
            get => _layer;
            set
            {
                _layer = value;
            }
        }
        public string ShortName
        {
            get
            {
                return ThMEPXRefService.OriginalFromXref(Name);
            }
        }
        public string ShortLayer
        {
            get
            {
                return ThMEPXRefService.OriginalFromXref(Layer);
            }
        }
        public ThContainerInfo()
        {
            Name = "";
            Layer = "";
        }
        public ThContainerInfo(string name, string layer)
        {
            _name = name;
            _layer = layer;
        }
    }
    public interface ISetContainer
    {
        List<ThContainerInfo> Containers { get; }
        void SetContainers(List<ThContainerInfo> containers);
    }
}
