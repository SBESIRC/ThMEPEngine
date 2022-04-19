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
    /// 型号编辑器
    /// </summary>
    public class ThPDSModelPropertyEditor : PropertyEditorBase
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
                return contactor.AlternativeModels;
            }
            if (propertyItem.Value is ThPDSIsolatingSwitchModel isolatingSwitch)
            {
                return isolatingSwitch.AlternativeModels;
            }
            if (propertyItem.Value is ThPDSThermalRelayModel thermalRelay)
            {
                return thermalRelay.AlternativeModels;
            }
            if (propertyItem.Value is ThPDSATSEModel atse)
            {
                return atse.AlternativeModels;
            }
            if (propertyItem.Value is ThPDSMTSEModel mtse)
            {
                return mtse.AlternativeModels;
            }
            if (propertyItem.Value is ThPDSCPSModel cps)
            {
                return cps.AlternativeModels;
            }
            if (propertyItem.Value is ThPDSBreakerModel breaker)
            {
                return breaker.AlternativeModels;
            }
            if (propertyItem.Value is ThPDSOUVPModel ouvp)
            {
                return ouvp.AlternativeModels;
            }
            throw new NotSupportedException();
        }
    }
}
