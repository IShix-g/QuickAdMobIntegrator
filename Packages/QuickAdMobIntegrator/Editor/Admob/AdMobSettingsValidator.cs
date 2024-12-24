
namespace QuickAdMobIntegrator.Admob.Editor
{
    internal sealed class AdMobSettingsValidator
    {
        public static class Errors
        {
            public static readonly ValidationErrorState AdmobNotInstalled = new (
                ValidationErrorType.NotInstalled,
                "Google Mobile Ads is not installed. Please install it first."
            );
            
            public static readonly ValidationErrorState MissingAppId = new (
                ValidationErrorType.MissingAppId,
                "Set the App ID via the toolbar menu: `Assets > Google Mobile Ads > Settings...`"
            );
        }

        public bool IsValid => !Error.IsValid;
        public bool IsChecked { get; private set; }
        public ValidationErrorState Error { get; private set; }

        public bool Validate()
        {
            if (!AdMobSettingsSupport.IsAdmobInstalled())
            {
                SetValidationState(Errors.AdmobNotInstalled);
                return true;
            }

            try
            {
                var androidAppId = AdMobSettingsSupport.GetAndroidAppId();
                var iosAppId = AdMobSettingsSupport.GetIOSAppId();
                if (string.IsNullOrWhiteSpace(androidAppId)
                    || string.IsNullOrWhiteSpace(iosAppId))
                {
                    SetValidationState(Errors.MissingAppId);
                    return false;
                }
                SetValidationState(ValidationErrorState.Empty);
                return true;
            }
            catch
            {
                // If exception occurs, this code is likely to have a problem, so set to true.
                SetValidationState(ValidationErrorState.Empty);
                return true;
            }
        }

        public void OpenSettings() => AdMobSettingsSupport.OpenSettings();
        
        void SetValidationState(ValidationErrorState error)
        {
            IsChecked = true;
            Error = error;
        }
    }
}