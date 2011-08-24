using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel.DataAnnotations;

namespace GoogleMaps.Geolocation
{
    public class Region
    {
        public string Name { get; set; }
        [StringLength(2,MinimumLength=2)]
        public string ShortCode { get; set; }
    }
}
