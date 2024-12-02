using System.Collections.Concurrent;

namespace Challenge2_Group16_GUI_WebAPI.Services
{
    public class ChainService
    {
        private readonly ConcurrentDictionary<byte[], TaskCompletionSource<object>> _expectedResponseChains = new();

        public TaskCompletionSource<object>? GetExpectedResponseChain(byte[] chainIdentifier)
        {
            _expectedResponseChains.TryGetValue(chainIdentifier, out var response);
            return response;
        }

        public async Task<T?> Expect<T>(byte[] chainIdentifier) where T : class
        {
            var tcs = new TaskCompletionSource<object>();
            var result = AddExpectedResponseChain(chainIdentifier, tcs);
            if (!result)
            {
                return default;
            }

            var completedTask = await AwaitResponse(chainIdentifier, TimeSpan.FromSeconds(10));
            if (completedTask == tcs.Task)
            {
                // Successfully received a response
                var response = await tcs.Task;
                RemoveExpectedResponseChain(chainIdentifier);
                return response as T;
            }
            else
            {
                // Timeout occurred
                RemoveExpectedResponseChain(chainIdentifier);
                return default;
            }
        }

        public async Task<bool> ExpectAck(byte[] chainIdentifier)
        {
            var tcs = new TaskCompletionSource<object>();
            var result = AddExpectedResponseChain(chainIdentifier, tcs);
            if (!result)
            {
                return default;
            }

            var completedTask = await AwaitResponse(chainIdentifier, TimeSpan.FromSeconds(5));
            if (completedTask == tcs.Task)
            {
                // Successfully received a response
                var response = await tcs.Task;
                RemoveExpectedResponseChain(chainIdentifier);
                return true;
            }
            else
            {
                // Timeout occurred
                RemoveExpectedResponseChain(chainIdentifier);
                return false;
            }
        }

        public bool AckChain(byte[] chainIdentifier)
        {
            var tcs = GetExpectedResponseChain(chainIdentifier);
            if (tcs == null)
            {
                return false;
            }

            tcs.SetResult(true);
            return true;
        }

        public bool RespondChain<T>(byte[] chainIdentifier, T response) where T : class
        {
            var tcs = GetExpectedResponseChain(chainIdentifier);
            if (tcs == null)
            {
                return false;
            }

            tcs.SetResult(response);
            return true;
        }

        private bool AddExpectedResponseChain(byte[] chainIdentifier, TaskCompletionSource<object> tcs)
        {
            return _expectedResponseChains.TryAdd(chainIdentifier, tcs);
        }

        private async Task<object?> AwaitResponse(byte[] chainIdentifier, TimeSpan timeout)
        {
            if (!_expectedResponseChains.TryGetValue(chainIdentifier, out var chain))
            {
                return null;
            }

            return await Task.WhenAny(chain.Task, Task.Delay(timeout));
        }

        private bool RemoveExpectedResponseChain(byte[] chainIdentifier)
        {
            return _expectedResponseChains.TryRemove(chainIdentifier, out var _);
        }
    }
}
