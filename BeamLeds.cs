// BeamNG Wheel LEDs - OutGauge (BeamNG.drive) -> RPM LEDs on Logitech wheels via the Logitech Steering Wheel SDK.
// C# 5 compatible: builds with the csc.exe that ships with Windows (see build.bat).
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

static class Logi
{
    const string DLL = "LogitechSteeringWheelEnginesWrapper";
    [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.U1)]
    public static extern bool LogiSteeringInitializeWithWindow([MarshalAs(UnmanagedType.U1)] bool ignoreXInput, IntPtr hwnd);
    [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.U1)]
    public static extern bool LogiUpdate();
    [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.U1)]
    public static extern bool LogiIsConnected(int index);
    [DllImport(DLL, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.U1)]
    public static extern bool LogiGetFriendlyProductName(int index, StringBuilder buf, int size);
    [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.U1)]
    public static extern bool LogiPlayLeds(int index, float rpm, float first, float red);
    [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.U1)]
    public static extern bool LogiButtonIsPressed(int index, int buttonNbr);
    [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
    public static extern void LogiSteeringShutdown();
}

static class Win
{
    [DllImport("user32.dll")] public static extern bool RegisterHotKey(IntPtr h, int id, uint mod, uint vk);
    [DllImport("user32.dll")] public static extern bool UnregisterHotKey(IntPtr h, int id);
    [DllImport("user32.dll")] static extern uint SendInput(uint n, INPUT[] inputs, int size);
    [DllImport("user32.dll")] static extern uint MapVirtualKey(uint code, uint mapType);

    [StructLayout(LayoutKind.Sequential)]
    struct KEYBDINPUT { public ushort wVk; public ushort wScan; public uint dwFlags; public uint time; public IntPtr dwExtraInfo; }
    [StructLayout(LayoutKind.Explicit, Size = 40)]   // x64 layout (the app is built as x64)
    struct INPUT { [FieldOffset(0)] public uint type; [FieldOffset(8)] public KEYBDINPUT ki; }

    // Sends a keyboard event using scan codes (works with most games).
    public static void Key(ushort vk, bool down)
    {
        INPUT i = new INPUT();
        i.type = 1;
        ushort scan = (ushort)MapVirtualKey(vk, 0);
        i.ki.wVk = scan == 0 ? vk : (ushort)0;
        i.ki.wScan = scan;
        i.ki.dwFlags = (scan != 0 ? 0x0008u : 0u) | (down ? 0u : 0x0002u);
        SendInput(1, new INPUT[] { i }, Marshal.SizeOf(typeof(INPUT)));
    }
}

class DevItem
{
    public int Index; public string Name;
    public override string ToString() { return Index + ": " + Name; }
}

