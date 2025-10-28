using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;

namespace fiskaltrust.AndroidLauncher.Services
{
    public class StateProvider
    {
        private static readonly Lazy<StateProvider> _lazyInstance = new Lazy<StateProvider>(() => new StateProvider());
        private readonly object _lock = new object();

        public static StateProvider Instance => _lazyInstance.Value;
        public MiddlewareState CurrentValue { get; private set; } = new MiddlewareState
        {
            CurrentState = State.Uninitialized,
            Reason = ""
        };

        public void SetState(State state, string reason = "")
        {
            lock (_lock)
            {
                CurrentValue = new MiddlewareState
                {
                    CurrentState = state,
                    Reason = reason
                };
            }
        }
    }

    public class MiddlewareState
    {
        [JsonConverter(typeof(StringEnumConverter))]
        public State CurrentState { get; set; }
        public string Reason { get; set; }
    }

    public enum State
    {
        Uninitialized,
        Initializing,
        Running,
        Error
    }
}