# WebSharper Push API Binding

This repository provides an F# [WebSharper](https://websharper.com/) binding for the [Push API](https://developer.mozilla.org/en-US/docs/Web/API/Push_API), enabling seamless integration of push notifications into WebSharper applications.

## Repository Structure

The repository consists of two main projects:

1. **Binding Project**:

   - Contains the F# WebSharper binding for the Push API.

2. **Sample Project**:
   - Demonstrates how to use the Push API with WebSharper syntax.

## Installation

To use this package in your WebSharper project, add the NuGet package:

```bash
   dotnet add package WebSharper.Push
```

## Building

### Prerequisites

- [.NET SDK](https://dotnet.microsoft.com/download) installed on your machine.

### Steps

1. Clone the repository:

   ```bash
   git clone https://github.com/dotnet-websharper/Push.git
   cd Push
   ```

2. Build the Binding Project:

   ```bash
   dotnet build WebSharper.Push/WebSharper.Push.fsproj
   ```

3. Build and Run the Sample Project:

   ```bash
   cd WebSharper.Push.Sample
   dotnet build
   dotnet run
   ```
