using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Web.OData.Builder;

namespace AutoExpand.Models
{
    public class Contribution
    {
        public int ContributionID { get; set; }
        public decimal Amount { get; set; }
        public Contact Contact { get; set; }
    }

    public class Contact
    {
        public int ContactID { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public ICollection<Contribution> Contributions { get; set; }
    }
}
