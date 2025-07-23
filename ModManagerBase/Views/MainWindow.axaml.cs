using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Interactivity;
using SharpCompress;
using Avalonia.Platform.Storage;
using SharpCompress.Archives;
using SharpCompress.Common;
using MsBox.Avalonia.Enums;
using MsBox.Avalonia;


namespace ModManagerBase.Views
{
    /// <summary>
    /// Interaction logic for MainWindow.axaml
    /// </summary>
    
    public partial class MainWindow : Window
    {
        /// This is largely copied from Pulsar. It's software also developed by me.
        private List<string> enabledmods = new List<string>();
        private bool isInitialized = false;
        private ObservableCollection<Meta> mods = new ObservableCollection<Meta>();

        public MainWindow()
        {
            InitializeComponent();
            ModsWindow(true);
            SettingsWindow.IsVisible = false;
            MusicWindow.IsVisible = false;
            Directory.CreateDirectory(Misc.Paths.mods);
            if (!System.IO.File.Exists(Misc.Jsons.settings))
            {
                var jsonoptions = new JsonSerializerOptions
                {
                    WriteIndented = true
                };
                Settings settings = new Settings();
                settings.DeployPath = "";
                settings.DefaultImage = 0;
                string jsonString = JsonSerializer.Serialize<Settings>(settings, jsonoptions);
                System.IO.File.WriteAllText(Misc.Jsons.settings, jsonString);
            }
            if (!System.IO.File.Exists(Misc.Jsons.enabled))
            {
                var jsonoptions = new JsonSerializerOptions
                {
                    WriteIndented = true
                };
                string jsonString = JsonSerializer.Serialize<List<string>>(new List<string>(), jsonoptions);
                System.IO.File.WriteAllText(Misc.Jsons.enabled, jsonString);
            }
            Refresh();
            isInitialized = true;
        }

        private string[] CountFolders(string folderPath)
        {
            if (Directory.Exists(folderPath))
            {
                string[] directories = Directory.GetDirectories(folderPath);
                int folderCount = directories.Length;
                return directories;
            }
            else
            {
                return null;
            }
        }

        public string CreateLinkImage(string link)
        {
            try
            {
                if (link.Contains("gamebanana.com"))
                {
                    return "Assets/Gamebanana.png";
                }
                else if (link.Contains("nexusmods.com"))
                {
                    return "Assets/Nexus.png";
                }
                else if (link.Contains("github.com"))
                {
                    return "Assets/Github.png";
                }
                else if (!string.IsNullOrWhiteSpace(link))
                {
                    return "Assets/Web.png";
                }
                else
                {
                    return null;
                }
            }
            catch { return null; }
        }
        public void Refresh()
        {
            try
            {
                try
                {
                    enabledmods.Clear();
                }
                catch { }
                enabledmods = QuickJson(false, enabledmods, "enabledmods.json");
            }
            catch { }
            mods.Clear();
            string[] griditems = CountFolders(Misc.Paths.mods);
            Settings settings = new Settings();
            List<string> blacklist = new List<string>();
            if (System.IO.File.Exists(Misc.Jsons.settings))
            {
                var jsonoptions = new JsonSerializerOptions
                {
                    WriteIndented = true
                };
                string jsonString = System.IO.File.ReadAllText(Misc.Jsons.settings);
                settings = JsonSerializer.Deserialize<Settings>(jsonString, jsonoptions);
                PathBox.Text = settings.DeployPath;
                DefPrevBox.SelectedIndex = settings.DefaultImage; 
                if (DefPrevBox.SelectedIndex < 0 || settings.DefaultImage < 0)
                {
                    settings.DefaultImage = 0;
                    DefPrevBox.SelectedIndex = 0;
                }
                try
                {
                    Preview.Source = new Bitmap(AssetLoader.Open(new Uri($"avares://{App.projectName}/Assets/Preview{DefPrevBox.SelectedIndex}.png", UriKind.RelativeOrAbsolute)));
                }
                catch (Exception e)
                {

                }
            }
            foreach (string modpath in griditems)
            {
                Meta mod = new Meta();
                string filepath = Path.Combine(modpath, "meta.json");
                if (!System.IO.File.Exists(filepath))
                {
                    string genid = modpath.Replace(Misc.Paths.mods, "");
                    mod.Name = mod.ID = genid = genid.TrimStart(Path.DirectorySeparatorChar);
                    var jsonoptions = new JsonSerializerOptions
                    {
                        WriteIndented = true
                    };
                    string jsonString = JsonSerializer.Serialize(mod, jsonoptions);
                    System.IO.File.WriteAllText(filepath, jsonString);
                }
                if (System.IO.File.Exists(filepath))
                {
                    var jsonoptions = new JsonSerializerOptions
                    {
                        WriteIndented = true
                    };
                    string jsonString = System.IO.File.ReadAllText(filepath);
                    mod = JsonSerializer.Deserialize<Meta>(jsonString, jsonoptions);
                    if (!mods.Contains(mod))
                    {
                        if (enabledmods.Contains(mod.ID))
                            mod.IsChecked = true;
                        else
                            mod.IsChecked = false;
                        mod.LinkImage = CreateLinkImage(mod.Link);
                        mods.Add(mod);
                    }
                }
            }
        }
        private void New_OnClick(object sender, RoutedEventArgs e)
        {
            var mp = new MakePack();
            mp.Show();
        }

