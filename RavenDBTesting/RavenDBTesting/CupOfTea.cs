using System;
using System.Collections.Generic;

namespace RavenDBTesting
{
    public class CupOfTea
    {
        public string Id { get; set; }
        public string TeaProfileId { get; set; }
        public decimal Temperature { get; set; }
        public DateTime PouredOn { get; set; }
    }
}
