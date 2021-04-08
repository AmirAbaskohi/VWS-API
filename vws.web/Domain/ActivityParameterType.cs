﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace vws.web.Domain
{
    [Table("ActivityParameterType")]
    public class ActivityParameterType
    {
        public byte Id { get; set; }

        public string Name { get; set; }
    }
}
