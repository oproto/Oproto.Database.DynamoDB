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
using Amazon.DynamoDBv2.DocumentModel;
using Newtonsoft.Json;

namespace Oproto.Database.DynamoDB
{
    public static class Extensions
    {
        public static T ToObject<T>(this Document doc)
        {
            var json = doc.ToJson();
            return JsonConvert.DeserializeObject<T>(json);
        }

        public static object ToObject(this Document doc, Type t)
        {
            var json = doc.ToJson();
            return JsonConvert.DeserializeObject(json, t);
        }

        public static void Add<T>(this IDictionary<string, Type> dict, string typeName)
        {
            dict.Add(typeName, typeof(T));
        }


        public static IList ToList(this IEnumerable input, Type type)
        {
            var listType = typeof(List<>).MakeGenericType(type);
            var dynamicList = (IList)Activator.CreateInstance(listType);
            foreach (var item in input)
            {
                dynamicList.Add(item);
            }
            return dynamicList;
        }

        public static T SafeValueForKey<K, T>(this IDictionary<K, T> dict, K key)
        {
            if (dict.ContainsKey(key))
            {
                return dict[key];
            }
            return default(T);
        }
    }
}