        private void Folder_OnClick(object sender, RoutedEventArgs e)
        {
            string modpath = "";
            foreach (var item in ModDataGrid.SelectedItems)
            {
                Meta row = (Meta)item;
                if (row != null)
                {
                    foreach (string path in CountFolders(Misc.Paths.mods))
                    {
                        Meta mod = new Meta();
                        string filepath = Path.Combine(path, "meta.json");
                        if (!System.IO.File.Exists(filepath))
                        {
                            continue;
                        }
                        if (System.IO.File.Exists(filepath))
                        {
                            var jsonoptions = new JsonSerializerOptions
                            {
                                WriteIndented = true
                            };
                            string jsonString = System.IO.File.ReadAllText(filepath);
                            mod = JsonSerializer.Deserialize<Meta>(jsonString, jsonoptions);
                            if (mod.ID == row.ID)
                            {
                                modpath = path;
                            }
                        }
                    }
                    if (Directory.Exists(modpath))
                    {
                        try
                        {
                            ProcessStartInfo StartInformation = new ProcessStartInfo();
                            StartInformation.FileName = modpath;
                            StartInformation.UseShellExecute = true;
                            Process process = Process.Start(StartInformation);
                        }
                        catch { }
                    }
                }
            }
        }

        private async void Delete_OnClick(object sender, RoutedEventArgs e)
        {
            string modpath = "";
            foreach (var item in ModDataGrid.SelectedItems)
            {
                Meta row = (Meta)item;
                if (row != null)
                {
                    foreach (string path in CountFolders(Misc.Paths.mods))
                    {
                        Meta mod = new Meta();
                        string filepath = Path.Combine(path, "meta.json");
                        if (!System.IO.File.Exists(filepath))
                        {
                            continue;
                        }
                        if (System.IO.File.Exists(filepath))
                        {
                            var jsonoptions = new JsonSerializerOptions
                            {
                                WriteIndented = true
                            };
                            string jsonString = System.IO.File.ReadAllText(filepath);
                            mod = JsonSerializer.Deserialize<Meta>(jsonString, jsonoptions);
                            if (mod.ID == row.ID)
                            {
                                modpath = path;
                            }
                        }
                    }
                    var box = MessageBoxManager.GetMessageBoxStandard(
                        $"Delete {row.Name}",
                        $"Are you sure you want to delete {row.Name}?",
                        ButtonEnum.YesNo,
                        MsBox.Avalonia.Enums.Icon.Question
                    );

                    var result = await box.ShowAsPopupAsync(this);

                    if (result == ButtonResult.Yes)
                    {
                        Directory.Delete(modpath, true);
                    }
                }
            }
            Refresh();
        }

