param (
    [switch]$BuildAndPublish,
	$Configuration = 'Release'
)

Push-Location $PSScriptRoot

if ($BuildAndPublish -eq $true) {
    dotnet clean
    dotnet build -c $Configuration 
    dotnet publish -c $Configuration
}

Import-Module  $PSScriptRoot\src\Powershell\bin\$Configuration\net10.0\publish\Cmf.CustomerPortal.Sdk.Powershell.dll

Pop-Location