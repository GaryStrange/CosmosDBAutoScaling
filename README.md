# CosmosDBAutoScaling

This is a little repository I put together to implement auto-scaling for CosmosDB accounts. The general approach is to use Azure Metric Alerts to call an Azure function. The Azure Function retrieves the CosmosDB account secrets from Azure Key Vault and then Scales the CosmosDB account up when the Alert is breached and back down again when the alert is resolved. 

# Introduction

It's impossible for humans to monitor a system 24/7 so we employ monitoring systems to do it for us. Imagine your application or service had an unexpected surge is throughput, you would want your application to be resilient enough to deal with that unexpected surge and carry on serving requests. This resilience can be achieved with automated monitoring and alerting. Some cloud components come with auto-scale capabilities baked in. For example service fabric has an auto scale feature. Unfortunately CosmosDB does not current support any auto-scaling function.

However it is possible to compose an auto-scale feature by utilizing a few Azure components. Azure monitoring and alerting allows alert rules to be created against a CosmosDB account. Alert rules can fire webhooks. Webhooks can call Azure Functions. Azure Functions can retrieve CosmosDB authentication securely from Key Vault. Azure Functions can utilize the CosmosDB client to scale the CosmosDB account assets. Below is a high-level diagram illustrating how these Azure components can be combined to create an server-less auto scale function.

