using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IPN_DeemedSuccess
{
    class API_Response
    {
        public bool Status { get; set; }
        public string Message { get; set; }
        public List<ApiResponseList> Data { get; set; }

    }
}
