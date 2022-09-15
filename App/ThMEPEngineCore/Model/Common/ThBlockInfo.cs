using ThMEPEngineCore.Algorithm;

namespace ThMEPEngineCore.Model.Common
{
    public class ThBlockInfo
    {
        private string _name = "";
        /// <summary>
        /// 全称
        /// </summary>
        public string Name
        {
            get => _name;
            set
            {
                _name = value;
                var newName = ThMEPXRefService.OriginalFromXref(_name);
                if(newName.Length!= _name.Length)
                {
                    _display = "*|" + newName;
                }
                else
                {
                    _display = _name;
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
