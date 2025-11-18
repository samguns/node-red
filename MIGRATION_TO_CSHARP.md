# Node-RED Backend/Runtime Migration to C# Guide

## Overview

This guide outlines the complete migration strategy for converting Node-RED's backend and runtime from Node.js to C# (.NET). The editor client (browser-based) will remain largely unchanged, only requiring API endpoint updates.

## Architecture Mapping

### Current Node.js Architecture → C# Architecture

| Node.js Component | C# Equivalent | Technology Stack |
|-------------------|----------------|------------------|
| `@node-red/runtime` | `NodeRed.Runtime` | ASP.NET Core, JavaScript Engine (Jint/ClearScript) |
| `@node-red/editor-api` | `NodeRed.EditorApi` | ASP.NET Core Web API |
| `@node-red/registry` | `NodeRed.Registry` | C# Class Library |
| `@node-red/util` | `NodeRed.Util` | C# Class Library |
| `@node-red/nodes` | `NodeRed.Nodes` | C# Class Library with JS execution |
| Express.js | ASP.NET Core | ASP.NET Core MVC/Web API |
| Node.js EventEmitter | C# Events/MediatR | Built-in events or MediatR |

## Key Challenges & Solutions

### 1. JavaScript Execution
**Challenge**: Node-RED nodes and flows execute JavaScript code.

**Solutions**:
- **Jint** (Recommended): Pure C# JavaScript engine, lightweight, good performance
- **ClearScript**: Uses V8 engine, better compatibility but requires native binaries
- **ChakraCore**: Microsoft's JavaScript engine (deprecated but still works)

### 2. Node Module System
**Challenge**: Node.js `require()` system for loading modules.

**Solution**: 
- Create a C# module loader that wraps JavaScript files
- Use Jint's module loading capabilities
- Map npm packages to NuGet packages where possible

### 3. Asynchronous Execution
**Challenge**: Node.js callback/promise patterns.

**Solution**: 
- Use C# async/await with Task-based patterns
- Wrap JavaScript promises in C# Tasks

### 4. File System Operations
**Challenge**: Node.js `fs` module operations.

**Solution**: 
- Use `System.IO` namespace
- Create wrapper classes for Node.js-style file operations

## Project Structure

```
NodeRed.CSharp/
├── src/
│   ├── NodeRed.Runtime/           # Core runtime engine
│   │   ├── Nodes/
│   │   │   ├── NodeBase.cs        # Base node class
│   │   │   ├── NodeFactory.cs     # Node instantiation
│   │   │   └── NodeExecutor.cs    # Node execution engine
│   │   ├── Flows/
│   │   │   ├── FlowManager.cs     # Flow loading/execution
│   │   │   └── FlowValidator.cs   # Flow validation
│   │   ├── Context/
│   │   │   ├── ContextStore.cs    # Node context storage
│   │   │   └── MemoryContext.cs   # In-memory context
│   │   ├── Storage/
│   │   │   └── FlowStorage.cs     # Flow persistence
│   │   └── Runtime.cs             # Main runtime class
│   │
│   ├── NodeRed.EditorApi/          # REST API for editor
│   │   ├── Controllers/
│   │   │   ├── FlowsController.cs
│   │   │   ├── NodesController.cs
│   │   │   ├── SettingsController.cs
│   │   │   └── AuthController.cs
│   │   ├── Middleware/
│   │   │   └── AuthenticationMiddleware.cs
│   │   └── Program.cs
│   │
│   ├── NodeRed.Registry/            # Node type registry
│   │   ├── NodeRegistry.cs
│   │   ├── ModuleLoader.cs
│   │   └── TypeRegistry.cs
│   │
│   ├── NodeRed.Util/                # Shared utilities
│   │   ├── Logging/
│   │   ├── Events/
│   │   ├── I18n/
│   │   └── Helpers/
│   │
│   └── NodeRed.Nodes/               # Core nodes (C# wrappers)
│       ├── Core/
│       │   ├── InjectNode.cs
│       │   ├── DebugNode.cs
│       │   └── FunctionNode.cs
│       └── Network/
│           ├── HttpInNode.cs
│           └── HttpOutNode.cs
│
├── tests/
│   ├── NodeRed.Runtime.Tests/
│   ├── NodeRed.EditorApi.Tests/
│   └── NodeRed.Registry.Tests/
│
└── NodeRed.CSharp.sln              # Solution file
```

## Step-by-Step Migration Strategy

