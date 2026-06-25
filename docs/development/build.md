# Build Instructions

## Build All

```bash
dotnet build NexusCode.slnx
```

## Build Individual Projects

```bash
# API
dotnet build src/NexusCode.Api

# Roslyn Engine
dotnet build src/NexusCode.Roslyn

# NexusGraph (Frontend)
cd NexusGraph && npm run build
```

## Run Tests

```bash
dotnet test NexusCode.slnx
```

## Publish

```bash
# API
dotnet publish src/NexusCode.Api -c Release

# NexusGraph
cd NexusGraph && npm run build
```
