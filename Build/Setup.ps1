function Install-NeededFor {
param(
   [string] $packageName = ''
  ,[bool] $defaultAnswer = $true
  ,[string] $QuestionTemplate = 'Do you need to install'
)
  if ($packageName -eq '') {return $false}

  $yes = '6'
  $no = '7'
  $msgBoxTimeout='-1'
  $defaultAnswerDisplay = 'Yes'
  $buttonType = 0x4;
  if (!$defaultAnswer) { $defaultAnswerDisplay = 'No'; $buttonType= 0x104;}

  $answer = $msgBoxTimeout
  try {
    $timeout = 10
    $question = "$($QuestionTemplate) $($packageName)? Defaults to `'$defaultAnswerDisplay`' after $timeout seconds"
    $msgBox = New-Object -ComObject WScript.Shell
    $answer = $msgBox.Popup($question, $timeout, "Install $packageName", $buttonType)
  }
  catch {
  }

  if ($answer -eq $yes -or ($answer -eq $msgBoxTimeout -and $defaultAnswer -eq $true)) {
    write-host "Installing $packageName"
    return $true
  }

  write-host "Not installing $packageName"
  return $false
}

# Install Chocolatey
if (Install-NeededFor 'chocolatey' $false) {
  iex ((new-object net.webclient).DownloadString('http://chocolatey.org/install.ps1'))
}

# Install .Net Frameworks
if (Install-NeededFor '.net frameworks') {
    Write-Host "Grabbing required frameworks"
    cinst webpicmd
    cinst netframework2 -source webpi
    cinst NETFramework35 -source webpi
    cinst NETFramework4 -source webpi
    cinst NETFramework4Update402 -source webpi
    cinst NETFramework4Update402_KB2544514_Only -source webpi
    cinst WindowsInstaller31 -source webpi
    cinst WindowsInstaller45 -source webpi
}

cinst nuget.commandline

# Install ruby
if (Install-NeededFor 'ruby' $false) {
    cinst ruby -y
}

# Install node.js
if (Install-NeededFor 'node.js' $false) {
    #cinst nodejs -version 6.6.0 --force --allowdowngrade -y
    cinst nodejs.install -version 7.7.2 --force --allowdowngrade -y
}

# Install yarn
if (Install-NeededFor 'yarn' $false) {
    cinst yarn
}

Write-Host "Performing required gem commands"
# perform ruby updates and get gems
$gemLink = ((Invoke-WebRequest -Uri "https://rubygems.org/pages/download").Links | Where-Object {$_ -like '*rubygems-update-*' -and $_ -like '*.gem*'}).href
$fileName = $gemLink.Substring($gemLink.LastIndexOf('/')+1)
$installedGemVersion = (gem --version);

if($fileName -notlike "*$installedGemVersion*")
{
    Write-Host "Upgrading ruby gems to $installedGemVersion..."
    Invoke-WebRequest $gemLink -OutFile "$env:temp\$fileName" -PassThru

    gem install --local "$env:temp\$fileName"
    update_rubygems

    Write-Host "Finished the new version is '$(gem --version)'"
    gem update --system
}
else
{
    Write-Host "Latest ruby gems already installed, Skipping."
}

gem install bundler

bundle install
git add "../Gemfile" "../Gemfile.lock"

if (Install-NeededFor 'visual studio extensions' $false) {
  cinst batch-install-vsix -params '.\extensions.config' -force -y
}

#cmd.exe /c "npm cache clean"
#cmd.exe /c "npm install -g npm-check-updates"
#cmd.exe /c "ncu"

#if(Install-NeededFor 'any npm package updates?' $false)
#{
#$cmdOutput
#  <ncu> | Tee-Object -Variable cmdOutput
#  if($cmdOutput -notlike "*All dependencies match the latest package versions :)*")
#  {
#    if(Install-NeededFor 'Do you want to perform those updates?' $false)
#    {
#      ncu -u
#    }
#  }
#}

Write-Host "If you have made it here without errors, you should be setup and ready to hack on the apps."
Write-Warning "If you see any failures happen, you may want to reboot and continue to let installers catch up. This script is idempotent and will only apply changes that have not yet been applied."