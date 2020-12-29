using System;
using ThCADCore.NTS;
using ThMEPLighting.Common;
using System.Collections.Generic;
using ThMEPLighting.Garage.Model;
using ThMEPLighting.Garage.Service;
using Autodesk.AutoCAD.DatabaseServices;

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
           
            //单位化
            var nomalLines = new List<Line>();
            splitDxLines.ForEach(o => nomalLines.Add(o.Normalize()));
            splitFdxLines.ForEach(o => nomalLines.Add(o.Normalize()));

            //创建1号、2号线
            nomalLines.ForEach(o =>
            {
                bool isDx = splitDxLines.IsContains(o);
                var offsetLines = ThOffsetLineService.Offset(o, isDx ? offsetDistance : 0.0);
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
