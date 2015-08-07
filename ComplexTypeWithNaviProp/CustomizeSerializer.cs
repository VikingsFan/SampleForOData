using System.Linq;
using System.Web.OData;
using System.Web.OData.Formatter.Serialization;
using Microsoft.OData.Core;
using Microsoft.OData.Edm;

namespace ODataReferentialConstraintSample
{
    public class CustomODataSerializerProvider : DefaultODataSerializerProvider
    {
        private CustomODataEntityTypeSerializer _entitySerializer;

        public CustomODataSerializerProvider()
        {
            _entitySerializer = new CustomODataEntityTypeSerializer(this);
        }

        public override ODataEdmTypeSerializer GetEdmTypeSerializer(IEdmTypeReference edmType)
        {
            if (edmType.TypeKind() == EdmTypeKind.Entity)
            {
                return _entitySerializer;
            }

            return base.GetEdmTypeSerializer(edmType);
        }
    }

    public class CustomODataEntityTypeSerializer : ODataEntityTypeSerializer
    {
        public CustomODataEntityTypeSerializer(ODataSerializerProvider serializerProvider)
            : base(serializerProvider)
        {
        }

        public override ODataEntry CreateEntry(SelectExpandNode selectExpandNode, EntityInstanceContext entityInstanceContext)
        {
            if (entityInstanceContext.EntityType.Name == "Order")
            {
                var idProp = selectExpandNode.SelectedStructuralProperties.FirstOrDefault(p => p.Name == "Id");
                if (idProp != null)
                {
                    selectExpandNode.SelectedStructuralProperties.Remove(idProp);
                }
            }
            ODataEntry entry = base.CreateEntry(selectExpandNode, entityInstanceContext);
            return entry;
        }
    }

}
