using MaterialSkin;
using MaterialSkin.Controls;
using System;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using SharpPcap;

namespace NetworkDiscovery
{
    public partial class MainForm : MaterialForm
    {
        private NetworkSniffer sniffer;
        private MaterialListView deviceListView;
        private MaterialComboBox interfaceComboBox;
        private MaterialComboBox languageComboBox;
        private MaterialButton scanButton;
        private MaterialMultiLineTextBox2 logTextBox;
        private MaterialLabel interfaceLabel;
        private MaterialLabel languageLabel;
        private string currentLanguage = "en";

        public MainForm()
        {
            InitializeComponent();
            InitializeMaterialSkin();
            InitializeSniffer();
            LoadNetworkInterfaces();
            languageComboBox.Items.Add("English");
            languageComboBox.Items.Add("Português");
            languageComboBox.SelectedIndex = 0;
            UpdateLanguage();
        }

        private void InitializeMaterialSkin()
        {
            var materialSkinManager = MaterialSkinManager.Instance;
            materialSkinManager.AddFormToManage(this);
            materialSkinManager.Theme = MaterialSkinManager.Themes.LIGHT;
            materialSkinManager.ColorScheme = new ColorScheme(
                Primary.Blue600, Primary.Blue700, Primary.Blue100, Accent.LightBlue200, TextShade.WHITE);
        }

        private void InitializeComponent()
        {
            this.Text = "ISP Discovery by Jp Tools v1.2";
            this.Size = new Size(1000, 700);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.MinimumSize = new Size(1000, 700);

            var mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 2,
                ColumnCount = 1,
                Padding = new Padding(10),
                BackColor = Color.White
            };
            mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            var topFlow = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                AutoSize = true,
                BackColor = Color.Transparent,
                Padding = new Padding(5),
                Margin = new Padding(0, 0, 0, 5),
                AutoScroll = true
            };
            ConfigureTopPanel(topFlow);

