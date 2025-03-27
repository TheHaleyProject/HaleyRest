using Haley.Enums;
using System;

namespace Haley.Models {
    public class ProgressTracker {
        /// <summary>
        /// Title could also hold the file name
        /// </summary>
        public string Title { get; set; }
        /// <summary>
        /// Description could also hold the file path
        /// </summary>
        public string Description { get; set; }
        public string RequestId { get; set; }
        /// <summary>
        /// In Bytes
        /// </summary>
        public long TotalSize { get; set; }
        /// <summary>
        /// In bytes
        /// </summary>
        public long ConsumedSize { get; set; }

        public ProgressTracker WithSize(long totalsize, long consumedsize) {
            TotalSize = totalsize;
            ConsumedSize = consumedsize;
            return this;
        }

        public ProgressState State { get; set; }

        public override string ToString() {
            return $@"TotalSize = {TotalSize} kb | ConsumedSize = {ConsumedSize} kb | Title: {Title}";
        }
        public DateTime Time { get; set; }
        public ProgressTracker(string requestId) {
            RequestId = requestId;
            State = ProgressState.Initializing;
        }
    }
}
