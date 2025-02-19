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
        private MaterialButton scanButton;
        private MaterialMultiLineTextBox2 logTextBox;
        private MaterialLabel interfaceLabel;
        
        public MainForm()
        {
            InitializeComponent();
            AppConfig.Load();
            InitializeMaterialSkin();
            InitializeSniffer();
            LoadNetworkInterfaces();
            UpdateLanguage();
        }

        private void InitializeMaterialSkin()
        {
            var materialSkinManager = MaterialSkinManager.Instance;
            materialSkinManager.AddFormToManage(this);
            materialSkinManager.Theme = AppConfig.DarkMode ? 
                MaterialSkinManager.Themes.DARK : 
                MaterialSkinManager.Themes.LIGHT;
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

            scanButton = new MaterialButton
            {
                Text = "Iniciar Varredura",
                Margin = new Padding(10, 10, 15, 0),
                AutoSize = true,
                Font = new Font("Segoe UI", 10, FontStyle.Regular)
            };

            var settingsButton = new MaterialButton
            {
                Text = "⚙",
                Margin = new Padding(10),
                MinimumSize = new Size(40, 36),
                AutoSize = true
            };

            scanButton.Click += ScanButton_Click;
            settingsButton.Click += (s, e) => {
                var settingsForm = new SettingsForm { Owner = this };
                settingsForm.SettingsChanged += () => {
                    UpdateLanguage();
                    var materialSkinManager = MaterialSkinManager.Instance;
                    materialSkinManager.Theme = AppConfig.DarkMode ? 
                        MaterialSkinManager.Themes.DARK : 
                        MaterialSkinManager.Themes.LIGHT;
                };
                settingsForm.ShowDialog();
            };

            panel.Controls.Add(interfaceLabel);
            panel.Controls.Add(interfaceComboBox);
            panel.Controls.Add(scanButton);
            panel.Controls.Add(settingsButton);
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
            deviceListView.Columns.Add("Modelo", 150);

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

        public void UpdateLanguage()
        {
            var trans = Languages.Translations[AppConfig.Language];
            
            // Atualizar interface
            this.Text = trans["Title"];
            interfaceLabel.Text = trans["SelectInterface"];
            scanButton.Text = trans["StartScan"];
            
            // Atualizar colunas
            deviceListView.Columns[0].Text = trans["Brand"];
            deviceListView.Columns[1].Text = trans["IPAddress"];
            deviceListView.Columns[2].Text = trans["MACAddress"];
            deviceListView.Columns[3].Text = trans["Name"];
            deviceListView.Columns[4].Text = trans["DiscoveryMethod"];
            deviceListView.Columns[5].Text = trans["Model"];
            
            // logTextBox.LabelText = trans["LogTitle"]; // Removed because MaterialMultiLineTextBox2 does not have LabelText property
        }

        private void Sniffer_OnPacketCaptured(object sender, PacketCaptureEventArgs e)
        {
            logTextBox.Text += $"[{DateTime.Now:HH:mm:ss.fff}] " +
                string.Format(Languages.Translations[AppConfig.Language]["PacketCaptured"], e.SourceIP)
                + Environment.NewLine;
        }

        private void Sniffer_OnDeviceDiscovered(object sender, Device device)
        {
            AddDeviceToList(device);
            logTextBox.Text += $"[{DateTime.Now:HH:mm:ss.fff}] " +
                string.Format(Languages.Translations[AppConfig.Language]["DeviceFound"], device.Brand, device.IPAddress)
                + Environment.NewLine;
        }

        private void AddDeviceToList(Device device)
        {
            var row = new ListViewItem(device.Brand);
            row.SubItems.Add(device.IPAddress);
            row.SubItems.Add(device.MacAddress);
            row.SubItems.Add(device.Name);
            row.SubItems.Add(device.DiscoveryMethod);
            row.SubItems.Add(device.Model);
            deviceListView.Items.Add(row);
        }

        private async void ScanButton_Click(object sender, EventArgs e)
        {
            if (interfaceComboBox.SelectedItem == null)
            {
                MessageBox.Show(Languages.Translations[AppConfig.Language]["ErrorNoInterface"], 
                    "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (scanButton.Text == Languages.Translations[AppConfig.Language]["StartScan"])
            {
                try
                {
                    var selectedInterface = (NetworkInterfaceItem)interfaceComboBox.SelectedItem;
                    deviceListView.Items.Clear();
                    logTextBox.Text = "";
                    
                    await Task.Run(() => sniffer.StartCapture(selectedInterface.Device.Name));
                    
                    scanButton.Text = Languages.Translations[AppConfig.Language]["StopScan"];
                    interfaceComboBox.Enabled = false;
                }
                catch (Exception ex)
                {
                    MessageBox.Show(string.Format(Languages.Translations[AppConfig.Language]["ErrorCapture"], ex.Message),
                        "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else
            {
                sniffer.StopCapture();
                scanButton.Text = Languages.Translations[AppConfig.Language]["StartScan"];
                interfaceComboBox.Enabled = true;
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
        public NetworkInterfaceItem(ICaptureDevice device) => Device = device;
        public override string ToString() => Device.Description;
    }
}