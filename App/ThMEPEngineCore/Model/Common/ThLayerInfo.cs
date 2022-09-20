using ThMEPEngineCore.Algorithm;

namespace ThMEPEngineCore.Model.Common
{
    public class ThLayerInfo
    {
        private string _layer = "";
        public string Layer
        {
            get => _layer;
            set
            {
                _layer = value;
                var newLayer = ThMEPXRefService.OriginalFromXref(_layer);
                if (newLayer.Length != _layer.Length)
                {
                    _display = "*|" + newLayer;
                }
                else
                {
                    _display = _layer;
                }
            }
        }

        private bool _isSelected = false;
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                _isSelected = value;
            }
        }


        private string _display = "";
        public string Display
        {
            get => _display;
            set
            {
                _display = value;
            }
        }
    }
}
