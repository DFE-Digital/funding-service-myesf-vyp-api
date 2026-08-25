namespace ViewYourPayments.Core.AppConfig
{
    public class AppSettings
    {
        public string Environment { get; set; }
        public string APPINSIGHTS_INSTRUMENTATIONKEY { get; set; }
        public AzureAdOptions AzureAdOptions { get; set; }
    }
}
