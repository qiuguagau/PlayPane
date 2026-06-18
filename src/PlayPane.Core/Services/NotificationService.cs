using System;

namespace PlayPane.Core.Services
{
    public sealed class NotificationService
    {
        public event EventHandler<string> NotificationRaised;

        public void Raise(string message)
        {
            EventHandler<string> handler = NotificationRaised;
            if (handler != null)
            {
                handler(this, message);
            }
        }
    }
}
