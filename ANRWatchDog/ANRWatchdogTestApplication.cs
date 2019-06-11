using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Xamarin.ANRWatchDog;
using App1;
using Android.Util;

namespace ANRWatchDog
{
    [Application(
#if DEBUG
        Debuggable = true
#endif
    )]
    public class ANRWatchdogTestApplication : Application
    {
        class SilentListener : App1.ANRWatchDog.IANRListener
        {
            public void OnAppNotResponding(App1.ANRError error) =>
                Log.Error("ANR-Watchdog-Demo", error, "ANR");
        }

        class DefaultListener : App1.ANRWatchDog.IANRListener
        {
            public void OnAppNotResponding(App1.ANRError error)
            {
                Log.Error("ANR-Watchdog-Demo", "Detected Application Not Responding!");

                // Test serialization
                try
                {
                    IFormatter serializeFormatter = new BinaryFormatter();
                    using (var stream = new MemoryStream())
                    {
                        // Serialize
                        serializeFormatter.Serialize(stream, error);

                        // Deserialize
                        IFormatter deserializeFormatter = new BinaryFormatter();
                        stream.Position = 0;
                        var deserializedError = (App1.ANRError)deserializeFormatter.Deserialize(stream);
                        Log.Error("ANR-Watchdog-Demo", error, "Original ANR");
                        Log.Error("ANR-Watchdog-Demo", deserializedError, "Deserialized ANR");
                    }
                }
                catch (Exception ex)
                {
                    throw ex;
                }

                Log.Info("ANR-Watchdog-Demo", "Error was successfully serialized");

                throw error;
            }
        }

        class DefaultInterceptor : App1.ANRWatchDog.IANRInterceptor
        {
            public long Intercept(long duration)
            {
                long ret = (ANRWatchdogTestApplication.duration * 1000) - duration;
                if (ret > 0)
                    Log.Warn("ANR-Watchdog-Demo", $"Intercepted ANR that is too short ({duration} ms), postponing for {ret} ms.");
                return ret;
            }
        }

        internal App1.ANRWatchDog _anrWatchDog = new App1.ANRWatchDog(2000);

        internal static int duration = 4;

        internal readonly App1.ANRWatchDog.IANRListener _silentListener = new SilentListener();

        public ANRWatchdogTestApplication(IntPtr intPtr, JniHandleOwnership jniHandleOwnership)
            : base(intPtr, jniHandleOwnership) { }

        public override void OnCreate()
        {
            base.OnCreate();

            _anrWatchDog
                .SetANRListener(new DefaultListener())
                .SetANRInterceptor(new DefaultInterceptor())
                .Start();
        }
    }
}