        private void currentrow(object sender, SelectionChangedEventArgs e)
        {
            Meta row = (Meta)ModDataGrid.SelectedItem;
            string modpath = "";
            try
            {
                if (string.IsNullOrWhiteSpace(row.Description))
                    DescBox.Text = "Create a mod manager yourself with this base. You're seeing this because this mod has no description, or no mod is selected.\n\nConfused about the buttons at the bottom? Hover over them for more info.";
                else
                    DescBox.Text = row.Description;
            }
            catch
            {
                DescBox.Text = "Create a mod manager yourself with this base. You're seeing this because this mod has no description, or no mod is selected.\n\nConfused about the buttons at the bottom? Hover over them for more info.";
            }
            try
            {
                foreach (string path in CountFolders(Misc.Paths.mods))
                {
                    Meta mod = new Meta();
                    string filepath = Path.Combine(path, "meta.json");
                    if (!System.IO.File.Exists(filepath))
                    {
                        continue;
                    }
                    if (System.IO.File.Exists(filepath))
                    {
                        var jsonoptions = new JsonSerializerOptions
                        {
                            WriteIndented = true
                        };
                        string jsonString = System.IO.File.ReadAllText(filepath);
                        mod = JsonSerializer.Deserialize<Meta>(jsonString, jsonoptions);
                        if (mod.ID == row.ID)
                        {
                            modpath = path;
                        }
                    }
                }
                if (System.IO.File.Exists(Path.Combine(modpath, "preview.webp")))
                {
                    string imagePath = Path.Combine(modpath, "preview.webp");

                    if (File.Exists(imagePath))
                    {
                        using var stream = File.OpenRead(imagePath);
                        Preview.Source = new Bitmap(stream);
                    }
                }
                else
                {
                    Preview.Source = new Bitmap(AssetLoader.Open(new Uri($"avares://{App.projectName}/Assets/Preview{DefPrevBox.SelectedIndex}.png", UriKind.RelativeOrAbsolute)));
                }
            }
            catch
            {
                Preview.Source = new Bitmap(AssetLoader.Open(new Uri($"avares://{App.projectName}/Assets/Preview{DefPrevBox.SelectedIndex}.png", UriKind.RelativeOrAbsolute)));
            }
        }

        private void Mods_Click(object sender, RoutedEventArgs e)
        {
            ModsImage.Source = new Bitmap(AssetLoader.Open(new Uri($"avares://{App.projectName}/Assets/ModsSel.png", UriKind.RelativeOrAbsolute)));
            SettingsImage.Source = new Bitmap(AssetLoader.Open(new Uri($"avares://{App.projectName}/Assets/SettingsUnsel.png", UriKind.RelativeOrAbsolute)));
            MusicImage.Source = new Bitmap(AssetLoader.Open(new Uri($"avares://{App.projectName}/Assets/OtherUnsel.png", UriKind.RelativeOrAbsolute)));
            ModsWindow(true);
            MusicWindow.IsVisible = false;
            SettingsWindow.IsVisible = false;
        }
        private void Settings_Click(object sender, RoutedEventArgs e)
        {
            ModsImage.Source = new Bitmap(AssetLoader.Open(new Uri($"avares://{App.projectName}/Assets/ModsUnsel.png", UriKind.RelativeOrAbsolute)));
            SettingsImage.Source = new Bitmap(AssetLoader.Open(new Uri($"avares://{App.projectName}/Assets/SettingsSel.png", UriKind.RelativeOrAbsolute)));
            MusicImage.Source = new Bitmap(AssetLoader.Open(new Uri($"avares://{App.projectName}/Assets/OtherUnsel.png", UriKind.RelativeOrAbsolute)));
            ModsWindow(false);
            MusicWindow.IsVisible = false;
            SettingsWindow.IsVisible = true;
        }
        private void Other_Click(object sender, RoutedEventArgs e)
        {
            ModsImage.Source = new Bitmap(AssetLoader.Open(new Uri($"avares://{App.projectName}/Assets/ModsUnsel.png", UriKind.RelativeOrAbsolute)));
            SettingsImage.Source = new Bitmap(AssetLoader.Open(new Uri($"avares://{App.projectName}/Assets/SettingsUnsel.png", UriKind.RelativeOrAbsolute)));
            MusicImage.Source = new Bitmap(AssetLoader.Open(new Uri($"avares://{App.projectName}/Assets/OtherSel.png", UriKind.RelativeOrAbsolute)));
            ModsWindow(false);
            MusicWindow.IsVisible = true;
            SettingsWindow.IsVisible = false;
        }