class Settings
{
    public int Mode = 1;
    public decimal Redline = 7000, First = 4500, FlashPct = 96, FlashHz = 6, Port = 4444;
    public bool Auto = true, IgnoreX = false;
    public int Device = 0;
    public bool RevOn = false;
    public int RevButton = 5, RevKey = 0, SixButton = 6, NeuKey = 1;
    public bool SixOn = false;
    public decimal RevSpeed = 5, BoostMax = 2;
    static string PathIni { get { return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "BeamLeds.ini"); } }

    public void Load()
    {
        try
        {
            if (!File.Exists(PathIni)) return;
            foreach (string line in File.ReadAllLines(PathIni))
            {
                int p = line.IndexOf('=');
                if (p < 1) continue;
                string k = line.Substring(0, p).Trim().ToLowerInvariant(), v = line.Substring(p + 1).Trim();
                decimal d; bool b;
                bool isNum = decimal.TryParse(v, NumberStyles.Any, CultureInfo.InvariantCulture, out d);
                bool isBool = bool.TryParse(v, out b);
                if (k == "mode" && isNum) Mode = (int)d;
                else if (k == "redline" && isNum) Redline = d;
                else if (k == "first" && isNum) First = d;
                else if (k == "flashpct" && isNum) FlashPct = d;
                else if (k == "flashhz" && isNum) FlashHz = d;
                else if (k == "port" && isNum) Port = d;
                else if (k == "device" && isNum) Device = (int)d;
                else if (k == "revbutton" && isNum) RevButton = (int)d;
                else if (k == "revkey" && isNum) RevKey = (int)d;
                else if (k == "sixbutton" && isNum) SixButton = (int)d;
                else if (k == "neukey" && isNum) NeuKey = (int)d;
                else if (k == "sixon" && isBool) SixOn = b;
                else if (k == "revspeed" && isNum) RevSpeed = d;
                else if (k == "boostmax" && isNum) BoostMax = d;
                else if (k == "revon" && isBool) RevOn = b;
                else if (k == "auto" && isBool) Auto = b;
                else if (k == "ignorexinput" && isBool) IgnoreX = b;
            }
        }
        catch { }
    }

    public void Save()
    {
        try
        {
            CultureInfo c = CultureInfo.InvariantCulture;
            File.WriteAllLines(PathIni, new string[] {
                "mode=" + Mode, "redline=" + Redline.ToString(c), "first=" + First.ToString(c),
                "flashpct=" + FlashPct.ToString(c), "flashhz=" + FlashHz.ToString(c), "port=" + Port.ToString(c),
                "device=" + Device, "auto=" + Auto, "ignorexinput=" + IgnoreX,
                "revon=" + RevOn, "revbutton=" + RevButton, "revkey=" + RevKey,
                "sixon=" + SixOn, "sixbutton=" + SixButton, "neukey=" + NeuKey,
                "revspeed=" + RevSpeed.ToString(c), "boostmax=" + BoostMax.ToString(c) });
        }
        catch { }
    }
}

class MainForm : Form
{
    static readonly string[] ModeNames = new string[] {
        "Progressive (Logitech default)",
        "Race (progressive + shift flash)",
        "Shift light only (flash near limit)",
        "Gauge: fuel level (flashes when low)",
        "Gauge: engine temperature (flashes when hot)",
        "Gauge: oil temperature (flashes when hot)",
        "Gauge: turbo boost / forced induction" };
    static readonly string[] RevKeyNames = new string[] {
        "F13", "F14", "F15", "F16", "F17", "F18", "F19", "F20", "F21", "F22", "F23", "F24",
        "Numpad 0", "Numpad 1", "Numpad 2", "Numpad 3", "Numpad 4", "Numpad 5", "Numpad 6", "Numpad 7", "Numpad 8", "Numpad 9" };
    static readonly ushort[] RevVk = new ushort[] {
        0x7C, 0x7D, 0x7E, 0x7F, 0x80, 0x81, 0x82, 0x83, 0x84, 0x85, 0x86, 0x87,
        0x60, 0x61, 0x62, 0x63, 0x64, 0x65, 0x66, 0x67, 0x68, 0x69 };

    Settings S = new Settings();
    bool loading = true;

    ComboBox cbDev = new ComboBox(), cbMode = new ComboBox();
    NumericUpDown nPort = new NumericUpDown(), nRed = new NumericUpDown(), nFirst = new NumericUpDown(),
                  nPct = new NumericUpDown(), nHz = new NumericUpDown();
    CheckBox chAuto = new CheckBox(), chIgnoreX = new CheckBox(), chTest = new CheckBox();
    Button btnReset = new Button(), btnLearn = new Button();
    NumericUpDown nBoost = new NumericUpDown(), nRevBtn = new NumericUpDown(), nRevSpd = new NumericUpDown();
    ComboBox cbRevKey = new ComboBox();
    CheckBox chRev = new CheckBox();
    Label lblRev = new Label();
    CheckBox chSix = new CheckBox();
    NumericUpDown nSixBtn = new NumericUpDown();
    Button btnLearn6 = new Button();
    ComboBox cbNeuKey = new ComboBox();
    int learnTarget = 1;
    bool weReversed = false, neutralPending = false;
    DateTime neutralUntil = DateTime.MinValue;
    ushort downVk = 0;
    Label lblDev = new Label(), lblTel = new Label(), lblRpm = new Label(), lblStatus = new Label();
    ProgressBar bar = new ProgressBar();
    NotifyIcon tray = new NotifyIcon();
    Timer timer = new Timer();

