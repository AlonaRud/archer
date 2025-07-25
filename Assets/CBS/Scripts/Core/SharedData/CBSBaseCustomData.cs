using System.Collections.Generic;
using System.Linq;

namespace CBS
{
    public class CBSBaseCustomData
    {
        public virtual float Space { get; set; } = 0;

        public Dictionary<string, object> ToDictionary()
        {
            var type = GetType();
            var list = type.GetFields().Where(f => f.IsPublic);
            return list.ToDictionary(x => x.Name, x => x.GetValue(this));
        }
    }
}
