# Development Setup

## Prerequisites

- .NET 10 SDK
- Node.js 18+ (for NexusGraph)
- Ollama (optional, for embeddings)

## Backend Setup

```bash
# Clone repository
git clone <repo-url>
cd NexusCode

# Build solution
dotnet build

# Run tests
dotnet test

# Run API
dotnet run --project src/NexusCode.Api

# Run Indexer CLI
dotnet run --project src/NexusCode.Indexer -- "D:\path\to\project"
```

## Frontend Setup (NexusGraph)

```bash
cd NexusGraph

# Install dependencies
npm install

# Run dev server
npm run dev

# Build for production
npm run build
```

## Running Together

**Terminal 1 - API:**
```bash
dotnet run --project src/NexusCode.Api
```

**Terminal 2 - NexusGraph:**
```bash
cd NexusGraph && npm run dev
```

**Terminal 3 - Index Repository:**
```bash
Invoke-RestMethod -Uri "http://localhost:5000/api/index/repository" `
  -Method POST -ContentType "application/json" `
  -Body '{"path": "D:\\path\\to\\project"}'
```

Open `http://localhost:3000`
