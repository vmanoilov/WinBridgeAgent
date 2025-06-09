# PowerShell script to rebuild the project folder and write files
$projectRoot = "$PSScriptRoot\ChatGPTFileBridge"

$files = @{
    "src\ChatGPTFileBridge.API\Program.cs" = @'
using ChatGPTFileBridge.Core.Services;
// ... (rest of file)
'@
    "src\ChatGPTFileBridge.UI\App.xaml" = @'
<Application x:Class="ChatGPTFileBridge.UI.App"
// ... (rest of file)
'@
    "src\ChatGPTFileBridge.API\Controllers\FileAccessController.cs" = @'
using Microsoft.AspNetCore.Mvc;
// ... (rest of file)
'@
    "README.md" = @'
# ChatGPT File Bridge

This is the full project export...
'@
}

foreach ($path in $files.Keys) {
    $fullPath = Join-Path $projectRoot $path
    $dir = Split-Path $fullPath
    if (-not (Test-Path $dir)) {
        New-Item -ItemType Directory -Path $dir -Force | Out-Null
    }
    $files[$path] | Out-File -FilePath $fullPath -Encoding UTF8
}

Write-Host "Project recreated successfully at $projectRoot"