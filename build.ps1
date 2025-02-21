# PowerShell Build Script

Write-Host "Building ISP Discovery..." -ForegroundColor Green

# Step 1: Install Node.js dependencies
Write-Host "Installing Node.js dependencies..." -ForegroundColor Yellow
npm install
if ($LASTEXITCODE -ne 0) {
    Write-Host "Error installing Node.js dependencies" -ForegroundColor Red
    exit 1
}

# Step 2: Install Electron globally if not present
Write-Host "Installing Electron globally..." -ForegroundColor Yellow
npm install -g electron
if ($LASTEXITCODE -ne 0) {
    Write-Host "Error installing Electron globally" -ForegroundColor Red
    exit 1
}

# Step 3: Build Tailwind CSS
Write-Host "Building Tailwind CSS..." -ForegroundColor Yellow
if (!(Test-Path "styles")) {
    New-Item -ItemType Directory -Path "styles"
}
npx tailwindcss -i ./styles/input.css -o ./styles/output.css
if ($LASTEXITCODE -ne 0) {
    Write-Host "Error building Tailwind CSS" -ForegroundColor Red
    exit 1
}

# Step 4: Build .NET backend
Write-Host "Building .NET backend..." -ForegroundColor Yellow
dotnet restore NetworkDiscovery.csproj
if ($LASTEXITCODE -ne 0) {
    Write-Host "Error restoring .NET packages" -ForegroundColor Red
    exit 1
}

dotnet build NetworkDiscovery.csproj --configuration Release
if ($LASTEXITCODE -ne 0) {
    Write-Host "Error building .NET backend" -ForegroundColor Red
    exit 1
}

Write-Host "Build completed successfully!" -ForegroundColor Green
Write-Host "To start the application, run: npm start" -ForegroundColor Cyan
