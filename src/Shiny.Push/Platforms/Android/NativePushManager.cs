using System;
using System.Threading.Tasks;
using Android.Gms.Extensions;
using Firebase.Messaging;
using Java.Interop;


namespace Shiny.Push
{
    public class NativePushManager
    {
        public async Task<string> RequestToken()
        {
            FirebaseMessaging.Instance.AutoInitEnabled = true;

            var task = await FirebaseMessaging.Instance.GetToken();
            var token = task.JavaCast<Java.Lang.String>().ToString();
            return token;
        }
    }
}
