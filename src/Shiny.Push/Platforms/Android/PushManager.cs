using System;
using System.Threading.Tasks;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Microsoft.Extensions.Logging;
using Android.Runtime;
using Android.Gms.Extensions;
using Firebase.Messaging;
using Shiny.Notifications;
using Task = System.Threading.Tasks.Task;
using CancellationToken = System.Threading.CancellationToken;


namespace Shiny.Push
{
    public sealed class PushManager : IPushManager,
                                      IPushTagSupport,
                                      IShinyStartupTask
    {
        readonly Subject<PushNotification> receiveSubj;
        readonly IServiceProvider services;
        readonly INotificationManager notificationManager;
        readonly PushStore store;
        readonly ILogger logger;



        public PushManager(IServiceProvider services,
                           INotificationManager notificationManager,
                           PushStore store,
                           ILogger<IPushManager> logger)
        {
            this.services = services;
            this.notificationManager = notificationManager;
            this.logger = logger;
            this.store = store;
            this.receiveSubj = new Subject<PushNotification>();
        }


        public DateTime? CurrentRegistrationTokenDate => this.store.RegistrationTokenDate;
        public string? CurrentRegistrationToken => this.store.RegistrationToken;
        public string[]? RegisteredTags => this.store.Tags;


        public void Start()
        {
            // wireup firebase if it was active
            if (this.store.RegistrationToken != null)
                FirebaseMessaging.Instance.AutoInitEnabled = true;

            ShinyFirebaseService.NewToken = async token =>
            {
                if (this.store.RegistrationToken != null)
                {
                    this.store.RegistrationToken = token;
                    this.store.RegistrationTokenDate = DateTime.UtcNow;

                    await this.services
                        .RunDelegates<IPushDelegate>(
                            x => x.OnTokenChanged(token)
                        )
                        .ConfigureAwait(false);
                }
            };

            ShinyFirebaseService.MessageReceived = async message =>
            {
                try
                {
                    var pr = this.FromNative(message);
                    await this.OnPushReceived(pr).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    this.logger.LogError(ex, "Error processing received message");
                }
            };
        }


        public async Task<PushAccessState> RequestAccess(CancellationToken cancelToken = default)
        {
            var nresult = await this.notificationManager.RequestAccess();
            if (nresult != AccessState.Available)
                return new PushAccessState(nresult, null);

            FirebaseMessaging.Instance.AutoInitEnabled = true;

            var task = await FirebaseMessaging.Instance.GetToken();
            var token = task.JavaCast<Java.Lang.String>().ToString();

            this.store.RegistrationToken = token;
            this.store.RegistrationTokenDate = DateTime.UtcNow;

            return new PushAccessState(AccessState.Available, this.CurrentRegistrationToken);
        }


        public async Task UnRegister()
        {
            this.store.Clear();
            FirebaseMessaging.Instance.AutoInitEnabled = false;
            await Task.Run(() => FirebaseMessaging.Instance.DeleteToken()).ConfigureAwait(false);
        }


        public IObservable<PushNotification> WhenReceived()
            => this.receiveSubj;


        public async Task AddTag(string tag)
        {
            await FirebaseMessaging.Instance.SubscribeToTopic(tag);
            this.store.AddTag(tag);
        }


        public async Task RemoveTag(string tag)
        {
            await FirebaseMessaging.Instance.UnsubscribeFromTopic(tag);
            this.store.RemoveTag(tag);
        }


        public async Task ClearTags()
        {
            if (this.store.Tags != null)
                foreach (var tag in this.store.Tags)
                    await FirebaseMessaging.Instance.UnsubscribeFromTopic(tag);

            this.store.Tags = null;
        }


        public async Task SetTags(params string[]? tags)
        {
            await this.ClearTags();
            if (tags != null)
                foreach (var tag in tags)
                    await this.AddTag(tag);
        }


        protected async Task OnPushReceived(PushNotification push)
        {
            this.receiveSubj.OnNext(push);

            await this.services.RunDelegates<IPushDelegate>(
                x => x.OnReceived(push)
            );

            if (push.Notification != null)
                await this.notificationManager.Send(push.Notification);
        }


        public PushNotification FromNative(RemoteMessage message)
        {
            Notification? notification = null;
            var native = message.GetNotification();

            if (native != null)
            {
                notification = new Notification
                {
                    Title = native.Title,
                    Message = native.Body,
                    Channel = native.ChannelId
                };
                if (!native.Icon.IsEmpty())
                    notification.Android.SmallIconResourceName = native.Icon;

                if (!native.Color.IsEmpty())
                    notification.Android.ColorResourceName = native.Color;
            }
            return new PushNotification(message.Data, notification);
        }
    }
}
