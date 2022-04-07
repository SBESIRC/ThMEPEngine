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
            if (propertyItem.Value is ThPDSATSEModel atse)
            {
                //  1. 型号决定了可选的额定电流选项
                return ATSEConfiguration.ATSEComponentInfos.Where(o=>o.Model==atse.Model).SelectMany(o => o.Amps.Split(';')).Distinct();
            }
            if (propertyItem.Value is ThPDSMTSEModel mtse)
            {
                //  1. 型号决定了可选的额定电流选项
                return MTSEConfiguration.MTSEComponentInfos.Where(o => o.Model == mtse.Model).SelectMany(o => o.Amps.Split(';')).Distinct();
            }
            if (propertyItem.Value is ThPDSIsolatingSwitchModel isolator)
            {
                // 1. 型号决定了可选的额定电流选项
                return IsolatorConfiguration.isolatorInfos.Where(o => o.Model == isolator.Model).Select(o => o.Amps.ToString()).Distinct();
            }
            if (propertyItem.Value is ThPDSCPSModel cps)
            {
                return cps.AlternativeRatedCurrents;
            }
            if (propertyItem.Value is ThPDSBreakerBaseModel breaker)
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
