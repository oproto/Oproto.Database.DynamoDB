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
using Amazon.DynamoDBv2.DocumentModel;

namespace Oproto.Database.DynamoDB.Metadata
{
    public class IndexMetadata
    {
        public string Name { get; set; }
        public string PartitionKeyName { get; set; }
        public string SortKeyName { get; set; }


        public Dictionary<string, AccessPattern> AccessPatterns { get; private set; } = new Dictionary<string, AccessPattern>();


        /// <summary>
        /// Adds an access pattern to the table metadata
        /// </summary>
        /// <param name="name">Name.</param>
        /// <param name="pkFormat">Pk format.</param>
        /// <param name="skFormat">Sk format.</param>
        /// <param name="returnType">Return type.</param>
        /// <param name="bIsList">If set to <c>true</c> b is list.</param>
        public IndexMetadata AddAccessPattern(string name, string pkFormat, string skFormat, QueryOperator? skQueryOperator, string returnType, bool bIsList)
        {
            var accessPattern = new AccessPattern
            {
                PatternName = name,
                PkFormat = pkFormat,
                SkFormat = skFormat,
                SkQueryOperator = skQueryOperator ?? QueryOperator.Equal,
                ReturnType = returnType,
                ReturnTypeIsList = bIsList
            };
            this.AccessPatterns.Add(name, accessPattern);

            return this;
        }
    }


}
