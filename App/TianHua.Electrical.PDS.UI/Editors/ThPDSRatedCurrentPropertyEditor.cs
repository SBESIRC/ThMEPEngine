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
            if (propertyItem.Value is ThPDSContactorModel contactor)
            {
                return contactor.AlternativeRatedCurrents;
            }
            if (propertyItem.Value is ThPDSATSEModel atse)
            {
                return atse.AlternativeRatedCurrents;
            }
            if (propertyItem.Value is ThPDSMTSEModel mtse)
            {
                return mtse.AlternativeRatedCurrents;
            }
            if (propertyItem.Value is ThPDSIsolatingSwitchModel isolator)
            {
                return isolator.AlternativeRatedCurrents;
            }
            if (propertyItem.Value is ThPDSCPSModel cps)
            {
                return cps.AlternativeRatedCurrents;
            }
            if (propertyItem.Value is ThPDSBreakerModel breaker)
            {
                return breaker.AlternativeRatedCurrents;
            }
            if (propertyItem.Value is ThPDSOUVPModel ouvp)
            {
                return ouvp.AlternativeRatedCurrents;
            }
            throw new NotSupportedException();
        }
    }
}
