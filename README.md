# ISP Discovery by Jp Tools 🌐

## Description
ISP Discovery is a network scanning application that helps discover and monitor devices on your local network. This version features a modern web-based UI built with Electron and a powerful .NET Core backend for network operations.

## Features
- Modern, responsive UI with dark/light theme support
- Real-time device discovery and monitoring
- Network packet capture and analysis
- Cross-platform compatibility
- WebSocket-based real-time updates

## Prerequisites
- Node.js (v14 or later)
- .NET 6.0 SDK
- npm (comes with Node.js)

## Installation

1. Clone the repository:
```bash
git clone https://github.com/yourusername/ISP-DISCOVERY.git
cd ISP-DISCOVERY
```

2. Run the build script:
```bash
build.bat
```

This will:
- Install Node.js dependencies
- Build the Tailwind CSS styles
- Restore .NET packages
- Build the .NET backend
- Start the application

## Development

To run the application in development mode with DevTools:
```bash
npm run dev
```

## Architecture
- Frontend: Electron + HTML/CSS/JavaScript with Tailwind CSS
- Backend: .NET 6.0 Web API with WebSocket support
- Network Operations: SharpPcap for packet capture and analysis

## License
MIT License - See LICENSE file for details

## Version
v1.2
