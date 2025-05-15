# LightBox 项目代码状态文档

## 1. 目录结构

```
LightBox/
├── doc/                      # 文档目录
├── LightBox.Core/            # 核心逻辑项目
│   ├── Host/                 # 主机相关代码
│   ├── Models/               # 数据模型
│   ├── Services/             # 服务实现
│   │   ├── Implementations/  # 服务实现类
│   │   └── Interfaces/       # 服务接口定义
├── LightBox.PluginContracts/ # 插件契约项目
├── LightBox.WebViewUI/       # WebView UI 前端项目
│   ├── public/               # 静态资源
│   └── src/                  # 源代码
│       ├── assets/           # 资源文件
│       ├── locales/          # 国际化文件
│       ├── services/         # 服务
│       └── stores/           # 状态管理
├── LightBox.WPF/             # WPF 主应用项目
├── TestPlugins/              # 测试插件
│   ├── PluginA/              # 测试插件 A
│   └── PluginB/              # 测试插件 B
└── LightBox.sln              # 解决方案文件
```

## 2. 代码文件实现状态

### LightBox.PluginContracts

#### ILightBoxPlugin.cs
- **已实现**: 插件接口定义
- **暴露接口**:
  - `string Id { get; }`: 插件标识符
  - `void Initialize(ILightBoxHostContext hostContext, string instanceId, string configurationJson)`: 初始化插件
  - `void Start()`: 启动插件
  - `void Stop()`: 停止插件
  - `Task<object> ExecuteCommand(string commandName, object payload)`: 执行命令

#### ILightBoxHostContext.cs
- **已实现**: 主机上下文接口
- **暴露接口**:
  - 未详细暴露的方法
  
#### LogLevel.cs
- **已实现**: 日志级别枚举
- **暴露类型**: 日志级别枚举

### LightBox.Core

#### Models/

##### ApplicationSettings.cs
- **已实现**: 应用程序设置模型
- **暴露类型**:
  - 设置属性包括插件扫描目录和日志设置

##### Workspace.cs
- **已实现**: 工作区模型
- **暴露类型**:
  - 工作区属性包括ID、名称、创建时间、插件实例列表等

##### WorkspaceInfo.cs
- **已实现**: 工作区基本信息模型
- **暴露类型**:
  - 基本工作区元数据属性

##### PluginDefinition.cs
- **已实现**: 插件定义模型
- **暴露类型**:
  - 插件元数据如ID、名称、版本、类型等

##### PluginType.cs
- **已实现**: 插件类型枚举
- **暴露类型**:
  - `CSharpLibrary`: C#库插件
  - `ExternalProcess`: 外部进程插件

##### PluginInstanceStatus.cs
- **已实现**: 插件实例状态枚举
- **暴露类型**:
  - 描述插件实例生命周期的各个状态（如Created, Initializing, Running, Stopped等）

##### PluginInstance.cs
- **已实现**: 插件实例模型
- **暴露类型**:
  - 插件实例属性包括ID、状态、配置、时间戳等
  - 内部引用C#库插件对象或外部进程

##### PluginInstanceInfo.cs
- **已实现**: 插件实例信息模型（用于API返回）
- **暴露类型**:
  - 插件实例的摘要信息，如ID、名称、状态等

#### Services/Interfaces/

##### ILoggingService.cs
- **已实现**: 日志服务接口
- **暴露接口**:
  - 日志记录方法(Error, Warning, Info, Debug)

##### IApplicationSettingsService.cs
- **已实现**: 应用设置服务接口
- **暴露接口**:
  - `Task<ApplicationSettings> LoadSettingsAsync()`: 加载设置
  - `Task SaveSettingsAsync(ApplicationSettings settings)`: 保存设置

##### IWorkspaceService.cs
- **已实现**: 工作区服务接口
- **暴露接口**:
  - 工作区CRUD操作
  - 活动工作区管理

##### IPluginService.cs
- **已实现**: 插件服务接口
- **暴露接口**:
  - 插件发现方法
  - 插件实例生命周期管理方法

#### Services/Implementations/

##### SimpleFileLogger.cs
- **已实现**: 基本文件日志实现
- **实现接口**: `ILoggingService`

##### ApplicationSettingsService.cs
- **已实现**: 应用设置服务实现
- **实现接口**: `IApplicationSettingsService`

##### WorkspaceManager.cs
- **已实现**: 工作区管理服务
- **实现接口**: `IWorkspaceService`
- **主要功能**:
  - 工作区列表管理
  - 活动工作区设置
  - 工作区文件持久化

##### PluginManager.cs
- **已实现**: 插件管理服务
- **实现接口**: `IPluginService`
- **主要功能**:
  - 插件发现与加载
  - 插件实例创建与初始化
  - C#库插件和外部进程插件生命周期管理
  - 插件实例状态跟踪

