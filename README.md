# ProblemsResponse 项目

## 项目概述
ProblemsResponse 是一个自动化工具，用于定期检查Gitea上的问题(Issues)，使用阿里云DashScope大模型分析问题内容，并自动回复和关闭问题。支持文件操作功能，包括读取、搜索和替换文件内容。

## 功能特性
- 定期检查Gitea上的开放问题(默认每10秒检查一次)
- 使用阿里云DashScope API进行问题分析
- 自动回复问题并关闭
- 支持文件操作：
  - 读取文件内容(read_file)
  - 搜索文件(search_files)
- 支持多轮对话处理复杂问题

## 配置要求
- .NET 8.0 SDK
- 阿里云DashScope API密钥
- Gitea访问令牌

## 安装部署
1. 克隆项目仓库：
   ```bash
   git clone https://git
   ```
2. 进入项目目录：
   ```bash
   cd ProblemsResponse
   ```
3. 恢复NuGet包：
   ```bash
   dotnet restore
   ```
4. 编译项目：
   ```bash
   dotnet build
   ```

## 使用说明
1. 修改Program.cs中的配置参数：
   ```csharp
   const string apiUrl = "http://git.xxx.com/api/v1/";
   const string owner = "USERS";
   const string repo = "Problems";
   const string token = "your_gitea_token";
   const string aiUrl = "https://dashscope.aliyuncs.com/compatible-mode/v1/chat/completions";
   const string aiKey = "your_dashscope_api_key";
   const string aiModel = "qwen3-235b-a22b";
   ```
2. 运行项目：
   ```bash
   dotnet run
   ```

## 依赖项
- DiffMatchPatch 3.0.0
- DiffPlex 1.8.0
- Newtonsoft.Json 13.0.3

## 注意事项
1. 确保Gitea API和DashScope API可访问
2. 文件操作默认基于路径：E:\Codes\FocusNew
3. 替换文件操作需要精确匹配内容
4. 请妥善保管API密钥和访问令牌
5. 项目默认运行在Windows环境，如需在其他平台运行可能需要调整路径处理

## 文件操作示例
### 读取文件
```xml
<read_file>
<path>src/main.js</path>
</read_file>
```

### 搜索文件
```xml
<search_files>
<path>src</path>
<regex>.*\.js$</regex>
<file_pattern>*.js</file_pattern>
</search_files>
```
