using ThMEPEngineCore.Algorithm;
using Autodesk.AutoCAD.ApplicationServices;

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
                    var prefix = GetDisplayPrefix(_layer);
                    if (string.IsNullOrEmpty(prefix))
                    {
                        _display = "*|" + newLayer;
                    }
                    else
                    {
                        if(prefix.Length<=40)
                        {
                            _display = prefix + "|" + newLayer;
                        }
                        else
                        {
                            _display = prefix.Substring(0, 40) + "*|" + newLayer;
                        }
                    }
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

        private string GetDisplayPrefix(string xrefLayer)
        {
            // 已绑定外参
            if (xrefLayer.Matches("*`$#`$*"))
            {
                return xrefLayer.Substring(0, xrefLayer.IndexOf('$'));
            }

            // 未绑定外参
            if (xrefLayer.Matches("*|*"))
            {
                return xrefLayer.Substring(0, xrefLayer.IndexOf('*'));
            }

            // 其他非外参
            return "";
        }
    }
}
