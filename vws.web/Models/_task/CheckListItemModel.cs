using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace vws.web.Models._task
{
    public class CheckListItemModel
    {
        [Required]
        public string Title { get; set; }
        [Required]
        public bool IsChecked { get; set; }
    }
}
