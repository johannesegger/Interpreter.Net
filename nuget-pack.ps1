Param(
    [Parameter(Mandatory=$true)][string]$version
)
dotnet pack Interpreter.Net /p:PackageVersion=$version -o "$PSScriptRoot\dist"
