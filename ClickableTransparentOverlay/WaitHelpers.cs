namespace ClickableTransparentOverlay
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    internal static class WaitHelpers
    {
        /// <summary>
        /// Creates a Task that finishes once the <paramref name="stopWaitingCondition"/> is true, checking the condition every 100ms
        /// or stopping once the <paramref name="timeout"/> has elapsed
        /// </summary>
        /// <param name="stopWaitingCondition">Condition for finishing the task</param>
        /// <param name="timeout">timeout to finish waiting regardless of the condition</param>
        internal static Task SpinWait(Func<bool> stopWaitingCondition, TimeSpan? timeout = null)
        {
            if (timeout == null)
            {
                return SpinWaitInternal(stopWaitingCondition);
            }

            var cancellationToken = new CancellationTokenSource(timeout.Value).Token;
            return SpinWaitInternal(() => stopWaitingCondition() || cancellationToken.IsCancellationRequested);
        }

        private static async Task SpinWaitInternal(Func<bool> stopWaitingCondition)
        {
            var checkInterval = TimeSpan.FromSeconds(1 / 10d);
            while (!stopWaitingCondition())
            {
                await Task.Delay(checkInterval);
            }
        }
    }
}