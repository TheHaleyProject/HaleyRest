using Haley.Models;
using System.Collections.Generic;

namespace Haley.Abstractions {
    public interface IFormDataRequestContent : IFormRequestContent {
        new Dictionary<string, IRawBodyRequestContent> Value { get; }
        void Add(List<IQueryRequestContent> queryParamList);
    }
}
