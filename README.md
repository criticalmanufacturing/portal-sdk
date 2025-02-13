# portal-sdk
Customer Portal SDK that allows the creation of automation script for integration with CM DevOps Center.

# Pre-Requisites
Make sure to run the Powershell Cmdlets from in Powershell Core 7.1.3 or above.

# Documentation

The Customer Portal SDK has 2 versions that can be used:
 - <a href="#console">```Console```</a> - this version works as a standalone executable. To use it run the executable in a console or powershell with the appropriate commands.
 - <a href="#powershell">```Powershell```</a> - this version compiles into a DLL with a cmdlet for each command. To use it, import the DLL into a powershell and then call the desired cmdlet. 

**Important:** You need to authenticate before running other commands/cmdlets. There are two ways to achieve this:
1. Provide a ```CM_PORTAL_TOKEN``` environment variable - variable read from the system where the application is run.
1. Explicit login operation - login using a PAT as a parameter or in an interactive way if none is provided. **This alternative caches the Auth Token in a file** on the host filesystem, under _{AppDataFolder}/cmfportal/cmfportaltoken_. The command is ```login``` and the cmdlet is ```Set-Login```.

You can get the tool from the Releases section in GitHub, from npm using ```npm install @criticalmanufacturing/portal --global``` or by compiling the source code manually.

Below we will provide documentation on each version's usage and commands. This documentation provides the same information as the documentation built-in to the tools themselves. The way to access it depends on the version used but will be explained appropriately below. Since the information is the same here in this README and on the tools themselves you can decide to use whichever you prefer without the fear of missing out on any information.

## Console

- If you downloaded the zip from the Releases section, you just need to run the executable ```cmf-portal.exe``` with a valid command and/or option(s).

- If you installed from npm using ```npm install @criticalmanufacturing/portal --global``` you can use the tool from any console on any directory. 

- If you're not using the Release published on GitHub you'll have to first compile the solution and then run the output executable with a valid command and/or option(s). Note that some commands require certain options in order to execute.

Usage: 
  - Executable from Release or manually compiled:
    - ```.\cmf-portal.exe [options]```
    - ```.\cmf-portal.exe [command] [commandOptions] ```
  - Installed from npm:  
    - ```cmf-portal [options]```
    - ```cmf-portal [command] [commandOptions] ```

Options:
  - ```--version```         Show version information
  - ```-?, -h, --help```    Show help and usage information

Commands:
  - <a href="#checkagentconnection">```checkagentconnection```</a> - Check if an Infrastructure Agent is connected
  - <a href="#createinfrastructure">```createinfrastructure```</a> - Creates a customer Infrastructure
  - <a href="#deployagent">```deployagent```</a> - Creates and deploys a new Infrastructure Agent
  - <a href="#deploy">```deploy```</a> - Creates and deploys a new Customer Environment
  - <a href="#download-artifacts">```download-artifacts```</a> - Downloads all Deployment Artifacts of a specific Customer Environment from the Customer Portal
  - <a href="#install-app">```install-app```</a> - Installs an App in a previous deployed Convergence Customer Environment
  - <a href="#login">```login```</a> - Log in to the CM Portal
  - <a href="#publish">```publish```</a> - Publishes one or more Deployment Manifests into Customer Portal
  - <a href="#publish-package">```publish-package```</a> - Publishes one or more Customization Packages into Customer Portal


Examples:
  - ```cmf-portal.exe -h``` - displays help about the tool's usage, available flags and commands.
  - ```cmf-portal.exe checkagentconnection -n <agent-name>``` - Check if the Infrastructure Agent named <agent-name> is connected
  - ```cmf-portal.exe checkagentconnection -h``` - Displays help about the command checkagentconnection including a brief description of its function, how to use it and what options are available and/or required.

Below we will show the documentation for each command.

### checkagentconnection

Check if an Infrastructure Agent is connected

Equivalent to the Powershell cmdlet <a href="#get-agentconnection">Get-AgentConnection</a>

Usage: ```cmf-portal checkagentconnection [options]```

