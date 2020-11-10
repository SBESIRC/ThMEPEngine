using System;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace TianHua.FanSelection.UI.CAD
{
    public class CustomCommandMappers : Dictionary<Type, ICustomCommandMapper>
    {
        public string GetCustomCommand(ObjectId entId)
        {
            string cmd = string.Empty;
            foreach (KeyValuePair<Type, ICustomCommandMapper> item in this)
            {
                ICustomCommandMapper custCommand = item.Value;
                string c = custCommand.GetMappedCustomCommand(entId);
                if (!string.IsNullOrEmpty(c))
                {
                    cmd = c;
                    break;
                }
            }
            return cmd;
        }
    }

    public class CustomCommandsFactory
    {
        public static CustomCommandMappers CreateDefaultCustomCommandMappers()
        {
            return new CustomCommandMappers()
            {
                { typeof(BlockReference), new ThModelBlockCustomCommandMapper()}
            };
        }
    }
}
