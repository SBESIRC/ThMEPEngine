using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using DotNetARX;
using Dreambuild.AutoCAD;
using Linq2Acad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADExtension;
using ThMEPWSS.Assistant;

namespace ThMEPWSS.Common
{
    class LevelFloorUtil
    {
        List<string> blockNames = new List<string>(){"标高"};
        List<string> layerNames = new List<string>() {  "W-NOTE", "W-WSUP-NOTE", };
        List<string> textStyleNames = new List<string> { ThWSSCommon.Layout_TextStyle };

        private Vector3d _xAxis = Vector3d.XAxis;
        private Vector3d _yAxis = Vector3d.YAxis;
        private Vector3d _normal;
        private double _floorSpace;
        private List<LevelFloor> _levelFloors;
        private double _floorLineLength;
        double _levelDistanceToStart = 550;
        double _textDistanceToStart = 1900;
        double _textDistanceToLine = 50;
        double _textHeight = 350;
        public LevelFloorUtil(double floorLineLength, double floorSpace)
            :this(floorLineLength, floorSpace,Vector3d.XAxis,Vector3d.YAxis)
        { }
        public LevelFloorUtil(double floorLineLength, double floorSpace, Vector3d xAxis, Vector3d yAxis) 
        {
            this._xAxis = xAxis.GetNormal();
            this._yAxis = yAxis.GetNormal();
            this._floorSpace = floorSpace;
            this._floorLineLength = floorLineLength;
            _normal = _xAxis.CrossProduct(_yAxis).GetNormal();
            var dotXY = _xAxis.DotProduct(_yAxis);
            if (Math.Abs(dotXY) > 0.00001)
                throw new Exception("XAxis,YAxis must Orthogonal");
            _levelFloors = new List<LevelFloor>();
        }
        public void InitFloorLevel(int startFloor,int endFloor,double startElevation,double levelHeight) 
        {
            ClearFloorLevel();
            AddFloorLevel(startFloor,endFloor,startElevation,levelHeight);
        }
        public void AddFloorLevel(LevelFloor levelFloor) 
        {
            _levelFloors.Add(levelFloor);
        }
        public void AddFloorLevel(int startFloor, int endFloor, double startElevation, double levelHeight) 
        {
            double elevation = startElevation;
            for (int i = startFloor; i <= endFloor; i++)
            {
                var thisElevaltion = elevation + (i - startFloor) * levelHeight;
                string name = string.Format("{0}F", i);
                _levelFloors.Add(new LevelFloor(i, thisElevaltion, name));
            }
        }
        public void RemoveFloorLevel(int floorId)
        {
            _levelFloors = _levelFloors.Where(c => c.LevelNum != floorId).ToList();
        }
        public void ClearFloorLevel() 
        {
            _levelFloors.Clear();
        }
        public void CreateFloorLines(Database database, Point3d origin) 
        {
            if (null == _levelFloors || _levelFloors.Count < 1)
                return;
            LoadBlockLayerToDocument(database);
            _levelFloors = _levelFloors.OrderBy(c => c.LevelNum).ToList();
            var floorLines = new List<Line>();
            for (int i = 0; i < _levelFloors.Count; i++) 
            {
                var floor = _levelFloors[i];
                if (null == floor)
                    continue;
                var lineStartPoint = origin + _yAxis.MultiplyBy(i * _floorSpace);
                var lineEndPoint = lineStartPoint + _xAxis.MultiplyBy(_floorLineLength);
                AddLevlBlock(database, lineStartPoint, floor.ShowElevation);
                AddLevelLine(database,lineStartPoint,lineEndPoint);
                AddLevelNumText(database,lineStartPoint, floor.LevelName);
            }
        }
        void AddLevelLine(Database database, Point3d startPoint,Point3d endPoint) 
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Use(database))
            {
                var levelLine = new Line(startPoint,endPoint);
                levelLine.Layer = "W-NOTE";
                var id = acadDatabase.ModelSpace.Add(levelLine);
            }
        }
        void AddLevelNumText(Database database, Point3d levelLineStartPoint, string levelName) 
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Use(database))
            {
                var textPosition = levelLineStartPoint + _xAxis.MultiplyBy(_textDistanceToStart);
                textPosition += _yAxis.MultiplyBy(_textDistanceToLine);
                DBText infotext = new DBText()
                {
                    TextString = levelName,
                    Height = _textHeight,
                    WidthFactor = 0.7,
                    HorizontalMode = TextHorizontalMode.TextLeft,
                    Oblique = 0,
                    Position = textPosition,
                    Rotation = GetRotation(),
                    Normal = _normal,
                };
                infotext.Layer = "W-WSUP-NOTE";
                var styleId = DrawUtils.GetTextStyleId(ThWSSCommon.Layout_TextStyle);
                if (null != styleId && styleId.IsValid)
                {
                    infotext.TextStyleId = styleId;
                }
                var id = acadDatabase.ModelSpace.Add(infotext);
                
            }
        }
        void AddLevlBlock(Database database,Point3d levelLineStartPoint,string elevation) 
        {
            var blockCreatePoint = levelLineStartPoint + _xAxis.MultiplyBy(_levelDistanceToStart);
            
            using (AcadDatabase acadDatabase = AcadDatabase.Use(database))
            {
                var attrs = new Dictionary<string, string>();
                if(!string.IsNullOrEmpty(elevation))
                    attrs.Add("标高", elevation.ToString());
                var id = acadDatabase.ModelSpace.ObjectId.InsertBlockReference(
                          "W-WSUP-NOTE",
                          "标高",
                          blockCreatePoint,
                          new Scale3d(0),
                          GetRotation(),
                          attrs);
            }
        }
        void LoadBlockLayerToDocument(Database database)
        {
            using (AcadDatabase currentDb = AcadDatabase.Use(database))
            using (AcadDatabase blockDb = AcadDatabase.Open(ThCADCommon.WSSDwgPath(), DwgOpenMode.ReadOnly, false))
            {
                foreach (var item in blockNames)
                {
                    if (string.IsNullOrEmpty(item))
                        continue;
                    var block = blockDb.Blocks.ElementOrDefault(item);
                    if (null == block)
                        continue;
                    currentDb.Blocks.Import(block, false);
                }
                foreach (var item in layerNames)
                {
                    if (string.IsNullOrEmpty(item))
                        continue;
                    var layer = blockDb.Layers.ElementOrDefault(item);
                    if (null == layer)
                        continue;
                    currentDb.Layers.Import(layer, false);
                    DbHelper.EnsureLayerOn(item);
                }
                foreach (var item in textStyleNames)
                {
                    if (string.IsNullOrEmpty(item))
                        continue;
                    var currentStyle = currentDb.TextStyles.ElementOrDefault(item);
                    if (null != currentStyle)
                        continue;
                    var style = blockDb.TextStyles.ElementOrDefault(item);
                    if (style == null)
                        continue;
                    currentDb.TextStyles.Import(style);
                }
            }
        }
        double GetRotation() 
        {
            return Vector3d.XAxis.GetAngleTo(_xAxis, _normal);
        }
    }
    class LevelFloor 
    {
        public int LevelNum { get;}
        public double Elevation { get; set; }
        public string ShowElevation { get; set; }
        public string LevelName { get; set; }
        public LevelFloor(int levelNum,double elevation,string levelName) 
        {
            this.LevelNum = levelNum;
            this.Elevation = elevation;
            this.LevelName = levelName;
        }
    }
}
