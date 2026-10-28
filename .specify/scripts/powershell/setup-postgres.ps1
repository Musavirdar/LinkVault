$ErrorActionPreference = "Stop"

$destDir = "$env:LocalAppData\Programs\PostgreSQL16"
$zipPath = "$env:Temp\postgresql-16-binaries.zip"
$dataDir = "$destDir\data"
$pwFile = "$destDir\pw.txt"
$url = "https://get.enterprisedb.com/postgresql/postgresql-16.3-1-windows-x64-binaries.zip"

if (-not (Test-Path "$destDir\bin\postgres.exe")) {
    Write-Host "Downloading portable PostgreSQL 16..."
    if (-not (Test-Path $zipPath) -or ((Get-Item $zipPath).Length -lt 300MB)) {
        Invoke-WebRequest -Uri $url -OutFile $zipPath -UseBasicParsing
    }
    Write-Host "Extracting PostgreSQL binaries..."
    New-Item -ItemType Directory -Path $destDir -Force | Out-Null
    Expand-Archive -Path $zipPath -DestinationPath "$env:Temp\pg_extract" -Force
    Move-Item -Path "$env:Temp\pg_extract\pgsql\*" -Destination $destDir -Force
    Remove-Item -Path "$env:Temp\pg_extract" -Recurse -Force -ErrorAction SilentlyContinue
}

Set-Content -Path $pwFile -Value "linkvault123" -NoNewline

if (-not (Test-Path "$dataDir\PG_VERSION")) {
    Write-Host "Initializing PostgreSQL cluster..."
    & "$destDir\bin\initdb.exe" -D $dataDir -U linkvault -A scram-sha-256 --pwfile=$pwFile --encoding=UTF8
}

Write-Host "Starting PostgreSQL on port 5433..."
& "$destDir\bin\pg_ctl.exe" -D $dataDir -o "-p 5433" -l "$destDir\postgres.log" start

Start-Sleep -Seconds 3

Write-Host "Creating database linkvault..."
$env:PGPASSWORD = "linkvault123"
& "$destDir\bin\createdb.exe" -h localhost -p 5433 -U linkvault linkvault 2>$null

Write-Host "PostgreSQL is running and ready on port 5433!"
