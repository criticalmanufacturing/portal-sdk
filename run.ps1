param (
	$Configuration = 'Release'
)
Start-Process pwsh '-NoExit', '-c', "cd $PSScriptRoot; .\import.ps1 -BuildAndPublish -Configuration $Configuration; sleep -m 100"
