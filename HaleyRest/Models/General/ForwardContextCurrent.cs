using Haley.Abstractions;
using Haley.Models;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Haley.Models {
    public static class ForwardContextCurrent {
        //Even though ForwardContextCurrent is a static class/field, the value stored in AsyncLocal<T> is not one global value.
        //Request A sets AsyncLocal.Value = A
        //Request B sets AsyncLocal.Value = B
        //They run in parallel, and each one sees its own value when code runs under that request’s async chain.
        //It can break if we use background threads that are not part of the original async chain.

        private static readonly AsyncLocal<ForwardContext> _current = new AsyncLocal<ForwardContext>();
        public static ForwardContext Current {
            get => _current.Value;
            set => _current.Value = value;
        }
    }
}