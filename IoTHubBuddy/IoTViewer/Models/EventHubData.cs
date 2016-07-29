﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IoTHubBuddy.Models
{
    public class EventHubData
    {
        public ICollection<string> PartitionIds { get; set; }
        public string EventHubEntity { get; set; }
        public string EventHubPort { get; set; }
        public string HubName { get; set; }

        public EventHubData(ICollection<string> ids, string entity, string port, string name)
        {
            PartitionIds = ids;
            EventHubEntity = entity;
            EventHubPort = port;
            HubName = name;
        }
    }
}
