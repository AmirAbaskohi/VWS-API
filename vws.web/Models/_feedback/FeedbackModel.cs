using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace vws.web.Models._feedback
{
    public class FeedbackModel
    {
        [Required]
        public string Title { get; set; }
        public string Description { get; set; }
    }
}
