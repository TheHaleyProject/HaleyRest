﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Threading.Tasks;
using Haley.Enums;

namespace Haley.Models
{
    /// <summary>
    /// Arguments that will be used with a Rest Request. 
    /// Arguments can be added to a query or to the body based on the type of object used.
    /// <list type="table">
    /// <item>
    /// <description><see cref="QueryParam"/> - For adding parameters to URL Query.</description>
    /// </item>
    /// <item>
    /// <description><see cref="RawBodyRequest"/> - For adding a body content to the request.</description>
    /// </item>
    ///  <item>
    /// <description><see cref="FormMultiPartRequest"/> - For adding content as a Multi-part form content.</description>
    /// </item>
    /// </list>
    /// </summary>
    public abstract class RequestObject
    {
        public string Id { get;}
        public object Value { get; private set; }
        /// <summary>
        /// This will be shared with the HTTP Request. Can be used for sending the request with file name.
        /// </summary>
        public string Title { get; set; }
        /// <summary>
        /// Additional information for this object
        /// </summary>
        public string Description { get; set; }
        public void UpdateValue(object value) {
            Value = value;
        }
        /// <summary>
        /// Rest Param Object
        /// </summary>
        /// <param name="value"></param>
        protected RequestObject(object value)
        {
            Id = Guid.NewGuid().ToString();
            Value = value;
        }
    }
}
