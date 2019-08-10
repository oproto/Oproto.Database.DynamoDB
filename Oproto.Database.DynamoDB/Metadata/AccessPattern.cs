﻿/*

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
using Amazon.DynamoDBv2.DocumentModel;

namespace Oproto.Database.DynamoDB.Metadata
{
    /// <summary>
    /// Represents an Access Pattern on a DynamoDB table.  An access pattern consists
    /// of a name used by the request, an index name, formats for Partition and Sort keys
    /// using parameters passed in through the request, and any additional filtering.
    /// 
    /// Also consists of the main return type for the root level object.
    /// </summary>
    public class AccessPattern
    {
        /// <summary>
        /// The name of the access pattern (ex: getEntityXByValueY)
        /// </summary>
        /// <value>The name of the pattern.</value>
        public string PatternName { get; set; }

        public string PkFormat { get; set; }
        public string SkFormat { get; set; }
        public QueryOperator SkQueryOperator { get; set; }

        public string ReturnType { get; set; }
        public bool ReturnTypeIsList { get; set; }
    }
}
