using System;
using System.IO;
using System.Linq;
using System.ComponentModel;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using ThMEPTCH.Services;
using ThPlatform3D.Model;
using ThPlatform3D.Service;

namespace Tianhua.Platform3D.UI.ViewModels
{
    public class ViewManagerVM : INotifyPropertyChanged
    {
        private string _docFullName = ""; //X:\\xxxx.dwg
        private string _docName = ""; //xxxx
        private string _jsonExtensionName = ".StoreyInfo.json";
        public ViewManagerVM(string docFullName) : this()
        {
            
            _docFullName = docFullName;
            _docName = GetFileName(docFullName);
            Init();
            Load();
        }

        private string _id = "";
        public string Id
        {
            get => _id;
        }


        private List<ThProjectFile> _prjectFiles;
        public List<ThProjectFile> PrjectFiles => _prjectFiles;

        private Dictionary<string, List<ThEditStoreyInfo>> _storeyInfoMap;
        public Dictionary<string, List<ThEditStoreyInfo>> StoreyInfoMap => _storeyInfoMap;


        private ObservableCollection<string> _buildingNos;
        public ObservableCollection<string> BuildingNos
        {
            get => _buildingNos;
            set
            {
                _buildingNos = value;
                RaisePropertyChanged("BuildingNos");
            }
        }
        private ObservableCollection<string> _viewScales;
        public ObservableCollection<string> ViewScales
        {
            get => _viewScales;
            set
            {
                _viewScales = value;
                RaisePropertyChanged("ViewScales");
            }
        }
        private ObservableCollection<string> _viewTypes;
        public ObservableCollection<string> ViewTypes
        {
            get => _viewTypes;
            set
            {
                _viewTypes = value;
                RaisePropertyChanged("ViewTypes");
            }
        }

        private List<ThViewDetailInfo> _totalViewDetailInfos;

        private ObservableCollection<ThViewDetailInfo> _viewDetailInfos;

        public ObservableCollection<ThViewDetailInfo> ViewDetailInfos
        {
            get => _viewDetailInfos;
            set
            {
                _viewDetailInfos = value;
                RaisePropertyChanged("ViewDetailInfos");
            }
        }

        private string _selectedBuildingNo = "";
        public string SelectedBuildingNo
        {
            get => _selectedBuildingNo;
            set
            {
                _selectedBuildingNo = value;
                RaisePropertyChanged("SelectedBuildingNo");
            }
        }
        private string _selectedViewType = "";
        public string SelectedViewType
        {
            get => _selectedViewType;
            set
            {
                _selectedViewType = value;
                RaisePropertyChanged("SelectedViewType");
            }
        }

        private string _selectedViewScale = "";
        public string SelectedViewScale
        {
            get => _selectedViewScale;
            set
            {
                _selectedViewType = value;
                RaisePropertyChanged("SelectedViewScale");
            }
        }

        private ViewManagerVM()
        {
            _id = Guid.NewGuid().ToString();
        }

        private void Init()
        {
            _viewTypes = new ObservableCollection<string>();
            _viewScales = new ObservableCollection<string>();
            _buildingNos = new ObservableCollection<string>();
            _viewDetailInfos = new ObservableCollection<ThViewDetailInfo>();

            _prjectFiles = new List<ThProjectFile>();
            _totalViewDetailInfos = new List<ThViewDetailInfo>();
            _storeyInfoMap = new Dictionary<string, List<ThEditStoreyInfo>>();
        }

