﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnifiedAutomation.UaBase;

namespace IP21Streamer.Source.UaSource
{
    class UaNode
    {
        public UaNode()
        {
        }

        public NodeId NodeId { get; set; }
        public string BrowseName { get; set; }
        public string DisplayName { get; set; }
        public string Description { get; set; }
    }
}
