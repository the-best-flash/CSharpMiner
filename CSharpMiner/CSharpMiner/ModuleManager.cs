using CSharpMiner.MiningDevices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSharpMiner
{
    public static class ModuleManager
    {
        private static Dictionary<string, Type> _miningManagers = null;
        private static Dictionary<string, Type> _miningDevices = null;

        public static Dictionary<string, Type> MiningManagers
        {
            get
            {
                if(_miningManagers == null)
                {
                    LoadModules();
                }

                return _miningManagers;
            }
        }

        private static void LoadModules()
        {
            _miningManagers = new Dictionary<string, Type>();
            _miningManagers.Add("testdevicemanager", typeof(TestDeviceManager));

            _miningDevices = new Dictionary<string, Type>();

            // TODO: Load more managers and devices from the modules in the Modules folder
        }
    }
}
