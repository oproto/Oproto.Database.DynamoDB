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
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Amazon.DynamoDBv2.DocumentModel;

namespace Oproto.Database.DynamoDB.Metadata
{
    public class TypeMetadata
    {
        public TypeMetadata(Type t, string name, Action<CalculatedAttributesCollection> initCallback)
        {
            this.Type = t;
            this.Name = name;
            initCallback(this.CalculatedAttributes);
        }

        public Type Type { get; private set; }
        public string Name { get; private set; }

        public CalculatedAttributesCollection CalculatedAttributes { get; set; } = new CalculatedAttributesCollection();


        public Document CalculateAttributes(Document docIn, bool bUpdate, Regex paramRegex)
        {
            // If we are performing an update, we can ignore individual calculated attributes if none of the
            // provided values is changing
            foreach (var calcAttributeName in CalculatedAttributes.Keys)
            {
                var calcAttribute = CalculatedAttributes[calcAttributeName];

                // Are any of the attributes included?  if so we need them all
                bool bHasOne = false;
                bool bHasAll = true;
                Dictionary<string, string> attrValues = new Dictionary<string, string>();

                MatchCollection matches = paramRegex.Matches(calcAttribute.Format);
                var attributes = matches.OfType<Match>().Select(x => ((Match)x).Value.Replace("${", "").Replace("}", "")).ToList();

                foreach (string strAttrName in attributes)
                {
                    var value = ValueForAttributePath(docIn, strAttrName);
                    if (!String.IsNullOrEmpty(value))
                    {
                        bHasOne = true;
                        attrValues.Add(strAttrName, value);
                    }
                    else
                    {
                        bHasAll = false;
                    }
                }
                if (bHasOne == false && calcAttribute.IsGeneratedKey == true) bHasOne = true;
                if (bHasOne == false && !attributes.Any()) bHasOne = true;

                if (bHasOne == true && bHasAll == false) throw new Exception($"Required attributes for calculated attribute '{calcAttribute.Name}' not present.");

                if (bHasOne == true && bHasAll == true)
                {
                    bool bHasAttribute = docIn.ContainsKey(calcAttribute.Name);
                    if (!calcAttribute.IsGeneratedKey ||
                        (calcAttribute.IsGeneratedKey && (bHasAttribute == false || String.IsNullOrEmpty(docIn[calcAttribute.Name]))
                        ))
                    {
                        // Calculate the attribute
                        var strCalcAttributeValue = calcAttribute.Format;
                        strCalcAttributeValue = strCalcAttributeValue.Replace("${func:newguid}", Guid.NewGuid().ToString());
                        foreach (var attrName in attributes)
                        {
                            strCalcAttributeValue = strCalcAttributeValue.Replace("${" + attrName + "}",
                                attrValues[attrName]);
                        }
                        docIn[calcAttribute.Name] = strCalcAttributeValue;

                    }
                }
            }

            return docIn;
        }

        public string ValueForAttributePath(Document doc, string path)
        {
            string[] pathParts = path.Split('.');
            var attrDoc = doc;
            for (int i = 0; i < pathParts.Length - 1; i++)
            {
                attrDoc = (Document)attrDoc[pathParts[i]];
            }

            return (string)attrDoc[pathParts[pathParts.Length - 1]];
        }
    }
}
