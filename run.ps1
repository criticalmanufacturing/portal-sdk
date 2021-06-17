param (
	$Configuration = 'Release'
)
Start-Process pwsh '-c', "cd $PSScriptRoot; .\import.ps1 -BuildAndPublish -Configuration $Configuration; sleep 100"