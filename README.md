<p align="center">
    <img src="https://raw.githubusercontent.com/Draco-lang/Language-suggestions/main/Resources/Logo-Long.svg#gh-light-mode-only" width=60%>
    <img src="https://raw.githubusercontent.com/Draco-lang/Language-suggestions/main/Resources/Logo-Long-Inverted.svg#gh-dark-mode-only" width=60%>
</p>

<h1 align="center">The Draco programming language compiler</h1>

<p align="center">
    <a href="http://discord.draco-lang.org/"><img alt="Discord" src="https://badgen.net/discord/members/gHfhpPFzYu?icon=discord&color=D70&label=Join+our+Discord!"></a>
    <a href="https://playground.draco-lang.org/"><img alt="Online Playground" src="https://img.shields.io/badge/Online-Playground-009a95.svg?logo=data:image/svg+xml;base64,PD94bWwgdmVyc2lvbj0iMS4wIiBlbmNvZGluZz0iVVRGLTgiPz4NCjxzdmcgd2lkdGg9IjgwIiBoZWlnaHQ9Ijc2IiB2aWV3Qm94PSIwIDAgNzkgNzYiIHhtbG5zPSJodHRwOi8vd3d3LnczLm9yZy8yMDAwL3N2ZyI+DQo8cGF0aCBkPSJtNTEgMjZzLTMtMS0yLTVsNy02LTExLTlzLTktNS0xNC02YzAgMCAzIDQgMSA1bC02IDFzMjMgMTUgMTggMjRjMCAwIDUtMSA3LTR6IiBmaWxsPSIjMDA5YTk0Ii8+DQo8cGF0aCBkPSJtNDcgMThzLTQtOC0xNy0xMWMwIDAtNy0zLTIwLTJsLTEwIDIgMTAgMXMzIDIgNSA2bC0yIDUtMyAzczctMiAxMSAwYzAgMCAyIDAgMyAyIDAgMCAyIDIgMSA0bC0yIDQgNi0zczQtMSA2IDFjMCAwIDYgMiA3IDVsNS0yLTItOHMwLTQgMi03eiIgZmlsbD0iIzAwYzhiZCIvPg0KPHBhdGggZD0ibTQzIDMxdjQ0aDE3czE0LTEgMTgtMTVjMCAwIDMtOS0yLTE4IDAgMC00LTktMTUtMTB6bTExIDljOSAwIDEyIDUgMTIgNSAzIDMgMyAxMCAzIDEwLTEgNS0zIDctMyA3LTQgNi0xMiA0LTEyIDR2LTI2em0tMTEgMzVoLTRzNC00IDQtOXoiIGZpbGw9IiNmZmYiLz4NCjxwYXRoIHRyYW5zZm9ybT0ibWF0cml4KC42NTg2IDAgMCAuNjY2NiAtNSAtODMpIiBkPSJtMjIgMjEzczEgMTEgOSAxOGMwIDAgOCA2IDE2IDcgMCAwIDkgMSAxOS0ybDEwLTQgOS04IDMtNiAyLTYtMi0yczAgNS00IDEwYzAgMC01IDYtMTIgOSAwIDAtNiA0LTE1IDUgMCAwLTggMS0xNi00IDAgMC00LTEtNy04IDAgMC0yLTQtMS03bDMtOXptMTQtN3M1LTggMTMtMTJjMCAwIDUtMyAxMS0zIDAgMCA3LTEgMTIgMSAwIDAgMjItOSAyNy0yMGwzLTExdi0xMnMxLTQgNC02YzAgMCAxLTIgNC0xbDEwIDQgNiAyIDEtMy01LTUtNC02LTEtMnMtMTAtOC0yNCAwYzAgMCAxMi01IDE3IDEgMCAwLTYgMC0xMiA1IDAgMC0zIDItNCA1IDAgMC0yIDItMyA4djRsLTQgNXMtMyA0LTEwIDdsLTIwIDVzLTEwIDItMTggOWMwIDAtOSA2LTEzIDE2IDAgMC00IDUtNCAxNnoiIGZpbGw9IiMwMGM4YmQiIHN0cm9rZT0iIzAwYzhiZCIgc3Ryb2tlLXdpZHRoPSIuMiIvPg0KPHBhdGggZD0ibTUzIDQxczEwLTcgOC05bC0xOC0xdjE0czUgMCAxMC00IiBmaWxsPSIjZmZmIi8+DQo8cGF0aCBkPSJtNDMgNThzNyAwIDExLTRjMCAwIDMtMiA0LTYgMCAwIDEtMi0yLTMgMCAwIDIgMSAxIDMgMCAwLTEgMy02IDVsLTggMi0xIDIgMSAxeiIgZmlsbD0iIzAwYzhiZCIvPg0KPHBhdGggZD0ibTQzIDMxaC00czMgNCA0IDljMCAwIDMtOCAwLTl6IiBmaWxsPSIjZmZmIi8+DQo8L3N2Zz4NCg=="></a>
    <a href="https://www.nuget.org/packages?q=Draco+owner%3ADraco-lang"><img src="https://img.shields.io/nuget/v/Draco.Compiler.svg?logo=nuget" title="Go To Nuget Package" /></a>
