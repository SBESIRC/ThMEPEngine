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
            if (propertyItem.Value is ThPDSContactorModel)
            {
                return ContactorConfiguration.contactorInfos.Select(o => o.Model).Distinct();
            }
            if (propertyItem.Value is ThPDSIsolatingSwitchModel)
            {
                return IsolatorConfiguration.isolatorInfos.Select(o => o.Model).Distinct();
            }
            if (propertyItem.Value is ThPDSThermalRelayModel)
            {
                return ThermalRelayConfiguration.thermalRelayInfos.Select(o => o.Model).Distinct();
            }
            if (propertyItem.Value is ThPDSATSEModel)
            {
                return ATSEConfiguration.ATSEComponentInfos.Select(o => o.Model).Distinct();
            }
            if (propertyItem.Value is ThPDSMTSEModel)
            {
                return MTSEConfiguration.MTSEComponentInfos.Select(o => o.Model).Distinct();
            }
            if (propertyItem.Value is ThPDSCPSModel cps)
            {
                return cps.AlternativeModels;
            }
            if (propertyItem.Value is ThPDSBreakerBaseModel breaker)
            {
                return breaker.AlternativeModels;
            }
            throw new NotSupportedException();
        }
    }
}
