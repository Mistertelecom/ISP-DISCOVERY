@echo off
echo Installing global dependencies...
call npm install -g electron

echo Installing project dependencies...
call npm install electron --save-dev
call npm install

echo Building Tailwind CSS...
call npx tailwindcss -i ./styles/input.css -o ./styles/output.css

echo Building .NET backend...
dotnet restore
dotnet build

echo Starting the application...
call npm start
