# Check if running with administrator privileges
$isAdmin = ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)

if (-not $isAdmin) {
    Write-Host "This script requires administrator privileges. Restarting with elevated permissions..." -ForegroundColor Yellow
    Start-Process powershell -ArgumentList "-NoProfile -ExecutionPolicy Bypass -File `"$PSCommandPath`"" -Verb RunAs
    exit
}

Write-Host "Building and Starting ISP Discovery..." -ForegroundColor Green

# Function to check if a command exists
function Test-Command($CommandName) {
    return $null -ne (Get-Command -Name $CommandName -ErrorAction SilentlyContinue)
}

# Check prerequisites
if (-not (Test-Command "dotnet")) {
    Write-Host "Error: .NET SDK is not installed. Please install .NET 9.0 or later." -ForegroundColor Red
    exit 1
}

if (-not (Test-Command "npm")) {
    Write-Host "Error: Node.js/npm is not installed. Please install Node.js." -ForegroundColor Red
    exit 1
}

# Check for WinPcap/Npcap
$npcapService = Get-Service -Name "npcap" -ErrorAction SilentlyContinue
if ($null -eq $npcapService) {
    Write-Host "Npcap is not installed. Would you like to download and install it? (Y/N)" -ForegroundColor Yellow
    $response = Read-Host
    if ($response -eq 'Y' -or $response -eq 'y') {
        $npcapUrl = "https://npcap.com/dist/npcap-1.75.exe"
        $npcapInstaller = "$env:TEMP\npcap-installer.exe"
        
        Write-Host "Downloading Npcap installer..." -ForegroundColor Yellow
        Invoke-WebRequest -Uri $npcapUrl -OutFile $npcapInstaller
        
        Write-Host "Installing Npcap..." -ForegroundColor Yellow
        Start-Process -FilePath $npcapInstaller -ArgumentList "/S" -Wait
        
        Remove-Item $npcapInstaller
        
        Write-Host "Npcap installation completed. Please restart your computer and run this script again." -ForegroundColor Green
        exit
    } else {
        Write-Host "Npcap is required for network packet capture. Please install it manually from https://npcap.com/" -ForegroundColor Red
        exit 1
    }
}

# Stop existing processes
Write-Host "Stopping existing processes..." -ForegroundColor Yellow
Get-Process | Where-Object { $_.ProcessName -match 'electron|dotnet|ISP Discovery' } | Stop-Process -Force -ErrorAction SilentlyContinue
Start-Sleep -Seconds 5  # Wait for processes to fully terminate

# Clean up any existing lock files or temporary files
Write-Host "Cleaning up..." -ForegroundColor Yellow
if (Test-Path ".\node_modules") {
    Remove-Item ".\node_modules" -Recurse -Force -ErrorAction SilentlyContinue
}
if (Test-Path ".\bin") {
    Remove-Item ".\bin" -Recurse -Force -ErrorAction SilentlyContinue
}
if (Test-Path ".\obj") {
    Remove-Item ".\obj" -Recurse -Force -ErrorAction SilentlyContinue
}

# Install Node.js dependencies
Write-Host "Installing Node.js dependencies..." -ForegroundColor Yellow
npm install --force
if ($LASTEXITCODE -ne 0) {
    Write-Host "Error installing Node.js dependencies" -ForegroundColor Red
    exit 1
}

# Create styles directory if it doesn't exist
if (-not (Test-Path ".\styles")) {
    New-Item -ItemType Directory -Path ".\styles"
}

# Build Tailwind CSS
Write-Host "Building Tailwind CSS..." -ForegroundColor Yellow
npx tailwindcss -i ./styles/input.css -o ./styles/output.css
if ($LASTEXITCODE -ne 0) {
    Write-Host "Error building Tailwind CSS" -ForegroundColor Red
    exit 1
}

# Build .NET backend
Write-Host "Building .NET backend..." -ForegroundColor Yellow
dotnet clean NetworkDiscovery.csproj
dotnet build NetworkDiscovery.csproj --configuration Release
if ($LASTEXITCODE -ne 0) {
    Write-Host "Error building .NET backend" -ForegroundColor Red
    exit 1
}

Write-Host "Build completed successfully!" -ForegroundColor Green

# Start the application with a delay between backend and frontend
Write-Host "Starting the application..." -ForegroundColor Green
Start-Sleep -Seconds 2  # Give some time for ports to be released
npm start

# Keep the window open if there's an error
if ($LASTEXITCODE -ne 0) {
    Write-Host "Press any key to exit..."
    $null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
}
