using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Util;
using MauiAotHang.Features;

namespace MauiAotHang
{
    [Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
    public class MainActivity : MauiAppCompatActivity
    {
        protected override void OnCreate(Bundle? savedInstanceState)
        {
            Task.Run(() =>
            {
                Log.Debug("MyLog", $"Calling mt1 from thread #{System.Environment.CurrentManagedThreadId}");
                var mt1 = new ManualType1(42);
                if (System.Environment.CurrentManagedThreadId > -1)
                {
                    mt1.Num = 100;
                }
                Log.Debug("MyLog", $"Mt1 result: {mt1}");
            }).Wait();
            base.OnCreate(savedInstanceState);
        }
    }
}
