using MaterialSkin;
using MaterialSkin.Controls;
using System;
using System.Windows.Forms;
using static MaterialSkin.MaterialSkinManager;

namespace NetworkDiscovery
{
    public partial class SettingsForm : MaterialForm
    {
        private MaterialComboBox languageComboBox;
        private MaterialSwitch themeSwitch;
        private MaterialButton btnSave;
        public delegate void SettingsChangedEventHandler();
        public event SettingsChangedEventHandler SettingsChanged;

        public SettingsForm()
        {
            InitializeComponent();
            
            // Initialize MaterialSkin
            var materialSkinManager = MaterialSkinManager.Instance;
            materialSkinManager.ColorScheme = new ColorScheme(
                Primary.Blue800,
                Primary.Blue900,
                Primary.Blue500,
                Accent.LightBlue200,
                TextShade.WHITE
            );
            materialSkinManager.AddFormToManage(this);
            materialSkinManager.Theme = AppConfig.DarkMode ? MaterialSkinManager.Themes.DARK : MaterialSkinManager.Themes.LIGHT;
            
            // Initialize controls
            languageComboBox = new MaterialComboBox { Width = 200, Margin = new Padding(10) };
            languageComboBox.Items.AddRange(new[] { "English", "Português" });
            languageComboBox.SelectedIndex = AppConfig.Language == "en" ? 0 : 1;

            themeSwitch = new MaterialSwitch { 
                Text = Languages.Translations[AppConfig.Language]["DarkMode"], 
                Checked = AppConfig.DarkMode, 
                Margin = new Padding(10) 
            };
            btnSave = new MaterialButton { 
                Text = Languages.Translations[AppConfig.Language]["Save"], 
                Width = 100, 
                Margin = new Padding(10) 
            };

            // Layout
            var panel = new FlowLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(20) };
            panel.Controls.Add(new MaterialLabel { Text = Languages.Translations[AppConfig.Language]["Language"] });
            panel.Controls.Add(languageComboBox);
            panel.Controls.Add(themeSwitch);
            panel.Controls.Add(btnSave);

            btnSave.Click += (sender, e) => 
            {
                string newLanguage = languageComboBox.SelectedIndex == 0 ? "en" : "pt-BR";
                bool newDarkMode = themeSwitch.Checked;
                
                if (newLanguage != AppConfig.Language || newDarkMode != AppConfig.DarkMode)
                {
                    AppConfig.Language = newLanguage;
                    AppConfig.DarkMode = newDarkMode;
                    AppConfig.Save();
                    
                    // Update UI with new language
                    themeSwitch.Text = Languages.Translations[newLanguage]["DarkMode"];
                    btnSave.Text = Languages.Translations[newLanguage]["Save"];
                    
                    // Notify main form of changes
                    SettingsChanged?.Invoke();
                }
                
                this.Close();
            };

            this.Controls.Add(panel);
        }
    }
}