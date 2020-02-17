using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Hackathon.Web.Models
{
    public class CustomerDetails
    {
        public int statusCode { get; set; }
        public CustomerDetailsBody body { get; set; }
    }

    public class CustomerDetailsBody
    {
        public Customerdetails CustomerDetails { get; set; }
        public Cardinfo CardInfo { get; set; }
        public Cardbenefit[] CardBenefits { get; set; }
    }

    public class Customerdetails
    {
        public string CustomerId { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public string ImageUrl { get; set; }
    }

    public class Cardinfo
    {
        public string CustomerId { get; set; }
        public string CardNumber { get; set; }
        public string CardSegment { get; set; }
        public int CardTypeId { get; set; }
        public int CreditLimit { get; set; }
        public string NameOnCard { get; set; }
        public DateTime ExpiryDate { get; set; }
    }

    public class Cardbenefit
    {
        public int BenefitId { get; set; }
        public string Description { get; set; }
    }

}
