using Autodesk.AutoCAD.DatabaseServices;
using HandyControl.Controls;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using ThControlLibraryWPF.ControlUtils;
using ThMEPTCH.PropertyServices;
using ThMEPTCH.PropertyServices.PropertyVMoldels;

namespace Tianhua.Platform3D.UI.ViewModels
{
    class PropertiesViewModel : NotifyPropertyChangedBase
    {
        private PropertyService propertyService;
        public static readonly PropertiesViewModel Instacne = new PropertiesViewModel();
        private List<EntityProperties> EntityProperties;
        private PropertyGrid propertyGrid;
        private PropertyVMBase propertyVM { get; set; }
        public PropertyVMBase PropertyVM 
        {
            get { return propertyVM; }
            set 
            {
                if (null != propertyVM)
                    propertyVM.PropertyChanged -= PropertyVM_PropertyChanged;
                propertyVM = value;
                this.RaisePropertyChanged();
                if(null != propertyVM)
                    propertyVM.PropertyChanged += PropertyVM_PropertyChanged;
                PropertyGridUpdata();
            }
        }

        private void PropertyVM_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (selectIds.Count < 1)
                return;
            var newModel = sender as PropertyVMBase;
            if (null == newModel)
                return;
            string fieldName = e.PropertyName;
            var type = newModel.GetType();
            object value = type.GetProperty(fieldName).GetValue(newModel, null);
            foreach (var id in selectIds) 
            {
                var entityOldProperty = EntityProperties.Find(c => c.EntityId == id).Properties;
                var oldType = entityOldProperty.GetType();
                oldType.GetProperty(fieldName).SetValue(entityOldProperty, value);
                propertyService.LastSvrCache.SetProperty(id, entityOldProperty.Property,false);
            }
        }

        PropertiesViewModel() 
        {
            selectIds = new List<ObjectId>();
            propertyService = new PropertyService();
            EntityProperties = new List<EntityProperties>();
            ClearHisData();
        }
        public void InitPropertyGrid(PropertyGrid property)
        {
            propertyGrid = property;
        }
        public void SelectIds(List<ObjectId> selectIds) 
        {
            SelectChanged(selectIds);
        }
        private List<ObjectId> selectIds { get; }
        private bool isMultipleType { get; set; }
        public bool IsMultipleType 
        {
            get { return isMultipleType; }
            set 
            {
                isMultipleType = value;
                this.RaisePropertyChanged();
            }
        }
        public string ShowTypeName
        {
            get { return string.Format("{0}({1})",TypeName,Count); }
            set 
            {
                this.RaisePropertyChanged();
            }
        }

        private string typeName { get; set; }
        public string TypeName
        {
            get { return typeName; }
            set
            {
                typeName = value;
                this.RaisePropertyChanged();
                RaisePropertyChanged("ShowTypeName");
            }
        }
        private int count { get; set; }
        public int Count 
        {
            get { return count; }
            set 
            {
                count = value;
                this.RaisePropertyChanged();
                RaisePropertyChanged("ShowTypeName");
            }
        }
        private void SelectChanged(List<ObjectId> objIds) 
        {
            ClearHisData();
            if (null != objIds) 
            {
                foreach (var item in objIds)
                {
                    var isVaild = propertyService.GetShowProperties(item, out PropertyVMBase properties);
                    if (!isVaild)
                        continue;
                    selectIds.Add(item);
                    var tempProp = new EntityProperties(item,properties.TypeName, properties);
                    EntityProperties.Add(tempProp);
                }
            }
            PropertyVM = GetShowViewModel();
        }
        private void ClearHisData() 
        {
            PropertyVM = null;
            selectIds.Clear();
            EntityProperties.Clear();
            IsMultipleType = false;
            TypeName = "未选择";
            Count = 0;
        }
        private PropertyVMBase GetShowViewModel() 
        {
            PropertyVMBase propertyVM = null;
            if (EntityProperties.Count < 1)
            {
                propertyVM = propertyService.GetNoSelectVMProperty();
            }
            else 
            {
                var types = EntityProperties.Select(c => c.TypeName).Distinct().ToList();
                Count = EntityProperties.Count;
                IsMultipleType = types.Count > 1;
                if (IsMultipleType)
                {
                    propertyVM = propertyService.GetMultiSelectVMProperty();
                }
                else 
                {
                    propertyVM = propertyService.LastSvrCache.MergePropertyVM(EntityProperties.Select(c => c.Properties).ToList());
                    propertyVM.A01_ShowTypeName = string.Format("{0}({1})", types.First(), Count);
                }
            }
            return propertyVM;
        }
        private void PropertyGridUpdata() 
        {
            //测试代码，设置排序问题,目前没有调试通。
            if (null == propertyGrid)
                return;
            foreach (CommandBinding item in propertyGrid.CommandBindings) 
            {
                var commandName = ((System.Windows.Input.RoutedCommand)(item.Command)).Name;
                if (string.IsNullOrEmpty(commandName))
                    continue;
                if (item.Command.CanExecute(null))
                {

                }
                else if (item.Command.CanExecute(propertyGrid))
                {

                }
                else if (item.Command.CanExecute(propertyGrid.DataContext))
                {

                }
                else if (item.Command.CanExecute(propertyGrid.SelectedObject)) 
                {
                
                }
                item.Command.Execute(null);
            }
        }
    }
    class EntityProperties 
    {
        public ObjectId EntityId { get; }
        public string TypeName { get; set; }
        public PropertyVMBase Properties { get; }
        public EntityProperties(ObjectId objectId,string type, PropertyVMBase propertyVM) 
        {
            TypeName = type;
            Properties = propertyVM;
            EntityId = objectId;
        }
    }
}
