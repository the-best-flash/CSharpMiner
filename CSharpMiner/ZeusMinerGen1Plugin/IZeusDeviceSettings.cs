﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZeusMinerGen1Plugin
{
    public interface IZeusDeviceSettings
    {
        int Cores { get; set; }
        int LtcClk { get; set; }
    }
}
