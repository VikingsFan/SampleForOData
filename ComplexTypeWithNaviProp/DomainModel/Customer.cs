using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity.Infrastructure;
using Microsoft.Owin.BuilderProperties;
using Newtonsoft.Json;

namespace ODataReferentialConstraintSample
{
    public class Customer
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public DateTime? Time { get; set; }

        public Order Order { get; set; }

        public MyAddress Address { get; set; }
    }

    public class MyAddress
    {
        public string Name { get; set; }
    }
}