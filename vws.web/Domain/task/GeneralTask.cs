using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace vws.web.Domain.task
{
    [Table("GeneralTask", Schema = "task")]
    public class GeneralTask
    {
        public long Id { get; set; }

        [Required, MaxLength(1000, ErrorMessage = "Max allowed length is 1000 char")]
        public string Title { get; set; }

        public DateTime CreatedOn { get; set; }

        public DateTime ModifiedOn { get; set; }
    }
}
