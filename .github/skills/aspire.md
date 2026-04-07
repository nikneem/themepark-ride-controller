---
name: aspire
description: 'Aspire skill covering the Aspire CLI, AppHost orchestration, service discovery, integrations, MCP server, VS Code extension, Dev Containers, GitHub Codespaces, templates, dashboard, and deployment. Use when the user asks to create, run, debug, configure, deploy, or troubleshoot an Aspire distributed application.'
---

# Aspire — Polyglot Distributed-App Orchestration

Aspire is a **code-first, polyglot toolchain** for building observable, production-ready distributed applications. It orchestrates containers, executables, and cloud resources from a single AppHost project — regardless of whether the workloads are C#, Python, JavaScript/TypeScript, Go, Java, Rust, Bun, Deno, or PowerShell.

> **Mental model:** The AppHost is a *conductor* — it doesn't play the instruments, it tells every service when to start, how to find each other, and watches for problems.

---

## 1. Researching Aspire Documentation

The Aspire team ships an **MCP server** that provides documentation tools directly inside your AI assistant.

### Aspire CLI 13.2+ (recommended — has built-in docs search)

| Tool | Description |
|---|---|
| `list_docs` | Lists all available documentation from aspire.dev |
| `search_docs` | Performs weighted lexical search across indexed documentation |
| `get_doc` | Retrieves a specific document by its slug |

---

## 2. Prerequisites & Install

| Requirement | Details |
|---|---|
| **.NET SDK** | 10.0+ (required even for non-.NET workloads — the AppHost is .NET) |
| **Container runtime** | Docker Desktop, Podman, or Rancher Desktop |
| **IDE (optional)** | VS Code + C# Dev Kit, Visual Studio 2022, JetBrains Rider |

```bash
# Windows PowerShell
irm https://aspire.dev/install.ps1 | iex

# Verify
aspire --version

# Install templates
dotnet new install Aspire.ProjectTemplates
```

---

## 3. AppHost Quick Start

```csharp
var builder = DistributedApplication.CreateBuilder(args);

// Infrastructure
var redis = builder.AddRedis("cache");
var postgres = builder.AddPostgres("pg").AddDatabase("catalog");

// .NET API
var api = builder.AddProject<Projects.CatalogApi>("api")
    .WithReference(postgres).WithReference(redis);

builder.Build().Run();
```

---

## 4. Core Concepts

| Concept | Key point |
|---|---|
| **Run vs Publish** | `aspire run` = local dev (DCP engine). `aspire publish` = generate deployment manifests. |
| **Service discovery** | Automatic via env vars: `ConnectionStrings__<name>`, `services__<name>__http__0` |
| **Resource lifecycle** | DAG ordering — dependencies start first. `.WaitFor()` gates on health checks. |
| **Resource types** | `ProjectResource`, `ContainerResource`, `ExecutableResource`, `ParameterResource` |
| **Integrations** | 144+ across 13 categories. Hosting package (AppHost) + Client package (service). |
| **Dashboard** | Real-time logs, traces, metrics, GenAI visualizer. Runs automatically with `aspire run`. |
| **Testing** | `Aspire.Hosting.Testing` — spin up full AppHost in xUnit/MSTest/NUnit. |
| **Deployment** | Docker, Kubernetes, Azure Container Apps, Azure App Service. |

---

## 5. CLI Quick Reference

| Command | Description |
|---|---|
| `aspire new <template>` | Create from template |
| `aspire run` | Start all resources locally |
| `aspire add <integration>` | Add an integration |
| `aspire publish` | Generate deployment manifests |
| `aspire deploy` | Deploy to defined targets |
| `aspire mcp init` | Configure MCP for AI assistants |
| `aspire mcp start` | Start the MCP server |

---

## 6. Key URLs

| Resource | URL |
|---|---|
| **Documentation** | https://aspire.dev |
| **Runtime repo** | https://github.com/dotnet/aspire |
| **Samples** | https://github.com/dotnet/aspire-samples |
| **Community Toolkit** | https://github.com/CommunityToolkit/Aspire |
