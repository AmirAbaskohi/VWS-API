using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;
using vws.web.Domain._base;
using vws.web.Domain._project;
using vws.web.Domain._team;

namespace vws.web.Domain._task
{
    [Table("Task_TaskStatus")]
    public class TaskStatus
    {
        public int Id { get; set; }

        [MaxLength(100)]
        public string Title { get; set; }

        public int EvenOrder { get; set; }

        public int? ProjectId { get; set; }

        public int? TeamId { get; set; }

        public bool IsDeleted { get; set; }

        public Guid? UserProfileId { get; set; }

        public virtual Project Project { get; set; }
        
        public virtual Team Team { get; set; }
        
        public virtual UserProfile UserProfile { get; set; }
    }
}
