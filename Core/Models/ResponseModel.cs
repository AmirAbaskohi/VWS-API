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
        public void AddErrors(IEnumerable<string> errors) => Errors.AddRange(errors);
        public string Status { get => Errors.Count == 0 ? "Success" : "Error"; }
        public string Message { get; set; }
        public bool HasError { get => Errors.Count > 0;}
        public List<string> Errors { get; set; }
    }

    public class ResponseModel<T> : ResponseModel
    {
        public T Value { get; set; }
        public ResponseModel() { }
        public ResponseModel(T value)
        {
            Value = value;
        }
        public ResponseModel(T value, List<string> errors)
        {
            Value = value;
            AddErrors(errors);
        }
    }
}
