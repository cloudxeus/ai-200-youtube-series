# AI-200 Hands-on Code Examples

C# and Azure code examples accompanying my YouTube series on preparing for the Microsoft AI-200 exam. Use this repository to follow the demonstrations, explore the code, and practice with your own Azure resources.

Each numbered folder focuses on a topic from the series. These are teaching examples; follow the corresponding video for resource provisioning and the context behind each implementation.

## Lessons

| Folder | What you will explore |
| --- | --- |
| [01_AI200FoundryChat](01_AI200FoundryChat/) | An ASP.NET Core Razor Pages chat application using Microsoft Foundry, with a Dockerfile for containerization. |
| [02_Kubernetes](02_Kubernetes/) | Kubernetes Deployment and Service manifests for the chat application. |
| [03_postgre_sql](03_postgre_sql/) | Reading and creating support articles in PostgreSQL using Npgsql and ASP.NET Core APIs. |
| [04_postgre_sql_embedding](04_postgre_sql_embedding/) | Generating embeddings for PostgreSQL support articles using Azure OpenAI. |
| [05_cosmos_db](05_cosmos_db/) | Reading support articles from Azure Cosmos DB through ASP.NET Core APIs. |
| [06_cosmos_db_embedding](06_cosmos_db_embedding/) | Generating embeddings and performing vector searches with Azure Cosmos DB and Azure OpenAI. |
| [07_redis](07_redis/) | Storing article embeddings and performing vector searches with Redis. |
| [08_azure_functions](08_azure_functions/) | An HTTP-triggered Azure Function that processes an order request. |
| [09_servicebus_queue](09_servicebus_queue/) | Separate console applications for sending and receiving Azure Service Bus queue messages. |
| [10_custom_topic](10_custom_topic/) | Publishing events to an Azure Event Grid custom topic and handling events with an Azure Function. |
| [11_key_vault](11_key_vault/) | Retrieving a secret from Azure Key Vault using Azure Identity. |

## Prerequisites

- .NET 10 SDK: all C# projects target `net10.0`.
- An editor or IDE with C# support.
- An Azure subscription and the resources required for the lesson you are following.
- Azure CLI for local Azure sign-in where the examples use `DefaultAzureCredential`.
- For the Functions lessons: Azure Functions Core Tools v4 with support for the project's .NET runtime.
- For the container and Kubernetes lessons: Docker, `kubectl`, a container registry, and a Kubernetes cluster.

Database schemas, containers, indexes, model deployments, queues, and event subscriptions must be configured as demonstrated in the videos. The repository does not provision all of these resources automatically.

## Getting started

1. Clone or download this repository and open it in your editor.
2. Choose a lesson folder and follow its video to create the required resources.
3. Configure your own endpoints, deployment names, and credentials as described below.
4. Run the relevant project from the repository root. For example, after configuring the chat application:

   ```powershell
   dotnet restore ./01_AI200FoundryChat/AI200FoundryChat/AI200FoundryChat.csproj
   dotnet run --project ./01_AI200FoundryChat/AI200FoundryChat/AI200FoundryChat.csproj
   ```

5. For web applications, open the address printed in the terminal. Some API projects also include `.http` files with sample requests.

Run each lesson independently; there is no single application or solution covering the entire repository.

## Configuration

Credentials are intentionally empty. Existing Azure resource names and endpoints are examples from the demonstrations; replace them with your own values.

| Lesson | Configuration to supply |
| --- | --- |
| 01 | `Foundry:ProjectEndpoint` and `Foundry:ModelDeployment` through ASP.NET Core configuration. Sign in with an identity authorized to access your Foundry project. |
| 02 | Your container image in `deployment.yaml`, plus the authentication and application configuration needed inside the cluster. |
| 03 | `ConnectionStrings:Postgres`. |
| 04 | `ConnectionStrings:Postgres`, `AzureOpenAI:Endpoint`, `AzureOpenAI:ApiKey`, and `AzureOpenAI:EmbeddingDeployment`. |
| 05 | `CosmosDb:ConnectionString`, `CosmosDb:DatabaseName`, and `CosmosDb:ContainerName`. |
| 06 | The Cosmos DB and Azure OpenAI settings in `appsettings.json`, configured for your own resources. |
| 07 | Redis host/key and Azure OpenAI endpoint/key/deployment variables in `Program.cs`. Your Redis service must support the search and vector operations used by the example. |
| 08 | Local Functions settings in `local.settings.json`, including storage configuration as needed for your environment. |
| 09 | Service Bus connection string and queue name in both sender and receiver `Program.cs` files. |
| 10 | Event Grid endpoint/key in the publisher's `Program.cs`, Functions local settings, and an event subscription targeting the function. |
| 11 | Key Vault URL and secret name in `Program.cs`, plus an identity authorized to read the secret. |

For ASP.NET Core projects, environment variables can override `appsettings.json`; use double underscores for nested keys. For example, in PowerShell before running lesson 01:

```powershell
$env:Foundry__ProjectEndpoint = "https://<your-resource>.services.ai.azure.com/api/projects/<your-project>"
$env:Foundry__ModelDeployment = "<your-model-deployment>"
az login
```

The console examples in lessons 07, 09, 10, and 11 currently use variables directly in `Program.cs`. To keep credentials outside source files, change those assignments to read environment variables before supplying real secrets. Setting environment variables alone will not configure those existing assignments.

For an Azure Functions lesson, run the host from the folder containing `host.json`, for example:

```powershell
cd ./08_azure_functions
func start
```

## Working safely with the examples

- Keep real API keys, passwords, connection strings, and local settings out of commits. Use environment variables, .NET user secrets where configured, or an appropriate secret store.
- The Key Vault example prints the retrieved secret to the console. Use a disposable demonstration secret and avoid exposing the output in recordings or shared logs.
- Azure resources and model calls can incur charges. Delete resources you no longer need after practicing.
