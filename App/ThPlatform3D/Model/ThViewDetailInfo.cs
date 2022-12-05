using System;
using System.ComponentModel;
using System.Collections.ObjectModel;
using SqlSugar;
using System.Linq;

namespace ThPlatform3D.Model
{
    public class ThViewDetailInfo: INotifyPropertyChanged,ICloneable
    {
        public ThViewDetailInfo()
        {
            //初始化数据
            Direction = "";
            PCMajorData = true;
            StructureMajorData = true;
            Id = Guid.NewGuid().ToString();
            ViewSectionDirection = ViewDirection.Front;
            SectionFrames = new ObservableCollection<string>();
        }
        private string _id = "";
        [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
        public string Id
        {
            get=> _id;
            set => _id = value;
        }

        private string _projectFileId = "";
        public string ProjectFileId
        {
            get => _projectFileId;
            set => _projectFileId = value;
        }

        private string _fileName = "";
        /// <summary>
        /// 文件名称
        /// </summary>
        public string FileName
        {
            get => _fileName;
            set
            {
                _fileName = value;
                RaisePropertyChanged("FileName");
            }
        }

        private string _buildingNo = "";
        /// <summary>
        /// 单体子项:1#,2#...
        /// </summary>
        public string BuildingNo
        {
            get
            {
                return _buildingNo;
            }
            set
            {
                _buildingNo = value;
                RaisePropertyChanged("BuildingNo");
            }
        }

        private string major = "";
        /// <summary>
        /// 专业
        /// </summary>
        public string Major
        {
            get => major;
            set
            {
                major = value;
                RaisePropertyChanged("Major");
            }
        }

        private string _viewName = "";
        /// <summary>
        /// 视图名称
        /// </summary>
        public string ViewName
        {
            get
            {
                return _viewName;
            }
            set
            {
                _viewName = value;
                RaisePropertyChanged("ViewName");
            }
        }

        private bool _structureMajorData;
        /// <summary>
        /// 专业数据->结构
        /// </summary>
        public bool StructureMajorData
        {
            get
            {
                return _structureMajorData;
            }
            set
            {
                _structureMajorData = value;
                RaisePropertyChanged("StructureMajorData");
            }
        }
        private bool _pcMajorData;
        /// <summary>
        /// 专业数据->PC
        /// </summary>
        public bool PCMajorData
        {
            get
            {
                return _pcMajorData;
            }
            set
            {
                _pcMajorData = value;
                RaisePropertyChanged("PCMajorData");
            }
        }

        private string _viewType = "";
        /// <summary>
        /// 视图类型：平面图、立面图...
        /// </summary>
        public string ViewType
        {
            get
            {
                return _viewType;
            }
            set
            {
                _viewType = value;
                RaisePropertyChanged("ViewType");
            }
        }
        private string _viewScale = "";
        /// <summary>
        /// 视图比例：1:50,1:100...
        /// </summary>
        public string ViewScale
        {
            get
            {
                return _viewScale;
            }
            set
            {
                _viewScale = value;
                RaisePropertyChanged("ViewScale");
            }
        }
        private string _viewTemplate = "";
        /// <summary>
        /// 视图模板：天华默认...
        /// </summary>
        public string ViewTemplate
        {
            get
            {
                return _viewTemplate;
            }
            set
            {
                _viewTemplate = value;
                RaisePropertyChanged("ViewTemplate");
            }
        }

        private string _viewState = "";
        public string ViewState
        {
            get => _viewState;
            set
            {
                _viewState = value;               
                _viewStateImgPath = GetViewStateDisplayImagePath(_viewState);
            }
        }

        private string _viewStateImgPath = "";
        public string ViewStateImgPath
        {
            get => _viewStateImgPath;
            set
            {
                _viewStateImgPath = value;
                RaisePropertyChanged("ViewStateImgPath");
            }
        }

        private string _direction = "";
        /// <summary>
        /// 方向
        /// </summary>
        public string Direction
        {
            get
            {
                return _direction;
            }
            set
            {
                _direction = value;
                RaisePropertyChanged("Direction");
            }
        }

        public double _outDoorFloorElevation;
        /// <summary>
        /// 室外地坪标高
        /// </summary>
        public double OutDoorFloorElevation
        {
            get
            {
                return _outDoorFloorElevation;
            }
            set
            {
                _outDoorFloorElevation = value;
                RaisePropertyChanged("OutDoorFloorFloorElevation");
            }
        }
        #region ---------- 平面图 -----------
        private string _floor = "";
        /// <summary>
        /// 标高:1F,2F...
        /// </summary>
        public string Floor
        {
            get { return _floor; }
            set
            {
                _floor = value;
                RaisePropertyChanged("Floor");
            }
        }
        private double _bottomElevation;
        /// <summary>
        /// 底高度
        /// </summary>
        public double BottomElevation
        {
            get
            {
                return _bottomElevation;
            }
            set
            {
                _bottomElevation = value;
                RaisePropertyChanged("BottomElevation");
            }
        }

        private double _sectionElevation;
        /// <summary>
        /// 剖切高度
        /// </summary>
        public double SectionElevation
        {
            get
            {
                return _sectionElevation;
            }
            set
            {
                _sectionElevation = value;
                RaisePropertyChanged("SectionElevation");
            }
        }

        private double _topElevation;
        /// <summary>
        /// 顶高度
        /// </summary>
        public double TopElevation
        {
            get
            {
                return _topElevation;
            }
            set
            {
                _topElevation = value;
                RaisePropertyChanged("TopElevation");
            }
        }
        #endregion
        #region---------- 剖面图 -----------
        private ObservableCollection<string> _sectionFrames;
        /// <summary>
        /// 剖面框
        /// </summary>
        public ObservableCollection<string> SectionFrames
        {
            get => _sectionFrames;
            set
            {
                _sectionFrames = value;
                RaisePropertyChanged("SectionFrames");
            }
        }

        private string _sectionFrame = "";
        public string SectionFrame
        {
            get => _sectionFrame;
            set
            {
                _sectionFrame = value;
                RaisePropertyChanged("SectionFrame");
            }
        }

        private bool _useFloorSection = false;
        /// <summary>
        /// 采用楼层裁切
        /// </summary>
        public bool UseFloorSection
        {
            get => _useFloorSection;
            set
            {
                _useFloorSection = value;
                RaisePropertyChanged("UseFloorSection");
            }
        }

        private double _sectionDistance;
        public double SectionDistance
        {
            get => _sectionDistance;
            set
            {
                _sectionDistance = value;
                RaisePropertyChanged("SectionDistance");
            }
        }

        private ViewDirection _viewSectionDirection = ViewDirection.Front;
        public ViewDirection ViewSectionDirection
        {
            get
            {
                return _viewSectionDirection;
            }
            set
            {
                _viewSectionDirection = value;
                RaisePropertyChanged("ViewSectionDirection");
            }
        }
        #endregion
        #region---------- 立面图 -----------
        private bool _front = true;
        /// <summary>
        /// 前视图
        /// </summary>
        public bool Front
        {
            get => _front;
            set
            {
                _front = value;
                RaisePropertyChanged("Front");
            }
        }
        private bool _back = true;
        /// <summary>
        /// 后视图
        /// </summary>
        public bool Back
        {
            get => _back;
            set
            {
                _back = value;
                RaisePropertyChanged("Back");
            }
        }
        private bool _left = true;
        /// <summary>
        /// 左视图
        /// </summary>
        public bool Left
        {
            get => _left;
            set
            {
                _left = value;
                RaisePropertyChanged("Left");
            }
        }
        private bool _right = true;

        /// <summary>
        /// 右视图
        /// </summary>
        public bool Right
        {
            get => _right;
            set
            {
                _right = value;
                RaisePropertyChanged("Right");
            }
        }
        #endregion
        private string GetViewStateDisplayImagePath(string viewState)
        {
            string viewStateImgPath = "";
            switch (viewState)
            {
                case "未生成数据":
                    viewStateImgPath = "/Resources/Images/unGeneratedData.png";
                    break;
                case "待插入":
                    viewStateImgPath = "/Resources/Images/waitToInsert.png";
                    break;
                case "待更新":
                    viewStateImgPath = "/Resources/Images/waitToUpdate.png";
                    break;
                case "正常":
                    viewStateImgPath = "/Resources/Images/normal.png";
                    break;
                case "生成中":
                    viewStateImgPath = "";
                    break;
                default:
                    viewStateImgPath = "";
                    break;
            }
            return viewStateImgPath;
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

        public object Clone()
        {
            var clone = new ThViewDetailInfo();
            clone.ProjectFileId = this.ProjectFileId;
            clone.FileName = this.FileName;
            clone.ViewState = this.ViewState;
            clone.Floor = this.Floor;
            clone.BottomElevation = this.BottomElevation;
            clone.TopElevation = this.TopElevation;
            clone.SectionElevation = this.SectionElevation;
            clone.BuildingNo = this.BuildingNo;
            clone.Direction = this.Direction;
            clone.Major = this.Major;
            clone.OutDoorFloorElevation = this.OutDoorFloorElevation;
            clone.PCMajorData = this.PCMajorData;
            clone.SectionDistance = this.SectionDistance;
            clone.SectionElevation = this.SectionElevation;
            clone.SectionFrame = this.SectionFrame;
            clone.SectionFrames = new ObservableCollection<string>(this.SectionFrames.OfType<string>());
            clone.StructureMajorData = this.StructureMajorData;
            clone.UseFloorSection = this.UseFloorSection;
            clone.ViewName = this.ViewName;
            clone.ViewScale = this.ViewScale;
            clone.ViewSectionDirection = this.ViewSectionDirection;
            clone.ViewTemplate = this.ViewTemplate;
            clone.ViewType = this.ViewType;
            return clone;
        }
    }

    public enum ViewDirection
    {
        Front=0,
        Back=1,
        Left=2,
        Right=3,
    }    
}
