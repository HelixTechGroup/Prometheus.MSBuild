namespace Net6.Android.App.Baseline
{
  [Activity(Label = "@string/app_name", MainLauncher = true)]
  public class MainActivity : Activity
  {
    protected override void OnCreate(Bundle? savedInstanceState)
    {
      base.OnCreate(savedInstanceState);

      // Set our view from the "main" layout resource
      SetContentView(Resource.Layout.activity_main);
    }
  }
}