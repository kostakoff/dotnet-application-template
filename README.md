# dotnet-application-template

## init instruction

- how to init this project from cratch 
```pwsh
# create dotnet project root dir
mkdir dotnet-application-template; cd dotnet-application-template

# cretae new vs project
dotnet new web -lang "C#" -n ApplicationTemplate

# create sln file
dotnet new sln --name ApplicationTemplate

# add vs project to sln file
dotnet sln .\ApplicationTemplate.sln add .\ApplicationTemplate

# set global settings
dotnet new globaljson --sdk-version 9.0.203 --roll-forward latestFeature
```