        private void Download_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start(new ProcessStartInfo
                {
                    FileName = "https://gamebanana.com/games/17208",
                    UseShellExecute = true
                });
            }
            catch { }
        }
        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            Refresh();
        }

        private async void Deploy_Click(object sender, RoutedEventArgs e)
        {
            // This looks different for every mod manager.
            // This button should be used to load the mods into the game, whether that's placing them into a folder or packing them into a file.
            // A basic function to determine whether a mod is enabled or not is provided.
            foreach (string path in CountFolders(Misc.Paths.mods))
            {
                Meta mod = new Meta();
                string filepath = Path.Combine(path, "meta.json");
                if (!System.IO.File.Exists(filepath))
                {
                    continue;
                }
                if (System.IO.File.Exists(filepath))
                {
                    var jsonoptions = new JsonSerializerOptions
                    {
                        WriteIndented = true
                    };
                    string jsonString = System.IO.File.ReadAllText(filepath);
                    mod = JsonSerializer.Deserialize<Meta>(jsonString, jsonoptions);
                    if (enabledmods.Contains(mod.ID))
                    {
                        // Continue function here.
                    }
                }
            }
        }

        private void ModsWindow(bool sender)
        {
            if (sender == false)
            {
                Mods.IsVisible = false;
                ModContent.IsVisible = false;
            }
            else
            {
                Mods.IsVisible = true;
                ModContent.IsVisible = true;
            }
        }

        private async void Path_Click(object sender, RoutedEventArgs e)
        {
            var files = await this.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
            {
                Title = "Select Mod Deploy Path",
                AllowMultiple = false
            });
            if (files.Count == 1)
            {
                if (!string.IsNullOrWhiteSpace(files[0].Path.LocalPath))
                {
                    PathBox.Text = files[0].Path.LocalPath;
                }
            }
        }

        private void PathBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!isInitialized) return;
            Settings settings = new Settings();
            settings.DeployPath = PathBox.Text;
            settings.DefaultImage = DefPrevBox.SelectedIndex;
            string jsonString = System.IO.File.ReadAllText(Misc.Jsons.settings);
            var jsonoptions = new JsonSerializerOptions
            {
                WriteIndented = true
            };
            jsonString = JsonSerializer.Serialize(settings, jsonoptions);
            System.IO.File.WriteAllText(Misc.Jsons.settings, jsonString);
            settings = JsonSerializer.Deserialize<Settings>(jsonString, jsonoptions);
            Refresh();
        }

        private void OpenFolder_Click(object sender, RoutedEventArgs e)
        {
            if (Directory.Exists(Misc.Paths.mods))
            {
                ProcessStartInfo StartInformation = new ProcessStartInfo();
                StartInformation.FileName = Misc.Paths.mods;
                StartInformation.UseShellExecute = true;
                Process process = Process.Start(StartInformation);
            }
        }

        private void OpenLink_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.CommandParameter is string url)
            {
                try
                {
                    System.Diagnostics.Process.Start(new ProcessStartInfo
                    {
                        FileName = url,
                        UseShellExecute = true
                    });
                }
                catch { }
            }
        }

        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox checkBox)
            {
                var row = checkBox.DataContext as Meta;
                if (row != null)
                {
                    if (!enabledmods.Contains(row.ID))
                        enabledmods.Add(row.ID);
                    QuickJson(true, enabledmods, "enabledmods.json");
                    enabledmods = QuickJson(false, enabledmods, "enabledmods.json");
                }
            }
        }

        private void CheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox checkBox)
            {
                var row = checkBox.DataContext as Meta;
                if (row != null)
                {
                    if (enabledmods.Contains(row.ID))
                        enabledmods.Remove(row.ID);
                    QuickJson(true, enabledmods, "enabledmods.json");
                    enabledmods = QuickJson(false, enabledmods, "enabledmods.json");
                }
            }
        }

        private List<string> QuickJson(bool write, List<string> what, string filename)
        {
            if (write)
            {
                var jsonoptions = new JsonSerializerOptions
                {
                    WriteIndented = true
                };
                string jsonString = JsonSerializer.Serialize(what, jsonoptions);
                System.IO.File.WriteAllText(Path.Combine(Misc.Paths.program, filename), jsonString);
                return null;
            }
            else
            {
                var jsonoptions = new JsonSerializerOptions
                {
                    WriteIndented = true
                };
                string jsonString = System.IO.File.ReadAllText(Path.Combine(Misc.Paths.program, filename));
                what = JsonSerializer.Deserialize<List<string>>(jsonString, jsonoptions);
                return what;
            }
        }

        public static string[] ListToArray(List<string> sender)
        {
            string[] send = new string[sender.Count];
            for (var i = 0; i < sender.Count; ++i)
                send[i] = sender[i];
            return send;
        }

        private void DefPrevBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!isInitialized) return;
            PathBox_TextChanged(null, null);
            Refresh();
        }

        private void Edit_OnClick(object sender, RoutedEventArgs e)
        {
            Meta row = (Meta)ModDataGrid.SelectedItem;
            MakePack edit = new MakePack(row);
            Preview.Source = new Bitmap(AssetLoader.Open(new Uri($"avares://{App.projectName}/Assets/Preview{DefPrevBox.SelectedIndex}.png", UriKind.RelativeOrAbsolute)));
            try
            {
                edit.ShowDialog(this);
            }
            catch { }
            Refresh();
        }
    }
}

