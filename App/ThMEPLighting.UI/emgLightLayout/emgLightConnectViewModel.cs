using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ThControlLibraryWPF.ControlUtils;



namespace ThMEPLighting.UI.emgLightLayout
{
   public class emgLightConnectViewModel: NotifyPropertyChangedBase
    {
        private int m_groupMin = 5;
        private int m_groupMax = 25;
        /// <summary>
        /// 普通层消火栓数量
        /// </summary>
        public int groupMin
        {
            get
            {
                return m_groupMin;
            }
            set
            {
                m_groupMin = value;
                RaisePropertyChanged("groupMin");
            }
        }

        public int groupMax
        {
            get
            {
                return m_groupMax;
            }
            set
            {
                m_groupMax = value;
                RaisePropertyChanged("groupMax");
            }
        }
        public emgLightConnectViewModel()
        {
          
        }


    }
}
