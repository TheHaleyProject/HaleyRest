using Haley.Abstractions;
using Haley.Enums;
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace Haley.Models {
    public class ProgressReporter : IProgressReporter {
        ConcurrentDictionary<string, bool> _trackers = new ConcurrentDictionary<string, bool>();
        public Guid Id { get; }

        public ProgressReporter() {
            Id = Guid.NewGuid();
        }


        public event EventHandler<(string id, ProgressState state)> StateChanged;
        public event EventHandler<(string id, long consumed_size)> ProgressChanged;
        public event EventHandler<ProgressTracker> TrackerInitialized;

        public void ChangeProgress(string id, long consumed_size) {
            try {
                //https://stackoverflow.com/questions/1916095/how-do-i-make-an-eventhandler-run-asynchronously
                // first an important note! Whenever you call BeginInvoke you must call the corresponding EndInvoke, otherwise if the invoked method threw an exception or returned a value then the ThreadPool thread will never be released back to the pool, resulting in a thread-leak!
                Task.Run(() => ProgressChanged?.Invoke(this, (id, consumed_size)));
            } catch (Exception ex) {
            }
        }

        public override string ToString() {
            return $@"Progress reporter: Id = {Id.ToString()}";
        }

        public void ChangeState(string id, ProgressState state) {
            if (state == ProgressState.TransferComplete) { _trackers.TryRemove(id, out _); }
            Task.Run(() => StateChanged?.Invoke(this, (id, state)));
        }

        public void InitializeTracker(ProgressTracker tracker) {
            if (_trackers.TryAdd(tracker.RequestId, true)) {
                TrackerInitialized?.Invoke(this, tracker); //This tracker is now initialized
            }
        }
    }
}
