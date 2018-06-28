# CosmosDBAutoScaling

This is a little repository I put together to implement auto-scaling for CosmosDB accounts. The general approach is to use Azure Metric Alerts to call an Azure function. The Azure Function retrieves the CosmosDB account secrets from Azure Key Vault and then Scales the CosmosDB account up when the Alert is breached and back down again when the alert is resolved. 

# Introduction

It's impossible for humans to monitor a system 24/7 so we employ monitoring systems to do it for us. Imagine your application or service had an unexpected surge is throughput, you would want your application to be resilient enough to deal with that unexpected surge and carry on serving requests. This resilience can be achieved with automated monitoring and alerting. Some cloud components come with auto-scale capabilities baked in. For example service fabric has an auto scale feature. Unfortunately CosmosDB does not current support any auto-scaling function.

However it is possible to compose an auto-scale feature by utilizing a few Azure components. Azure monitoring and alerting allows alert rules to be created against a CosmosDB account. Alert rules can fire webhooks. Webhooks can call Azure Functions. Azure Functions can retrieve CosmosDB authentication securely from Key Vault. Azure Functions can utilize the CosmosDB client to scale the CosmosDB account assets. Below is a high-level diagram illustrating how these Azure components can be combined to create an server-less auto scale function.

This document gives examples of how to interact which each Azure component in the topology above. I create a function which utilises each component in isolation. I'll then conclude by giving any example of the complete solution.



# Function App
You'll see many examples of using Azure Functions via the Azure portal, coding out a single method that has a single limited responsibility. For this exercise I'm using visual studio to run and debug my code locally. When opting for this route to function apps the code is compiled into an assembly and deployed to the app services. This makes the code read only via the portal. 

This was fairly straight forward using the Microsoft online documentation. I did run into a problem with a reference to System.ValueTuples from the Microsoft.NET.Sdk.Functions nuget. I upgraded to .net .1.47 and this seemed to fix my issue.

# Out Bindings and MSI
An objective of this exercise is to make management calls to a CosmosDB account. Azure Functions natively support output bindings to CosmosDB. The binding allows you to create new documents in a collections. However my objective is to change the provisioned RU so the CosmosDB output binding isn't going to help me here. I need to take advantage of the CosmosDB sdk and make database and collection level calls. To make alterations to the RU provisioned setting I'll need masterkey access and I'll also need to secure the account master auth key. In steps Key Vault. Before I can obtain sensitive information from key vault I need to have permission to read from Key Vault. This is done using a managed service identity (MSI) for the function app. Using an MSI I can configure other Azure resources to allow access to calls originating from the service identity. 



Configure the MSI via the Azure Portal


Configuring the MSI via the Azure Portal couldn't be easier. Open your functionapp navigate to platform features→ Managed service Identity.



From there it's a simple matter of toggling the "Register with Azure Active Directory" option.



Visit the Microsoft documentation for more info on configuring the MSI programatically.




# Key Vault
Key Vault is used to secure the sensitive CosmosDB connection information. Both account url and master authkey will be stored in Key Vault. This keeps them secure and configurable at deploy time.

Keeping a Secret


In the Azure Key Vault blade select "Secrets" and "Generate/Import". To create a secret you'll need to supply a name for the secret and the CosmosDB authkey.







Once created open the secret and click the latest version. You'll then have access to the "secret identifier". You'll need the URL to access the secret via the Key Vault SDK.


Accessing a Secret
The next step is to give your functionapp MSI access to the secrete. Goto "Access policies" on the main Key Vault blade. You should see an existing policy that enables the owner to modify content in the vault. Add a new policy and select the functionapp MSI from the "select principal" selector. Considering the least privileges security principal from the "Secret Permission" drop down select "Secret Management Operations"→ "Get". This is the only permission necessary.





# Function Code
I produced a set functions that tackle each problme seperately. From getting enviromental information, to getting protected data, to making the CosmosDB call.

Get Environment Variables
I use Azure Function application setting to store the Url's the Key Vault location I want to access securely.

Get Key Vault Secrets
This function uses the MSI registered for the Function App to instantiate a Key Vault client. The client is then used to retrieve the sensitive CosmosDB connection information.

CosmosDB Scaler
The scaler makes a connection to the CosmosDB account and then traverses the database-collection hierarchy. An async scale up task is created for each collection with a scale up expression specified. In this example all collection are increased 100 RU.

# Tier Box
The Azure Alerting structure only allows us to configure trigger conditions against absolute value. For example we can configure an alert to trigger when the Max RU Per Second metric is greater than 1000RU. Ideally we'd like to express relative values. For example when the Max RU Per Second metric is greater than 60% trigger the alert. However this is just not currently possible. When it comes to automatically scaling a CosmosDB account we might like to scale it at a number of trigger points, creating a number of scale tiers. Using a single alert with an absolute trigger threshold means we're only able to configure a two tier system. One tier for expected usage and a second tier for higher than usual RU consumption. Configuring multiple alerts at increasing RU threshold's means we can ramp the RU up in increasing increments. Thus engineering a multi-tier auto-scale.

When I started thinking about a multi-tier auto-scale I immediately thought about how a car gear box works. The gear ratio's exponentially increase to deliver increasing units of speed. Throughput surges come in bumps and mountains so it makes sense to deliver increasing units of RU. So I started thinking about a formula I could use to setup multiple alerts that drive the addition of increasing amounts of RU to the CosmosDB account.



Let
s = starting RU provisioned

e = the maximum RU I'm willing to provision to

n = the number of steps I'd like to take.

sf = the RU scaling factor I'll need to apply

Scale Function
sf = n √(e ÷ s)



Perhaps there are better ways and more sophisticated algorithms. But this approach seemed fairly straight forward and it's not too taxing.