    UdpClient udp;
    bool inited = false;
    DateTime lastPacket = DateTime.MinValue, lastInit = DateTime.MinValue, lastDevScan = DateTime.MinValue,
             lastWrite = DateTime.MinValue, t0 = DateTime.Now;
    long packets = 0;
    float rpm = 0, peak = 0, speedMs = 0, turbo = 0, engT = 0, oilT = 0, fuel = 0;
    byte gear = 1;
    bool learning = false;
    DateTime learnUntil = DateTime.MinValue, keyDown = DateTime.MinValue, lastTap = DateTime.MinValue;
    string initMsg = "starting...";
    string netMsg = "";

    public MainForm()
    {
        S.Load();
        Text = "BeamNG Wheel LEDs";
        AutoScroll = true;
        MinimumSize = new Size(320, 240);
        Padding = new Padding(8);
        Font = new Font("Segoe UI", 9f);

        FlowLayoutPanel root = new FlowLayoutPanel();
        root.FlowDirection = FlowDirection.LeftToRight; root.WrapContents = false; root.AutoSize = true;
        FlowLayoutPanel colL = new FlowLayoutPanel(), colR = new FlowLayoutPanel();
        colL.FlowDirection = colR.FlowDirection = FlowDirection.TopDown;
        colL.WrapContents = colR.WrapContents = false;
        colL.AutoSize = colR.AutoSize = true;
        root.Controls.Add(colL); root.Controls.Add(colR);
        Controls.Add(root);

        // ---- Wheel
        TableLayoutPanel t1 = NewGroup(colL, "Wheel");
        cbDev.DropDownStyle = ComboBoxStyle.DropDownList; cbDev.Width = 250;
        AddRow(t1, "Device", cbDev);
        chIgnoreX.Text = "Ignore XInput controllers (restarts SDK)";
        chIgnoreX.AutoSize = true;
        AddRow(t1, "", chIgnoreX);
        lblDev.AutoSize = true; lblDev.MaximumSize = new Size(330, 0);
        AddRow(t1, "Status", lblDev);

        // ---- Telemetry
        TableLayoutPanel t2 = NewGroup(colL, "Telemetry (OutGauge UDP)");
        nPort.Minimum = 1024; nPort.Maximum = 65535; nPort.Width = 80;
        AddRow(t2, "UDP port", nPort);
        lblTel.AutoSize = true;
        AddRow(t2, "Packets", lblTel);
        lblRpm.AutoSize = true; lblRpm.Font = new Font("Consolas", 12f, FontStyle.Bold);
        AddRow(t2, "RPM", lblRpm);
        bar.Width = 250; bar.Height = 14; bar.Maximum = 1000;
        AddRow(t2, "", bar);

        // ---- LED mode
        TableLayoutPanel t3 = NewGroup(colR, "LED mode (changes apply instantly)");
        cbMode.DropDownStyle = ComboBoxStyle.DropDownList; cbMode.Width = 250;
        cbMode.Items.AddRange(ModeNames);
        AddRow(t3, "Mode", cbMode);
        nPct.Minimum = 50; nPct.Maximum = 100; nPct.Width = 80;
        AddRow(t3, "Flash from (% of redline)", nPct);
        nHz.Minimum = 1; nHz.Maximum = 15; nHz.Width = 80;
        AddRow(t3, "Flash rate (Hz)", nHz);
        nBoost.Minimum = 1; nBoost.Maximum = 10; nBoost.DecimalPlaces = 1; nBoost.Increment = 0.5m; nBoost.Width = 80;
        AddRow(t3, "Boost gauge full scale (bar)", nBoost);

        // ---- Redline
        TableLayoutPanel t4 = NewGroup(colL, "Redline");
        chAuto.Text = "Auto (learn highest RPM seen)"; chAuto.AutoSize = true;
        AddRow(t4, "", chAuto);
        nRed.Minimum = 1000; nRed.Maximum = 20000; nRed.Increment = 100; nRed.Width = 80;
        AddRow(t4, "Redline RPM", nRed);
        nFirst.Minimum = 0; nFirst.Maximum = 20000; nFirst.Increment = 100; nFirst.Width = 80;
        AddRow(t4, "First LED RPM", nFirst);
        btnReset.Text = "Reset learned peak (use when changing car)"; btnReset.AutoSize = true;
        AddRow(t4, "", btnReset);
        chTest.Text = "Simulate RPM sweep (test without the game)"; chTest.AutoSize = true;
        AddRow(t4, "", chTest);

        // ---- Reverse fix
        TableLayoutPanel t5 = NewGroup(colR, "Janky reverse gear fix (Worn shifter fixer)");
        chRev.Text = "Enable"; chRev.AutoSize = true;
        AddRow(t5, "", chRev);
        nRevBtn.Minimum = 0; nRevBtn.Maximum = 127; nRevBtn.Width = 80;
        AddRow(t5, "Reverse button # (reads as gear 6)", nRevBtn);
        btnLearn.Text = "Learn: click, then push into R within 8 s"; btnLearn.AutoSize = true;
        AddRow(t5, "", btnLearn);
        chSix.Text = "Real 6th gear is a different button"; chSix.AutoSize = true;
        AddRow(t5, "", chSix);
        nSixBtn.Minimum = 0; nSixBtn.Maximum = 127; nSixBtn.Width = 80;
        AddRow(t5, "Real 6th button #", nSixBtn);
        btnLearn6.Text = "Learn: click, then shift into real 6th"; btnLearn6.AutoSize = true;
        AddRow(t5, "", btnLearn6);
        nRevSpd.Minimum = 0; nRevSpd.Maximum = 60; nRevSpd.Width = 80;
        AddRow(t5, "Only below (km/h) - same-button mode", nRevSpd);
        cbRevKey.DropDownStyle = ComboBoxStyle.DropDownList; cbRevKey.Width = 100;
        cbRevKey.Items.AddRange(RevKeyNames);
        AddRow(t5, "Reverse key (bind in BeamNG)", cbRevKey);
        cbNeuKey.DropDownStyle = ComboBoxStyle.DropDownList; cbNeuKey.Width = 100;
        cbNeuKey.Items.AddRange(RevKeyNames);
        AddRow(t5, "Neutral key (sent on release)", cbNeuKey);
        lblRev.AutoSize = true; lblRev.MaximumSize = new Size(360, 0);
        AddRow(t5, "", lblRev);

        lblStatus.AutoSize = true; lblStatus.Margin = new Padding(6, 6, 6, 0);
        lblStatus.Text = "Hotkey: Ctrl+Alt+M cycles the LED mode (works while in-game)";
        lblStatus.MaximumSize = new Size(380, 0);
        colR.Controls.Add(lblStatus);

        // ---- Load settings into controls
        nPort.Value = Clamp(S.Port, nPort); nRed.Value = Clamp(S.Redline, nRed); nFirst.Value = Clamp(S.First, nFirst);
        nPct.Value = Clamp(S.FlashPct, nPct); nHz.Value = Clamp(S.FlashHz, nHz);
        cbMode.SelectedIndex = Math.Max(0, Math.Min(ModeNames.Length - 1, S.Mode));
        chAuto.Checked = S.Auto; chIgnoreX.Checked = S.IgnoreX;
        nRed.Enabled = nFirst.Enabled = !S.Auto;
        nBoost.Value = Clamp(S.BoostMax, nBoost); nRevBtn.Value = Clamp(S.RevButton, nRevBtn); nRevSpd.Value = Clamp(S.RevSpeed, nRevSpd);
        cbRevKey.SelectedIndex = Math.Max(0, Math.Min(RevKeyNames.Length - 1, S.RevKey));
        chRev.Checked = S.RevOn; chSix.Checked = S.SixOn;
        nSixBtn.Value = Clamp(S.SixButton, nSixBtn);
        cbNeuKey.SelectedIndex = Math.Max(0, Math.Min(RevKeyNames.Length - 1, S.NeuKey));
        nSixBtn.Enabled = btnLearn6.Enabled = S.SixOn; nRevSpd.Enabled = !S.SixOn;

        // ---- Events
        EventHandler changed = delegate { if (!loading) SaveSettings(); };
        cbMode.SelectedIndexChanged += changed; nPct.ValueChanged += changed; nHz.ValueChanged += changed;
        nRed.ValueChanged += changed; nFirst.ValueChanged += changed; cbDev.SelectedIndexChanged += changed;
        chRev.CheckedChanged += changed; nSixBtn.ValueChanged += changed; cbNeuKey.SelectedIndexChanged += changed;
        chSix.CheckedChanged += delegate { nSixBtn.Enabled = btnLearn6.Enabled = chSix.Checked; nRevSpd.Enabled = !chSix.Checked; if (!loading) SaveSettings(); };
        btnLearn6.Click += delegate { learning = true; learnTarget = 2; learnUntil = DateTime.Now.AddSeconds(8); }; nRevBtn.ValueChanged += changed; nRevSpd.ValueChanged += changed;
        cbRevKey.SelectedIndexChanged += changed; nBoost.ValueChanged += changed;
        btnLearn.Click += delegate { learning = true; learnTarget = 1; learnUntil = DateTime.Now.AddSeconds(8); };
        chAuto.CheckedChanged += delegate { nRed.Enabled = nFirst.Enabled = !chAuto.Checked; if (!loading) SaveSettings(); };
        chIgnoreX.CheckedChanged += delegate { if (loading) return; SaveSettings(); ResetSdk(); };
        nPort.ValueChanged += delegate { if (loading) return; SaveSettings(); Rebind(); };
        btnReset.Click += delegate { peak = 0; };

        // ---- Tray
        tray.Icon = SystemIcons.Application; tray.Text = "BeamNG Wheel LEDs";
        ContextMenu cm = new ContextMenu();
        cm.MenuItems.Add("Show", delegate { RestoreFromTray(); });
        cm.MenuItems.Add("Exit", delegate { Close(); });
        tray.ContextMenu = cm;
        tray.DoubleClick += delegate { RestoreFromTray(); };
        Resize += delegate { if (WindowState == FormWindowState.Minimized) { Hide(); tray.Visible = true; } };

        root.PerformLayout();
        Size pref = root.GetPreferredSize(Size.Empty);
        Rectangle wa = Screen.PrimaryScreen.WorkingArea;
        ClientSize = new Size(Math.Min(pref.Width + 24 + SystemInformation.VerticalScrollBarWidth, wa.Width - 40),
                              Math.Min(pref.Height + 24, wa.Height - 90));
        Rebind();
        loading = false;
        timer.Interval = 15; timer.Tick += Tick; timer.Start();
    }

