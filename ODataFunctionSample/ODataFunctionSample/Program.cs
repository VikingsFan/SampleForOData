using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Web.Http;
using System.Web.OData.Builder;
using System.Web.OData.Extensions;
using Microsoft.OData.Edm;
using Microsoft.Owin.Hosting;
using Owin;

namespace ODataReferentialConstraintSample
{
    public class Program
    {
        private static readonly string _baseAddress = string.Format("http://{0}:12345", Environment.MachineName);
        private static readonly HttpClient _httpClient = new HttpClient();

        public static void Main(string[] args)
        {
            _httpClient.DefaultRequestHeaders.Accept.Add(MediaTypeWithQualityHeaderValue.Parse("application/json"));
            using (WebApp.Start(_baseAddress, Configuration))
            {
                Console.WriteLine("Listening on " + _baseAddress);

                string requestUri = _baseAddress + "/odata/Customers/ODataReferentialConstraintSample.MyFunction(Id=123,SomeString='')";
                HttpResponseMessage response = _httpClient.GetAsync(requestUri).Result;

                response.EnsureSuccessStatusCode();

                requestUri = _baseAddress + "/odata/Customers/ODataReferentialConstraintSample.MyFunction(Id=123,SomeString=null)";
                response = _httpClient.GetAsync(requestUri).Result;

                response.EnsureSuccessStatusCode();
                Console.WriteLine("\nPress any key to continue...");
                Console.ReadKey();
            }
        }

        public static void Configuration(IAppBuilder builder)
        {
            HttpConfiguration config = new HttpConfiguration();
            config.MapODataServiceRoute(routeName: "OData", routePrefix: "odata", model: GetModel());
            builder.UseWebApi(config);
        }

        private static IEdmModel GetModel()
        {
            ODataModelBuilder builder = new ODataConventionModelBuilder();
            builder.EntitySet<Customer>("Customers");
            builder.EntitySet<Order>("Orders");
            builder.Action("ResetDataSource");


            var config = builder.EntityType<Customer>()
                .Collection
                .Function("MyFunction")
                .Returns<bool>();
            config.Parameter<int>("Id");
            config.Parameter<string>("SomeString");

            builder.Namespace = typeof(Customer).Namespace;

            return builder.GetEdmModel();
        }
    }
}
