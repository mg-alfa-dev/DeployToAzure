$pathToDeployToAzureExe = (Join-Path (Split-Path -Parent $MyInvocation.MyCommand.Definition) DeployToAzure.exe)

function Create-DeployParams
{
  param 
  (
    [string] $SubscriptionId = $(throw "Parameter -SubscriptionId [string] is required."),
    [string] $ServiceName = $(throw "Parameter -ServiceName [string] is required."),
    [string] $Slot = "production",
    [IO.FileInfo] $Package = $(throw "Parameter -Package [IO.FileInfo] is required."),
    [IO.FileInfo] $Configuration = $(throw "Parameter -Configuration [IO.FileInfo] is required."),
    [string] $StorageAccountName = $null,
    [string] $StorageAccountKey = $null,
    [string] $Label = "deployment",
    [string] $Name = "Deployment",
    [IO.FileInfo] $CertFile = $(throw "Parameter -CertFile [IO.FileInfo] is required."),
    [string] $CertPassword = $(throw "Parameter -CertPassword [string] is required."),
    [IO.FileInfo] $OutFile = $(throw "Parameter -OutFile [IO.FileInfo] is required."),
    [IO.FileInfo] $BlobPathToDeploy = $null,
    [string] $ChangeVMSize = $null,
    [string] $ChangeWebRoleVMSize = $null,
    [string] $ChangeWorkerRoleVMSize = $null
  )
  
  $params = @{
    SubscriptionId = $SubscriptionId
    ServiceName = $ServiceName
    DeploymentSlot = $Slot
    CertFileName = $CertFile.FullName
    CertPassword = $CertPassword
    DeploymentLabel = $Label
    DeploymentName = $Name
    PackageFileName = $Package.FullName
    ServiceConfigurationPath = $Configuration.FullName
  }
  
  if($StorageAccountName) { $params.StorageAccountName = $StorageAccountName }
  if($StorageAccountKey) { $params.StorageAccountKey = $StorageAccountKey }
  
  if(($Label.Length -gt 100) -or $Label.Contains('-') -or $Label.Contains(' ')) {
    throw "Deployment labels cannot exceed 100 chars or contain '-' or ' '"
  }
  
  if($Name.Contains(' ')) {
    throw "Deployment names cannot contain ' '"
  }

  if($BlobPathToDeploy -ne $null) {
    $params.BlobPathToDeploy = $BlobPathToDeploy.FullName
  }

  if($ChangeVMSize -ne $null) {
    $params.ChangeVMSize = $ChangeVMSize
  }
  
  if(($ChangeWebRoleVMSize -ne $null) -and ($ChangeWorkerRoleVMSize -ne $null)) {
    $params.ChangeWebRoleVMSize = $ChangeWebRoleVMSize
    $params.ChangeWorkerRoleVMSize = $ChangeWorkerRoleVMSize
}

  $xml = "<Params>"
  $params.Keys | %{ $xml += "`n  <$($_)>$($params[$_])</$($_)>" }
  $xml += "`n</Params>"
  
  Set-Content -Path $OutFile -Value $xml -Encoding UTF8
}

function Execute-Deployment
{
  param
  (
    [IO.FileInfo] $ParamsFile = $(throw "Parameter -ParamsFile [IO.FileInfo] is required.")
  )
  
  Write-Host "  Using DeployToAzure from: $($pathToDeployToAzureExe)"
  & $pathToDeployToAzureExe $ParamsFile
}

function Update-ServiceConfiguration
{
  param
  (
    [IO.FileInfo] $ConfigFile = $(throw "Parameter -ConfigFile [IO.FileInfo] is required."),
    [hashtable] $RewriteRules = $(throw "Parameter -RewriteRules [hashtable] is required.")
  )
  
  $filePath = resolve-path $ConfigFile
  
  Write-Host "  Rewriting '$($filePath)':"
  
  $xml = [xml](gc $filePath)

  foreach($ruleKey in $RewriteRules.Keys) {
    Write-Host "    Rule: [$($ruleKey)] -> [$($RewriteRules[$ruleKey])]"
    
    $ruleValue = $RewriteRules[$ruleKey]
    $keyParts = $ruleKey.Split(@([char]':'), 3)
    if($keyParts.Length -ne 3 -and $keyParts.Length -ne 2 ) { 
      throw "RuleKey ($($ruleKey)) should be in <role>:<section>:<property> form. (section is 'Config' or 'Cert'). Or <role>:Instances form" 
    }
    $roleName = $keyParts[0]
    $section = $keyParts[1]
    
    try {
      $roles = $xml.ServiceConfiguration.Role
      $role = $roles | ?{ $_.name -eq $roleName }
      if($role -eq $null) {
        throw "Role $roleName not found in config."
      }

      switch($section) {
        "Cert" { 
          $target = $keyParts[2]
          $configSetting = $role.Certificates.Certificate | ?{ $_.name -eq $target }
          if($configSetting -eq $null) {
            throw "Certificate $target not found in role $roleName"
          }
          $configSetting.SetAttribute("thumbprint", $ruleValue)
        }
        "Config" {
          $target = $keyParts[2]
          $configSetting = $role.ConfigurationSettings.Setting | ?{ $_.name -eq $target }
          if($configSetting -eq $null) {
            throw "Configuration setting $target not found in role $roleName"
          }
          $configSetting.value = $ruleValue
        }
        "Instances" {
          $configSetting = $role.Instances
          if($configSetting -eq $null) {
            throw "Configuration setting $target not found in role $roleName"
          }
          $configSetting.count = $ruleValue
        }
        default {
          throw "Rule contains an unrecognized section prefix: $($section): $(ruleKey)"
        }
      }
    }
    catch {
      throw "Configuration file couldn't be patched.  Check the syntax of the file and/or for missing configuration setting entries."
    }
  }
  $xml.Save($filePath)
}

