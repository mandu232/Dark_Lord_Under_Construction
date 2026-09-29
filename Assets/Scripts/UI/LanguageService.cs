using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

namespace DarkLord.UI
{
    public static class LanguageService
    {
        public const string English = "en";
        public const string Korean = "ko";
        private static string pendingCode;
        private static bool waitingForInitialization;

        public static string Normalize(string code) => code == Korean ? Korean : English;
        public static string Next(string code) => Normalize(code) == English ? Korean : English;
        public static string DisplayName(string code) => Normalize(code) == Korean ? "한국어" : "영어";

        public static void Apply(string code)
        {
            pendingCode = Normalize(code);
            var initialization = LocalizationSettings.InitializationOperation;
            if (initialization.IsDone)
            {
                SelectPendingLocale();
                return;
            }

            if (waitingForInitialization) return;
            waitingForInitialization = true;
            initialization.Completed += _ =>
            {
                waitingForInitialization = false;
                SelectPendingLocale();
            };
        }

        private static void SelectPendingLocale()
        {
            Locale locale = LocalizationSettings.AvailableLocales.GetLocale(new LocaleIdentifier(pendingCode));
            if (locale != null)
                LocalizationSettings.SelectedLocale = locale;
        }
    }
}
