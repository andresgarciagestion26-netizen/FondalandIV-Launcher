using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using FondalandIVLauncher.Services;

namespace FondalandIVLauncher;

public partial class MainWindow : Window
{
    private readonly ModManager _mods = new();
    // Cambia esta URL por la ubicación real del manifest del servidor.
    private const string ManifestUrl = "https://TU-DOMINIO/fondaland/manifest.json";
    private readonly string _modsDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "FondalandIV", "mods");

    public MainWindow() { InitializeComponent(); Loaded += async (_, _) => await CheckModsAsync(); }

    private async Task CheckModsAsync()
    {
        try
        {
            Progress.Value = 15; Percent.Text = "15%"; BottomText.Text = "Consultando la versión del servidor...";
            if (ManifestUrl.Contains("TU-DOMINIO"))
            {
                ModsCount.Text = "-- / --"; StatusText.Text = "⚠ Configura el servidor"; StatusText.Foreground = System.Windows.Media.Brushes.Gold;
                DetailText.Text = "El launcher ya está preparado. Falta conectar el manifest real del servidor.";
                BottomText.Text = "Modo desarrollo: configura ManifestUrl en MainWindow.xaml.cs.";
                Progress.Value = 100; Percent.Text = "100%"; return;
            }
            var manifest = await _mods.LoadManifestAsync(ManifestUrl);
            var result = await _mods.CheckAsync(_modsDir, manifest);
            ModsCount.Text = $"{result.ok} / {manifest.Mods.Count}";
            Progress.Value = 100; Percent.Text = "100%";
            if (result.missing == 0 && result.outdated == 0)
            {
                StatusText.Text = "✓  Todo actualizado"; StatusText.Foreground = System.Windows.Media.Brushes.LightGreen;
                DetailText.Text = $"{result.ok} mods verificados correctamente.";
                BottomText.Text = "Todos los mods están actualizados.";
            }
            else
            {
                StatusText.Text = "⚠  Actualización necesaria"; StatusText.Foreground = System.Windows.Media.Brushes.Gold;
                DetailText.Text = $"Faltantes: {result.missing}  •  Desactualizados: {result.outdated}";
                BottomText.Text = "Hay archivos que deben actualizarse antes de jugar.";
            }
        }
        catch (Exception ex)
        {
            StatusText.Text = "⚠  Error de conexión"; StatusText.Foreground = System.Windows.Media.Brushes.OrangeRed;
            DetailText.Text = ex.Message; BottomText.Text = "No fue posible comprobar el servidor.";
            Progress.Value = 0; Percent.Text = "0%";
        }
    }

    private async void Check_Click(object sender, RoutedEventArgs e) => await CheckModsAsync();
    private void Play_Click(object sender, RoutedEventArgs e)
    {
        // En la siguiente fase conectaremos aquí el perfil Forge 1.20.1.
        MessageBox.Show("El botón JUGAR ya está conectado. En la siguiente fase se añadirá el arranque automático de Forge 1.20.1.", "Fondaland IV", MessageBoxButton.OK, MessageBoxImage.Information);
    }
    private void Minimize_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;
    private void Maximize_Click(object sender, RoutedEventArgs e) => WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
    private void Close_Click(object sender, RoutedEventArgs e) => Close();
    private void Home_Click(object sender, RoutedEventArgs e) { }
    private void Mods_Click(object sender, RoutedEventArgs e) => MessageBox.Show("Panel de mods: siguiente fase.");
    private void Install_Click(object sender, RoutedEventArgs e) => MessageBox.Show("Instalador de Forge: siguiente fase.");
    private void Config_Click(object sender, RoutedEventArgs e) => MessageBox.Show("Configuración: siguiente fase.");
    private void Logs_Click(object sender, RoutedEventArgs e) => MessageBox.Show("Registros: siguiente fase.");
    private void Discord_Click(object sender, RoutedEventArgs e) => Process.Start(new ProcessStartInfo("https://discord.com") { UseShellExecute = true });
}
