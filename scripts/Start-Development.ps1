param([string]$ConnectionString, [switch]$VerificationDatabase)
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path $PSScriptRoot -Parent
Set-Location -LiteralPath $projectRoot
$env:DOTNET_CLI_HOME = Join-Path $projectRoot '.dotnet-home'
$env:APPDATA = Join-Path $projectRoot '.appdata'
$env:NUGET_PACKAGES = Join-Path $projectRoot '.packages'
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
$env:ASPNETCORE_ENVIRONMENT = 'Development'
$env:ASPNETCORE_URLS = 'http://127.0.0.1:5080'
$env:Storage__Root = Join-Path $projectRoot 'data'
if ($VerificationDatabase) {
    $env:Database__Provider = 'Sqlite'
    $env:Database__Initialize = 'true'
    $env:ConnectionStrings__Database = "Data Source=$projectRoot\data\verification.db"
} else {
    if (-not $ConnectionString) { throw 'Indica -ConnectionString para a base PostgreSQL dedicada.' }
    $env:Database__Provider = 'PostgreSQL'
    $env:ConnectionStrings__Database = $ConnectionString
    $env:Database__Initialize = 'false'
}
if (-not $env:Bootstrap__Token) {
    $bootstrapSecure = Read-Host 'Define o código de instalação que usarás na primeira entrada' -AsSecureString
    $env:Bootstrap__Token = [System.Net.NetworkCredential]::new('', $bootstrapSecure).Password
}
dotnet run --project api/Ucall.Api.csproj --no-launch-profile