Options:  
  - ```-v, --verbose``` - Show detailed logging  
  - ```-n, --agent-name, --name <agent-name>``` - **REQUIRED** - The name of the Infrastructure Agent  
  - ```-?, -h, --help``` - Show help and usage information  

### createinfrastructure

Creates a customer Infrastructure

Equivalent to the Powershell cmdlet <a href="#new-infrastructure">New-Infrastructure</a>

Usage: ```cmf-portal createinfrastructure [options]```

Options:
  - ```-v, --verbose``` - Show detailed logging
  - ```-n, --name <name>``` - The name of the Customer Infrastructure to be created
  - ```-s, --site <site>``` - **deprecated** - Name of a Site used to match a Customer with the Customer Infrastructure
  - ```-c, --customer <customer>``` - Name of the Customer associated with the Customer Infrastructure
  - ```--ignore-if-exists``` - Flag that ignores a throw if an error of type 'Customer Infrastructure already exist' occurs
  - ```-params, --parameters <filePath>``` - Path to parameters json file that includes parameters for the Customer Infrastructure
  - ```-?, -h, --help``` - Show help and usage information

### deployagent

Creates and deploys a new Infrastructure Agent

Equivalent to the Powershell cmdlet <a href="#new-infrastructureagent">New-InfrastructureAgent</a>

Usage: ```cmf-portal deployagent [options]```

