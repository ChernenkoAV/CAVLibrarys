﻿using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace Cav.Json
{
    internal class FlagEnumStringConverter : StringEnumConverter
    {
        public FlagEnumStringConverter()
        {
            this.AllowIntegerValues = true;
        }

        public override bool CanConvert(Type objectType)
        {
            return base.CanConvert(objectType);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var enumType = Nullable.GetUnderlyingType(objectType) ?? objectType;

            var isFlag = enumType.GetCustomAttribute<FlagsAttribute>() != null;

            if (isFlag)
            {
                if (reader.TokenType == JsonToken.Integer)
                    return Enum.ToObject(enumType, serializer.Deserialize<int>(reader));

                return Enum.ToObject(
                    enumType,
                    serializer.Deserialize<string[]>(reader)
                        .Select(x => Enum.Parse(enumType, x))
                        .Aggregate(0, (cur, val) => cur | (int)val));
            }
            else
                return base.ReadJson(reader, enumType, existingValue, serializer);
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
            this.valueProvider = jsonProperty.ValueProvider;
        }
        public object GetValue(object target)
        {
            var inst = valueProvider.GetValue(target);

            if (inst == null)
                return null;

            if (!(inst as IEnumerable).GetEnumerator().MoveNext())
                return null;

            return inst;
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

    internal class CustomJsonContractResolver : DefaultContractResolver
    {
        public override JsonContract ResolveContract(Type type)
        {
            return base.ResolveContract(type);
        }
        protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
        {
            var jProperty = base.CreateProperty(member, memberSerialization);

            if (jProperty.PropertyType.IsArray ||
                (typeof(IEnumerable).IsAssignableFrom(jProperty.PropertyType) &&
                jProperty.PropertyType.GetConstructor(Array.Empty<Type>()) != null))
            {
                var olsShouldSerialise = jProperty.ShouldSerialize;
                if (olsShouldSerialise == null)
                    olsShouldSerialise = x => true;

                jProperty.ShouldSerialize = obj => olsShouldSerialise(obj) && jProperty.ValueProvider.GetValue(obj) != null;

                jProperty.ValueProvider = new NullListValueProvider(jProperty);
                jProperty.NullValueHandling = NullValueHandling.Include;
                jProperty.DefaultValueHandling = DefaultValueHandling.Populate;
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

        private GenericJsonSerializerSetting()
        {
            this.NullValueHandling = NullValueHandling.Ignore;
            this.DefaultValueHandling = DefaultValueHandling.Ignore;
            this.Converters.Add(new FlagEnumStringConverter());
            this.ContractResolver = new CustomJsonContractResolver();
        }
    }
}
