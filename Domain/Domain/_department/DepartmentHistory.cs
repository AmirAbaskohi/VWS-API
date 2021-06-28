using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace vws.web.Domain._department
{
    [Table("Department_DepartmentHistory")]
    public class DepartmentHistory
    {
        public DepartmentHistory()
        {
            DepartmentHistoriesarameters = new HashSet<DepartmentHistoryParameter>();
        }

        public long Id { get; set; }

        public int DepartmentId { get; set; }

        public string EventBody { get; set; }

        public DateTime EventTime { get; set; }

        public virtual Department Department { get; set; }

        public virtual ICollection<DepartmentHistoryParameter> DepartmentHistoriesarameters { get; set; }
    }
}
