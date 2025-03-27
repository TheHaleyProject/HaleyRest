using Haley.Models;
using System.Collections.Generic;

namespace Haley.Abstractions {
    public interface IFormDataRequestContent : IFormRequestContent {
        new Dictionary<string, RawBodyRequestContent> Value { get; }
        void Add(List<QueryParam> queryParamList);
    }
}
