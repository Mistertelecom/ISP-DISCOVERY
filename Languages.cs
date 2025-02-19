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
                ["Brand"] = "Brand",
                ["IPAddress"] = "IP Address",
                ["MACAddress"] = "MAC Address",
                ["Name"] = "Name",
                ["DiscoveryMethod"] = "Discovery Method",
                ["Model"] = "Model",
                ["LogTitle"] = "Network Activity Log",
                ["Settings"] = "Settings",
                ["Language"] = "Language:",
                ["DarkMode"] = "Dark Mode",
                ["Save"] = "Save",
                ["ErrorNoInterface"] = "Please select a network interface.",
                ["ErrorCapture"] = "Error starting capture: {0}",
                ["PacketCaptured"] = "Packet captured from {0}",
                ["DeviceFound"] = "Device found: {0} ({1})"
            },
            ["pt-BR"] = new Dictionary<string, string>
            {
                ["Title"] = "ISP Discovery by Jp Tools v1.2",
                ["SelectInterface"] = "Selecione a Interface de Rede:",
                ["StartScan"] = "Iniciar varredura",
                ["StopScan"] = "Parar varredura",
                ["Brand"] = "Marca",
                ["IPAddress"] = "Endereço IP",
                ["MACAddress"] = "Endereço MAC",
                ["Name"] = "Nome",
                ["DiscoveryMethod"] = "Método de Descoberta",
                ["Model"] = "Modelo",
                ["LogTitle"] = "Log de Atividade de Rede",
                ["Settings"] = "Configurações",
                ["Language"] = "Idioma:",
                ["DarkMode"] = "Modo Escuro",
                ["Save"] = "Salvar",
                ["ErrorNoInterface"] = "Por favor, selecione uma interface de rede.",
                ["ErrorCapture"] = "Erro ao iniciar captura: {0}",
                ["PacketCaptured"] = "Pacote capturado de {0}",
                ["DeviceFound"] = "Dispositivo encontrado: {0} ({1})"
            }
        };
    }
}