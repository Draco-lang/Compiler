# Getting started with Draco

If you want to experiment with Draco, you basically have 2 options: Install the latest published SDK from NuGet, or install from source. Neither are too complicated.

## Installing the latest release from NuGet

Note, that this might not reflect the latest developments, as we don't release too frequently until we hit a stable point, which hopefully will be the 1.0 milestone.

 0. First, you need to have the [.NET SDK](https://dotnet.microsoft.com/en-us/download) installed. The minimum required SDK version is the one the compiler targets, which is currently .NET 8.
 1. Run `dotnet new install Draco.ProjectTemplates` in a terminal. This installs dotnet project templates for the Draco language.
 2. Now, that you have project templates installed, you can run `dotnet new console --language draco` to create a new Draco project. This is a regular .NET project, you will find `yourprojectname.dracoproj` file and `main.draco` file there. The projectfile references the SDK and MSBuild will automatically pull this for you on the first run. If you really want, you can even skip installing the project templates by making a new projectfile and referencing `Sdk="Draco.Sdk/<version>"` there.
 3. All of the usual `dotnet` commands should work as expected. You can build with `dotnet build`, or directly run with `dotnet run`.
 4. If you want editor support for Draco, you can use our [VS Code](https://code.visualstudio.com/download) extension [Draco language support](https://marketplace.visualstudio.com/items?itemName=Draco-lang.draco-language-support). This extension adds syntax highlighting, error detection, go to definition, and other features to VS Code. Once you open any file with the `.draco` extension in VS Code, you will be prompted with a message to install the Draco language server. Click yes. This will install the language server as a global .NET tool. If you run, you will also be prompted if you want to install the debug adapter, allowing you to debug Draco programs from VS Code.

We also have an extension for [NeoVim](https://neovim.io/), you can find a tutorial on how to install it [here](https://github.com/Draco-lang/draco-nvim).

## Installing from source

Installing everything from source should also be relatively easy, as we made PowerShell scripts that you can find in the `scripts` folder. If you are not on Windows, most of the script should be trivially executable by hand. We are still looking into how to make these scripts completely cross-platform without needing to duplicate them.

 0. First, you need to have the [.NET SDK](https://dotnet.microsoft.com/en-us/download) installed. The minimum required SDK version is the one the compiler targets, which is currently .NET 8.
 1. Run `install_toolchain.ps1` and specify a folder where you want to install the SDK. This will build and install the SDK into that location. For example, you can install it into the `examples` folder to run them on your source-built SDK.
 2. If you want editor support, you can install the components with their respective scripts:
    * Language Server: `install_langserver.ps1`
    * Debug Adapter: `install_debugadapter.ps1`
    * VS Code extension: `install_vscext.ps1`
