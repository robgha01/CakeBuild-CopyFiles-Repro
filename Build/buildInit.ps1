$gcmDir = Join-Path $PSScriptRoot "../.tools/gcm";
$gcmFile = Join-Path $gcmDir "git-credential-manager.exe";
$gcmDownloadUrl = "https://github.com/Microsoft/Git-Credential-Manager-for-Windows/releases/download/v1.4.0/gcm-v1.4.0.zip";
$tmpDir = Join-Path $PSScriptRoot "gcm";

if(![System.IO.File]::Exists($gcmFile))
{
    if(![System.IO.Directory]::Exists($tmpDir))
    {
        [System.IO.Directory]::CreateDirectory($tmpDir);
    }

    $WebClient = New-Object System.Net.WebClient
    $WebClient.DownloadFile($gcmDownloadUrl, (Join-Path $tmpDir "/gcm-v1.4.0.zip"))

    Expand-Archive (Join-Path $tmpDir "/gcm-v1.4.0.zip") -DestinationPath $gcmDir

    [System.IO.Directory]::Delete($tmpDir, $True);
}

# Install ModPhuserEx
$modPhuserExDownloadUrl = "https://ci.appveyor.com/api/projects/0xFireball/modphuserex/artifacts/Build/ModPhuserEx_bin.zip";
$modPhuserExDir = Join-Path $PSScriptRoot "../.tools/ModPhuserEx";
$modPhuserEXFile = Join-Path $modPhuserExDir "ModPhuserEx.CLI.exe";

if(![System.IO.File]::Exists($modPhuserEXFile))
{
    if(![System.IO.Directory]::Exists($tmpDir))
    {
        [System.IO.Directory]::CreateDirectory($tmpDir);
    }

    $WebClient = New-Object System.Net.WebClient
    $WebClient.DownloadFile($modPhuserExDownloadUrl, (Join-Path $tmpDir "/ModPhuserEx_bin.zip"))

    Expand-Archive (Join-Path $tmpDir "/ModPhuserEx_bin.zip") -DestinationPath $modPhuserExDir

    [System.IO.Directory]::Delete($tmpDir, $True);
}