function Get-RdpPassword($certificateFileName, $privateKeyPassword, $servicePassword) {
  Add-Type -AssemblyName System.Security
  $certificate = New-Object System.Security.Cryptography.X509Certificates.X509Certificate2($certificateFileName, $privateKeyPassword)
  $servicePasswordBytes = [Text.Encoding]::UTF8.GetBytes($servicePassword)
  $content = New-Object System.Security.Cryptography.Pkcs.ContentInfo(,$servicePasswordBytes)
  $envelope = New-Object System.Security.Cryptography.Pkcs.EnvelopedCms($content)
  $recipient = New-Object System.Security.Cryptography.Pkcs.CmsRecipient($certificate)
  $envelope.Encrypt($recipient)
  [Convert]::ToBase64String($envelope.Encode())
}

function Get-Thumbprint($certificateFileName, $privateKeyPassword) {
  Add-Type -AssemblyName System.Security
  $certificate = New-Object System.Security.Cryptography.X509Certificates.X509Certificate2($certificateFileName, $privateKeyPassword)
  $certificate.Thumbprint
}

function Update-DiagnosticsConfiguration{
  param 
  (
    [string] $SubscriptionId = $(throw "Parameter -SubscriptionId [string] is required."),
    [string] $ServiceName = $(throw "Parameter -ServiceName [string] is required."),
    [string] $Slot = "production",
    [string] $Configuration = $(throw "Parameter -Configuration [string] is required."),
    [string] $StorageAccountName = $(throw "Parameter -StorageAccountName [string] is required."),
    [string] $StorageAccountKey = $(throw "Parameter -StorageAccountKey [string] is required."),
    [string] $PublishSettingsFile = $(throw "Parameter -PublishSettingsFile [string] is required."),
    [string] $SubscriptionName = $(throw "Parameter -SubscriptionName [string] is required."),
    [string] $RoleName = $(throw "Parameter -RoleName [string] is required.")
  )

  $params = @{
    SubscriptionId = $SubscriptionId
    ServiceName = $ServiceName
    Slot = $Slot
    Configuration = $Configuration
    StorageAccountName = $StorageAccountName
    StorageAccountKey = $StorageAccountKey
    PublishSettingsFile = $PublishSettingsFile
    SubscriptionName = $SubscriptionName
    RoleName = $RoleName
  }

  Import-Module Azure
  
  Write-Host "Importing Azure Publish Settings file: $($params.PublishSettingsFile)"
  Import-AzurePublishSettingsFile $params.PublishSettingsFile -ErrorAction Stop 
  
  Write-Host "Selecting Azure Subscription: $($params.SubscriptionName)"
  Select-AzureSubscription $params.SubscriptionName -ErrorAction Stop 
  
  Write-Host "Creating Azure Storage Context for account: $($params.StorageAccountName)"
  $storageContext = New-AzureStorageContext -StorageAccountName $params.StorageAccountName -StorageAccountKey $params.StorageAccountKey -ErrorAction Stop 

  Write-Host "Adding Azure Service Diagnostics to the $($params.ServiceName) $($params.RoleName) role using configuration at: $($params.Configuration)"
  Write-Host "This command may take several minutes to complete due to installation and other actions happening within the role"
  Set-AzureServiceDiagnosticsExtension -StorageContext $storageContext -DiagnosticsConfigurationPath "$($params.Configuration)" –ServiceName  "$($params.ServiceName)" -Slot 'Production' -Role "$($params.RoleName)" -ErrorAction Stop 

  Remove-Module Azure -Force
}