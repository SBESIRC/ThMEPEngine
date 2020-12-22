using System;
using ThCADCore.NTS;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPLighting.Garage.Model;
using ThMEPLighting.Garage.Service;

namespace ThMEPLighting.Garage.Engine
{
    public class ThInnerOuterCirclesEngine : IDisposable
    {
        public List<ThWireOffsetData> WireOffsetDatas { get; private set; }       
        public ThInnerOuterCirclesEngine()
        {            
            WireOffsetDatas = new List<ThWireOffsetData>();
        } 
        public void Dispose()
        {           
        }
        public void Reconize(List<Line> dxLines,List<Line> fdxLines, double offsetDistance)
        {
            //在T型、十字型处分割线
            var totalLines = new List<Line>();
            dxLines.ForEach(o => totalLines.Add(new Line(o.StartPoint,o.EndPoint)));
            fdxLines.ForEach(o => totalLines.Add(new Line(o.StartPoint, o.EndPoint)));
            
            var splitDxLines = new List<Line>();
            var splitFdxLines = new List<Line>();
            using (var splitEngine=new ThSplitLineEngine(totalLines))
            {
                splitEngine.Split();
                foreach (var item in splitEngine.Results)
                {
                    if(dxLines.IsContains(item.Key))
                    {
                        if (item.Value.Count > 0)
                        {
                            splitDxLines.AddRange(item.Value);
                        }
                        else
                        {
                            splitDxLines.Add(new Line(item.Key.StartPoint, item.Key.EndPoint));
                        }
                    }
                    else if(fdxLines.IsContains(item.Key))
                    {
                        if (item.Value.Count > 0)
                        {
                            splitFdxLines.AddRange(item.Value);
                        }
                        else
                        {
                            splitFdxLines.Add(new Line(item.Key.StartPoint, item.Key.EndPoint));
                        }
                    }
                }             
            }

            //对于较短直线(小于偏移值 eg. offsetDistance=2700/2.0）
            //此直线在T型或十字处,且一端未连接任何物体
            var removeLines = new List<Line>();
            removeLines.AddRange(splitDxLines);
            removeLines.AddRange(splitFdxLines);
            var validLines = ThRemoveShortCenterLineService.Remove(removeLines, offsetDistance);

            //单位化
            var nomalLines = new List<Line>();
            validLines.ForEach(o => nomalLines.Add(o.Normalize()));

            //创建1号、2号线
            nomalLines.ForEach(o =>
            {
                bool isDx = splitDxLines.IsContains(o);                
                var offsetLines = ThOffsetLineService.Offset(o, offsetDistance);
                var offsetData = new ThWireOffsetData
                {
                    Center = o,
                    First = offsetLines.First,
                    Second = offsetLines.Second,
                    IsDX = isDx
                };
                WireOffsetDatas.Add(offsetData);
            });

            //连接弯头、T型、
            ThLinkElbowService.Link(WireOffsetDatas);
        }        
    }
}