### LightBox.WPF

#### MainWindow.xaml/MainWindow.xaml.cs
- **已实现**: 主窗口UI和逻辑
- **主要功能**:
  - WebView2控件初始化
  - 应用程序主框架

#### LightBoxJsBridge.cs
- **已实现**: JavaScript通信桥接
- **暴露方法**:
  - 应用设置管理
  - 工作区管理
  - 插件管理
  - 允许WebView UI与后端交互的API

#### App.xaml/App.xaml.cs
- **已实现**: 应用程序入口
- **主要功能**:
  - 应用初始化
  - 服务注册

### LightBox.WebViewUI

#### src/services/lightboxApi.js
- **已实现**: 前端API服务
- **暴露方法**:
  - 初始化WebMessage监听器
  - 应用设置管理
  - 工作区管理
  - 插件管理

#### src/stores/
- **已实现**: 状态管理
- **主要文件**:
  - `settingsStore.js`: 应用设置状态
  - `workspaceStore.js`: 工作区状态
  - `pluginStore.js`: 插件状态

#### src/App.svelte
- **已实现**: 主应用组件
- **功能**:
  - 基本UI框架
  - 设置显示

### TestPlugins

#### PluginA/
- **已部分实现**: C#库插件示例
- **包含文件**:
  - manifest.json: 插件清单

#### PluginB/
- **已部分实现**: 外部进程插件示例
- **包含文件**:
  - manifest.json: 插件清单

## 3. 接口暴露详情

### LightBox.Core.Services.Interfaces.ILoggingService
```csharp
public interface ILoggingService
{
    void LogError(string message, Exception exception = null);
    void LogWarning(string message);
    void LogInfo(string message);
    void LogDebug(string message);
}
```

### LightBox.Core.Services.Interfaces.IApplicationSettingsService
```csharp
public interface IApplicationSettingsService
{
    Task<ApplicationSettings> LoadSettingsAsync();
    Task SaveSettingsAsync(ApplicationSettings settings);
}
```

### LightBox.Core.Services.Interfaces.IWorkspaceService
```csharp
public interface IWorkspaceService
{
    Task<List<WorkspaceInfo>> GetWorkspacesAsync();
    Task<WorkspaceInfo> CreateWorkspaceAsync(string name, string icon);
    Task<Workspace> GetWorkspaceAsync(string workspaceId);
    Task<Workspace> GetActiveWorkspaceAsync();
    Task SetActiveWorkspaceIdAsync(string workspaceId);
    Task SaveWorkspaceAsync(Workspace workspace);
    Task DeleteWorkspaceAsync(string workspaceId);
}
```

### LightBox.Core.Services.Interfaces.IPluginService
```csharp
public interface IPluginService
{
    Task<List<PluginDefinition>> DiscoverPluginsAsync();
    Task<PluginDefinition?> GetPluginDefinitionByIdAsync(string pluginId);
    
    // 插件实例生命周期管理
    Task<PluginInstanceInfo> CreatePluginInstanceAsync(string pluginId, string workspaceId, string initialConfigurationJson);
    Task<bool> InitializePluginInstanceAsync(string instanceId);
    Task<bool> StartPluginInstanceAsync(string instanceId);
    Task<bool> StopPluginInstanceAsync(string instanceId);
    Task<bool> DisposePluginInstanceAsync(string instanceId);
    
    // 插件实例查询
    Task<PluginInstanceStatus> GetPluginInstanceStatusAsync(string instanceId);
    Task<PluginInstanceInfo> GetPluginInstanceInfoAsync(string instanceId);
    Task<IEnumerable<PluginInstanceInfo>> GetPluginInstancesByWorkspaceAsync(string workspaceId);
    Task<IEnumerable<PluginInstanceInfo>> GetAllActivePluginInstancesAsync();
}
```

### LightBox.WPF.LightBoxJsBridge (前端可用API)
```javascript
// 应用设置
getApplicationSettings()
saveApplicationSettings(settings)

// 插件管理
getAllPluginDefinitions()

// 工作区管理
getWorkspaces()
createWorkspace(name, icon)
setActiveWorkspace(workspaceId)
getActiveWorkspace()
updateWorkspace(workspaceJson)
deleteWorkspace(workspaceId)
```

## 4. 实现进度总结

### 完成度较高的模块
- 核心接口定义 (Interfaces)
- 基础数据模型 (Models)
- 工作区管理功能
- JavaScript桥接机制
- 应用设置管理
- 插件实例生命周期管理

### 部分完成的模块
- 测试插件实现
- JavaScript前端对插件实例管理的调用

### 待完成的模块
- 完整的WebView UI实现
- 动态配置表单
- 插件通信机制的完整实现 
- JavaScript桥接插件实例生命周期管理API的暴露 