﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ApplicationService.Videos.Export
{
    public class BattleExportData
    {
        public int Seconds { get; init; }
        public string Rule { get; init; }
        public string Stage { get; init; }
        public string Weapon { get; init; }
        public int RoomPower { get; init; }
    }
}
