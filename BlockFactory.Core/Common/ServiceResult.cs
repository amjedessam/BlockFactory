using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlockFactory.Core.Common
{
    public class ServiceResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;

        public static ServiceResult Ok(string message = "تمت العملية بنجاح")
            => new() { Success = true, Message = message };

        public static ServiceResult Fail(string message)
            => new() { Success = false, Message = message };
    }

    public class ServiceResult<T> : ServiceResult
    {
        public T? Data { get; set; }

        public static ServiceResult<T> Ok(T data, string message = "تمت العملية بنجاح")
            => new() { Success = true, Data = data, Message = message };

        public static new ServiceResult<T> Fail(string message)
            => new() { Success = false, Message = message };
    }
}