Options:
  - ```-v, --verbose``` - Show detailed logging
  - ```--replace-tokens <MyToken=value MyToken2=value2>``` - Replace the tokens specified in the input files using the proper syntax (e.g. #{MyToken}#) with the specified values. E.g. MyToken=value MyToken2=value2.
  - ```-ci, --customer-infrastructure-name <customer-infrastructure-name>``` - Name of the existing Customer Infrastructure
  - ```-n, --name <name>``` - Name of the new Customer Environment. --name is also supported
  - ```-a, --alias <alias>``` - Command Alias
  - ```-d, --description <description>``` - Description of the new Customer Environment
  - ```-params, --parameters <parameters>``` - Path to parameters file that describes the Customer Environment
  - ```-type <Development|Production|Staging|Testing>``` - Type of the Customer Environment to deploy [default: Development]
  - ```-trg, --target <AzureKubernetesServiceTarget|dockerswarm|KubernetesOnPremisesTarget|KubernetesRemoteTarget|OpenShiftOnPremisesTarget|OpenShiftRemoteTarget|portainer>``` - **REQUIRED** - Name of the Deployment Target to use for the Customer Environment [default: dockerswarm]
  - ```-o, --output <output>``` - Directory to place any artifacts generated by the deployment
  - ```-i, --interactive``` - Flag that controls if the user should be prompted to go to the portal to initialize the installation manually
  - ```-tov, --terminateOtherVersions``` - Flag that controls if all the other versions of the Customer Environment should be terminated
  - ```-to, --deploymentTimeoutMinutes <deploymentTimeoutMinutes>``` - Number of minutes that are allowed to wait for the deployment to succeed. The default is 360 minutes.
  - ```-tombm, --deploymentTimeoutMinutesToGetSomeMBMsg <deploymentTimeoutMinutesToGetSomeMBMsg>``` - Timeout, in minutes, that the SDK client waits to receive any message from the portal via Message Bus. The default is 30 minutes.
  - ```-tovr, --terminateOtherVersionsRemove``` - Flag that controls if the deployments of the versions of the Customer Environment that will be terminated should be removed. Requires the terminateOtherVersions flag.
  - ```-tovrv, --terminateOtherVersionsRemoveVolumes``` - Flag that controls if the volumes of the versions of the Customer Environment that will be terminated should be removed. Requires the terminateOtherVersions and terminateOtherVersionsRemove flags.
  - ```-?, -h, --help``` - Show help and usage information

### deploy

Creates and deploys a new Customer Environment

Equivalent to powershell cmdlet <a href="#new-environment">New-Environment</a>

Usage: ```cmf-portal deploy [options]```

Options:
  - ```-v, --verbose``` - Show detailed logging
  - ```--replace-tokens <MyToken=value MyToken2=value2>``` - Replace the tokens specified in the input files using the proper syntax (e.g. #{MyToken}#) with the specified values. E.g. MyToken=value MyToken2=value2.
  - ```-ci, --customer-infrastructure-name <customer-infrastructure-name>``` - Name of the existing Customer Infrastructure
  - ```-n, --name <name>``` - Name of the new Customer Environment. --name is also supported
  - ```-a, --alias <alias>``` - Command Alias
  - ```-d, --description <description>``` - Description of the new Customer Environment
  - ```-params, --parameters <parameters>``` - Path to parameters file that describes the Customer Environment
  - ```-type <Development|Production|Staging|Testing>``` - Type of the Customer Environment to deploy [default: Development]
  - ```-trg, --target <AzureKubernetesServiceTarget|dockerswarm|KubernetesOnPremisesTarget|KubernetesRemoteTarget|OpenShiftOnPremisesTarget|OpenShiftRemoteTarget|portainer>``` - **REQUIRED** - Name of the Deployment Target to use for the Customer Environment [default: dockerswarm]
  - ```-o, --output <output>``` - Directory to place any artifacts generated by the deployment
  - ```-i, --interactive``` - Flag that controls if the user should be prompted to go to the portal to initialize the installation manually
  - ```-s, --site <site>``` - **REQUIRED** - Name of the Site associated with the Customer
  - ```-pck, --package <package>``` - **REQUIRED** - Name of the Deployment Package to use for the Customer Environment
  - ```-lic, --license <license>``` - **REQUIRED** - Comma-separated names of the Licenses' Unique Name
  - ```-tov, --terminateOtherVersions``` - Flag that controls if all the other versions of the Customer Environment should be terminated
  - ```-to, --deploymentTimeoutMinutes <deploymentTimeoutMinutes>``` - Number of minutes that are allowed to wait for the deployment to succeed. The default is 360 minutes.
  - ```-tombm, --deploymentTimeoutMinutesToGetSomeMBMsg <deploymentTimeoutMinutesToGetSomeMBMsg>``` - Timeout, in minutes, that the SDK client waits to receive any message from the portal via Message Bus. The default is 30 minutes.
  - ```-tovr, --terminateOtherVersionsRemove``` - Flag that controls if the deployments of the versions of the Customer Environment that will be terminated should be removed. Requires the terminateOtherVersions flag.
  - ```-tovrv, --terminateOtherVersionsRemoveVolumes``` - Flag that controls if the volumes of the versions of the Customer Environment that will be terminated should be removed. Requires the terminateOtherVersions and terminateOtherVersionsRemove flags.
  - ```-?, -h, --help``` - Show help and usage information

### download-artifacts

Downloads all Deployment Artifacts of a specific Customer Environment from the Customer Portal.

Usage: ```cmf-portal download-artifacts [options]```

Options:
  - ```-v, --verbose``` - Show detailed logging
  - ```-n, --name <name>``` - Name of the new Customer Environment. --name is also supported
  - ```-o, --output <output>``` - Directory to place all artifacts downloaded from the Customer Portal.
  - ```-?, -h, --help``` - Show help and usage information

### install-app

Installs an App in a previous deployed Convergence Customer Environment.

Usage: ```cmf-portal install-app [options]```

Options:
  - ```-v, --verbose``` - Show detailed logging.
  - ```--replace-tokens <MyToken=value MyToken2=value2>``` - Replace the tokens specified in the input files using the proper syntax (e.g. #{MyToken}#) with the specified values. E.g. MyToken=value MyToken2=value2.
  - ```-n, --name <name>``` - **REQUIRED** - The name of the App to install.
  - ```-av, --app-version <app-version>``` - **REQUIRED** - The version of the App to install.
  - ```-ce, --customer-environment <customer-environment>``` - **REQUIRED** - The name of a Convergence Customer Environment to install the App on.
  - ```-lic, --license <license>``` - **REQUIRED** - Name of the License to use for the App.
  - ```-params, --parameters <parameters>``` - Path to parameters file that describes the App in a Convergence Customer Environment.
  - ```-o, --output <output>``` - Directory to place any artifacts generated by the deployment.
  - ```-to, --timeout <timeout>``` - Timeout, in minutes, to wait for an App to install. The default is 360 minutes.
  - ```-tombm, --timeoutToGetSomeMBMsg <timeoutToGetSomeMBMsg>``` - Timeout, in minutes, that the SDK client waits to receive any message from the portal via Message Bus. The default is 30 minutes.
  - ```-?, -h, --help``` - Show help and usage information.

### login

Log in to the CM Portal. This command will cache the Auth Token to a file in the host filesystem. 

Equivalent to powershell cmdlet <a href="#set-login">Set-Login</a>

Usage: ```cmf-portal login [options]```

Options:
  - ```-v, --verbose``` - Show detailed logging
  - ```-t, --pat, --token <token>``` - Personal Access Token used to access the Customer Portal
  - ```-?, -h, --help``` - Show help and usage information

### publish

Publishes one or more Deployment Manifests into Customer Portal

Equivalent to powershell cmdlet <a href="#add-manifests">Add-Manifests</a>

Usage: ```cmf-portal publish [options]```

Options:
  - ```-v, --verbose``` - Show detailed logging
  - ```--replace-tokens <MyToken=value MyToken2=value2>``` - Replace the tokens specified in the input files using the proper syntax (e.g. #{MyToken}#) with the specified values. E.g. MyToken=value MyToken2=value2.
  - ```-p, --path <path>``` - **REQUIRED** - Path to the manifest file or path to a folder containing multiple manifest files
  - ```-dg, --datagroup <datagroup>``` - Name of the existing datagroup to assign to the published manifests
  - ```-?, -h, --help``` - Show help and usage information

### publish-package

Publishes one or more Customization Packages into Customer Portal

Equivalent to powershell cmdlet <a href="#add-package">Add-Package</a>

Usage: ```cmf-portal publish-package [options]```

Options:
  - ```-v, --verbose``` - Show detailed logging
  - ```-p, --path <path>``` - **REQUIRED** - Path to the package zip file or folder containing multiple package files
  - ```-dg, --datagroup <datagroup>``` - Name of the existing datagroup to assign to the published packages
  - ```-?, -h, --help``` - Show help and usage information

## Powershell

- If you downloaded the zip from the Releases section, you just need to import the dll using  
```Import-Module .\Cmf.CustomerPortal.Sdk.Powershell.dll``` 

- If you're not using the Release published on GitHub you'll have to first compile the solution and then import the DLL with the cmdlets. 
  - If the solution hasn't been compiled you can simply run the ```run.ps1``` script found in the root of repository.  
  Simply running ```.\run.ps1``` is enough and no other parameters are necessary. Optionally you can use the ```Configuration``` parameter but it is set to ```Release``` by default which should be the use case for most users.
  This will compile the Powershell version and automatically import the module in a new powershell window. It is important to note that imported cmdlets only work on the powershell where the import is called and will not be recognized on other powershell windows.
  - If the project has already been compiled and you only wish to import the cmdlets you can run  
  ```Import-Module .\src\Powershell\bin\Release\netstandard2.0\publish\Cmf.CustomerPortal.Sdk.Powershell.dll``` 

After this the cmdlets can be called to execute the desired operations. Note that some cmdlets require certain options in order to execute. These options can be passed directly in the cmdlet call or if none is provided the cmdlet will ask for the parameters one by one.

Usage: ```[cmdlet] [options]```

Cmdlets:
  - <a href="#add-manifests">```Add-Manifests```</a> - Publishes one or more Deployment Manifests into Customer Portal
  - <a href="#add-package">```Add-Package```</a> - Publishes one or more Customization Packages into Customer Portal
  - <a href="#get-agentconnection">```Get-AgentConnection```</a> - Check if an Infrastructure Agent is connected
  - <a href="#new-environment">```New-Environment```</a> - Creates and deploys a new Customer Environment
  - <a href="#new-infrastructure">```New-Infrastructure```</a> - Creates a customer Infrastructure
  - <a href="#new-infrastructureagent">```New-InfrastructureAgent```</a> - Creates and deploys a new Infrastructure Agent
  - <a href="#set-login">```Set-Login```</a> - Log in to the CM Portal


Examples:
- ```Get-AgentConnection <agent-name>``` - Check if the Infrastructure Agent named <agent-name> is connected
- ```Get-AgentConnection``` - Same as before but instead of inputting all the parameters upfront, the cmdlet will ask you to fill each parameter one by one. At any time you can type ```!?``` to get help for that parameter

Below we will show the documentation for each cmdlet.

### Add-Manifests

Publishes one or more Deployment Manifests into Customer Portal

Equivalent to the console command <a href="#publish">publish</a>

Usage: ```Add-Manifests [options]```

Options:
  - ```-ReplaceTokens <MyToken=value MyToken2=value2>``` - Replace the tokens specified in the input files using the proper syntax (e.g. #{MyToken}#) with the specified values. E.g. MyToken=value MyToken2=value2.
  - ```-Path <path>``` - **REQUIRED** - Path to the manifest file or path to a folder containing multiple manifest files
  - ```-Datagroup <datagroup>``` - Name of the existing datagroup to assign to the published deployment manifests

### Add-Package

Publishes one or more Customization Packages into Customer Portal

Equivalent to the console command <a href="#publish-package">publish-package</a>

Usage: ```Add-Package [options]```

Options:
  - ```-Path <path>``` - **REQUIRED** - Path to the package zip file or folder containing multiple package files
  - ```-Datagroup <datagroup>``` - Name of the existing datagroup to assign to the published packages

### Get-AgentConnection

Check if an Infrastructure Agent is connected

Equivalent to the console command <a href="#checkagentconnection">checkagentconnection</a>

Usage: ```Get-AgentConnection [options]```

Options:  
  - ```-Name <agent-name>``` - **REQUIRED** - The name of the Infrastructure Agent  

### New-Environment

Creates and deploys a new Customer Environment

Equivalent to the console command <a href="#deploy">deploy</a>

Usage: ```New-Environment [options]```

Options:
  - ```-Interactive``` - **Position 1** - Flag that controls if the user should be prompted to go to the portal to initialize the installation manually
  - ```-TerminateOtherVersions``` - **Position 2** - Flag that controls if all the other versions of the Customer Environment should be terminated
  - ```-TerminateOtherVersionsRemove``` - **Position 3** - Flag that controls if the deployments of the versions of the Customer Environment that will be terminated should be removed. Requires the terminateOtherVersions flag.
  - ```-TerminateOtherVersionsRemoveVolumes``` - **Position 4** - Flag that controls if the volumes of the versions of the Customer Environment that will be terminated should be removed. Requires the terminateOtherVersions and terminateOtherVersionsRemove flags.
  - ```-ReplaceTokens <MyToken=value MyToken2=value2>``` - Replace the tokens specified in the input files using the proper syntax (e.g. #{MyToken}#) with the specified values. E.g. MyToken=value MyToken2=value2.
  - ```-CustomerInfrastructureName <customer-infrastructure-name>``` - Name of the existing Customer Infrastructure
  - ```-Name <name>``` - Name of the new Customer Environment. --name is also supported
  - ```-Description <description>``` - Description of the new Customer Environment
  - ```-ParametersPath <parameters>``` - Path to parameters file that describes the Customer Environment
  - ```-EnvironmentType <Development|Production|Staging|Testing>``` - Type of the Customer Environment to deploy [default: Development]
  - ```-DeploymentTargetName <AzureKubernetesServiceTarget|dockerswarm|KubernetesOnPremisesTarget|KubernetesRemoteTarget|OpenShiftOnPremisesTarget|OpenShiftRemoteTarget|portainer>``` - **REQUIRED** - Name of the Deployment Target to use for the Customer Environment [default: dockerswarm]
  - ```-OutputDir <output>``` - Directory to place any artifacts generated by the deployment
  - ```-SiteName <site>``` - **REQUIRED** - Name of the Site associated with the Customer
  - ```-DeploymentPackageName <package>``` - **REQUIRED** - Name of the Deployment Package to use for the Customer Environment
  - ```-LicenseName <license>``` - **REQUIRED** - Comma-separated names of the Licenses' Unique Name
  - ```-DeploymentTimeoutMinutes <deploymentTimeoutMinutes>``` - Number of minutes that are allowed to wait for the deployment to succeed. The default is 360 minutes.
  - ```-DeploymentTimeoutMinutesToGetSomeMBMsg <DeploymentTimeoutMinutesToGetSomeMBMsg>``` - Timeout, in minutes, that the SDK client waits to receive any message from the portal via Message Bus. The default is 30 minutes.

### New-Infrastructure

Creates a customer Infrastructure

Equivalent to the console command <a href="#createinfrastructure">createinfrastructure</a>

Usage: ```New-Infrastructure [options]```

Options:
  - ```-Name <name>``` - The name of the Customer Infrastructure to be created
  - ```-SiteName <site>``` - **deprecated** - Name of a Site used to match a Customer with the Customer Infrastructure
  - ```-CustomerName <customer>``` - Name of the Customer associated with the Customer Infrastructure
  - ```-IgnoreIfExists``` - Flag that ignores a throw if an error of type 'Customer Infrastructure already exist' occurs
  - ```-ParametersPath <parameters>``` - Path to parameters json file that includes parameters for the Customer Infrastructure

### New-InfrastructureAgent

Creates and deploys a new Infrastructure Agent

Equivalent to the console command <a href="#deployagent">deployagent</a>

Usage: ```New-InfrastructureAgent [options]```

Options:
  - ```-Interactive``` - **Position 1** - Flag that controls if the user should be prompted to go to the portal to initialize the installation manually
  - ```-TerminateOtherVersions``` - **Position 2** - Flag that controls if all the other versions of the Customer Environment should be terminated
  - ```-TerminateOtherVersionsRemove``` - **Position 3** - Flag that controls if the deployments of the versions of the Customer Environment that will be terminated should be removed. Requires the terminateOtherVersions flag.
  - ```-TerminateOtherVersionsRemoveVolumes``` - **Position 4** - Flag that controls if the volumes of the versions of the Customer Environment that will be terminated should be removed. Requires the terminateOtherVersions and terminateOtherVersionsRemove flags.
  - ```-ReplaceTokens <MyToken=value MyToken2=value2>``` - Replace the tokens specified in the input files using the proper syntax (e.g. #{MyToken}#) with the specified values. E.g. MyToken=value MyToken2=value2.
  - ```-CustomerInfrastructureName <customer-infrastructure-name>``` - Name of the existing Customer Infrastructure
  - ```-Name <name>``` - Name of the new Customer Environment. --name is also supported
  - ```-Description <description>``` - Description of the new Customer Environment
  - ```-ParametersPath <parameters>``` - Path to parameters file that describes the Customer Environment
  - ```-EnvironmentType <Development|Production|Staging|Testing>``` - Type of the Customer Environment to deploy [default: Development]
  - ```-DeploymentTargetName <AzureKubernetesServiceTarget|dockerswarm|KubernetesOnPremisesTarget|KubernetesRemoteTarget|OpenShiftOnPremisesTarget|OpenShiftRemoteTarget|portainer>``` - **REQUIRED** - Name of the Deployment Target to use for the Customer Environment [default: dockerswarm]
  - ```-OutputDir <output>``` - Directory to place any artifacts generated by the deployment
  - ```-DeploymentTimeoutMinutes <deploymentTimeoutMinutes>``` - Number of minutes that are allowed to wait for the deployment to succeed. The default is 360 minutes.
  - ```-DeploymentTimeoutMinutesToGetSomeMBMsg <DeploymentTimeoutMinutesToGetSomeMBMsg>``` - Timeout, in minutes, that the SDK client waits to receive any message from the portal via Message Bus. The default is 30 minutes.

### Set-Login

Log in to the CM Portal. This command will cache the Auth Token to a file in the host filesystem. 

Equivalent to the console command <a href="#login">login</a>

Usage: ```Set-Login [options]```

Options:
  - ```-PAT <token>``` - Personal Access Token used to access the Customer Portal
