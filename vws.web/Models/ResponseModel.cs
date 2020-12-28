using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace vws.web.Models
{
    public class ResponseModel
    {
        public ResponseModel()
        {
            Errors = new List<string>();
        }
        public void AddError(string error) => Errors.Add(error);
        public string Status { get; set; }
        public string Message { get; set; }
        public bool HasError { get; set; }
        public List<string> Errors { get; set; }
    }
}
