using System.Linq;
using System.Collections.Generic;

namespace THMEPCore3D.Utils
{
    public class ThQueryFieldManager
    {
        public static List<string> FieldCollection()
        {
            var fields = new List<string>();
            fields.Add("ModelId");
            fields.Add("ModelSubEntryId");
            return fields;
        }
        public static bool Contains(string field)
        {
            var upperWord = field.ToUpper();
            return FieldCollection().Where(o => o.ToUpper() == upperWord).Any();
        }
    }
}