    static decimal Clamp(decimal v, NumericUpDown n) { return Math.Max(n.Minimum, Math.Min(n.Maximum, v)); }

    static TableLayoutPanel NewGroup(FlowLayoutPanel parent, string title)
    {
        GroupBox g = new GroupBox(); g.Text = title; g.AutoSize = true; g.Padding = new Padding(8, 4, 8, 8);
        TableLayoutPanel t = new TableLayoutPanel(); t.ColumnCount = 2; t.AutoSize = true; t.Dock = DockStyle.Fill;
        g.Controls.Add(t); parent.Controls.Add(g);
        return t;
    }

    static void AddRow(TableLayoutPanel t, string label, Control c)
    {
        Label l = new Label(); l.Text = label; l.AutoSize = true; l.Anchor = AnchorStyles.Left; l.Margin = new Padding(0, 4, 12, 4);
        c.Anchor = AnchorStyles.Left; c.Margin = new Padding(0, 1, 0, 1);
        int r = t.RowCount++;
        t.Controls.Add(l, 0, r); t.Controls.Add(c, 1, r);
    }

    void SaveSettings()
    {
        S.Mode = cbMode.SelectedIndex; S.Redline = nRed.Value; S.First = nFirst.Value; S.FlashPct = nPct.Value;
        S.FlashHz = nHz.Value; S.Port = nPort.Value;
        S.RevOn = chRev.Checked; S.RevButton = (int)nRevBtn.Value; S.RevSpeed = nRevSpd.Value;
        S.RevKey = cbRevKey.SelectedIndex; S.SixOn = chSix.Checked; S.SixButton = (int)nSixBtn.Value; S.NeuKey = cbNeuKey.SelectedIndex; S.BoostMax = nBoost.Value; S.Auto = chAuto.Checked; S.IgnoreX = chIgnoreX.Checked;
        DevItem d = cbDev.SelectedItem as DevItem; if (d != null) S.Device = d.Index;
        S.Save();
    }

