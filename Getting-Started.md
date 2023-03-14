# Getting started with Draco
First, you need to have the [.NET SDK](https://dotnet.microsoft.com/en-us/download) installed.  
Run `dotnet new install Draco.ProjectTemplates` in a terminal. This installs dotnet project templates for the Draco language.  
Now, that you have project templates installed, you can run `dotnet new console --language draco` to create a new Draco project. This is a regular .NET project, you will find `yourprojectname.dracoproj` file and `main.draco` file there. You can run the project with the command `dotnet run`. 
If you want editor support for Draco, you can use our [VS Code](https://code.visualstudio.com/download) `Draco language support` extension. This extension adds syntax highlighting, error detection, go to definition, and other features to VS Code.
Once you open any file with the `.draco` extension in VS Code, you will be prompted with a message to install the Draco language server. Click yes.  
We also have an extension for [NeoVim](https://neovim.io/), you can find a tutorial on how to install it [here](https://github.com/Draco-lang/draco-nvim).
