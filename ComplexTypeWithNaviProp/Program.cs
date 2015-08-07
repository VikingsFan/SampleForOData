using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Web.Http;
using System.Web.OData.Builder;
using System.Web.OData.Extensions;
using System.Web.OData.Formatter.Serialization;
using Microsoft.OData.Edm;
using Microsoft.OData.Core;
using Microsoft.Owin.Hosting;
using System.Web.OData.Formatter;
using System.Web.OData.Formatter.Deserialization;
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

                ResetDataSource();
                // The referential constraint is present on metadata document
                HttpResponseMessage response = QueryMetadata();
                string metadata = response.Content.ReadAsStringAsync().Result;

                response = GetOrders();
                string responseString = response.Content.ReadAsStringAsync().Result;

                Console.WriteLine("\nPress any key to continue...");
                Console.ReadKey();
            }
        }

        public static void Configuration(IAppBuilder builder)
        {
            HttpConfiguration config = new HttpConfiguration();

            var cusFormatters = new List<CustomODataMediaTypeFormatter>();
            var format = new CustomODataMediaTypeFormatter(
                new DefaultODataSerializerProvider(),
                new DefaultODataDeserializerProvider(),
                ODataPayloadKind.MetadataDocument);
            format.SupportedMediaTypes.Add((MediaTypeHeaderValue)((ICloneable)new MediaTypeHeaderValue("application/xml")).Clone());
            cusFormatters.Add(format);
            config.Formatters.Clear();
            config.Formatters.AddRange(cusFormatters);
            config.Formatters.AddRange(ODataMediaTypeFormatters.Create(new CustomODataSerializerProvider(), new DefaultODataDeserializerProvider()));
            config.MapODataServiceRoute(routeName: "OData", routePrefix: "odata", model: GetModel());
            
            builder.UseWebApi(config);
        }

        public static HttpResponseMessage QueryMetadata()
        {
            string requestUri = _baseAddress + "/odata/$metadata";
            HttpResponseMessage response = _httpClient.GetAsync(requestUri).Result;

            return response;
        }
        public static HttpResponseMessage GetOrders()
        {
            string requestUri = _baseAddress + "/odata/GetOrders";
            HttpResponseMessage response = _httpClient.PostAsync(requestUri, null).Result;

            return response;
        }

        public static HttpResponseMessage ResetDataSource()
        {
            string requestUri = _baseAddress + "/odata/ResetDataSource";

            HttpResponseMessage response = _httpClient.PostAsync(requestUri, null).Result;
            response.EnsureSuccessStatusCode();
            return response;
        }

        private static IEdmModel GetModel()
        {
            ODataModelBuilder builder = new ODataConventionModelBuilder();
            builder.EntitySet<Customer>("Customers");
            builder.EntitySet<Region>("Regions");
            builder.EntitySet<Order>("Orders");
            builder.Action("ResetDataSource");
            builder.Action("GetOrders").ReturnsCollectionFromEntitySet<Order>("Orders");
            builder.Action("GetAddress").ReturnsCollection<MyAddress>();

            builder.Namespace = typeof(Customer).Namespace;

            return builder.GetEdmModel();
        }
    }
}
