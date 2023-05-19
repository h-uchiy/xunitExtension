#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit.Sdk;

namespace xUnitExtension
{
    public class JsonDataAttribute : DataAttribute
    {
        private readonly object[] _inlineData;
        private readonly string? _key;
        private string? _fileName;

        /// <summary>
        ///     Initializes a new instance of the <see cref="JsonDataAttribute" /> class with default key (=same as method name)
        ///     definition.
        /// </summary>
        public JsonDataAttribute()
        {
            _inlineData = Array.Empty<object>();
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="JsonDataAttribute" /> class with key definition.
        /// </summary>
        public JsonDataAttribute(string key)
        {
            _inlineData = Array.Empty<object>();
            _key = string.IsNullOrWhiteSpace(key) ? throw new ArgumentNullException(nameof(key)) : key;
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="JsonDataAttribute" /> class with inline data definitions.
        /// </summary>
        /// <param name="data">The inline data values to pass to the theory.</param>
        public JsonDataAttribute(params object[] data)
        {
            _inlineData = data ?? throw new ArgumentNullException(nameof(data));
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="JsonDataAttribute" /> class with inline data definitions.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="data">The inline data values to pass to the theory.</param>
        public JsonDataAttribute(string key, params object[] data)
        {
            _key = key ?? throw new ArgumentNullException(nameof(key));
            _inlineData = data ?? throw new ArgumentNullException(nameof(data));
        }

        public string? FileName
        {
            get => _fileName;
            set => _fileName = string.IsNullOrWhiteSpace(value) ? null : value;
        }

        public override IEnumerable<object?[]> GetData(MethodInfo testMethod)
        {
            object?[] PopulateTestMethodParameters(IEnumerable<ParameterInfo> parameters, JToken jObject)
            {
                var parameterValues = jObject.Children().OfType<JProperty>().Append(new("key", _key));
                return _inlineData.Concat(from parameter in parameters.Skip(_inlineData.Length)
                        join jProperty in parameterValues on parameter.Name equals jProperty.Name into gj
                        from jProperty in gj.DefaultIfEmpty()
                        select jProperty?.Value.ToObject(parameter.ParameterType))
                    .ToArray();
            }

            var key = _key ?? testMethod.Name;
            var rootJToken = LoadJson(
                testMethod.DeclaringType ?? throw new ArgumentException("DeclaringType is null", nameof(testMethod)),
                this.FileName);
            return rootJToken[key] switch
            {
                JArray jArray => jArray.Children()
                    .Select(x => PopulateTestMethodParameters(testMethod.GetParameters(), x)),
                JObject jObject => new[] { PopulateTestMethodParameters(testMethod.GetParameters(), jObject) },
                null => throw new InvalidOperationException($"'{key}' was not found in the JSON root."),
                _ =>  throw new InvalidOperationException($"value of property '{key}' must be array or object."),
            };
        }

        private static JToken LoadJson(Type declaringType, string? fileName)
        {
            using var stream = GetStream(declaringType, fileName);
            using var streamReader = new StreamReader(stream);
            using var jsonReader = new JsonTextReader(streamReader);
            return JToken.Load(jsonReader);
        }

        private static Stream GetStream(Type declaringType, string? fileName)
        {
            if (declaringType.FullName == null) throw new ArgumentException("FullName is null", nameof(declaringType));

            Stream? TryToLoadFromResource()
            {
                var resourceName = fileName ?? declaringType.FullName;
                return declaringType.Assembly.GetManifestResourceNames().Any(x => x == resourceName)
                    ? declaringType.Assembly.GetManifestResourceStream(resourceName)
                    : null;
            }

            string GetFileName() =>
                fileName != null
                    ? Path.HasExtension(fileName) ? fileName : $"{fileName}.json"
                    : $"{declaringType.FullName}.json";

            return TryToLoadFromResource() ?? File.OpenRead(GetFileName());
        }
    }
}