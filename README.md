# Manage Your Education and Skills Funding View Your Payments API
The Manage Your Education and Skills Funding View Your Payments API is used for providing the payments related information from the View Your Payments Database.

## Provider

[The Department for Education](https://www.gov.uk/government/organisations/department-for-education)

## About this project

This project is a .Net 8 API project utilizing an Azure App Service for deployment.

**Note:** The project is currently being updated to be containerised via Docker where the deployment method and target will change, this document will be updated when these changes have been finalised.

# Local Configuration Guide

For running the application locally, a `appsettings.json` file will need to be created in the `ViewYourPayments.Api` project. Below, and included in the repo, there is `appsettings.example.json` which can be used as a base and populated with the required values, which can be retrieved from the Azure Portal.

## Application Settings (`local.settings.json`)
```json
{
  "ConnectionStrings": {
    "ViewYourPaymentsDbContext": ""
  },
  "APPINSIGHTS_INSTRUMENTATIONKEY": "",
  "APPLOGGINGAPPINSIGHTS_INSTRUMKEY": "",
  "AppSettings": {
    "AzureAdOptions": {
      "Domain": "",
      "Instance": "https://login.microsoftonline.com/",
      "TenantId": ""
    },
    "Environment": ""
  },
  "Logging": {
    "LogLevel": {
      "Default": "Warning"
    }
  }
}
```
### Setting Details

- **`ConnectionStrings:ViewYourPaymentsDbContext`**  
  The connection string used to connect the application to its primary database context via Entity Framework Core.

- **`APPINSIGHTS_INSTRUMENTATIONKEY`**  
  The instrumentation key for Application Insights.

- **`APPLOGGINGAPPINSIGHTS_INSTRUMKEY`**  
  The instrumentation key for application logging with Application Insights.

- **`AppSettings:AzureAdOptions:Domain`**  
  The specific registered domain linked to your Microsoft Entra ID / Azure AD tenant.

- **`AppSettings:AzureAdOptions:Instance`**  
  The identity provider authority URL used for authenticating users, pointing directly to the Microsoft Identity Platform.

- **`AppSettings:AzureAdOptions:TenantId`**  
  The unique globally unique identifier (GUID) representing your dedicated directory or tenant within Azure.

- **`AppSettings:Environment`**  
  The current runtime environment identifier for the project.

- **`Logging:LogLevel:Default`**  
  The default logging verbosity level for the entire application, which is currently configured to capture `Warning` messages and above.

## Build and Test

To build and test locally, you can either use Visual Studio, Visual Studio Code or simply use dotnet CLI `dotnet build` and `dotnet test` more information in dotnet CLI can be found at <https://docs.microsoft.com/en-us/dotnet/core/tools/>.

## Contribute

To contribute,

- If you are part of the team then create a branch for changes and then submit your changes for review by creating a pull request.
- If you are external to the organisation then fork this repository and make necessary changes and then submit your changes for review by creating a pull request.