        private void Load()
        {
            //ThBimProjectDataQueryService.CreateClass();

            // 从数据中读取信息(用到的)
            var projectFiles = ThBimProjectDataDBHelper.QueryProjectFiles(_docName);
            var planeViewInfos= ThBimProjectDataDBHelper.QueryPlaneViewInfosByFileName(_docName);
            var sectionViewInfos = ThBimProjectDataDBHelper.QuerySectionViewInfosByFileName(_docName);
            var elevationViewInfos = ThBimProjectDataDBHelper.QueryElevationViewInfosByFileName(_docName);
            projectFiles.ForEach(p => _prjectFiles.Add(p.ToThProjectFile()));
            _totalViewDetailInfos.AddRange(planeViewInfos.Select(p=> p.ToViewDetailInfo()));
            _totalViewDetailInfos.AddRange(sectionViewInfos.Select(s=> s.ToViewDetailInfo()));
            _totalViewDetailInfos.AddRange(elevationViewInfos.Select(e=> e.ToViewDetailInfo()));

            // 加载楼层信息，
            var storeyInfoFilePath = GetStoreyConfigFileName(_docFullName);
            _storeyInfoMap = ThIfcStoreyParseTool.DeSerialize(storeyInfoFilePath); //从楼层信息表中获取
            _buildingNos = new ObservableCollection<string>() { "全部"};
            foreach(var item in _storeyInfoMap)
            {
                _buildingNos.Add(item.Key);
            }
            if(string.IsNullOrEmpty(this._selectedBuildingNo) ||!_buildingNos.Contains(this._selectedBuildingNo))
            {
                this._selectedBuildingNo = "全部";
            }

            // 
            _viewTypes = new ObservableCollection<string> { "全部", "平面图", "剖面图", "立面图" };
            if (string.IsNullOrEmpty(this._selectedViewType) ||!_buildingNos.Contains(this._selectedViewType))
            {
                this._selectedViewType = "全部";
            }

            //
            _viewScales = new ObservableCollection<string> { "全部", "1:20", "1:50", "1:100", "1:150", "1:200", "1:500" };
            if (string.IsNullOrEmpty(this._selectedViewScale) || !_buildingNos.Contains(this._selectedViewScale))
            {
                this._selectedViewScale = "全部";
            }

            //
            UpdateViewDetailInfos();
        }

        public void UpdateViewDetailInfos()
        {
            ViewDetailInfos = new ObservableCollection<ThViewDetailInfo>(QueryFromTotalViewDetailInfos());
        }

