using System.Linq;
using System.Web.Http;
using System.Web.OData;
using System.Web.OData.Builder;
using System.Web.OData.Extensions;
using AutoExpand.Models;
using Microsoft.OData.Edm;

namespace AutoExpand
{
    public static class WebApiConfig
    {
        public static void Register(HttpConfiguration config)
        {
            // Web API configuration and services

            // Web API routes
            config.MapHttpAttributeRoutes();

            var builder = new ODataConventionModelBuilder();
            builder.EntitySet<Contribution>("Contributions");
            builder.EntitySet<Contact>("Contacts");
            var model = builder.GetEdmModel();

            //var choiceOrd = model.SchemaElements.OfType<IEdmEntityType>().Single(t => t.Name == "ChoiceOrder");
            //var choices = choiceOrd.DeclaredProperties.Single(prop => prop.Name == "Choices");
            //model.SetAnnotationValue(choices,
            //    new QueryableRestrictionsAnnotation(new QueryableRestrictions { AutoExpand = true }));

            config.MapODataServiceRoute("odata", "odata", model);
            config.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "api/{controller}/{id}",
                defaults: new { id = RouteParameter.Optional }
            );
        }
    }
}
