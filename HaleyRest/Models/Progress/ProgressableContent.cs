using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Haley.Abstractions;
using Haley.Enums;

namespace Haley.Models {

    //https://stackoverflow.com/questions/22528839/how-can-i-calculate-progress-with-httpclient-postasync
    //https://github.com/paulcbetts/ModernHttpClient/issues/80
    //https://stackoverflow.com/questions/21130362/progress-bar-for-httpclient-uploading

    //In order to avoid being buffered by HttpClient you either need to provide a content length (eg: implement HttpContent.TryComputeLength, or set the header) or enable HttpRequestHeaders.TransferEncodingChunked. This is necessary because otherwise HttpClient can't determine the content length header, so it reads in the entire content to memory first.


    public class ProgressableStreamContent : HttpContent {
        //const int defaultBufferSize = 4096; //4kb
        //const int defaultBufferSize = 32768; //32kb = 32 * 1024
        const int defaultBufferSize = 524288; //512kb = 512 * 1024
        Stream _content;
        int _bufferSize;
        bool _contentConsumed;
        IProgressReporter _reporter;
        string _requestObjId;
        public string Title { get; set; }
        public string Description { get; set; }

        public ProgressableStreamContent(Stream content, IProgressReporter reporter,string requestObjectId) : this(content, defaultBufferSize, reporter,requestObjectId) { }
        public ProgressableStreamContent(Stream content, int bufferSize, IProgressReporter reporter, string requestObjectId) {
            if (content == null) {
                throw new ArgumentNullException("content");
            }
            if (bufferSize <= 0) {
                throw new ArgumentOutOfRangeException("bufferSize");
            }

            this._content = content;
            this._bufferSize = bufferSize;
            this._requestObjId = requestObjectId;
            this._reporter = reporter;
        }

        protected override Task SerializeToStreamAsync(Stream stream, TransportContext context) {
            Contract.Assert(stream != null);

            PrepareContent();

            return Task.Run(() =>
            {
                var buffer = new Byte[this._bufferSize];
                var size = _content.Length;
                var uploaded = 0;

                _reporter.InitializeTracker(new ProgressTracker(_requestObjId) { Title = this.Title, Description = this.Description,TotalSize = size,ConsumedSize = 0 }); //Initialize this tracker first
                _reporter.ChangeState(_requestObjId, ProgressState.InProgress);

                using (_content) {
                    //Dispose content when leaving the block
                    while (true) {

                        var length = _content.Read(buffer, 0, buffer.Length);
                        if (length <= 0) break; //If we are no longer able to read the content or reached the end, break the loop.
                        uploaded += length;
                        stream.Write(buffer, 0, length);
                        _reporter.ChangeProgress(_requestObjId,uploaded); //this will call the method in a different thread.
                    }
                }
                _reporter.ChangeState(_requestObjId, ProgressState.TransferComplete);
            });
        }

        protected override bool TryComputeLength(out long length) {
            length = _content.Length;
            return true;
        }

        protected override void Dispose(bool disposing) {
            if (disposing) {
                _content.Dispose();
            }
            base.Dispose(disposing);
        }

        private void PrepareContent() {
            if (_contentConsumed) {
                // If the content needs to be written to a target stream a 2nd time, then the stream must support
                // seeking (e.g. a FileStream), otherwise the stream can't be copied a second time to a target 
                // stream (e.g. a NetworkStream).
                if (_content.CanSeek) {
                    _content.Position = 0;
                } else {
                    throw new InvalidOperationException("Content stream is already read. Cannot reset seek position to zero");
                }
            }
            _contentConsumed = true;
        }
    }
}
