using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web.Http;
using System.Web.OData;
using System.Web.OData.Routing;

namespace ODataReferentialConstraintSample
{
    public class CustomersController : ODataController
    {
        private readonly SampleContext _db = new SampleContext();
        private static List<Order> _orders;

        [HttpPost]
        [EnableQuery]
        [ODataRoute("GetOrders")]
        public IQueryable<Order> GetOrders()
        {
            return _orders.AsQueryable();
        }

        public IHttpActionResult Delete(int key)
        {
            Customer customer = _db.Customers.FirstOrDefault(c => c.Id == key);
            if (customer == null)
            {
                return NotFound();
            }

            _db.Customers.Remove(customer);
            _db.SaveChanges();

            return StatusCode(HttpStatusCode.NoContent);
        }

        [HttpPost]
        [ODataRoute("ResetDataSource")]
        public IHttpActionResult ResetDataSource()
        {
            Generate();
            return Ok();
        }

        private void Generate()
        {
            _orders = new List<Order>();
            for (int i = 0; i < 2; i++)
            {
                var order = new Order
                {
                    Id = i,
                    Name = "Order" + i,
                    Region = new Region
                    {
                        Id = i,
                        Name = "Region" + i
                    }
                };
                _orders.Add(order);
            }
        }
    }
}
