using Haley.Models;
using System.Collections.Generic;

namespace Haley.Abstractions {
    public interface IEncodedFormRequestContent : IFormRequestContent {
        new IList<QueryParam> Value { get; }
        string GetEncodedBodyContent();
    }
}
