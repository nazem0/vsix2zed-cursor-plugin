# Install Vsix2Zed into ~/.cursor/plugins/local/vsix2zed (fresh Cursor OK)
$ErrorActionPreference = "Stop"
$src = $PSScriptRoot
$dest = Join-Path $env:USERPROFILE ".cursor\plugins\local\vsix2zed"

Write-Host "Source: $src"
Write-Host "Dest:   $dest"

foreach ($cmd in @("node", "npm", "dotnet")) {
  if (-not (Get-Command $cmd -ErrorAction SilentlyContinue)) {
    throw "Missing '$cmd' on PATH. Install Node.js 18+ and .NET 8, then retry."
  }
}

New-Item -ItemType Directory -Force -Path (Split-Path $dest) | Out-Null
if (Test-Path $dest) { Remove-Item -Recurse -Force $dest }

# Copy everything except node_modules / obj
robocopy $src $dest /E /XD node_modules obj .git /XF *.pdb /NFL /NDL /NJH /NJS /nc /ns /np | Out-Null

Push-Location (Join-Path $dest "mcp")
try { npm install --omit=dev } finally { Pop-Location }

$csproj = Join-Path $dest "dotnet\Vsix2Zed.Cli\Vsix2Zed.Cli.csproj"
$bin = Join-Path $dest "bin"
dotnet publish $csproj -c Release -o $bin -v q
if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed" }

# mcp.json for local install uses absolute paths (CURSOR_PLUGIN_ROOT may not expand everywhere)
$server = (Join-Path $dest "mcp\server.mjs") -replace '\\', '/'
$root = $dest -replace '\\', '/'
$mcp = @{
  mcpServers = @{
    vsix2zed = @{
      command = "node"
      args    = @($server)
      env     = @{ VSIX2ZED_PLUGIN_ROOT = $root }
    }
    rea = @{
      command = "npx"
      args    = @("-y", "rea-agents@6.4.0")
    }
  }
}
$mcp | ConvertTo-Json -Depth 8 | Set-Content (Join-Path $dest "mcp.json") -Encoding utf8

Write-Host ""
Write-Host "Installed. Reload Cursor (Developer: Reload Window)."
Write-Host "Customize → enable MCP 'vsix2zed' and 'rea', skill 'vsix2zed'."
Write-Host "Repo: https://github.com/nazem0/vsix2zed-cursor-plugin"
