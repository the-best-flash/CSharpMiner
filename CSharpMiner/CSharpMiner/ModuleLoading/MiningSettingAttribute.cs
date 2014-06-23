using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSharpMiner.ModuleLoading
{
    public class MiningSettingAttribute : Attribute
    {
        public string Description { get; set; }
        public string ExampleValue { get; set; }
        public bool Optional { get; set; }
    }
}