            var contentPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White
            };
            ConfigureContentPanel(contentPanel);

            mainLayout.Controls.Add(topFlow, 0, 0);
            mainLayout.Controls.Add(contentPanel, 0, 1);
            this.Controls.Add(mainLayout);
        }

        private void ConfigureTopPanel(FlowLayoutPanel panel)
        {
            interfaceLabel = new MaterialLabel
            {
                Text = "Interface de Rede",
                AutoSize = true,
                Margin = new Padding(0, 15, 5, 0),
                Font = new Font("Segoe UI", 10, FontStyle.Regular),
                TextAlign = ContentAlignment.MiddleCenter
            };

            interfaceComboBox = new MaterialComboBox
            {
                Width = 220,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Margin = new Padding(0, 10, 15, 0),
                Font = new Font("Segoe UI", 10, FontStyle.Regular)
            };

            languageLabel = new MaterialLabel
            {
                Text = "Idioma",
                AutoSize = true,
                Margin = new Padding(20, 15, 5, 0),
                Font = new Font("Segoe UI", 10, FontStyle.Regular),
                TextAlign = ContentAlignment.MiddleCenter
            };

            languageComboBox = new MaterialComboBox
            {
                Width = 170,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Margin = new Padding(0, 10, 15, 0),
                Font = new Font("Segoe UI", 10, FontStyle.Regular)
            };

            scanButton = new MaterialButton
            {
                Text = "Iniciar Varredura",
                Margin = new Padding(10, 10, 15, 0),
                AutoSize = true,
                Font = new Font("Segoe UI", 10, FontStyle.Regular)
            };

            scanButton.Click += ScanButton_Click;
            languageComboBox.SelectedIndexChanged += LanguageComboBox_SelectedIndexChanged;

            panel.Controls.Add(interfaceLabel);
            panel.Controls.Add(interfaceComboBox);
            panel.Controls.Add(languageLabel);
            panel.Controls.Add(languageComboBox);
            panel.Controls.Add(scanButton);
        }

        private void ConfigureContentPanel(Panel panel)
        {
            deviceListView = new MaterialListView
            {
                Dock = DockStyle.Fill,
                FullRowSelect = true,
                GridLines = true,
                MultiSelect = false,
                BorderStyle = BorderStyle.None,
                AutoSizeTable = false,
                BackColor = Color.White,
                Font = new Font("Segoe UI", 10, FontStyle.Regular)
            };
            deviceListView.Columns.Add("Marca", 100);
            deviceListView.Columns.Add("Endereço IP", 150);
            deviceListView.Columns.Add("Endereço MAC", 150);
            deviceListView.Columns.Add("Nome", 200);
            deviceListView.Columns.Add("Método de Descoberta", 150);

            logTextBox = new MaterialMultiLineTextBox2
            {
                Dock = DockStyle.Bottom,
                Height = 200,
                ReadOnly = true,
                Visible = true,
                Margin = new Padding(0),
                ScrollBars = ScrollBars.Vertical,
                Font = new Font("Segoe UI", 10, FontStyle.Regular)
            };

            panel.Controls.Add(deviceListView);
            panel.Controls.Add(logTextBox);
        }

        private void InitializeSniffer()
        {
            sniffer = new NetworkSniffer();
            sniffer.OnDeviceDiscovered += Sniffer_OnDeviceDiscovered;
            sniffer.OnPacketCaptured += Sniffer_OnPacketCaptured;
        }

        private void LoadNetworkInterfaces()
        {
            var devices = CaptureDeviceList.Instance;
            foreach (var dev in devices)
                interfaceComboBox.Items.Add(new NetworkInterfaceItem(dev));

            if (interfaceComboBox.Items.Count > 0)
                interfaceComboBox.SelectedIndex = 0;
        }

        private void LanguageComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            currentLanguage = languageComboBox.SelectedIndex == 0 ? "en" : "pt-BR";
            UpdateLanguage();
        }

        private void UpdateLanguage()
        {
            var trans = Languages.Translations[currentLanguage];
            this.Text = trans["Title"];
            interfaceLabel.Text = trans["SelectInterface"];
            languageLabel.Text = trans["SelectLanguage"];
            scanButton.Text = trans["StartScan"];
        }

        private void Sniffer_OnPacketCaptured(object sender, PacketCaptureEventArgs e)
        {
            logTextBox.Text += $"[{DateTime.Now:HH:mm:ss.fff}] " +
                string.Format(Languages.Translations[currentLanguage]["PacketCaptured"], e.SourceIP)
                + Environment.NewLine;
        }

        private void Sniffer_OnDeviceDiscovered(object sender, Device device)
        {
            AddDeviceToList(device);
            logTextBox.Text += $"[{DateTime.Now:HH:mm:ss.fff}] " +
                string.Format(Languages.Translations[currentLanguage]["DeviceFound"], device.Brand, device.IPAddress)
                + Environment.NewLine;
        }

        private void AddDeviceToList(Device device)
        {
            var row = new ListViewItem(device.Brand);
            row.SubItems.Add(device.IPAddress);
            row.SubItems.Add(device.MacAddress);
            row.SubItems.Add(device.Name);
            row.SubItems.Add(device.DiscoveryMethod);
            deviceListView.Items.Add(row);
        }

        private async void ScanButton_Click(object sender, EventArgs e)
        {
            if (interfaceComboBox.SelectedItem == null)
            {
                MessageBox.Show(Languages.Translations[currentLanguage]["ErrorNoInterface"], 
                    "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (scanButton.Text == Languages.Translations[currentLanguage]["StartScan"])
            {
                try
                {
                    var selectedInterface = (NetworkInterfaceItem)interfaceComboBox.SelectedItem;
                    deviceListView.Items.Clear();
                    logTextBox.Text = "";

                    // Inicia a varredura em uma thread separada
                    await Task.Run(() => sniffer.StartCapture(selectedInterface.Device.Name));

                    scanButton.Text = Languages.Translations[currentLanguage]["StopScan"];
                    interfaceComboBox.Enabled = false;
                    languageComboBox.Enabled = false;

                    logTextBox.Text += $"[{DateTime.Now:HH:mm:ss.fff}] " +
                        Languages.Translations[currentLanguage]["Scanning"] + Environment.NewLine;
                }
                catch (Exception ex)
                {
                    MessageBox.Show(string.Format(Languages.Translations[currentLanguage]["ErrorCapture"], ex.Message),
                        "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else
            {
                sniffer.StopCapture();
                scanButton.Text = Languages.Translations[currentLanguage]["StartScan"];
                interfaceComboBox.Enabled = true;
                languageComboBox.Enabled = true;

                logTextBox.Text += $"[{DateTime.Now:HH:mm:ss.fff}] " +
                    Languages.Translations[currentLanguage]["ScanComplete"] + Environment.NewLine;
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            sniffer.StopCapture();
            base.OnFormClosing(e);
        }
    }

    public class NetworkInterfaceItem
    {
        public ICaptureDevice Device { get; }
        public NetworkInterfaceItem(ICaptureDevice device)
        {
            Device = device;
        }
        public override string ToString() => Device.Description;
    }
}