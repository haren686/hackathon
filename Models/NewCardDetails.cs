using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Hackathon.Web.Models
{
    public class NewCardDetails
    {
        public int statusCode { get; set; }
        public NewCardDetailsBody body { get; set; }
    }

    public class NewCardDetailsBody
    {
        public Carddetails CardDetails { get; set; }
        public Benefit[] Benefits { get; set; }
    }

    public class Carddetails
    {
        public int CardTypeId { get; set; }
        public string CardSegment { get; set; }
        public int CreditLimit { get; set; }
    }

    public class Benefit
    {
        public int BenefitId { get; set; }
        public string Description { get; set; }
    }

}

