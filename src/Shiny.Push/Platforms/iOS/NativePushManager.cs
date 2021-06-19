using System;
using System.Threading;
using System.Threading.Tasks;
using Foundation;

using UIKit;

using UserNotifications;

namespace Shiny.Push
{
    public class NativePushManager
    {
        readonly AppleLifecycle lifecycle;
        readonly IPlatform platform;


        public NativePushManager(AppleLifecycle lifecycle, IPlatform platform)
        {
            this.lifecycle = lifecycle;
            this.platform = platform;
        }


        public async Task<PushAccessState> RequestAccess(CancellationToken cancelToken = default)
        {
            var result = await UNUserNotificationCenter.Current.RequestAuthorizationAsync(
                UNAuthorizationOptions.Alert |
                UNAuthorizationOptions.Badge |
                UNAuthorizationOptions.Sound
            );
            if (!result.Item1)
                return PushAccessState.Denied;

            var deviceToken = await this.RequestDeviceToken(cancelToken).ConfigureAwait(false);

            //this.store.RegistrationToken = ToTokenString(deviceToken);
            //this.store.RegistrationTokenDate = DateTime.UtcNow;
            return new PushAccessState(AccessState.Available, "");
        }


        public async Task<NSData> RequestDeviceToken(CancellationToken cancelToken = default)
        {
            var tcs = new TaskCompletionSource<NSData>();
            //UIApplication.SharedApplication.RegisterForRemoteNotificationTypes(UIRemoteNotificationType.Alert)
            using (var caller = this.lifecycle.RegisterForRemoteNotificationToken(
                rawToken => tcs.TrySetResult(rawToken),
                err => tcs.TrySetException(new Exception(err.LocalizedDescription))
            ))
            {
                await this.platform
                    .InvokeOnMainThreadAsync(
                        () => UIApplication.SharedApplication.RegisterForRemoteNotifications()
                    )
                    .ConfigureAwait(false);
                var rawToken = await tcs.Task;
                var token = ToTokenString(rawToken);
                //await this.services.Services.SafeResolveAndExecute<IPushDelegate>(
                //    x => x.OnTokenChanged(token)
                //);
                return token;
            }
        }


        public string? ToTokenString(NSData deviceToken)
        {
            string? token = null;
            if (deviceToken.Length > 0)
            {
                if (UIDevice.CurrentDevice.CheckSystemVersion(13, 0))
                {
                    var data = deviceToken.ToArray();
                    token = BitConverter
                        .ToString(data)
                        .Replace("-", "")
                        .Replace("\"", "");
                }
                else if (!deviceToken.Description.IsEmpty())
                {
                    token = deviceToken.Description.Trim('<', '>');
                }
            }
            return token;
        }
    }
}
