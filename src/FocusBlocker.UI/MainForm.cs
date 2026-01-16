using FocusBlocker.Shared;

namespace FocusBlocker.UI;

public sealed class MainForm : Form
{
    private readonly IpcClient _ipcClient = new();
    private readonly System.Windows.Forms.Timer _timer;
    private readonly Label _statusLabel;
    private readonly Label _countdownLabel;
    private readonly Label _quotaLabel;
    private readonly Button _unblock5;
    private readonly Button _unblock10;
    private readonly Button _unblock60;
    private readonly Button _unblock120;
    private readonly Button _reblock;
    private readonly NotifyIcon _trayIcon;
    private ServiceStatus _status = new(true, null, 0, true, true, false);

    public MainForm()
    {
        Text = "Focus Blocker";
        Width = 360;
        Height = 260;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;

        _statusLabel = new Label { Left = 16, Top = 16, Width = 300, Text = "状态：未知" };
        _countdownLabel = new Label { Left = 16, Top = 40, Width = 300, Text = "剩余：--" };
        _quotaLabel = new Label { Left = 16, Top = 64, Width = 300, Text = "1h/2h：--" };

        _unblock5 = CreateButton("解除 5min", 16, 100, async () => await RequestUnblockAsync(5));
        _unblock10 = CreateButton("解除 10min", 180, 100, async () => await RequestUnblockAsync(10));
        _unblock60 = CreateButton("解除 1h", 16, 140, async () => await RequestUnblockAsync(60));
        _unblock120 = CreateButton("解除 2h", 180, 140, async () => await RequestUnblockAsync(120));
        _reblock = CreateButton("恢复屏蔽", 16, 180, async () => await RequestReblockAsync());
        _reblock.Width = 320;

        Controls.AddRange([
            _statusLabel,
            _countdownLabel,
            _quotaLabel,
            _unblock5,
            _unblock10,
            _unblock60,
            _unblock120,
            _reblock
        ]);

        _trayIcon = new NotifyIcon
        {
            Icon = SystemIcons.Shield,
            Visible = true,
            Text = "Focus Blocker"
        };
        _trayIcon.DoubleClick += (_, _) => ShowWindow();

        _timer = new System.Windows.Forms.Timer { Interval = 1000 };
        _timer.Tick += async (_, _) => await RefreshStatusAsync();
        _timer.Start();

        Resize += (_, _) =>
        {
            if (WindowState == FormWindowState.Minimized)
            {
                Hide();
            }
        };

        Shown += async (_, _) => await RefreshStatusAsync();
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (e.CloseReason == CloseReason.UserClosing)
        {
            e.Cancel = true;
            Hide();
            return;
        }

        _trayIcon.Visible = false;
        base.OnFormClosing(e);
    }

    private Button CreateButton(string text, int left, int top, Func<Task> handler)
    {
        var button = new Button
        {
            Text = text,
            Left = left,
            Top = top,
            Width = 140,
            Height = 30
        };
        button.Click += async (_, _) => await handler();
        return button;
    }

    private async Task RequestUnblockAsync(int minutes)
    {
        await SendAsync(new IpcRequest(IpcCommandType.Unblock, minutes));
    }

    private async Task RequestReblockAsync()
    {
        await SendAsync(new IpcRequest(IpcCommandType.Reblock, 0));
    }

    private async Task RefreshStatusAsync()
    {
        var response = await SendAsync(new IpcRequest(IpcCommandType.GetStatus, 0));
        if (!response.Success)
        {
            _statusLabel.Text = "状态：服务离线";
            _countdownLabel.Text = "剩余：--";
            _quotaLabel.Text = "1h/2h：--";
            _unblock60.Enabled = false;
            _unblock120.Enabled = false;
            return;
        }

        _status = response.Status;
        UpdateUi();
    }

    private void UpdateUi()
    {
        _statusLabel.Text = _status.IsBlocking ? "状态：屏蔽中" : "状态：临时解除中";
        _countdownLabel.Text = _status.IsBlocking
            ? "剩余：--"
            : $"剩余：{TimeSpan.FromSeconds(_status.RemainingSeconds):hh\:mm\:ss}";
        _quotaLabel.Text = $"1h：{(_status.OneHourAvailable ? "可用" : "明日可用")} / 2h：{(_status.TwoHoursAvailable ? "可用" : "明日可用")}";
        _unblock60.Enabled = _status.OneHourAvailable;
        _unblock120.Enabled = _status.TwoHoursAvailable;
    }

    private async Task<IpcResponse> SendAsync(IpcRequest request)
    {
        try
        {
            return await _ipcClient.SendAsync(request, CancellationToken.None);
        }
        catch
        {
            return new IpcResponse(false, "offline", new ServiceStatus(true, null, 0, true, true, false));
        }
    }

    private void ShowWindow()
    {
        Show();
        WindowState = FormWindowState.Normal;
        BringToFront();
    }
}
