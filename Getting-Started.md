# Getting started with Draco
First you need to have [vscode](https://code.visualstudio.com/download) and [dotnet](https://dotnet.microsoft.com/en-us/download) installed.  
In vscode install `Draco language support` extension.
Run `dotnet new install Draco.ProjectTemplates` in terminal.  
Now you have everything installed and you can run `dotnet new console --language draco` to create new draco project. This is regular dotnet project, you will find `yourprojectname.dracoproj` file and `main.draco` file there. You can run the project with command `dotnet run`.  
Once you open any file with the `.draco` extension, you will be prompted with message to install Draco language server. Click yes.
