/*

MIT License

Copyright (c) 2019 Oproto Inc

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.

 */


using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.Lambda.Core;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using Oproto.Database.DynamoDB.Metadata;
using Oproto.Database.DynamoDB.Models;

namespace Oproto.Database.DynamoDB
{
    public abstract class TableAccessBase
    {
        public TableAccessBase(IAmazonDynamoDB client, string tableName)
        {
            _client = client;
            _tableName = tableName;

            Console.WriteLine($"Initializing TableAccess Layer for Table {_tableName}");
            Table = Table.LoadTable(_client, _tableName);
        }

        private IAmazonDynamoDB _client { get; set; }
        private string _tableName { get; set; }

        public string TableName { get; protected set; }  // Different from _tableName, "TableName" is the friendly, fixed table name used in request routing...

        protected Table Table { get; private set; }
        protected Dictionary<string, IndexMetadata> Indicies { get; set; } = new Dictionary<string, IndexMetadata>();
        protected Dictionary<string, TypeMetadata> Types { get; set; } = new Dictionary<string, TypeMetadata>();
        private Dictionary<string, Dictionary<string, PropertyMetadata>> Metadata { get; set; } = new Dictionary<string, Dictionary<string, PropertyMetadata>>();


        private Regex _paramMatch = new Regex(@"(\${\w*}+)");
        private Regex _paramParse = new Regex(@"(\${[\w.]*}+)");

        protected abstract void Init();

        /// <summary>
        /// Adds a .NET Runtime Type mapping to the Table metadata
        /// </summary>
        /// <returns>The type.</returns>
        /// <param name="name">Name.</param>
        /// <param name="initCallback">Init callback.</param>
        /// <typeparam name="T">The 1st type parameter.</typeparam>
        public TypeMetadata AddType<T>(string name, Action<CalculatedAttributesCollection> initCallback)
        {
            var td = new TypeMetadata(typeof(T), name, initCallback);
            this.Types.Add(name, td);
            return td;
        }

        /// <summary>
        /// Adds a Table Index to the Table metadata
        /// </summary>
        /// <returns>The index.</returns>
        /// <param name="name">Name.</param>
        /// <param name="pk">Pk.</param>
        /// <param name="sk">Sk.</param>
        public IndexMetadata AddIndex(string name, string pk, string sk)
        {
            var index = new IndexMetadata()
            {
                Name = name,
                PartitionKeyName = pk,
                SortKeyName = sk
            };
            Indicies.Add(name, index);
            return index;
        }





        [LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]
        public async Task<Stream> ExecuteAsync(TableAccessRequest req, ILambdaContext context)
        {
            Object response = null;

            if (req.Query != null)
            {
                response = await this.QueryAsync(req.Query);
            }
            else if (req.Put != null)
            {
                Console.WriteLine($"Executing '{req.Put.Operation}' on Table '{_tableName}'");
                string jsonData = JsonConvert.SerializeObject(req.Put.Data);
                Console.WriteLine(jsonData);
                var doc = Document.FromJson(jsonData);
                response = await this.PutAsync(doc, req.Put.Operation);
            }

            DefaultContractResolver contractResolver = new DefaultContractResolver
            {
                NamingStrategy = new CamelCaseNamingStrategy()
            };

            string json = JsonConvert.SerializeObject(response, new JsonSerializerSettings
            {
                ContractResolver = contractResolver,
                Formatting = Formatting.Indented
            });
            Console.WriteLine(json);

            MemoryStream ms = new MemoryStream();
            StreamWriter wr = new StreamWriter(ms);
            await wr.WriteAsync(json);
            await wr.FlushAsync();
            ms.Position = 0;
            return ms;
        }


        /// <summary>
        /// Builds a key from a format string and a dictionary of parameters
        /// </summary>
        /// <returns>The key from parameters.</returns>
        /// <param name="parameters">Parameters.</param>
        /// <param name="format">Format.</param>
        private string BuildKeyFromParameters(IDictionary<string, string> parameters, string format)
        {
            if (format == null) return null;
            string value = format;
            var matches = _paramMatch.Matches(format);
            foreach (Match match in matches)
            {
                var paramName = match.Value.Replace("${", "").Replace("}", "");
                if (!parameters.ContainsKey(paramName))
                {
                    throw new Exception($"Parameter list doesn't contain key {paramName}");
                }
                value = value.Replace(match.Value, parameters[paramName]);
            }
            return value;
        }