        public void Insert(ThViewDetailInfo viewDetailInfo)
        {
            try
            {
                if(_prjectFiles.Count!=1 || string.IsNullOrEmpty(_docName))
                {
                    return;
                }
                var projectFile = _prjectFiles[0];
                viewDetailInfo.ProjectFileId = projectFile.ProjectFileId;
                //Test code
                viewDetailInfo.ProjectFileId = System.Guid.NewGuid().ToString();
                viewDetailInfo.FileName = _docName;

                if (viewDetailInfo.ViewType == "平面图")
                {
                    var planeViewInfo = viewDetailInfo.ToPlaneViewInfoInMySql();
                    planeViewInfo.InsertToMySqlDb();
                    _totalViewDetailInfos.Add(viewDetailInfo);
                }
                else if (viewDetailInfo.ViewType == "剖面图")
                {
                    var planeViewInfo = viewDetailInfo.ToSectionViewInfoInMySql();
                    planeViewInfo.InsertToMySqlDb();
                    _totalViewDetailInfos.Add(viewDetailInfo);
                }
                else if (viewDetailInfo.ViewType == "立面图")
                {
                    if (viewDetailInfo.Front)
                    {
                        InsertElevationViewInfo(viewDetailInfo);                        
                    }
                    if (viewDetailInfo.Back)
                    {
                        InsertElevationViewInfo(viewDetailInfo);
                    }
                    if(viewDetailInfo.Left)
                    {
                        InsertElevationViewInfo(viewDetailInfo);
                    }
                    if (viewDetailInfo.Right)
                    {
                        InsertElevationViewInfo(viewDetailInfo);
                    }
                }
                UpdateViewDetailInfos();
            }
            catch (Exception ex)
            {
                //
            }
        }
        public void Delete(List<ThViewDetailInfo> viewDetailInfos)
        {
            viewDetailInfos.ForEach(v =>
            {
                if (v.ViewType == "平面图")
                {
                    var existedPlaneViews = v.Id.QueryPlaneViewInfosById();
                    if (existedPlaneViews.Count == 1)
                    {
                        var planeViewInfo = v.ToPlaneViewInfoInMySql();
                        planeViewInfo.DeleteToMySqlDb();
                    }
                }
                else if (v.ViewType == "剖面图")
                {
                    var existedSectionViews = v.Id.QuerySectionViewInfosById();
                    if (existedSectionViews.Count == 1)
                    {
                        var sectionViewInfo = v.ToSectionViewInfoInMySql();
                        sectionViewInfo.DeleteToMySqlDb();
                    }
                }
                else if (v.ViewType == "立面图")
                {
                    var existedElevationViews = v.Id.QueryElevationViewInfosById();
                    if (existedElevationViews.Count == 1)
                    {
                        var elevationViewInfo = v.ToElevationViewInfoInMySql();
                        elevationViewInfo.DeleteToMySqlDb();
                    }
                }
            });
            if(viewDetailInfos.Count>0)
            {
                var ids = viewDetailInfos.Select(v => v.Id).ToList();
                _totalViewDetailInfos = _totalViewDetailInfos.Where(o=> !ids.Contains(o.Id)).ToList();
                UpdateViewDetailInfos();
            }            
        }
        public void Update(ThViewDetailInfo viewDetailInfo)
        {
            try
            {
                if (viewDetailInfo.ViewType == "平面图")
                {
                    var planeViewInfo = viewDetailInfo.ToPlaneViewInfoInMySql();
                    var existedPlaneViews = planeViewInfo.Id.QueryPlaneViewInfosById();
                    if (existedPlaneViews.Count == 1)
                    {
                        planeViewInfo.UpdateToMySqlDb();
                    }
                    else if (existedPlaneViews.Count == 0)
                    {
                        planeViewInfo.InsertToMySqlDb();
                    }
                }
                else if (viewDetailInfo.ViewType == "剖面图")
                {
                    var sectionViewInfo = viewDetailInfo.ToSectionViewInfoInMySql();
                    var existedSectionViews = sectionViewInfo.Id.QuerySectionViewInfosById();
                    if(existedSectionViews.Count==1)
                    {
                        sectionViewInfo.UpdateToMySqlDb();
                    }
                    else if(existedSectionViews.Count == 0)
                    {
                        sectionViewInfo.InsertToMySqlDb();
                    }
                }
                else if (viewDetailInfo.ViewType == "立面图")
                {
                    var elevationViewInfo = viewDetailInfo.ToElevationViewInfoInMySql();
                    var existedElevationViews = elevationViewInfo.Id.QueryElevationViewInfosById();
                    if (existedElevationViews.Count == 1)
                    {
                        elevationViewInfo.UpdateToMySqlDb();
                    }
                    else if (existedElevationViews.Count == 0)
                    {
                        elevationViewInfo.InsertToMySqlDb();
                    }
                }
            }
            catch (Exception ex)
            {
                //
            }
        }

        private void InsertElevationViewInfo(ThViewDetailInfo viewDetailInfo)
        {
            var elevationViewDetail = viewDetailInfo.Clone() as ThViewDetailInfo;
            var elevationViewDetailDb = elevationViewDetail.ToElevationViewInfoInMySql();
            elevationViewDetailDb.InsertToMySqlDb();
            _totalViewDetailInfos.Add(elevationViewDetail);
        }

        private List<ThViewDetailInfo> QueryFromTotalViewDetailInfos()
        {
            return _totalViewDetailInfos
                .Where(o => (_selectedBuildingNo == "全部" || o.BuildingNo == _selectedBuildingNo) &&
                (_selectedViewType == "全部" || o.ViewType == _selectedViewType) &&
                (_selectedViewScale == "全部" || o.ViewScale == _selectedViewScale))
                .ToList();
        }

        private string GetStoreyConfigFileName(string docFullName)
        {
            if (File.Exists(docFullName))
            {
                var fileInfo = new FileInfo(docFullName);
                var dir = fileInfo.Directory.FullName;
                var fileName = Path.GetFileNameWithoutExtension(docFullName);
                return Path.Combine(dir, fileName + _jsonExtensionName);
            }
            else
            {
                return "";
            }
        }

        private string GetFileName(string fileFullPath)
        {
            if (File.Exists(fileFullPath))
            {
                return Path.GetFileNameWithoutExtension(fileFullPath);
            }
            else
            {
                return "";
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public virtual void RaisePropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler propertyChanged = this.PropertyChanged;
            if (propertyChanged != null)
            {
                propertyChanged.Invoke((object)this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}
