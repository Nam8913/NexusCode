# Nexus Code Intelligence Platform - Hướng dẫn sử dụng

## Yêu cầu hệ thống

- .NET 10 SDK
- Ollama (cho embedding, tùy chọn)
- Qdrant (cho vector store, tùy chọn)

## Cài đặt

```bash
git clone <repo-url>
cd NexusCode
dotnet build
```

## Sử dụng

### 1. Index Repository (CLI)

```bash
dotnet run --project src/NexusCode.Indexer -- "D:\path\to\project"
```

Ví dụ:
```bash
# Index Unity project
dotnet run --project src/NexusCode.Indexer -- "D:\MyUnityGame"

# Index chính project này
dotnet run --project src/NexusCode.Indexer -- "D:\NexusCode"
```

Output mẫu:
```
Indexing repository: D:\MyUnityGame
Files indexed:      150
Symbols extracted:  1200
Graph nodes:        1150
Graph edges:        2300
Duration:           3.5s

Symbol table stats:
  Total symbols: 1200
  Classes: 85
  Methods: 320
  Properties: 450
  Fields: 120

Graph stats:
  Nodes: 1150
  Edges: 2300
```

### 2. Run API Server

```bash
dotnet run --project src/NexusCode.Api
```

Server chạy tại `http://localhost:5000`

### 3. REST API

#### Index Repository

```bash
curl -X POST http://localhost:5000/api/index/repository \
  -H "Content-Type: application/json" \
  -d '{"path": "D:\\MyUnityGame"}'
```

Response:
```json
{
  "success": true,
  "filesIndexed": 150,
  "symbolsExtracted": 1200,
  "graphNodesCreated": 1150,
  "graphEdgesCreated": 2300,
  "duration": "00:00:03.5000000"
}
```

#### Check Status

```bash
curl http://localhost:5000/api/index/status
```

Response:
```json
{
  "indexed": true,
  "symbols": 1200,
  "graphNodes": 1150,
  "graphEdges": 2300
}
```

#### Search Symbols

```bash
# Tìm class
curl "http://localhost:5000/api/search/symbol?query=PlayerController&kind=Type"

# Tìm method
curl "http://localhost:5000/api/search/symbol?query=Attack&kind=Method"

# Tìm property
curl "http://localhost:5000/api/search/symbol?query=Health&kind=Property"

# Tìm tất cả
curl "http://localhost:5000/api/search/symbol?query=Weapon"
```

Response:
```json
[
  {
    "name": "PlayerController",
    "fullName": "MyGame.PlayerController",
    "kind": "Type",
    "filePath": "Assets/Scripts/Player/PlayerController.cs",
    "startLine": 10,
    "score": 1.0,
    "matchType": "exact"
  }
]
```

#### Find Callers (Ai gọi method này)

```bash
curl "http://localhost:5000/api/search/callers/MyGame.Weapon.Fire"
```

Response:
```json
[
  {
    "name": "Attack",
    "fullName": "MyGame.PlayerController.Attack",
    "filePath": "Assets/Scripts/Player/PlayerController.cs",
    "depth": 1
  }
]
```

#### Find Callees (Method này gọi ai)

```bash
curl "http://localhost:5000/api/search/callees/MyGame.PlayerController.Attack"
```

Response:
```json
[
  {
    "name": "Fire",
    "fullName": "MyGame.Weapon.Fire",
    "filePath": "Assets/Scripts/Weapons/Weapon.cs",
    "depth": 1
  }
]
```

#### Find Implementations (Ai implement interface)

```bash
curl "http://localhost:5000/api/search/implementations/MyGame.IDamageable"
```

Response:
```json
[
  {
    "name": "Enemy",
    "fullName": "MyGame.Enemy",
    "filePath": "Assets/Scripts/Enemies/Enemy.cs"
  },
  {
    "name": "Player",
    "fullName": "MyGame.Player",
    "filePath": "Assets/Scripts/Player/Player.cs"
  }
]
```

#### Find Derived Types (Class nào kế thừa)

```bash
curl "http://localhost:5000/api/search/derived/MyGame.BaseEnemy"
```

Response:
```json
[
  {
    "name": "MeleeEnemy",
    "fullName": "MyGame.MeleeEnemy",
    "filePath": "Assets/Scripts/Enemies/MeleeEnemy.cs"
  },
  {
    "name": "RangedEnemy",
    "fullName": "MyGame.RangedEnemy",
    "filePath": "Assets/Scripts/Enemies/RangedEnemy.cs"
  }
]
```

#### Graph Statistics

```bash
curl http://localhost:5000/api/graph/stats
```

Response:
```json
{
  "nodes": 1150,
  "edges": 2300,
  "symbols": 1200,
  "nodeKinds": [
    { "kind": "Class", "count": 85 },
    { "kind": "Method", "count": 320 },
    { "kind": "Property", "count": 450 },
    { "kind": "Field", "count": 120 }
  ]
}
```

#### Get Nodes by Kind

```bash
# Lấy tất cả classes
curl http://localhost:5000/api/graph/nodes/Class

# Lấy tất cả methods
curl http://localhost:5000/api/graph/nodes/Method
```

#### Get Edges by Kind

```bash
# Lấy tất cả inheritance edges
curl http://localhost:5000/api/graph/edges/Inherits

# Lấy tất cả call edges
curl http://localhost:5000/api/graph/edges/Calls
```

#### Generate Mermaid Diagram

```bash
curl http://localhost:5000/api/graph/mermaid
```

