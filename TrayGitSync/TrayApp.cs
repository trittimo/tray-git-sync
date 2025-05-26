namespace TrayGitSync;

using System;
using System.IO;
using System.Text.Json;
using System.Windows.Forms;

public class TrayApp : ApplicationContext
{
    private readonly NotifyIcon? _trayIcon;
    private Configuration? _config;
    private Form? _progressForm;
    
    private class NoMessageEventArgs : EventArgs;
    private static readonly NoMessageEventArgs NoMessage = new();
    private readonly RemoteStorageGit _remoteStorage = new();
    private double _totalRepositories = 0;
    private double _currentRepositoryCount = -1;
    private string? _currentRepositoryName;
    public TrayApp()
    {
        LoadConfig(this, NoMessage);

        _remoteStorage.OnFatalError += (_, e) =>
        {
            ShowProgress($"Error: {e.Exception.Message}", true, true);
        };

        _remoteStorage.OnRemoteStorageInitialized += (_, e) =>
        {
            ShowProgress($"Initializing repositories: {string.Join(", ", e.Repositories)}");
            _totalRepositories = e.Repositories.Length;
        };

        _remoteStorage.OnRemoteStorageProgress += (_, e) =>
        {
            if (e.RepositoryName != _currentRepositoryName)
            {
                _currentRepositoryName = e.RepositoryName;
                _currentRepositoryCount++;
            }

            if (_progressForm?.Controls[0] is ProgressBar progressBar)
            {
                progressBar.Style = ProgressBarStyle.Blocks;
                progressBar.Value = Math.Clamp((int)((_currentRepositoryCount / _totalRepositories + e.PercentComplete / _totalRepositories) * 100), 0, 100);
            }
    
            ShowProgress($"{e.RepositoryName}: {e.Message}", e.IsComplete);
        };

        var contextMenu = new ContextMenuStrip();
        contextMenu.Items.Add("Upload", null, OnUpload);
        contextMenu.Items.Add("Download", null, OnDownload);
        contextMenu.Items.Add("Exit", null, OnExit);
        contextMenu.Items.Add("Reload Config", null, LoadConfig);

        _trayIcon = new NotifyIcon
        {
            Icon = SystemIcons.Application,
            ContextMenuStrip = contextMenu,
            Visible = true
        };
    }

    private void LoadConfig(object? sender, EventArgs e)
    {
        var drivePath = Path.GetPathRoot(AppContext.BaseDirectory) ?? throw new Exception("Cannot find root path to load config from");
        var configPath = Path.Combine(drivePath, "tray-sync", "config.json");
        _config = JsonSerializer.Deserialize<Configuration>(File.ReadAllText(configPath)) ??
                  throw new Exception("Unable to load config.json");

        if (e is not NoMessageEventArgs)
        {
            MessageBox.Show("Config reloaded.", "TrayGitSync", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }

    private void OnUpload(object? sender, EventArgs e)
    {
        try
        {
            ShowProgress("Uploading...");
            var result = _remoteStorage.Upload(_config!);
            ShowProgress(result.ToString(), true);
        }
        catch (Exception ex)
        {
            ShowProgress("Upload failed:\n" + ex.Message, true, true);
        }
    }

    private void OnDownload(object? sender, EventArgs e)
    {
        try
        {
            ShowProgress("Downloading...");
            _remoteStorage.Download(_config!);
            ShowProgress("Download complete.", true);
        }
        catch (Exception ex)
        {
            ShowProgress($"Error: {ex.Message}", true, true);
        }
    }

    private void OnExit(object? sender, EventArgs e)
    {
        _trayIcon!.Visible = false;
        Application.Exit();
    }
    
    private void ShowProgress(string message, bool enableCloseButton = false, bool isError = false)
    {
        if (_progressForm == null)
        {
            _progressForm = new Form
            {
                Size = new Size(300, 100),
                FormBorderStyle = FormBorderStyle.FixedDialog,
                StartPosition = FormStartPosition.CenterScreen,
                TopMost = true,
                ShowInTaskbar = false,
                ControlBox = false
            };
    
            var progressBar = new ProgressBar
            {
                Style = ProgressBarStyle.Blocks,
                Size = new Size(_progressForm.Width - 50, 23),
                Location = new Point(25, 15)
            };
    
            var label = new Label
            {
                AutoSize = true,
                Location = new Point(25, 45)
            };
    
            var closeButton = new Button
            {
                Text = "Close",
                Size = new Size(200, 23),
                Location = new Point(50, 70),
                Enabled = false
            };
            closeButton.Click += (_, _) =>
            {
                _progressForm.Close();
                _progressForm = null;
            };
    
            _progressForm.Controls.Add(progressBar);
            _progressForm.Controls.Add(label);
            _progressForm.Controls.Add(closeButton);
            _progressForm.Show();
        }
    
        if (_progressForm.Controls.Count < 3) return;
        
        if (_progressForm.Controls[1] is not Label messageLabel) return;
        messageLabel.Text = message;
        
        var formWidth = Math.Max(300, messageLabel.PreferredWidth + 50);
        var progressBarY = 15;
        var labelY = progressBarY + 30;
        var buttonY = labelY + messageLabel.Height + 10;
        var formHeight = buttonY + 33;
        
        _progressForm.Size = new Size(formWidth, formHeight);
        
        if (_progressForm.Controls[0] is not ProgressBar progressBarControl) return;
        progressBarControl.Size = new Size(formWidth - 50, 23);
        
        if (_progressForm.Controls[2] is not Button closeButtonControl) return;
        closeButtonControl.Location = new Point((formWidth - 200) / 2, buttonY);
        closeButtonControl.Enabled = enableCloseButton;

        if (enableCloseButton)
        {
            progressBarControl.Value = 100;
        }

        if (isError)
        {
            progressBarControl.Visible = false;
            _progressForm.Controls.Add(new Panel
            {
                BackColor = Color.Red,
                Width = progressBarControl.Size.Width,
                Height = progressBarControl.Size.Height,
                Location = progressBarControl.Location
            });
        }
    
        Application.DoEvents();
    }
}