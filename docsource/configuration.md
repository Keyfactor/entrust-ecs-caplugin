## Overview

The Entrust ECS AnyCA Gateway REST plugin extends the capabilities of Entrust Certificate Services to Keyfactor Command via the Keyfactor AnyCA Gateway REST. The plugin represents a fully featured AnyCA REST Plugin with the following capabilies:
* SSL Certificate Synchronization
* SSL Certificate Enrollment
* SSL Certificate Revocation

## Requirements

## Gateway Registration

In order to enroll for certificates the Keyfactor Command server must trust the trust chain. Once you know your Root and/or Subordinate CA in your Entrust account, make sure to download and import the certificate chain into the Command Server certificate store