Response:
```json
{
  "mermaid": "graph TD\n    ABC123[\"PlayerController\"]\n    DEF456[\"Weapon\"]\n    ABC123 --> DEF456\n    ..."
}
```

Copy output và dán vào [Mermaid Live Editor](https://mermaid.live) để xem diagram.

### 4. Ví dụ thực tế với Unity Project

Giả sử Unity project có cấu trúc:

```
Assets/
├── Scripts/
│   ├── Player/
│   │   └── PlayerController.cs
│   ├── Weapons/
│   │   └── Weapon.cs
│   ├── Projectiles/
│   │   └── Projectile.cs
│   └── Enemies/
│       └── Enemy.cs
```

Và code:

```csharp
// PlayerController.cs
public class PlayerController : MonoBehaviour
{
    public Weapon currentWeapon;
    
    void Update()
    {
        if (Input.GetButtonDown("Fire1"))
            Attack();
    }
    
    void Attack()
    {
        currentWeapon.Fire();
    }
}

// Weapon.cs
public class Weapon : MonoBehaviour
{
    public Projectile projectilePrefab;
    public int damage = 10;
    
    public void Fire()
    {
        var proj = Instantiate(projectilePrefab);
        proj.Spawn(transform.position);
    }
}

// Projectile.cs
public class Projectile : MonoBehaviour
{
    public float speed = 20f;
    
    public void Spawn(Vector3 position)
    {
        transform.position = position;
        GetComponent<Rigidbody>().velocity = transform.forward * speed;
    }
}

// Enemy.cs
public class Enemy : MonoBehaviour, IDamageable
{
    public int health = 100;
    
    public void TakeDamage(int amount)
    {
        health -= amount;
        if (health <= 0) Die();
    }
    
    void Die()
    {
        Destroy(gameObject);
    }
}

// IDamageable.cs
public interface IDamageable
{
    void TakeDamage(int amount);
}
```

**Các query hữu ích:**

```bash
# 1. Tìm tất cả MonoBehaviours
curl "http://localhost:5000/api/search/symbol?query=MonoBehaviour&kind=Type"

# 2. Weapon.Fire() được gọi ở đâu?
curl "http://localhost:5000/api/search/callers/Weapon.Fire"
# Output: PlayerController.Attack

# 3. Attack() gọi những method nào?
curl "http://localhost:5000/api/search/callees/PlayerController.Attack"
# Output: Weapon.Fire

# 4. Ai implement IDamageable?
curl "http://localhost:5000/api/search/implementations/IDamageable"
# Output: Enemy

# 5. Tìm tất cả methods có tên chứa "Damage"
curl "http://localhost:5000/api/search/symbol?query=Damage&kind=Method"

# 6. Xem graph statistics
curl http://localhost:5000/api/graph/stats
```

### 5. Sử dụng với Code Editors

#### VS Code (settings.json)

```json
{
  "mcpServers": {
    "nexus": {
      "command": "dotnet",
      "args": ["run", "--project", "D:\\NexusCode\\src\\NexusCode.Api"],
      "env": {}
    }
  }
}
```

### 6. MCP Tools (cho AI Agents)

Nếu tích hợp với AI coding assistant, các tool disponíveis:

| Tool | Mô tả | Input |
|------|-------|-------|
| `find_symbol` | Tìm symbol theo tên | `query`, `kind` |
| `find_references` | Tìm tất cả references | `symbolName` |
| `find_callers` | Tìm ai gọi method | `method`, `maxDepth` |
| `find_callees` | Tìm method gọi ai | `method`, `maxDepth` |
| `find_implementations` | Tìm implement interface | `interfaceName` |
| `find_derived_types` | Tìm class kế thừa | `typeName` |
| `search_code` | Tìm kiếm code | `query`, `maxResults` |
| `get_symbol_info` | Lấy chi tiết symbol | `symbolName` |
| `get_graph_stats` | Thống kê graph | - |
| `explain_architecture` | Giải thích kiến trúc | - |

Ví dụ:
```json
{
  "tool": "find_callers",
  "arguments": {
    "method": "Weapon.Fire",
    "maxDepth": 2
  }
}
```

## Troubleshooting

### Lỗi "No compilation found"

Đảm bảo project có file `.csproj` hợp lệ:
```bash
# Kiểm tra project có build được không
dotnet build path/to/project.csproj
```

### Lỗi "Symbols: 0"

Thư mục `bin` hoặc `obj` có thể chứa file `.cs` cũ. Scanner sẽ tự động exclude, nhưng nếu vẫn lỗi:

```bash
# Clean trước khi index
dotnet clean
dotnet build
dotnet run --project src/NexusCode.Indexer -- "path/to/project"
```

### Performance với project lớn

Với project > 10,000 files:

```bash
# Tăng parallelism
dotnet run --project src/NexusCode.Indexer -- "path/to/project" --parallelism 8
```

## Architecture

```
Code → Roslyn Analysis → Knowledge Graph → Symbol Search → MCP → Embeddings → Graph RAG
```

### Projects

| Project | Mô tả |
|---------|-------|
| NexusCode.Domain | Entities, Enums, Interfaces |
| NexusCode.Roslyn | Roslyn Engine, SymbolTable, KnowledgeGraph |
| NexusCode.Indexer | CLI Indexer |
| NexusCode.Mcp | MCP Server (10 tools) |
| NexusCode.Embedding | Ollama Embedding Engine |
| NexusCode.VectorStore | Qdrant + InMemory adapters |
| NexusCode.Database | SQLite persistence |
| NexusCode.Api | ASP.NET Core REST API |
