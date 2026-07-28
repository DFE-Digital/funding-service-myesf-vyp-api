using System.Configuration;
using ViewYourPayments.Core.Models.Payments;

namespace ViewYourPayments.DataImport.Domain
{
    /// <summary>
    /// Retrieve settings from configuration file.
    /// </summary>
    public class AppSettings
    {

        public string ConnectionString { get; set; }

        public DataImportSetting DataImportSetting { get; set; }
        
    }
}