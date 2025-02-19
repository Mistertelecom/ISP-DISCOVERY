using System.Collections.Generic;

namespace NetworkDiscovery
{
    public static class Languages
    {
        public static Dictionary<string, Dictionary<string, string>> Translations = new Dictionary<string, Dictionary<string, string>>
        {
            ["en"] = new Dictionary<string, string>
            {
                ["Title"] = "ISP Discovery by Jp Tools v1.2",
                ["SelectInterface"] = "Select Network Interface:",
                ["StartScan"] = "Start scanning",
                ["StopScan"] = "Stop scanning",
                ["ShowLog"] = "Show log",
                ["HideLog"] = "Hide log",
                ["Ready"] = "Ready to scan",
                ["Scanning"] = "Scanning network...",
                ["ScanComplete"] = "Scan completed",
                ["Brand"] = "Brand",
                ["IPAddress"] = "IP Address",
                ["MACAddress"] = "MAC Address",
                ["Name"] = "Name",
                ["Interface"] = "Interface",
                ["LogTitle"] = "Network Activity Log",
                ["SelectLanguage"] = "Language:",
                ["PacketCaptured"] = "Packet captured from {0}",
                ["DeviceFound"] = "New device found: {0} ({1})",
                ["ErrorNoInterface"] = "Please select a network interface",
                ["ErrorCapture"] = "Error starting capture: {0}"
            },
            ["pt-BR"] = new Dictionary<string, string>
            {
                ["Title"] = "ISP Discovery by Jp Tools v1.2",
                ["SelectInterface"] = "Selecione a Interface de Rede:",
                ["StartScan"] = "Iniciar varredura",
                ["StopScan"] = "Parar varredura",
                ["ShowLog"] = "Mostrar log",
                ["HideLog"] = "Ocultar log",
                ["Ready"] = "Pronto para iniciar",
                ["Scanning"] = "Procurando dispositivos...",
                ["ScanComplete"] = "Varredura concluída",
                ["Brand"] = "Marca",
                ["IPAddress"] = "Endereço IP",
                ["MACAddress"] = "Endereço MAC",
                ["Name"] = "Nome",
                ["Interface"] = "Interface",
                ["LogTitle"] = "Log de Atividade de Rede",
                ["SelectLanguage"] = "Idioma:",
                ["PacketCaptured"] = "Pacote capturado de {0}",
                ["DeviceFound"] = "Novo dispositivo encontrado: {0} ({1})",
                ["ErrorNoInterface"] = "Por favor, selecione uma interface de rede",
                ["ErrorCapture"] = "Erro ao iniciar captura: {0}"
            }
        };
    }
}
