using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace Cav.Json
{
    /// <summary>
    /// Конвертирование <see cref="DateTime"/> по фомату. По умолчанию "yyyy-MM-dd". Для указания своего формата используйте параметризированный конструктор
    /// </summary>
    public class DateConverter : IsoDateTimeConverter
    {
        /// <summary>
        /// 
        /// </summary>
        public DateConverter() : this("yyyy-MM-dd") { }

        /// <summary>
        /// 
        /// </summary>
        public DateConverter(string dateTimeFormat) => DateTimeFormat = dateTimeFormat;
    }

    internal class FlagEnumStringConverter : StringEnumConverter
    {
        public FlagEnumStringConverter() => AllowIntegerValues = true;

        public override bool CanConvert(Type objectType) => base.CanConvert(objectType);

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var enumType = Nullable.GetUnderlyingType(objectType) ?? objectType;

            var isFlag = enumType.GetCustomAttribute<FlagsAttribute>() != null;

            return isFlag
                ? reader.TokenType == JsonToken.Integer
                    ? Enum.ToObject(enumType, serializer.Deserialize<int>(reader))
                    : Enum.ToObject(
                        enumType,
                        serializer.Deserialize<string[]>(reader)
                            .Select(x => Enum.Parse(enumType, x))
                            .Aggregate(0, (cur, val) => cur | (int)val))
                : base.ReadJson(reader, enumType, existingValue, serializer);
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var isFlag = value.GetType().GetCustomAttributes(typeof(FlagsAttribute), false).Any();

            if (isFlag)
                serializer.Serialize(writer, (value as Enum).FlagToList().Select(x => x.ToString()).ToArray());
            else
                base.WriteJson(writer, value, serializer);
        }
    }

    internal class NullListValueProvider : IValueProvider
    {
        private JsonProperty jsonProperty;
        private IValueProvider valueProvider;

        public NullListValueProvider(JsonProperty jsonProperty)
        {
            this.jsonProperty = jsonProperty;
            valueProvider = jsonProperty.ValueProvider;
        }
        public object GetValue(object target)
        {
            var inst = valueProvider.GetValue(target);

            return inst == null
                ? null
                : !(inst as IEnumerable).GetEnumerator().MoveNext()
                    ? null
                    : inst;
        }

        public void SetValue(object target, object value)
        {
            if (value == null)
            {
                if (jsonProperty.PropertyType.IsArray)
                    value = Array.CreateInstance(jsonProperty.PropertyType.GetElementType(), 0);
                else
                {
                    if (jsonProperty.PropertyType.GetConstructor(Array.Empty<Type>()) != null)
                        value = Activator.CreateInstance(jsonProperty.PropertyType);
                }
            }

            valueProvider.SetValue(target, value);
        }
    }

    internal class StringNullEmtyValueProvider : IValueProvider
    {
        private IValueProvider valueProvider;

        public StringNullEmtyValueProvider(JsonProperty jsonProperty) =>
            valueProvider = jsonProperty.ValueProvider;
        public object GetValue(object target)
        {
            var inst = valueProvider.GetValue(target);

            return ((string)inst).GetNullIfIsNullOrWhiteSpace();
        }
        public void SetValue(object target, object value)
        {
            value = ((String)value).GetNullIfIsNullOrWhiteSpace();

            valueProvider.SetValue(target, value);
        }
    }

    internal class CustomJsonContractResolver : DefaultContractResolver
    {
        public override JsonContract ResolveContract(Type type) => base.ResolveContract(type);
        protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
        {
            var jProperty = base.CreateProperty(member, memberSerialization);

            if (jProperty.PropertyType.IsArray ||
                (typeof(IEnumerable).IsAssignableFrom(jProperty.PropertyType) &&
                jProperty.PropertyType.GetConstructor(Array.Empty<Type>()) != null))
            {
                var olsShouldSerialise = jProperty.ShouldSerialize ?? (x => true);

                jProperty.ShouldSerialize = obj => olsShouldSerialise(obj) && jProperty.ValueProvider.GetValue(obj) != null;

                jProperty.ValueProvider = new NullListValueProvider(jProperty);
                jProperty.NullValueHandling = NullValueHandling.Include;
                jProperty.DefaultValueHandling = DefaultValueHandling.Populate;
            }

            if (jProperty.PropertyType == typeof(string))
                jProperty.ValueProvider = new StringNullEmtyValueProvider(jProperty);

            if (jProperty.PropertyType.IsEnum)
            {
                jProperty.DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate;
            }

            return jProperty;
        }
    }

    /// <summary>
    /// Общая настройка сериализатора Json
    /// </summary>
    internal class GenericJsonSerializerSetting : JsonSerializerSettings
    {
        private static GenericJsonSerializerSetting instance;

        /// <summary>
        /// Получеие 
        /// </summary>
        public static GenericJsonSerializerSetting Instance
        {
            get
            {
                if (instance == null)
                    instance = new GenericJsonSerializerSetting();

                return instance;
            }
        }

        internal GenericJsonSerializerSetting(
            StreamingContextStates state,
            object additional) : this() =>
            Context = new StreamingContext(state, additional);

        private GenericJsonSerializerSetting()
        {
            NullValueHandling = NullValueHandling.Ignore;
            DefaultValueHandling = DefaultValueHandling.Ignore;
            Converters.Add(new FlagEnumStringConverter());
            ContractResolver = new CustomJsonContractResolver();
        }
    }
}
