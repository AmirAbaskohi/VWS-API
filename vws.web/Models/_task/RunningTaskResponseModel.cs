using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace vws.web.Models._task
{
    public class RunningTaskResponseModel
    {
        public long TaskId { get; set; }
        public bool IsPaused { get; set; }
        public DateTime? StartDate { get; set; }
        public double? TotalTimeInMinutes { get; set; }
    }

    class RunningTaskResponseModelComparer : IEqualityComparer<RunningTaskResponseModel>
    {
        public bool Equals(RunningTaskResponseModel first, RunningTaskResponseModel second)
        {

            if (Object.ReferenceEquals(first, second)) return true;

            if (Object.ReferenceEquals(first, null) || Object.ReferenceEquals(second, null))
                return false;

            return first.TaskId == second.TaskId;
        }

        public int GetHashCode(RunningTaskResponseModel model)
        {
            if (Object.ReferenceEquals(model, null)) return 0;

            return model.TaskId.GetHashCode();
        }
    }
}
