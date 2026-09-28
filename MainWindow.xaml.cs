using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using CmlLib.Core;
using CmlLib.Core.Auth;
using CmlLib.Core.Auth.Microsoft;
using CmlLib.Core.ProcessBuilder;
using FondalandIVLauncher.Models;
using FondalandIVLauncher.Services;

namespace FondalandIVLauncher;

public partial class MainWindow : Window
{
    private readonly ModManager _mods = new();
    private readonly ForgeManager _forgeManager = new();

    private const string ManifestUrl =
        "https://raw.githubusercontent.com/andresgarciagestion26-netizen/FondalandIV-Launcher/main/manifest.json?v=3";

    private const string ServerAddress = "Fondaland4.mcserver.us";
    private const int ServerPort = 25565;

    private readonly string _appRoot = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "FondalandIV");

    private ServerManifest? _manifest;
    private ForgeInstallation? _forge;
    private bool _busy;

    private string GameDir => Path.Combine(_appRoot, "game");
    private string ModsDir => Path.Combine(GameDir, "mods");

    public MainWindow()
    {
        InitializeComponent();
        Loaded += async (_, _) => await CheckModsAsync();
    }

    private async Task CheckModsAsync()
    {
        if (_busy)
            return;

        _busy = true;
        PlayButton.IsEnabled = false;

        try
        {
            SetProgress(5, "Conectando con GitHub...", "Descargando manifest...");

            _manifest = await _mods.LoadManifestAsync(ManifestUrl);

            if (_manifest.Mods.Count == 0)
                throw new InvalidDataException("El manifest no contiene mods.");

            ModsCount.Text = $"-- / {_manifest.Mods.Count}";
            DetailText.Text =
                $"Minecraft {_manifest.Pack.Minecraft} â€¢ Forge {_manifest.Pack.Forge} â€¢ {_manifest.Mods.Count} mods";

            SetProgress(
                20,
                "Verificando instalaciÃ³n...",
                $"Comprobando {_manifest.Mods.Count} mods...");

            var progress = new Progress<ModProgress>(p =>
            {
                double value = 20.0 + p.Index * 75.0 / p.Total;
                SetProgress(value, p.Status, $"{p.Index}/{p.Total} â€¢ {p.File}");
            });

            ModCheckResult result = await _mods.CheckAsync(ModsDir, _manifest, progress);

            ModsCount.Text = $"{result.Ok} / {_manifest.Mods.Count}";

            bool forgeInstalled =
                Directory.Exists(Path.Combine(GameDir, "versions")) &&
                Directory.EnumerateDirectories(
                    Path.Combine(GameDir, "versions"),
                    "1.20.1-forge-*",
                    SearchOption.TopDirectoryOnly).Any();

            if (result.Missing == 0 && result.Outdated == 0 && forgeInstalled)
            {
                SetProgress(
                    100,
                    "âœ“ Todo preparado",
                    "Pulsa JUGAR para iniciar Fondaland IV.");

                StatusText.Text = "âœ“  Todo preparado";
                StatusText.Foreground = Brushes.LightGreen;
                BottomText.Text =
                    "Minecraft, Java, Forge y los mods estÃ¡n preparados.";
            }
            else
            {
                SetProgress(
                    100,
                    "âœ“ Listo para instalar",
                    "JUGAR completarÃ¡ automÃ¡ticamente lo que falte.");

                StatusText.Text = "âœ“  Listo para jugar";
                StatusText.Foreground = Brushes.LightGreen;
                BottomText.Text =
                    "JUGAR instalarÃ¡ Minecraft 1.20.1, Java 17, Forge y los mods si es necesario.";
            }

            PlayButton.IsEnabled = true;
        }
        catch (Exception ex)
        {
            StatusText.Text = "âš   Error";
            StatusText.Foreground = Brushes.OrangeRed;
            DetailText.Text = ex.Message;
            BottomText.Text = "No fue posible comprobar GitHub.";
            Progress.Value = 0;
            Percent.Text = "0%";
        }
        finally
        {
            _busy = false;
            PlayButton.IsEnabled = _manifest != null;
        }
    }

    private async void Check_Click(object sender, RoutedEventArgs e)
    {
        await CheckModsAsync();
    }

    private async void Play_Click(object sender, RoutedEventArgs e)
    {
        if (_busy || _manifest == null)
            return;

        _busy = true;
        PlayButton.IsEnabled = false;

        try
        {
            Directory.CreateDirectory(_appRoot);
            Directory.CreateDirectory(GameDir);
            Directory.CreateDirectory(ModsDir);

            SetProgress(
                3,
                "Iniciando sesiÃ³n...",
                "Comprobando tu cuenta Microsoft de Minecraft...");

            string accountFile = Path.Combine(_appRoot, "cml_accounts.json");

            MSession session = await new JELoginHandlerBuilder()
                .WithAccountManager(accountFile)
                .Build()
                .Authenticate();

            if (session == null)
                throw new InvalidOperationException(
                    "No fue posible iniciar sesiÃ³n en Minecraft.");

            SetProgress(
                10,
                "Cuenta verificada",
                "Preparando el entorno de Fondaland IV...");

            MinecraftPath path = new(GameDir);
            MinecraftLauncher launcher = new(path);

            SetProgress(
                22,
                "Instalando Minecraft 1.20.1...",
                "Descargando los archivos oficiales y Java que falten...");

            await launcher.InstallAsync(_manifest.Pack.Minecraft);

            string javaPath = launcher.GetJavaPath(
                await launcher.GetVersionAsync(_manifest.Pack.Minecraft));

            if (string.IsNullOrWhiteSpace(javaPath) || !File.Exists(javaPath))
                throw new InvalidOperationException(
                    "No fue posible preparar automÃ¡ticamente el runtime de Java para Minecraft 1.20.1.");

            SetProgress(
                24,
                "âœ“ Minecraft y Java preparados",
                "Instalando Forge automÃ¡ticamente...");

            var forgeProgress = new Progress<ForgeProgress>(p =>
            {
                SetProgress(
                    25.0 + p.Percent * 0.35,
                    p.Status,
                    "InstalaciÃ³n automÃ¡tica de Forge...");
            });

            _forge = await _forgeManager.EnsureForgeAsync(
                launcher,
                GameDir,
                _manifest.Pack.Minecraft,
                _manifest.Pack.Forge,
                javaPath,
                forgeProgress);

            SetProgress(
                62,
                $"Forge {_forge.ForgeVersion} listo",
                $"Sincronizando {_manifest.Mods.Count} mods de Fondaland IV...");

            var modProgress = new Progress<ModProgress>(p =>
            {
                double value = 62.0 + p.Index * 25.0 / p.Total;
                string[] parts = p.Status.Split('|');
                string title = parts[0];
                string detail = parts.Length > 1 ? parts[1] : $"{p.Index}/{p.Total}";
                SetProgress(value, title, $"{p.File} â€¢ {detail}");
            });

            ModCheckResult modResult =
                await _mods.SyncAsync(ModsDir, _manifest, modProgress);

            ModsCount.Text = $"{modResult.Ok} / {_manifest.Mods.Count}";

            SetProgress(
                89,
                "VerificaciÃ³n final",
                "Comprobando que Forge tenga todos sus componentes...");

            await launcher.InstallAsync(_forge.VersionId);

            SetProgress(
                94,
                "Iniciando Minecraft...",
                $"Forge {_forge.ForgeVersion} â€¢ conectando a {ServerAddress}:{ServerPort}");

            MLaunchOption launchOption = new()
            {
                Session = session,
                MaximumRamMb = 6144,
                MinimumRamMb = 2048,
                ServerIp = ServerAddress,
                ServerPort = ServerPort,
                GameLauncherName = "Fondaland IV Launcher",
                GameLauncherVersion = "2.0.7",
                JavaPath = javaPath
            };

            Process process =
                await launcher.BuildProcessAsync(_forge.VersionId, launchOption);

            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.EnableRaisingEvents = true;

            StringBuilder output = new();
            StringBuilder error = new();

            process.OutputDataReceived += (_, e) =>
            {
                if (e.Data != null)
                {
                    lock (output)
                        output.AppendLine(e.Data);
                }
            };

            process.ErrorDataReceived += (_, e) =>
            {
                if (e.Data != null)
                {
                    lock (error)
                        error.AppendLine(e.Data);
                }
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            await Task.Delay(TimeSpan.FromSeconds(8));

            if (process.HasExited)
            {
                await Task.Delay(300);

                string diagnostic = await BuildLaunchDiagnosticAsync(
                    GameDir,
                    process.ExitCode,
                    output.ToString(),
                    error.ToString());

                StatusText.Text = "âš   Minecraft se cerrÃ³ al iniciar";
                StatusText.Foreground = Brushes.OrangeRed;
                DetailText.Text = diagnostic;
                BottomText.Text =
                    $"El error completo quedÃ³ guardado en {Path.Combine(GameDir, "launch-error.txt")}.";

                Progress.Value = 0;
                Percent.Text = "0%";
            }
            else
            {
                SetProgress(
                    100,
                    "âœ“ Fondaland IV iniciado",
                    $"Minecraft {_manifest.Pack.Minecraft} â€¢ Forge {_forge.ForgeVersion}");

                await Task.Delay(1200);
                Close();
            }
        }
        catch (Exception ex)
        {
            StatusText.Text = "âš   No se pudo iniciar Minecraft";
            StatusText.Foreground = Brushes.OrangeRed;
            DetailText.Text = ex.Message;
            BottomText.Text =
                "Revisa el mensaje y vuelve a pulsar JUGAR. Tu instalaciÃ³n no se borra por un error.";
            Progress.Value = 0;
            Percent.Text = "0%";
        }
        finally
        {
            _busy = false;

            if (IsLoaded)
                PlayButton.IsEnabled = _manifest != null;
        }
    }

    private static async Task<string> BuildLaunchDiagnosticAsync(
        string gameDir,
        int exitCode,
        string standardOutput,
        string standardError)
    {
        string logPath = Path.Combine(gameDir, "logs", "latest.log");
        string crashDir = Path.Combine(gameDir, "crash-reports");
        string diagnosticPath = Path.Combine(gameDir, "launch-error.txt");

        string message =
            $"Minecraft terminÃ³ inmediatamente (cÃ³digo {exitCode}).";

        StringBuilder report = new();

        report.AppendLine("Fondaland IV Launcher - diagnÃ³stico de lanzamiento");
        report.AppendLine($"Fecha: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        report.AppendLine($"ExitCode: {exitCode}");
        report.AppendLine();

        report.AppendLine("===== STDERR (Java/Forge) =====");
        report.AppendLine(
            string.IsNullOrWhiteSpace(standardError)
                ? "(vacÃ­o)"
                : standardError.Trim());

        report.AppendLine();
        report.AppendLine("===== STDOUT (Java/Forge) =====");
        report.AppendLine(
            string.IsNullOrWhiteSpace(standardOutput)
                ? "(vacÃ­o)"
                : standardOutput.Trim());

        try
        {
            if (File.Exists(logPath))
            {
                string[] lines = await File.ReadAllLinesAsync(logPath);
                string lastLines = string.Join("\n", lines.TakeLast(80));

                report.AppendLine();
                report.AppendLine("===== LATEST.LOG (Ãºltimas 80 lÃ­neas) =====");
                report.AppendLine(lastLines);

                string last18 = string.Join("\n", lines.TakeLast(18));

                if (!string.IsNullOrWhiteSpace(last18))
                {
                    message +=
                        "\n\nÃšltimas lÃ­neas de latest.log:\n" + last18;
                }
            }

            if (Directory.Exists(crashDir))
            {
                string? crashFile = Directory
                    .EnumerateFiles(crashDir, "*.txt")
                    .OrderByDescending(File.GetLastWriteTimeUtc)
                    .FirstOrDefault();

                if (crashFile != null)
                {
                    message +=
                        $"\n\nCrash report: {Path.GetFileName(crashFile)}";

                    report.AppendLine();
                    report.AppendLine(
                        $"===== CRASH REPORT: {Path.GetFileName(crashFile)} =====");
                    report.AppendLine(
                        await File.ReadAllTextAsync(crashFile));
                }
            }

            await File.WriteAllTextAsync(
                diagnosticPath,
                report.ToString(),
                new UTF8Encoding(false));
        }
        catch (Exception ex)
        {
            message +=
                "\nNo fue posible leer/guardar el diagnÃ³stico: " + ex.Message;

            try
            {
                await File.WriteAllTextAsync(
                    diagnosticPath,
                    report + "\n\n===== ERROR DEL DIAGNÃ“STICO =====\n" + ex,
                    new UTF8Encoding(false));
            }
            catch
            {
            }
        }

        if (!string.IsNullOrWhiteSpace(standardError))
        {
            string[] lines = standardError.Split(
                new[] { "\r\n", "\n" },
                StringSplitOptions.RemoveEmptyEntries);

            string last22 = string.Join("\n", lines.TakeLast(22));

            if (!string.IsNullOrWhiteSpace(last22))
                message += "\n\nSalida de error de Java/Forge:\n" + last22;
        }

        return message;
    }

    private void SetProgress(double value, string title, string detail)
    {
        Progress.Value = Math.Clamp(value, 0, 100);
        Percent.Text = $"{Progress.Value:0}%";
        BottomTitle.Text = title;
        BottomText.Text = detail;
    }

    private void Minimize_Click(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void Maximize_Click(object sender, RoutedEventArgs e)
    {
        WindowState =
            WindowState != WindowState.Maximized
                ? WindowState.Maximized
                : WindowState.Normal;
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void Home_Click(object sender, RoutedEventArgs e)
    {
    }

    private void Mods_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show(
            $"Fondaland IV tiene {_manifest?.Mods.Count ?? 0} mods en el manifest.",
            "Mods");
    }

    private void Install_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show(
            "La instalaciÃ³n es automÃ¡tica desde JUGAR. El launcher prepara Minecraft 1.20.1, Java 17, Forge y los mods.",
            "InstalaciÃ³n");
    }

    private void Config_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show(
            $"Instancia de Fondaland:\n{GameDir}\n\n" +
            $"Mods:\n{ModsDir}\n\n" +
            $"Forge:\n{_forge?.ForgeVersion ?? "AutomÃ¡tico"}\n\n" +
            $"Servidor:\n{ServerAddress}:{ServerPort}",
            "ConfiguraciÃ³n");
    }

    private void Logs_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show(
            "Los registros de Fondaland estÃ¡n en:\n" +
            Path.Combine(GameDir, "logs"),
            "Registros");
    }

    private void Discord_Click(object sender, RoutedEventArgs e)
    {
        Process.Start(new ProcessStartInfo("https://discord.com")
        {
            UseShellExecute = true
        });
    }
}

