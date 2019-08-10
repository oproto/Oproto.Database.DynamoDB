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
using Newtonsoft.Json.Linq;

namespace Oproto.Database.DynamoDB
{
    /// <summary>
    /// Request made from application or API to either write, delete or query
    /// data from the table.
    /// </summary>
    public class TableAccessRequest
    {
        public string TableName { get; set; }
        public QueryRequest Query { get; set; }
        public PutRequest Put { get; set; }
    }

    /// <summary>
    /// A table query specified by Access Pattern name and by dictionary 
    /// of Parameters
    /// </summary>
    public class QueryRequest
    {
        public string AccessPattern { get; set; }
        public Dictionary<string, string> Parameters { get; set; }
    }

    public class PutRequest
    {
        public string Operation { get; set; }
        public JToken Data { get; set; }
    }
}
