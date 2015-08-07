using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Web.OData.Builder;
using Microsoft.OData.Edm;

namespace ODataReferentialConstraintSample
{
    public class Order
    {
        public int Id { get; set; }

        public string Name { get; set; }

        [AutoExpand]
        public Region Region { get; set; }
    }

    public class Region
    {
        public int Id { get; set; }

        public string Name { get; set; }
    }
}
