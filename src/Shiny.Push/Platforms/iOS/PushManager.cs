using System;
using System.Threading;
using System.Threading.Tasks;
using System.Reactive.Subjects;
using Foundation;
using UIKit;
using UserNotifications;
using Shiny.Notifications;
using Shiny.Infrastructure;


namespace Shiny.Push
{
    public class PushManager : IPushManager, IShinyStartupTask
    {
        readonly Subject<PushNotification> payloadSubj = new Subject<PushNotification>();
        readonly ShinyCoreServices services;
        readonly PushStore store;





        public PushManager(ShinyCoreServices services, PushStore store)
        {
            this.services = services;
            this.store = store;
        }


        public void Start()
        {
            this.services.Lifecycle.RegisterToReceiveRemoteNotifications(async userInfo =>
            {
                var dict = userInfo.FromNsDictionary();
                var pr = new PushNotification(dict, null);
                await this.services
                    .Services
                    .SafeResolveAndExecute<IPushDelegate>(x => x.OnReceived(pr))
                    .ConfigureAwait(false);
                this.payloadSubj.OnNext(pr);
            });

            this.services.Lifecycle.RegisterForNotificationReceived(async response =>
            {
                if (response.Notification?.Request?.Trigger is UNPushNotificationTrigger)
                {
                    var shiny = response.FromNative();
                    var pr = new PushNotificationResponse(
                        shiny.Notification,
                        shiny.ActionIdentifier,
                        shiny.Text
                    );

                    await this.services
                        .Services
                        .RunDelegates<IPushDelegate>(x => x.OnEntry(pr))
                        .ConfigureAwait(false);
                }
            });

            if (!this.CurrentRegistrationToken.IsEmpty())
            {
                // do I need to do this?  I would normally be calling RequestAccess on startup anyhow
                this.RequestAccess().ContinueWith(x => { });
            }
        }


        public string? CurrentRegistrationToken => this.store.RegistrationToken;
        public DateTime? CurrentRegistrationTokenDate => this.store.RegistrationTokenDate;
        public IObservable<PushNotification> WhenReceived() => this.payloadSubj;


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
            this.store.RegistrationToken = ToTokenString(deviceToken);
            this.store.RegistrationTokenDate = DateTime.UtcNow;
            return new PushAccessState(AccessState.Available, this.CurrentRegistrationToken);
        }


        public async Task UnRegister()
        {
            await this.services
                .Platform
                .InvokeOnMainThreadAsync(UIApplication.SharedApplication.UnregisterForRemoteNotifications)
                .ConfigureAwait(false);
            this.store.Clear();
        }
    }
}
