# Node-RED C# Migration Examples

This directory contains example implementations for migrating Node-RED backend and runtime to C#.

## Files

- **NodeBase.cs** - Base class for all nodes (equivalent to Node.js Node class)
- **Runtime.cs** - Main runtime class (equivalent to @node-red/runtime)
- **Program.cs** - ASP.NET Core application entry point
- **FlowsController.cs** - REST API controller for flows management
- **NodeRegistry.cs** - Node type registry and loader
- **JavaScriptNodeWrapper.cs** - Wrapper for executing JavaScript nodes in C#
- **FlowManager.cs** - Flow loading and execution manager

## Key Implementation Notes

### JavaScript Execution
- Uses **Jint** JavaScript engine for executing JavaScript-based nodes
- Wraps JavaScript code execution in C# classes
- Maintains compatibility with existing Node-RED node JavaScript code

### API Compatibility
- Controllers maintain exact API compatibility with Node-RED admin API
- Same endpoints, same request/response formats
- Editor client should work without modifications

### Architecture
- Clean separation of concerns
- Dependency injection throughout
- Async/await for all I/O operations
- Event-driven architecture for node communication

## Next Steps

1. **Implement Missing Components**:
   - Context storage (memory/file-based)
   - Settings management
   - Authentication/authorization
   - WebSocket support (SignalR)

2. **Core Nodes**:
   - Inject node
   - Debug node
   - Function node (JavaScript execution)
   - HTTP nodes

3. **Testing**:
   - Unit tests for core components
   - Integration tests with editor client
   - Performance testing

4. **Production Readiness**:
   - Error handling improvements
   - Logging enhancements
   - Configuration management
   - Deployment scripts

## Dependencies

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Jint" Version="3.0.0" />
    <PackageReference Include="Microsoft.AspNetCore.SignalR" Version="8.0.0" />
    <PackageReference Include="Newtonsoft.Json" Version="13.0.3" />
    <PackageReference Include="Serilog.AspNetCore" Version="8.0.0" />
    <PackageReference Include="Microsoft.Extensions.Logging" Version="8.0.0" />
  </ItemGroup>
</Project>
```

## Running the Migration

1. Install .NET 8 SDK
2. Create new ASP.NET Core project
3. Copy these example files
4. Install NuGet packages
5. Run `dotnet run`

## Important Considerations

- **Performance**: JavaScript execution in Jint may be slower than native Node.js
- **Compatibility**: Some Node.js-specific APIs may need shims
- **Module System**: npm package loading requires custom implementation
- **Testing**: Extensive testing needed to ensure compatibility