</p>

### What is this?
This is the repository for the work-in-progress compiler for the Draco programming language, a new .NET programming language under development. If you want further details on the language itself or want to contribute ideas, head over to the [language suggestions repository](https://github.com/Draco-lang/Language-suggestions).

### Try it out
You can either use the online [playground](https://playground.draco-lang.org/), or you can play with it locally. To install it from NuGet or right from source, look at the instructions below.

#### Installing the latest release from NuGet

Note, that this might not reflect the latest developments, as we don't release too frequently until we hit a stable point, which hopefully will be the 1.0 milestone.

 0. First, you need to have the [.NET SDK](https://dotnet.microsoft.com/en-us/download) installed. The minimum required SDK version is the one the compiler targets, which is currently .NET 8.
 1. Run `dotnet new install Draco.ProjectTemplates` in a terminal. This installs dotnet project templates for the Draco language.
 2. Now, that you have project templates installed, you can run `dotnet new console --language draco` to create a new Draco project. This is a regular .NET project, you will find `yourprojectname.dracoproj` file and `main.draco` file there. The projectfile references the SDK and MSBuild will automatically pull this for you on the first run. If you really want, you can even skip installing the project templates by making a new projectfile and referencing `Sdk="Draco.Sdk/<version>"` there.
 3. All of the usual `dotnet` commands should work as expected. You can build with `dotnet build`, or directly run with `dotnet run`.
 4. If you want editor support for Draco, you can use our [VS Code](https://code.visualstudio.com/download) extension [Draco language support](https://marketplace.visualstudio.com/items?itemName=Draco-lang.draco-language-support). This extension adds syntax highlighting, error detection, go to definition, and other features to VS Code. Once you open any file with the `.draco` extension in VS Code, you will be prompted with a message to install the Draco language server. Click yes. This will install the language server as a global .NET tool. If you run, you will also be prompted if you want to install the debug adapter, allowing you to debug Draco programs from VS Code.

We also have an extension for [NeoVim](https://neovim.io/), you can find a tutorial on how to install it [here](https://github.com/Draco-lang/draco-nvim).

#### Installing from source

Installing everything from source should also be relatively easy, as we made PowerShell scripts that you can find in the `scripts` folder. If you are not on Windows, most of the script should be trivially executable by hand. We are still looking into how to make these scripts completely cross-platform without needing to duplicate them.

 0. First, you need to have the [.NET SDK](https://dotnet.microsoft.com/en-us/download) installed. The minimum required SDK version is the one the compiler targets, which is currently .NET 8.
 1. Run `install_toolchain.ps1` and specify a folder where you want to install the SDK. This will build and install the SDK into that location. For example, you can install it into the `examples` folder to run them on your source-built SDK.
 2. If you want editor support, you can install the components with their respective scripts:
    * Language Server: `install_langserver.ps1`
    * Debug Adapter: `install_debugadapter.ps1`
    * VS Code extension: `install_vscext.ps1`
