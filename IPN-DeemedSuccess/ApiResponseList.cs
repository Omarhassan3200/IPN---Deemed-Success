using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IPN_DeemedSuccess
{
    class ApiResponseList
    {
        public string Date { get; set; }
        public string FtReference { get; set; }
        public string ReversedftReference { get; set; }
        public string ReversedDate { get; set; }
        public string requestId { get; set; }
        public string transactionId { get; set; }
        public string requestType { get; set; }
        public string Account { get; set; }
        public string Customer { get; set; }
        public double Amount { get; set; }
        public double ReversedAmount { get; set; }
        public string Currency { get; set; }
        public string Status { get; set; }
        public string timestamp { get; set; }
        public string CreatedAt { get; set; }
        public string UpdatedAt { get; set; }

    }
}