        public async Task<object> QueryAsync(QueryRequest req)
        {
            var accessPatternAndIndex = this.Indicies.SelectMany(x => x.Value.AccessPatterns, (x, y) => new { Index = x.Value, AccessPattern = y.Value })
                .FirstOrDefault(x => x.AccessPattern.PatternName == req.AccessPattern);

            if (accessPatternAndIndex == null)
            {
                throw new Exception($"Access Pattern {req.AccessPattern} not found in Table metadata.");
            }

            var accessPattern = accessPatternAndIndex.AccessPattern;
            var index = accessPatternAndIndex.Index;
            string indexName = index.Name;
            if (indexName == "default") indexName = null;

            var hashKey = BuildKeyFromParameters(req.Parameters, accessPattern.PkFormat);
            var sortKey = BuildKeyFromParameters(req.Parameters, accessPattern.SkFormat);
            var filter = new Expression();
            var hashKeyFilter = new QueryFilter(index.PartitionKeyName, QueryOperator.Equal, hashKey);
            if (accessPattern.SkFormat != null) hashKeyFilter.AddCondition(index.SortKeyName, accessPattern.SkQueryOperator, sortKey);

            Expression expressionFilter = null;
            /*if (typeFilter != null)
            {
                expressionFilter = new Expression();
                expressionFilter.ExpressionAttributeNames["#type"] = "type";
                expressionFilter.ExpressionAttributeValues[":type"] = typeFilter;
                expressionFilter.ExpressionStatement = "#type = :type";
            }*/

            var search = this.Table.Query(new QueryOperationConfig
            {
                IndexName = indexName,
                Filter = hashKeyFilter,
                FilterExpression = expressionFilter

            });

            var documents = await search.GetRemainingAsync();

            var loadedObjects = new Dictionary<string, IList<object>>();

            // Converts an IEnumerable of Document into a dictionary with lists of objects grouped by "type"
            foreach (var doc in documents)
            {
                var t = doc["type"];
                if (Types.ContainsKey(t))
                {
                    if (!loadedObjects.ContainsKey(t))
                    {
                        loadedObjects.Add(t, new List<object>());
                    }

                    loadedObjects[t].Add(doc.ToObject(Types[t].Type));
                }
            }

            TransformData(loadedObjects);


            if (accessPattern.ReturnTypeIsList)
            {
                return loadedObjects.SafeValueForKey(accessPattern.ReturnType)?.ToList();
            }
            else
            {
                return loadedObjects.SafeValueForKey(accessPattern.ReturnType)?.FirstOrDefault();
            }
        }

        /// <summary>
        /// Performs a DynamoDB PutItem request
        /// </summary>
        /// <returns>The async.</returns>
        /// <param name="src">Source.</param>
        private async Task<JToken> PutAsync(Document src, string op)
        {
            var strType = src["type"];
            var t = this.Types[strType];
            src = t.CalculateAttributes(src, op == "UPDATE", _paramParse);

            if (op == "PUT")
            {
                await this.Table.PutItemAsync(src, new PutItemOperationConfig()
                {
                    ConditionalExpression = null,
                    Expected = null,
                    ExpectedState = null
                });

            }
            else
            {
                await this.Table.UpdateItemAsync(src, new UpdateItemOperationConfig()
                {
                    ConditionalExpression = null,
                    Expected = null,
                    ExpectedState = null
                });

            }

            var json = src.ToJson();
            return (JToken)JsonConvert.DeserializeObject(json);
        }

        private void PrepareTypeMetadata()
        {
            foreach (var t in this.Types)
            {

                var properties = t.Value.Type.GetProperties();

                var typeMetadata = new Dictionary<string, PropertyMetadata>();

                foreach (var prop in properties)
                {
                    bool bIsEnum = (typeof(IEnumerable).IsAssignableFrom(prop.PropertyType));

                    if (bIsEnum && prop.PropertyType.IsGenericType)
                    {
                        var genericType = prop.PropertyType.GenericTypeArguments.FirstOrDefault();
                        var typeDetails = this.Types.Values.FirstOrDefault(x => x.Type == genericType);

                        var propMetadata = new PropertyMetadata()
                        {
                            IsEnumerable = true,
                            Property = prop,
                            Type = genericType,
                            TypeName = typeDetails.Name
                        };
                        typeMetadata.Add(prop.Name, propMetadata);
                    }
                }

                Metadata[t.Value.Name] = typeMetadata;
            }
        }

        protected void TransformData(IDictionary<string, IList<object>> loadedObjects)
        {
            // Generate metadata if it doesn't exist
            if (!this.Metadata.Any()) PrepareTypeMetadata();

            // Go through each of the types registered for this table
            if (this.Types != null)
            {
                foreach (var t in this.Types)
                {
                    // Grab the items for this type
                    var items = loadedObjects.SafeValueForKey(t.Key)?.ToList();

                    // Get the metadata for this type
                    var currentTypeMetadata = this.Metadata[t.Key];

                    // Loop through the items and apply the properties
                    if (items != null)
                    {
                        foreach (var item in items)
                        {
                            // Loop through each property
                            foreach (var propMetadataPair in currentTypeMetadata)
                            {
                                var propTypeName = propMetadataPair.Value.TypeName;
                                var prop = propMetadataPair.Value.Property;
                                var propType = propMetadataPair.Value.Type;
                                // TODO: Do we ever need official relationships?
                                var subItems = loadedObjects.SafeValueForKey(propTypeName)?.ToList(propType);
                                prop.SetValue(item, subItems);
                            }
                        }
                    }
                }
            }
        }


        protected PaginatedItems<T> ExtractPaginatedItems<T>(IDictionary<string, IList<Object>> loadedObjects, string key) where T : class
        {
            var items = loadedObjects.SafeValueForKey(key)?.Select(x => x as T);
            if (items == null || !items.Any()) return null;
            return new PaginatedItems<T>(items);
        }
    }
}
