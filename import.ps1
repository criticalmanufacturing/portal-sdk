param (
    $BuildAndPublish = $false
)

Push-Location $PSScriptRoot

if ($BuildAndPublish -eq $true) {
    dotnet clean
    dotnet build -c Debug 
    dotnet publish -c Debug
}

Import-Module  C:\Product\portal-sdk\src\Powershell\bin\Debug\netstandard2.0\publish\Cmf.CustomerPortal.Sdk.Powershell.dll

Pop-Location