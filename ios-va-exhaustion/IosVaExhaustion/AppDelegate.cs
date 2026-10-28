namespace IosVaExhaustion;

[Register("AppDelegate")]
public class AppDelegate : UIApplicationDelegate
{
    public override bool FinishedLaunching(UIApplication application, NSDictionary? launchOptions)
    {
        application.IdleTimerDisabled = true;
        return true;
    }

    public override UISceneConfiguration GetConfiguration(UIApplication application, UISceneSession connectingSceneSession, UISceneConnectionOptions options)
    {
        return new UISceneConfiguration("Default Configuration", connectingSceneSession.Role);
    }
}