### Phase 1: Foundation (Weeks 1-2)
1. **Setup C# Solution Structure**
   - Create .NET 8+ solution
   - Setup project structure
   - Configure dependency injection

2. **JavaScript Engine Integration**
   - Integrate Jint or ClearScript
   - Create JS execution wrapper
   - Test basic JavaScript execution

3. **Core Utilities**
   - Port logging system
   - Port event system
   - Port i18n system

### Phase 2: Runtime Core (Weeks 3-5)
1. **Node Base Class**
   - Implement NodeBase equivalent
   - Port EventEmitter pattern
   - Implement message passing

2. **Flow Manager**
   - Flow loading from JSON
   - Flow validation
   - Flow execution engine

3. **Context System**
   - In-memory context
   - File-based context (optional)
   - Context scoping (flow/node/global)

### Phase 3: Registry & Module System (Weeks 6-7)
1. **Node Registry**
   - Type registration
   - Module loading
   - Node discovery

2. **Module Loader**
   - Load JavaScript node files
   - Handle npm packages
   - Module dependencies

### Phase 4: Editor API (Weeks 8-10)
1. **REST API Controllers**
   - Port all admin API endpoints
   - Maintain API compatibility
   - WebSocket support

2. **Authentication**
   - Port authentication system
   - OAuth2 support
   - Session management

### Phase 5: Core Nodes (Weeks 11-12)
1. **Port Core Nodes**
   - Inject, Debug, Function nodes
   - HTTP nodes
   - File nodes
   - Other core nodes

### Phase 6: Testing & Integration (Weeks 13-14)
1. **Unit Tests**
   - Port existing tests
   - Add C# specific tests

2. **Integration**
   - Test with editor client
   - Performance testing
   - Compatibility testing

## Technology Stack Recommendations

### Core Framework
- **.NET 8** or later (LTS)
- **ASP.NET Core** for Web API
- **SignalR** for WebSocket support

### JavaScript Execution
- **Jint** (Recommended) - Pure C# implementation
- **ClearScript** (Alternative) - V8-based, better compatibility

### Key NuGet Packages
```xml
<PackageReference Include="Jint" Version="3.0.0" />
<PackageReference Include="Microsoft.AspNetCore.SignalR" Version="8.0.0" />
<PackageReference Include="Newtonsoft.Json" Version="13.0.3" />
<PackageReference Include="Serilog.AspNetCore" Version="8.0.0" />
<PackageReference Include="Microsoft.Extensions.DependencyInjection" Version="8.0.0" />
<PackageReference Include="MediatR" Version="12.2.0" />
```

### Authentication
- **ASP.NET Core Identity** or
- **Passport.NET** (port of Passport.js)

## API Compatibility

**CRITICAL**: Maintain 100% API compatibility with existing Node-RED admin API to ensure the editor client works without changes.

Key endpoints to maintain:
- `GET /flows` - Get flows
- `POST /flows` - Set flows
- `GET /nodes` - Get node list
- `GET /nodes/:id` - Get node info
- `GET /settings` - Get settings
- `POST /settings` - Update settings
- WebSocket events for real-time updates

## Performance Considerations

1. **JavaScript Engine**: Jint performs well for most use cases
2. **Async Operations**: Use async/await throughout
3. **Message Passing**: Optimize message routing between nodes
4. **Context Storage**: Use efficient data structures
5. **Caching**: Cache compiled JavaScript code

## Migration Path Options

### Option A: Big Bang Migration
- Migrate everything at once
- **Pros**: Clean break, no hybrid system
- **Cons**: High risk, long development time

### Option B: Gradual Migration (Recommended)
- Keep Node.js runtime running
- Migrate API layer first
- Gradually migrate runtime components
- **Pros**: Lower risk, can test incrementally
- **Cons**: More complex deployment initially

### Option C: Hybrid Approach
- C# backend for API
- Node.js runtime for execution (interop)
- **Pros**: Faster initial migration
- **Cons**: Still requires Node.js runtime

## Next Steps

1. Review this guide
2. Setup development environment
3. Create proof-of-concept with Jint
4. Implement core runtime components
5. Build REST API layer
6. Test with existing editor client

## Estimated Timeline

- **Proof of Concept**: 2-3 weeks
- **Core Runtime**: 6-8 weeks
- **Full Migration**: 16-20 weeks
- **Testing & Polish**: 4-6 weeks

**Total**: 6-8 months for complete migration
