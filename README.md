<h1 align="center" style="border-bottom: none">
    Entrust ECS   Gateway AnyCA Gateway REST Plugin
</h1>

<p align="center">
  <!-- Badges -->
<img src="https://img.shields.io/badge/integration_status-production-3D1973?style=flat-square" alt="Integration Status: production" />
<a href="https://github.com/Keyfactor/entrust-ecs-caplugin/releases"><img src="https://img.shields.io/github/v/release/Keyfactor/entrust-ecs-caplugin?style=flat-square" alt="Release" /></a>
<img src="https://img.shields.io/github/issues/Keyfactor/entrust-ecs-caplugin?style=flat-square" alt="Issues" />
<img src="https://img.shields.io/github/downloads/Keyfactor/entrust-ecs-caplugin/total?style=flat-square&label=downloads&color=28B905" alt="GitHub Downloads (all assets, all releases)" />
</p>

<p align="center">
  <!-- TOC -->
  <a href="#support">
    <b>Support</b>
  </a> 
  ·
  <a href="#requirements">
    <b>Requirements</b>
  </a>
  ·
  <a href="#installation">
    <b>Installation</b>
  </a>
  ·
  <a href="#license">
    <b>License</b>
  </a>
  ·
  <a href="https://github.com/orgs/Keyfactor/repositories?q=anycagateway">
    <b>Related Integrations</b>
  </a>
</p>


The Entrust ECS AnyCA Gateway REST plugin extends the capabilities of Entrust Certificate Services to Keyfactor Command via the Keyfactor AnyCA Gateway REST. The plugin represents a fully featured AnyCA REST Plugin with the following capabilies:
* SSL Certificate Synchronization
* SSL Certificate Enrollment
* SSL Certificate Revocation

## Compatibility

The Entrust ECS   Gateway AnyCA Gateway REST plugin is compatible with the Keyfactor AnyCA Gateway REST 24.2.0 and later.

## Support
The Entrust ECS   Gateway AnyCA Gateway REST plugin is supported by Keyfactor for Keyfactor customers. If you have a support issue, please open a support ticket with your Keyfactor representative. If you have a support issue, please open a support ticket via the Keyfactor Support Portal at https://support.keyfactor.com. 

> To report a problem or suggest a new feature, use the **[Issues](../../issues)** tab. If you want to contribute actual bug fixes or proposed enhancements, use the **[Pull requests](../../pulls)** tab.

## Requirements



## Installation

1. Install the AnyCA Gateway REST per the [official Keyfactor documentation](https://software.keyfactor.com/Guides/AnyCAGatewayREST/Content/AnyCAGatewayREST/InstallIntroduction.htm).

2. On the server hosting the AnyCA Gateway REST, download and unzip the latest [Entrust ECS   Gateway AnyCA Gateway REST plugin](https://github.com/Keyfactor/entrust-ecs-caplugin/releases/latest) from GitHub.

3. Copy the unzipped directory (usually called `net6.0`) to the Extensions directory:

    ```shell
    Program Files\Keyfactor\AnyCA Gateway\AnyGatewayREST\net6.0\Extensions
    ```

    > The directory containing the Entrust ECS   Gateway AnyCA Gateway REST plugin DLLs (`net6.0`) can be named anything, as long as it is unique within the `Extensions` directory.

4. Restart the AnyCA Gateway REST service.

5. Navigate to the AnyCA Gateway REST portal and verify that the Gateway recognizes the Entrust ECS   Gateway plugin by hovering over the ⓘ symbol to the right of the Gateway on the top left of the portal.

## Configuration

1. Follow the [official AnyCA Gateway REST documentation](https://software.keyfactor.com/Guides/AnyCAGatewayREST/Content/AnyCAGatewayREST/AddCA-Gateway.htm) to define a new Certificate Authority, and use the notes below to configure the **Gateway Registration** and **CA Connection** tabs:

    * **Gateway Registration**

        In order to enroll for certificates the Keyfactor Command server must trust the trust chain. Once you know your Root and/or Subordinate CA in your Entrust account, make sure to download and import the certificate chain into the Command Server certificate store

    * **CA Connection**

        Populate using the configuration fields collected in the [requirements](#requirements) section.

        * **AuthUsername** - Username for the gateway to authenticate with Entrust 
        * **AuthPassword** - Password for the account used to authenticate with Entrust 
        * **ClientCertificate** - The client certificate information used to authenticate with Entrust, only if configured to use certificate authentication 
        * **Name** - The default requester name 
        * **Email** - The default requester email address 
        * **PhoneNumber** - The default requester phone number 
        * **IgnoreExpired** - If set to true, will not sync expired certs from Entrust 
        * **Enabled** - Flag to Enable or Disable gateway functionality. Disabling is primarily used to allow creation of the CA prior to configuration information being available. 

2. Define [Certificate Profiles](https://software.keyfactor.com/Guides/AnyCAGatewayREST/Content/AnyCAGatewayREST/AddCP-Gateway.htm) and [Certificate Templates](https://software.keyfactor.com/Guides/AnyCAGatewayREST/Content/AnyCAGatewayREST/AddCA-Gateway.htm) for the Certificate Authority as required. One Certificate Profile must be defined per Certificate Template. It's recommended that each Certificate Profile be named after the Product ID. The Entrust ECS   Gateway plugin supports the following product IDs:


3. Follow the [official Keyfactor documentation](https://software.keyfactor.com/Guides/AnyCAGatewayREST/Content/AnyCAGatewayREST/AddCA-Keyfactor.htm) to add each defined Certificate Authority to Keyfactor Command and import the newly defined Certificate Templates.

4. In Keyfactor Command (v12.3+), for each imported Certificate Template, follow the [official documentation](https://software.keyfactor.com/Core-OnPrem/Current/Content/ReferenceGuide/Configuring%20Template%20Options.htm) to define enrollment fields for each of the following parameters:

    * **LifetimeMonths** - OPTIONAL: The number of months of validity to use when requesting certs. If not provided, default is 12. 
    * **Organization** - OPTIONAL: For requests that will not have a subject (such as ACME) you can use this field to provide an organization name. Value supplied here will override any CSR values, so do not include this field if you want the organization from the CSR to be used. 
    * **CertificateUsage** - Required for public SSL certificate types. Represents the key usage for the certificates enrolled against this template. Valid values are 'server', 'client', or 'serverclient'. Do not provide a value for cert types that are not public SSL. 
    * **RenewalWindowDays** - OPTIONAL: The number of days from certificate expiration that the gateway should do a renewal rather than a reissue. If not provided, default is 90. 





## License

Apache License 2.0, see [LICENSE](LICENSE).

## Related Integrations

See all [Keyfactor Any CA Gateways (REST)](https://github.com/orgs/Keyfactor/repositories?q=anycagateway).