    void RestoreFromTray() { Show(); WindowState = FormWindowState.Normal; tray.Visible = false; Activate(); }

    void CycleMode()
    {
        cbMode.SelectedIndex = (cbMode.SelectedIndex + 1) % ModeNames.Length;
        tray.Visible = true;
        tray.ShowBalloonTip(1200, "LED mode", ModeNames[cbMode.SelectedIndex], ToolTipIcon.None);
    }

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        Win.RegisterHotKey(Handle, 1, 0x0003 /*Ctrl+Alt*/, 0x4D /*M*/);
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == 0x0312 && m.WParam.ToInt32() == 1) CycleMode();
        base.WndProc(ref m);
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        try { Win.UnregisterHotKey(Handle, 1); } catch { }
        try { DevItem d = cbDev.SelectedItem as DevItem; Logi.LogiPlayLeds(d != null ? d.Index : 0, 0, 1, 2); Logi.LogiSteeringShutdown(); } catch { }
        tray.Visible = false;
        SaveSettings();
        base.OnFormClosing(e);
    }

    void Rebind()
    {
        try { if (udp != null) udp.Close(); } catch { }
        udp = null; netMsg = "";
        try { udp = new UdpClient(new IPEndPoint(IPAddress.Loopback, (int)nPort.Value)); }
        catch (Exception ex) { netMsg = "Cannot open UDP port " + nPort.Value + ": " + ex.Message; }
    }

    void ResetSdk()
    {
        try { Logi.LogiSteeringShutdown(); } catch { }
        inited = false; lastInit = DateTime.MinValue; initMsg = "restarting SDK...";
    }

    void TryInit()
    {
        if (inited || (DateTime.Now - lastInit).TotalSeconds < 1) return;
        lastInit = DateTime.Now;
        try
        {
            inited = Logi.LogiSteeringInitializeWithWindow(chIgnoreX.Checked, Handle);
            initMsg = inited ? "SDK ready" : "SDK not ready yet - click this window to bring it to the foreground";
        }
        catch (Exception e) { initMsg = "SDK error: " + e.Message; }
    }

    void ScanDevices()
    {
        if ((DateTime.Now - lastDevScan).TotalSeconds < 1) return;
        lastDevScan = DateTime.Now;
        List<DevItem> found = new List<DevItem>();
        for (int i = 0; i < 4; i++)
        {
            try
            {
                if (!Logi.LogiIsConnected(i)) continue;
                StringBuilder sb = new StringBuilder(256);
                string n = Logi.LogiGetFriendlyProductName(i, sb, 256) && sb.Length > 0 ? sb.ToString() : "Logitech device";
                DevItem d = new DevItem(); d.Index = i; d.Name = n; found.Add(d);
            }
            catch { }
        }
        bool same = found.Count == cbDev.Items.Count;
        for (int i = 0; same && i < found.Count; i++) same = found[i].ToString() == cbDev.Items[i].ToString();
        if (same) return;

        DevItem cur = cbDev.SelectedItem as DevItem;
        int want = cur != null ? cur.Index : S.Device;
        bool was = loading; loading = true;
        cbDev.Items.Clear();
        int sel = 0;
        for (int i = 0; i < found.Count; i++) { cbDev.Items.Add(found[i]); if (found[i].Index == want) sel = i; }
        if (found.Count > 0) cbDev.SelectedIndex = sel;
        loading = was;
    }

    bool BlinkOn()
    {
        double half = 500.0 / (double)nHz.Value;
        return ((long)((DateTime.Now - t0).TotalMilliseconds / half) % 2) == 0;
    }

    // LEDs show a gauge (0..1) instead of RPM. Uses the SDK's LED scale 1..1000 as a generic bar.
    bool PlayGauge(int idx, int mode, bool live)
    {
        float g;
        if (chTest.Checked) g = (float)(0.5 - 0.5 * Math.Cos((DateTime.Now - t0).TotalSeconds * 0.8));
        else if (!live) return Logi.LogiPlayLeds(idx, 0f, 1f, 1000f);
        else if (mode == 3) g = fuel;                          // 0..1
        else if (mode == 4) g = (engT - 60f) / 60f;            // 60..120 C
        else if (mode == 5) g = (oilT - 60f) / 90f;            // 60..150 C
        else g = turbo / (float)nBoost.Value;                  // 0..full scale (bar)
        g = Math.Max(0f, Math.Min(1f, g));
        bool low = mode == 3;
        bool warn = low ? g <= 0.10f : g >= 0.92f;
        float lv = g * 999f;
        if (warn)
        {
            if (!BlinkOn()) lv = 0f;
            else lv = low ? Math.Max(lv, 200f) : 999f;
        }
        return Logi.LogiPlayLeds(idx, lv, 1f, 1000f);
    }

    ushort VkOf(ComboBox cb) { return RevVk[Math.Max(0, Math.Min(RevVk.Length - 1, cb.SelectedIndex))]; }

    void Tap(ushort vk)
    {
        if (keyDown != DateTime.MinValue) return;
        Win.Key(vk, true); downVk = vk; keyDown = DateTime.Now; lastTap = DateTime.Now;
    }

    // Worn shifter: reverse is read as the last gear button (or as a button of its own).
    // Engaging: tap the Reverse key. Leaving reverse (button released): tap the Neutral key.
    void HandleReverse(DevItem dev, bool live)
    {
        if (keyDown != DateTime.MinValue && (DateTime.Now - keyDown).TotalMilliseconds >= 45) { Win.Key(downVk, false); keyDown = DateTime.MinValue; }
        if (dev == null) { lblRev.Text = "No wheel selected"; return; }

        if (learning)
        {
            if (DateTime.Now > learnUntil) learning = false;
            else
            {
                lblRev.Text = "Listening... " + (learnTarget == 1 ? "push the shifter into R" : "shift into the real 6th");
                for (int b = 0; b < 128; b++)
                    if (Logi.LogiButtonIsPressed(dev.Index, b)) { (learnTarget == 1 ? nRevBtn : nSixBtn).Value = b; learning = false; break; }
                return;
            }
        }

        bool a = Logi.LogiButtonIsPressed(dev.Index, (int)nRevBtn.Value);
        bool six = chSix.Checked && Logi.LogiButtonIsPressed(dev.Index, (int)nSixBtn.Value);
        float kmh = speedMs * 3.6f;
        StringBuilder pr = new StringBuilder();
        int cnt = 0;
        for (int b = 0; b < 128 && cnt < 8; b++)
            if (Logi.LogiButtonIsPressed(dev.Index, b)) { if (cnt > 0) pr.Append(", "); pr.Append(b); cnt++; }
        lblRev.Text = "Pressed buttons: " + (cnt == 0 ? "-" : pr.ToString()) + string.Format("\nSpeed {0:0} km/h | game gear {1} | state: {2}",
            kmh, gear == 0 ? "R" : (gear == 1 ? "N" : (gear - 1).ToString()),
            weReversed ? "reverse" : (neutralPending ? "returning to neutral" : "idle"));

        if (!chRev.Checked || !live) { weReversed = false; neutralPending = false; return; }

        bool start = chSix.Checked ? (a && !six) : (a && kmh <= (float)nRevSpd.Value);   // speed check only when 6th and R share a button
        bool hold = weReversed && a && !six;                                              // once engaged, stay until released
        if (start || hold)
        {
            weReversed = true; neutralPending = false;
            if (gear != 0 && (DateTime.Now - lastTap).TotalMilliseconds >= 250) Tap(VkOf(cbRevKey));
        }
        else if (weReversed)
        {
            weReversed = false;                      // shifter left R -> put the car back in neutral
            neutralPending = true; neutralUntil = DateTime.Now.AddMilliseconds(1500);
        }

        if (neutralPending)
        {
            if (gear != 0 || DateTime.Now > neutralUntil) neutralPending = false;          // done (or gave up)
            else if ((DateTime.Now - lastTap).TotalMilliseconds >= 250) Tap(VkOf(cbNeuKey));
        }
    }

    void Tick(object s, EventArgs ev)
    {
        TryInit();

        // ---- telemetry
        try
        {
            while (udp != null && udp.Available > 0)
            {
                IPEndPoint ep = null;
                byte[] d = udp.Receive(ref ep);
                if (d.Length >= 92)
                {
                    gear = d[10];                              // 0 = reverse, 1 = neutral, 2 = 1st ...
                    speedMs = BitConverter.ToSingle(d, 12);
                    rpm = BitConverter.ToSingle(d, 16);
                    turbo = BitConverter.ToSingle(d, 20);      // bar
                    engT = BitConverter.ToSingle(d, 24);       // deg C
                    fuel = BitConverter.ToSingle(d, 28);       // 0..1
                    oilT = BitConverter.ToSingle(d, 36);       // deg C
                    lastPacket = DateTime.Now; packets++;
                }
            }
        }
        catch { }
        bool live = (DateTime.Now - lastPacket).TotalSeconds <= 1.0;
        if (!live) { rpm = 0; speedMs = 0; }

        // ---- redline / first LED
        float red, fst;
        if (chTest.Checked)
        {
            red = chAuto.Checked ? 7000f : (float)nRed.Value;
            double ph = 0.5 - 0.5 * Math.Cos((DateTime.Now - t0).TotalSeconds * 0.8);
            rpm = (float)(500 + (red * 1.05 - 500) * ph);
            fst = chAuto.Checked ? red * 0.70f : (float)nFirst.Value;
        }
        else if (chAuto.Checked)
        {
            if (rpm > peak) peak = rpm;
            red = Math.Max(peak * 0.97f, 2000f);
            fst = red * 0.70f;
        }
        else { red = (float)nRed.Value; fst = (float)nFirst.Value; }
        if (fst >= red) fst = red * 0.70f;

        // ---- wheel
        DevItem dev = cbDev.SelectedItem as DevItem;
        string ledResult = "-";
        if (inited)
        {
            try
            {
                Logi.LogiUpdate();
                ScanDevices();
                dev = cbDev.SelectedItem as DevItem;
                HandleReverse(dev, live);
                if (dev != null && (DateTime.Now - lastWrite).TotalMilliseconds >= 20)
                {
                    lastWrite = DateTime.Now;
                    int mode = cbMode.SelectedIndex;
                    bool flashZone = rpm >= red * (float)nPct.Value / 100f;
                    bool ok;
                    if (mode >= 3) ok = PlayGauge(dev.Index, mode, live);
                    else if (mode == 0) ok = Logi.LogiPlayLeds(dev.Index, rpm, fst, red);
                    else if (flashZone && rpm > 0)
                        ok = Logi.LogiPlayLeds(dev.Index, BlinkOn() ? red * 0.999f : 0f, fst, red);   // all on / all off
                    else if (mode == 1) ok = Logi.LogiPlayLeds(dev.Index, rpm, fst, red);
                    else ok = Logi.LogiPlayLeds(dev.Index, 0f, fst, red);                              // shift-only: dark until flash zone
                    ledResult = ok ? "ok" : "failed";
                }
            }
            catch (Exception ex) { initMsg = "SDK error: " + ex.Message; }
        }

        // ---- UI
        string name = dev != null ? dev.Name : null;
        if (name == null) lblDev.Text = inited ? "No Logitech wheel detected" : initMsg;
        else
        {
            string note = name.IndexOf("G920", StringComparison.OrdinalIgnoreCase) >= 0
                ? "\nNote: the G920 has no RPM LEDs, so nothing will light up." : "";
            lblDev.Text = initMsg + " | LEDs: " + ledResult + note;
        }
        lblTel.Text = netMsg.Length > 0 ? netMsg : (live ? packets + " (receiving)" : packets + " (waiting for BeamNG...)");
        lblRpm.Text = string.Format("{0:0}  /  {1:0}", rpm, red);
        bar.Value = (int)Math.Max(0, Math.Min(1000, rpm / (red * 1.1f) * 1000f));
    }
}

static class Program
{
    [STAThread]
    static void Main()
    {
        Application.EnableVisualStyles();
        Application.Run(new MainForm());
    }
}
