using System;
using System.Linq;
using System.Windows;
using System.Collections;
using System.Windows.Controls.Primitives;
using TianHua.Electrical.PDS.Project.Module.Configure;
using HandyControl.Controls;
using TianHua.Electrical.PDS.UI.Project.Module.Component;

namespace TianHua.Electrical.PDS.UI.Editors
{
    /// <summary>
    /// 额定电流编辑器
    /// </summary>
    public class ThPDSRatedCurrentPropertyEditor : PropertyEditorBase
    {
        public override FrameworkElement CreateElement(PropertyItem propertyItem) => new System.Windows.Controls.ComboBox
        {
            IsEnabled = !propertyItem.IsReadOnly,
            ItemsSource = GetItemsSource(propertyItem),
        };

        public override DependencyProperty GetDependencyProperty() => Selector.SelectedValueProperty;

        private IEnumerable GetItemsSource(PropertyItem propertyItem)
        {
            if (propertyItem.Value is ThPDSContactorModel model)
            {
                return ContactorConfiguration.contactorInfos.Select(o => o.Amps.ToString()).Distinct();
            }
            if (propertyItem.Value is ThATSEModel)
            {
                return ATSEConfiguration.ATSEComponentInfos.SelectMany(o => o.Amps.Split(';')).Distinct();
            }
            if (propertyItem.Value is ThMTSEModel)
            {
                return MTSEConfiguration.MTSEComponentInfos.SelectMany(o => o.Amps.Split(';')).Distinct();
            }
            throw new NotSupportedException();
        }
    }
}
