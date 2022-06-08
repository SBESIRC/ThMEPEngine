using System;
using System.Collections.Generic;
using System.Linq;
using ThMEPTCH.Model;
using ThMEPTCH.TCHXmlModels.TCHEntityModels;

namespace ThMEPTCH.TCHXmlDataConvert
{
    class TCHXmlDataConvertService
    {
        protected List<TCHXmlEntity> tchXmlEntities;
        protected string assemblyDllPath;
        protected List<ConvertCache> convertCaches;
        public TCHXmlDataConvertService() 
        {
            tchXmlEntities = new List<TCHXmlEntity>();
            assemblyDllPath = this.GetType().Assembly.Location.ToString();
            InitConvertTypes();
        }
        private void InitConvertTypes() 
        {
            convertCaches = new List<ConvertCache>();
            try
            {
                var types = TCHConvertAttribute.GetAvailabilityTypes(assemblyDllPath);
                if (null == types || types.Count < 1)
                    return;
                foreach (var type in types)
                {
                    if (type.CustomAttributes == null || type.CustomAttributes.Count() < 1)
                        continue;
                    var allAttr = type.GetCustomAttributes(typeof(TCHConvertAttribute), false);
                    if (allAttr == null || allAttr.Count() < 1)
                        continue;
                    var convertIns = Activator.CreateInstance(type) as ITCHXmlConvert;
                    var accTypes = convertIns.AcceptTCHEntityTypes;
                    if (accTypes == null || accTypes.Count < 1)
                        continue;
                    convertCaches.Add(new ConvertCache(type,accTypes.Select(c=>c.ToString()).ToList()));
                }
            }
            catch (Exception ex)
            {

            }
        }
        private void CacheXmlData()
        {
            var groupBy = tchXmlEntities.GroupBy(c => c.GetType().ToString());
            foreach (var value in groupBy)
            {
                var strType = value.Key;
                foreach (var convert in convertCaches)
                {
                    if (convert == null || convert.TCHXMLTypes == null || convert.TCHXMLTypes.Count < 1)
                        continue;
                    var targetXmlEntitys = new List<TCHXmlEntity>();

                    foreach (var type in convert.TCHXMLTypes)
                    {
                        if (type == strType)
                        {
                            convert.AcceptTCHEntityTypes.AddRange(value.ToList());
                            break;
                        }
                    }
                }
            }
        }
        public List<object> StartConvert(List<TCHXmlEntity> targetEntitys) 
        {
            tchXmlEntities.Clear();
            tchXmlEntities.AddRange(targetEntitys);
            CacheXmlData();
            var resList = new List<object>();
            var tempObjs = new List<object>();
            if (null == convertCaches || convertCaches.Count<1)
                return resList;
            if (null == tchXmlEntities || tchXmlEntities.Count < 1)
                return resList;
            foreach (var convert in convertCaches)
            {
                if (convert == null || convert.AcceptTCHEntityTypes == null || convert.AcceptTCHEntityTypes.Count < 1)
                    continue;
                var convertIns = Activator.CreateInstance(convert.ConvertType) as ITCHXmlConvert;
                convertIns.InitData(convert.AcceptTCHEntityTypes);
                var tempList = convertIns.ConvertToBuidingElement();
                if (null == tempList || tempList.Count < 1)
                    continue;
                tempObjs.AddRange(tempList);
            }

            resList = CalcRelationship(tempObjs);
            ClearData();
            return resList;
        }
        protected void ClearData() 
        {
            if (null == convertCaches)
                return;
            foreach (var item in convertCaches) 
            {
                item.AcceptTCHEntityTypes.Clear();
            }
        }
        protected List<object> CalcRelationship(List<object> targetObjs) 
        {
            var resList = new List<object>();
            if (null == targetObjs || targetObjs.Count < 1)
                return resList;

            List<ThTCHWall> walls = new List<ThTCHWall>();
            List<ThTCHWindow> windows = new List<ThTCHWindow>();
            List<ThTCHDoor> doors = new List<ThTCHDoor>();
            List<ThTCHOpening> openings = new List<ThTCHOpening>();
            foreach (var item in targetObjs)
            {
                if (item is ThTCHWall wall)
                    walls.Add(wall);
                else if (item is ThTCHDoor door)
                    doors.Add(door);
                else if (item is ThTCHWindow window)
                    windows.Add(window);
                else if (item is ThTCHOpening opening)
                    openings.Add(opening);
                else
                    resList.Add(item);
            }
            var hisDoorIds = new List<string>();
            var hisWindowIds = new List<string>();
            var hisOpeingIds = new List<string>();
            foreach (var wall in walls)
            {
                var checkId = string.Format(",{0},", wall.Uuid);
                foreach (var door in doors)
                {
                    if (string.IsNullOrEmpty(door.Useage))
                        continue;
                    if (door.Useage.Contains(checkId))
                    {
                        wall.Doors.Add(door);
                        hisDoorIds.Add(door.Uuid);
                    }
                }
                foreach (var window in windows)
                {
                    if (string.IsNullOrEmpty(window.Useage))
                        continue;
                    if (window.Useage.Contains(checkId))
                    {
                        wall.Windows.Add(window);
                        hisWindowIds.Add(window.Uuid);
                    }
                }
                foreach (var opening in openings)
                {
                    if (string.IsNullOrEmpty(opening.Useage))
                        continue;
                    if (opening.Useage.Contains(checkId))
                    {
                        wall.Openings.Add(opening);
                        hisOpeingIds.Add(opening.Uuid);
                    }
                }
            }
            resList.AddRange(walls);
            resList.AddRange(windows.Where(c => !hisWindowIds.Any(x => x == c.Uuid)).ToList());
            resList.AddRange(doors.Where(c => !hisDoorIds.Any(x => x == c.Uuid)).ToList());
            resList.AddRange(openings.Where(c => !hisOpeingIds.Any(x => x == c.Uuid)).ToList());
            return resList;
        }
    }
    class ConvertCache 
    {
        public List<string> TCHXMLTypes { get; }
        public Type ConvertType { get; }
        public string Name { get; }
        public List<TCHXmlEntity> AcceptTCHEntityTypes { get; }
        public ConvertCache(Type type,List<string> accTypes)
        {
            ConvertType = type;
            var allAttr = type.GetCustomAttributes(typeof(TCHConvertAttribute), false);
            var attr = allAttr.First(j => j is TCHConvertAttribute) as TCHConvertAttribute;
            Name = attr.AttributName;
            TCHXMLTypes = accTypes;
            AcceptTCHEntityTypes = new List<TCHXmlEntity>();
        }
    }

}
