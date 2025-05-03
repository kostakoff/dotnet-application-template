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

# create unit test project
dotnet new mstest -n ApplicationTemplateTest

#  add vs unit test project to sln file
dotnet sln .\ApplicationTemplate.sln add .\ApplicationTemplateTest
```

## build instruction

- how to build common way
```pwsh
dotnet restore

dotnet clean

dotnet build -c Release --no-restore

dotnet test -c Release
```

- how to run
```pwsh
.\ApplicationTemplate\bin\Release\net9.0\ApplicationTemplate.exe
```

- how to build through build wrapper
```pwsh
.\build.ps1
```
