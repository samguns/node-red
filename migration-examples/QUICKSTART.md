# Quick Start Guide: Node-RED C# Migration

## Prerequisites

- .NET 8 SDK or later
- Visual Studio 2022 / VS Code / Rider
- Node.js (for reference/testing existing Node-RED)

## Step 1: Create New Project

```bash
mkdir NodeRed.CSharp
cd NodeRed.CSharp
dotnet new web
dotnet add package Jint
dotnet add package Microsoft.AspNetCore.SignalR
dotnet add package Newtonsoft.Json
dotnet add package Serilog.AspNetCore
dotnet add package Serilog.Sinks.Console
```

## Step 2: Project Structure

```
NodeRed.CSharp/
├── src/
│   ├── NodeRed.Runtime/
│   ├── NodeRed.EditorApi/
│   ├── NodeRed.Registry/
│   └── NodeRed.Util/
├── Program.cs
└── NodeRed.CSharp.csproj
```

## Step 3: Copy Example Files

Copy all files from `migration-examples/` directory to your project.

## Step 4: Update Program.cs

Replace the default `Program.cs` with the example from `migration-examples/Program.cs`.

## Step 5: Fix Compilation Issues

The example files have some dependencies that need to be resolved:

1. **Add missing using statements**:
   - `using NodeRed.Runtime.Flows;`
   - `using NodeRed.Runtime.Nodes;`
   - etc.

2. **Fix NodeRegistry logger access**:
   ```csharp
   // In NodeRegistry.cs, add:
   public ILogger<T> GetLogger<T>() => _logger;
   ```

3. **Fix FlowManager constructor**:
   ```csharp
   // FlowManager needs NodeRegistry parameter
   ```

## Step 6: Update Startup Configuration

Ensure dependency injection is properly configured:

```csharp
services.AddSingleton<NodeRegistry>();
services.AddSingleton<FlowManager>();
services.AddSingleton<FlowStorage>();
services.AddSingleton<NodeRedRuntime>();
services.AddScoped<IEditorApiService, EditorApiService>();
```

## Step 7: Run

```bash
dotnet run
```

The application should start on `http://localhost:1880`.

## Step 8: Test with Editor Client

1. Copy the editor client from `packages/node_modules/@node-red/editor-client/public/`
2. Place it in `wwwroot/` directory
3. Access `http://localhost:1880` in browser
4. Editor should load and connect to C# backend

## Common Issues & Solutions

### Issue: JavaScript execution fails
**Solution**: Ensure Jint is properly initialized with Node-RED API shims.

### Issue: Editor client can't connect
**Solution**: Check CORS settings and API endpoint compatibility.

### Issue: Nodes not loading
**Solution**: Ensure `node_modules` directory is accessible and nodes are properly registered.

### Issue: Performance issues
**Solution**: Consider using ClearScript instead of Jint for better performance.

## Next Steps

1. **Implement missing core nodes** (Function, HTTP, etc.)
2. **Add context storage** (memory/file-based)
3. **Implement authentication**
4. **Add WebSocket support** (SignalR)
5. **Performance optimization**
6. **Comprehensive testing**

## Testing Strategy

1. **Unit Tests**: Test individual components
2. **Integration Tests**: Test API endpoints
3. **Compatibility Tests**: Test with existing Node-RED flows
4. **Performance Tests**: Compare with Node.js implementation

## Migration Checklist

- [ ] Core runtime implemented
- [ ] Node registry working
- [ ] Flow manager working
- [ ] JavaScript execution working
- [ ] Editor API endpoints implemented
- [ ] WebSocket communication working
- [ ] Core nodes implemented
- [ ] Authentication working
- [ ] Storage system working
- [ ] Tests passing
- [ ] Performance acceptable
- [ ] Documentation complete

## Resources

- [Jint Documentation](https://github.com/sebastienros/jint)
- [ASP.NET Core Documentation](https://docs.microsoft.com/en-us/aspnet/core/)
- [Node-RED API Documentation](https://nodered.org/docs/api/)
