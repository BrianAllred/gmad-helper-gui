using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using GMADHelper;
using Microsoft.Win32;

namespace GMADHelperGUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        #region Properties and fields

        private GMADHelper.GMADHelper _gmad;
        private string _installLocation = "";

        #endregion

        #region UI Initialization

        public MainWindow()
        {
            InitializeComponent();
            if (!LoadGMAD()) FindGMAD();
            
            if (_gmad == null) return;
            _gmad.ExtractMessage += PostExtractMessage;
            _gmad.CreateMessge += PostCreateMessage;
        }

        private void PostExtractMessage(object sender, EventArgs e)
        {
            Append(TextBlockExtract, _gmad.LastMessage);
        }

        private void PostCreateMessage(object sender, EventArgs e)
        {
            Append(TextBlockCreate, _gmad.LastMessage);
        }

        #endregion

        #region Find Exe

        private bool LoadGMAD()
        {
            if (!File.Exists(System.Windows.Forms.Application.UserAppDataPath + "\\GMADHelper.ini"))
            {
                File.Create(System.Windows.Forms.Application.UserAppDataPath + "\\GMADHelper.ini").Close();
            }
            
            _installLocation = File.ReadAllText(System.Windows.Forms.Application.UserAppDataPath + "\\GMADHelper.ini");
            try
            {
                _gmad = new GMADHelper.GMADHelper(_installLocation + Path.DirectorySeparatorChar + "gmad.exe");
            }
            catch (GMADException ex)
            {
                Console.WriteLine(ex.ToString());
            }
            if (_gmad == null)
            {
                try
                {
                    _gmad =
                        new GMADHelper.GMADHelper(_installLocation + Path.DirectorySeparatorChar + "bin" +
                                                  Path.DirectorySeparatorChar + "gmad.exe");
                }
                catch (GMADException ex)
                {
                    Console.WriteLine(ex.ToString());
                    return false;
                }
            }
            return true;
        }

        private void FindGMAD()
        {
            if (
                MessageBox.Show(
                    "GMADHelper will try to locate gmad.exe automatically. This may take a while. Once found, its location will be saved for future usage. Click OK to continue or Cancel to find manually.",
                    "Find gmad.exe", MessageBoxButton.OKCancel) == MessageBoxResult.OK)
            {
                if (!AutoFindGMAD())
                {
                    MessageBox.Show("Click OK to find \"gmad.exe\".", "Couldn't find gmad.exe automatically");
                    ManualFindGMAD();
                }
            }
            else
            {
                ManualFindGMAD();
            }
            if (File.Exists(_installLocation + Path.DirectorySeparatorChar + "gmad.exe"))
            {
                File.WriteAllText(System.Windows.Forms.Application.UserAppDataPath + "\\GMADHelper.ini",
                    _installLocation);
            }
            else if(File.Exists(_installLocation + Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar + "gmad.exe"))
            {
                File.WriteAllText(System.Windows.Forms.Application.UserAppDataPath + "\\GMADHelper.ini",
                     _installLocation + Path.DirectorySeparatorChar);
            }
        }

        private void ManualFindGMAD()
        {
            while (true)
            {
                var dialog = new OpenFileDialog {FileName = "gmad.exe", DefaultExt = ".exe", Filter = "GMAD Executable|gmad.exe"};
                if (dialog.ShowDialog() == true)
                {
                    try
                    {
                        _gmad = new GMADHelper.GMADHelper(dialog.FileName);
                        _installLocation = Path.GetDirectoryName(dialog.FileName);
                        return;
                    }
                    catch (GMADException e)
                    {
                        Console.WriteLine(e);
                    }
                }
                if (MessageBox.Show("Would you like to try again?", "Couldn't find gmad.exe", MessageBoxButton.YesNo) == MessageBoxResult.Yes) continue;
                Application.Current.Shutdown();
                break;
            }
        }

        private bool AutoFindGMAD()
        {
            RegistryKey regKey = Registry.LocalMachine.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Uninstall");
            _installLocation = FindPathByDisplayName(regKey, @"Garry's Mod");
            if (String.IsNullOrWhiteSpace(_installLocation)) return false;
            try
            {
                _gmad = new GMADHelper.GMADHelper(_installLocation + Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar + "gmad.exe");
                return true;
            }
            catch (GMADException e)
            {
                Console.WriteLine(e);
                return false;
            }
        }

        private string FindPathByDisplayName(RegistryKey parentKey, string name)
        {
            string[] nameList = parentKey.GetSubKeyNames();
            foreach (RegistryKey regKey in nameList.Select(parentKey.OpenSubKey))
            {
                try
                {
                    if (regKey.GetValue("DisplayName").ToString() == name)
                    {
                        return regKey.GetValue("InstallLocation").ToString();
                    }
                }
                catch(Exception e)
                {
                    Console.WriteLine(e.ToString());
                }
            }
            return "";
        }

        #endregion

        #region UI Events
        #region TextBox Events

        private void TextAddonExtract_MouseDown(object sender, RoutedEventArgs e)
        {
            var dialog = new FolderBrowserDialogEx { ShowNewFolderButton = false, ShowBothFilesAndFolders = true};
            if (TextAddonExtract.Text != "Click here to select addon or addon directory...")
                dialog.SelectedPath = TextAddonExtract.Text;
            else
            {
                dialog.SelectedPath = _installLocation + Path.DirectorySeparatorChar + "garrysmod" + Path.DirectorySeparatorChar + "addons";
            }
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                TextAddonExtract.Text = dialog.SelectedPath;
            }
            LoseFocus(TextAddonExtract);
            if (Directory.Exists(TextAddonExtract.Text))
            {
                CheckLUA.IsEnabled = true;
            }
            else
            {
                CheckLUA.IsEnabled = false;
                CheckLUA.IsChecked = false;
            }
        }

        private void TextOutExtract_MouseDown(object sender, RoutedEventArgs e)
        {
            var dialog = new FolderBrowserDialogEx {ShowNewFolderButton = true};
            if (TextOutExtract.Text != "Click here to select output (default is parent directory of addon)...")
                dialog.SelectedPath = TextOutExtract.Text;
            else
            {
                if (TextAddonExtract.Text != "Click here to select addon or addon directory...")
                    dialog.SelectedPath = TextAddonExtract.Text;
            }
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                TextOutExtract.Text = dialog.SelectedPath;
            }
            LoseFocus(TextOutExtract);
        }

        private void TextAddonCreate_MouseDown(object sender, RoutedEventArgs e)
        {
            var dialog = new FolderBrowserDialogEx {ShowNewFolderButton = false};
            if (TextAddonCreate.Text != "Click here to select addon directory...")
            {
                dialog.SelectedPath = TextAddonCreate.Text;
            }
            else
            {
                dialog.SelectedPath = _installLocation;
            }
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                TextAddonCreate.Text = dialog.SelectedPath;
            }
            LoseFocus(TextAddonCreate);
        }

        private void TextOutCreate_MouseDown(object sender, RoutedEventArgs e)
        {
            var dialog = new SaveFileDialog { DefaultExt = ".gma", Filter = "Garry's Mod Addon|*.gma" };
            if (TextOutCreate.Text != "Click here to select output path for addon file...")
            {
                dialog.InitialDirectory = Path.GetDirectoryName(TextOutCreate.Text);
                dialog.FileName = TextOutCreate.Text;
            }
            else if (TextAddonCreate.Text != "Click here to select addon directory...")
            {
                dialog.InitialDirectory = TextAddonCreate.Text;
                dialog.FileName = TextAddonCreate.Text;
            }
            else {
                dialog.InitialDirectory = _installLocation;
            }
            if (dialog.ShowDialog() == true)
            {
                TextOutCreate.Text = dialog.FileName;
            }
            LoseFocus(TextOutCreate);
        }

        #endregion
        #region Button Events

        private void ButtonExtract_Click(object sender, RoutedEventArgs e)
        {
            if (File.Exists(TextAddonExtract.Text) || Directory.Exists(TextAddonExtract.Text))
            {
                if (Directory.Exists(TextOutExtract.Text))
                {
                    try
                    {
                        Append(TextBlockExtract, "Extracting \"" +
                            TextAddonExtract.Text +
                            "\" to \"" + TextOutExtract.Text + "\"");
                        _gmad.Extract(TextAddonExtract.Text, TextOutExtract.Text, CheckConsoleOutputExtract.IsChecked != null && (bool)CheckConsoleOutputExtract.IsChecked);
                    }
                    catch (GMADException ex)
                    {
                        Append(TextBlockExtract, ex.ToString());
                    }
                }
                else
                {
                    try
                    {
                        Append(TextBlockExtract, "Extracting \"" +
                            TextAddonExtract.Text +
                            "\" to \"" +
                            TextAddonExtract.Text.Substring(0,
                                TextAddonExtract.Text.LastIndexOf(Path.DirectorySeparatorChar)) + "\"");
                        _gmad.Extract(TextAddonExtract.Text, CheckConsoleOutputExtract.IsChecked != null && (bool)CheckConsoleOutputExtract.IsChecked);
                    }
                    catch (GMADException ex)
                    {
                        Append(TextBlockExtract, ex.ToString());
                    }
                }
                if (CheckLUA.IsChecked == true) _gmad.WriteLua();
            }
            else
            {
                Append(TextBlockExtract, "File or folder \"" + TextAddonExtract.Text +
                    "\" does not exist.");
            }
        }

        private void ButtonCreate_Click(object sender, RoutedEventArgs e)
        {
            if (Directory.Exists(TextAddonCreate.Text))
            {
                try
                {
                    Append(TextBlockCreate, "Creating \"" + TextOutCreate.Text + "\" from directory \"" + TextAddonCreate.Text + "\"...");
                    _gmad.Create(TextAddonCreate.Text, TextOutCreate.Text, CheckConsoleOutputCreate.IsChecked != null && (bool)CheckConsoleOutputCreate.IsChecked);
                }
                catch (GMADException ex)
                {
                    Append(TextBlockCreate, ex.ToString());
                }
            }
            else
            {
                Append(TextBlockCreate, "Directory \"" + TextAddonCreate.Text + "\" does not exist.");
            }
        }

        #endregion
        #region TextBlock Events

        private void TextBlockExtract_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBlockExtract.ScrollToEnd();
        }

        private void TextBlockCreate_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBlockCreate.ScrollToEnd();
        }

        #endregion
        #region Helper Functions

        private static void LoseFocus(FrameworkElement textBox)
        {
            var parent = (FrameworkElement)textBox.Parent;
            while (parent != null && !((IInputElement)parent).Focusable)
            {
                parent = (FrameworkElement)parent.Parent;
            }

            DependencyObject scope = FocusManager.GetFocusScope(textBox);
            FocusManager.SetFocusedElement(scope, parent);
        }

        private static void Append(TextBox textBox, string message)
        {
            textBox.Text = String.Concat(textBox.Text, message, "\n");
        }

        #endregion
        #endregion

    }
}
