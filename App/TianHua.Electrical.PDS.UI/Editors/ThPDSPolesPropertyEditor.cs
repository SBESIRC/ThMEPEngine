﻿using System;
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
    /// 极数编辑器
    /// </summary>
    public class ThPDSPolesPropertyEditor : PropertyEditorBase
    {
        public override FrameworkElement CreateElement(PropertyItem propertyItem) => new System.Windows.Controls.ComboBox
        {
            IsEnabled = !propertyItem.IsReadOnly,
            ItemsSource = GetItemsSource(propertyItem),
        };

        public override DependencyProperty GetDependencyProperty() => Selector.SelectedValueProperty;

        private IEnumerable GetItemsSource(PropertyItem propertyItem)
        {
            if (propertyItem.Value is ThPDSThermalRelayModel)
            {
                return ThermalRelayConfiguration.thermalRelayInfos.Select(o => o.Poles).Distinct();
            }
            if (propertyItem.Value is ThPDSContactorModel)
            {
                return ContactorConfiguration.contactorInfos.Select(o => o.Poles).Distinct();
            }
            if (propertyItem.Value is ThPDSIsolatingSwitchModel switchModel)
            {
                return IsolatorConfiguration.isolatorInfos.Where(o => o.MaxKV == switchModel.MaxKV).Select(o => o.Poles).Distinct();
            }
            if (propertyItem.Value is ThATSEModel)
            {
                return ATSEConfiguration.ATSEComponentInfos.SelectMany(o => o.Poles.Split(';')).Distinct();
            }
            if (propertyItem.Value is ThMTSEModel)
            {
                return MTSEConfiguration.MTSEComponentInfos.SelectMany(o => o.Poles.Split(';')).Distinct();
            }
            throw new NotSupportedException();
        }
    }
}
