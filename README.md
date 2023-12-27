# CssCompiler

CssCompiler is a tool designed for compiling Counter-Strike: Sharp plugins, based on the old Sourcemod Compiler. This repository contains the source code for the CssCompiler application, which allows you to easily compile plugins for Counter-Strike 2. Below, you'll find some information on how to use this tool and its key features.

## Features
- Compile Counter-Strike: Sharp plugins.
- Update NuGet packages in your projects.
- Automatically download the latest version of the compiler from GitHub releases.

## Requirements
- .NET 7.0 or higher must be installed on your system to run CssCompiler.

## Usage
To use CssCompiler, follow these steps:

1. Clone or download this repository to your local machine.

2. Build the project using your preferred C# development environment. Ensure that .NET 7.0 or higher is installed and used for the build process.

3. Run the compiled executable in the command line with the following options, or use the drag-and-drop feature:
   - To compile a specific project: `CssCompiler.exe <path_to_project_folder>`
   - To compile all projects in the current directory: `CssCompiler.exe`
   - Alternatively, you can drag and drop a project folder onto the CssCompiler executable to compile it.

4. CssCompiler will scan the specified directory for C# projects (csproj files) and compile them, updating NuGet packages if necessary.

## Configuration
You can configure CssCompiler by editing the `config.json` file. Here are some key configuration options:

- `Version`: The current version of CssCompiler. //do not edit
- `Update`: Set to `true` to automatically update CssCompiler from GitHub releases.
- `Updateapis`: Set to `true` to update NuGet packages in your projects.
- `Silencelog`: Set to `true` to silence log output during compilation.

## Automatic Updates
CssCompiler can automatically check for updates on GitHub and download the latest version if available. When an update is detected, CssCompiler will replace its own executable with the new version and relaunch itself. This ensures you're always using the latest version of the compiler.

## Contributions
Contributions to this project are welcome. If you encounter any issues or have suggestions for improvements, please open an issue or submit a pull request.

Thank you for using CssCompiler!
