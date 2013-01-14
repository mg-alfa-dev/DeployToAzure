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
    [IO.FileInfo] $OutFile = $(throw "Parameter -OutFile [IO.FileInfo] is required.")
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
  exec {
    & $pathToDeployToAzureExe $ParamsFile
  }
}