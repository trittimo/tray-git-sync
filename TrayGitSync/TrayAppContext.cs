namespace TrayGitSync;

using System;
using System.IO;
using System.Text.Json;
using System.Windows.Forms;

public class TrayAppContext : ApplicationContext
{
    private readonly NotifyIcon? _trayIcon;
    private GitSyncConfig? _config;

    public TrayAppContext()
    {
        LoadConfig();

        var contextMenu = new ContextMenuStrip();
        contextMenu.Items.Add("Upload", null, OnUpload);
        contextMenu.Items.Add("Download", null, OnDownload);
        contextMenu.Items.Add("Exit", null, OnExit);

        _trayIcon = new NotifyIcon
        {
            Icon = SystemIcons.Application,
            ContextMenuStrip = contextMenu,
            Visible = true
        };
    }

    private void LoadConfig()
    {
        var configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.json");
        _config = JsonSerializer.Deserialize<GitSyncConfig>(File.ReadAllText(configPath)) ??
                  throw new Exception("Unable to load config.json");
    }

    private void OnUpload(object? sender, EventArgs e)
    {
        try
        {
            GitHelper.Upload(_config!);
            MessageBox.Show("Upload complete.");
        }
        catch (Exception ex)
        {
            MessageBox.Show("Upload failed:\n" + ex.Message);
        }
    }

    private void OnDownload(object? sender, EventArgs e)
    {
        try
        {
            GitHelper.Download(_config!);
            MessageBox.Show("Download complete.");
        }
        catch (Exception ex)
        {
            MessageBox.Show("Download failed:\n" + ex.Message);
        }
    }

    private void OnExit(object? sender, EventArgs e)
    {
        _trayIcon!.Visible = false;
        Application.Exit();
    }
}