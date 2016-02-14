using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Runtime.Remoting.Contexts;
using System.Web.Http;
using System.Web.OData;
using AutoExpand.Models;
using Microsoft.OData.Edm.Library;

namespace AutoExpand.Controllers
{
    public class ContributionsController : ODataController
    {
        private readonly AnyContext _context = new AnyContext();

        [HttpGet]
        [EnableQuery]
        public IQueryable<Contribution> Get()
        {
            return _context.Contributions;
        }
    }

    public class ContactsController : ODataController
    {
        private readonly AnyContext _context = new AnyContext();

        [HttpGet]
        [EnableQuery]
        public IQueryable<Contact> Get()
        {
            return _context.Contacts;
        }
    }
}
