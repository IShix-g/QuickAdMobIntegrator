
using System;

namespace QuickAdMobIntegrator.Editor
{
    public sealed class CustomGradleDialogContents : IDisposable
    {
        public readonly string Message;
        public Action OnClose { get; private set; }

        bool _isDisposed;

        public CustomGradleDialogContents(
            string message,
            Action onClose = default)
        {
            Message = message;
            OnClose = onClose;
        }

        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            _isDisposed = true;
            OnClose = default;
        }
    }
}