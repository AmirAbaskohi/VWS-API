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
        public string Status { get => Errors.Count == 0 ? "Success" : "Error"; }
        public string Message { get; set; }
        public bool HasError { get => Errors.Count > 0;}
        public List<string> Errors { get; set; }
    }
}
