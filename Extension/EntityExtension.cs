using Microsoft.Xrm.Sdk;

namespace SalesOrderPlugin.Extension
{
    public static class EntityExtension
    {
        public static void SetOptionValue(this Entity entity, string attributeName, int value)
        {
            if (entity.Contains(attributeName))
            {
                entity[attributeName] = new OptionSetValue(value);
            }
        }

        public static string ToJson(this Entity entity)
        {
            return Newtonsoft.Json.JsonConvert.SerializeObject(entity);
        }

        public static string ToJson(this EntityReference entity)
        {
            return Newtonsoft.Json.JsonConvert.SerializeObject(entity);
        }
        public static string ToJson(this EntityCollection entityCollection)
        {
            return Newtonsoft.Json.JsonConvert.SerializeObject(entityCollection);
        }

        public static string ToPrefix(this string propertyName)
        {
            return $"crbf2_{propertyName}";
        }
    }
}