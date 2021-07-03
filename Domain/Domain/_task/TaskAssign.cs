using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using vws.web.Domain._base;

namespace vws.web.Domain._task
{
    [Table("Task_TaskAssign")]
    public class TaskAssign
    {

        public long Id { get; set; }

        public Guid Guid { get; set; }

        public long GeneralTaskId { get; set; }

        public Guid UserProfileId { get; set; }

        public bool IsDeleted { get; set; }

        public Guid CreatedBy { get; set; }

        public Guid DeletedBy { get; set; }

        public DateTime CreatedOn { get; set; }

        public DateTime DeletedOn { get; set; }

        public virtual GeneralTask GeneralTask { get; set; }

        public virtual UserProfile UserProfile { get; set; }


    }
}
