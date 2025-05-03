[CmdletBinding()]
Param(
    [Parameter(Position=0, Mandatory=$false, ValueFromRemainingArguments=$true)]
    [string[]] $BuildArguments
)

Write-Output "PowerShell $($PSVersionTable.PSEdition) version $($PSVersionTable.PSVersion)"

Set-StrictMode -Version 2.0; $ErrorActionPreference = "Stop"; $ConfirmPreference = "None"
trap { Write-Error $_ -ErrorAction Continue; exit 1 }

###########################################################################
# CONFIGURATION
###########################################################################

$PSScriptRoot = Split-Path $MyInvocation.MyCommand.Path -Parent
$BuildProjectFile = "$PSScriptRoot\build\_build.csproj"
$DotNetGlobalFile = "$PSScriptRoot\global.json"

$env:APP_VERSION = (Get-Content version.txt).Trim()
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = 1
$env:DOTNET_CLI_TELEMETRY_OPTOUT = 1
$env:DOTNET_MULTILEVEL_LOOKUP = 0

###########################################################################
# EXECUTION
###########################################################################

function ExecSafe([scriptblock] $cmd) {
    & $cmd
    if ($LASTEXITCODE) { exit $LASTEXITCODE }
}

# If dotnet CLI is installed globally and it matches requested version, use for execution
$dotnet = Get-Command "dotnet" -ErrorAction SilentlyContinue
if ($null -ne $dotnet -and $(dotnet --version) -and $LASTEXITCODE -eq 0) {
    $env:DOTNET_EXE = $dotnet.Path
} else {
    # exception
}

Write-Output "Microsoft (R) .NET Core SDK version $(& $env:DOTNET_EXE --version) `n"

Write-Output "Build NUKE wrapper $BuildProjectFile `n"
ExecSafe { & $env:DOTNET_EXE build $BuildProjectFile /nodeReuse:false /p:UseSharedCompilation=false -nologo -clp:NoSummary }

Write-Output "Run NUKE wrapper $BuildProjectFile with args $BuildArguments `n"
ExecSafe { & $env:DOTNET_EXE run --project $BuildProjectFile --no-build -- $BuildArguments }
