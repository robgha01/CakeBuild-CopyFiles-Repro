Param(
    [Parameter(Mandatory=$true)]
    [string]$GcmDir, #= "D:/Workspace/BlueLeet/BlueLeet.Sitemap.EPiServer/.tools/gcm/",
    [Parameter(Mandatory=$true)]
    [string]$GcmProtocol,#="https",
    [Parameter(Mandatory=$true)]
    [string]$GcmHost,#="blueleet.visualstudio.com",
    [Parameter(Mandatory=$true)]
    [string]$GcmPath#="Main/_git/BlueLeet.Sitemap.EPiServer"
)
#$dict = @{}
$dict = New-Object 'system.collections.generic.dictionary[[string],[string]]'
$GcmExe = Join-Path $GcmDir "git-credential-manager.exe";

$pinfo = New-Object System.Diagnostics.ProcessStartInfo;
$pinfo.FileName = $GcmExe;
$pinfo.RedirectStandardError = $true;
$pinfo.RedirectStandardOutput = $true;
$pinfo.RedirectStandardInput = $true;
$pinfo.UseShellExecute = $false;
$pinfo.Arguments = "get";
$p = New-Object System.Diagnostics.Process;
$p.StartInfo = $pinfo;
$p.Start() | Out-Null;
$p.StandardInput.WriteLine("protocol=$GcmProtocol");
$p.StandardInput.WriteLine("host=$GcmHost");
$p.StandardInput.WriteLine("path=$GcmPath");
$p.StandardInput.WriteLine("");
$p.WaitForExit();
$output = $p.StandardOutput.ReadToEnd();
$outputErrors = $p.StandardError.ReadToEnd();
if(![string]::IsNullOrEmpty($outputErrors))
{
    Write-Host $outputErrors
}

foreach($item in $output -split '\n+')
{
    $t = $item -split '='
    if(![string]::IsNullOrEmpty($t[0]))
    {
        $TextInfo = (Get-Culture).TextInfo;
        $dict.Add($TextInfo.ToTitleCase($t[0]), $t[1]);
    }
}

Write-Output $dict | ConvertTo-Json