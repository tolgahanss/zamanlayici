using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Media;
using System.Net.NetworkInformation;
using System.Linq;

namespace Zamanlayici
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }

    // =============================================
    //  Double-buffered panel
    // =============================================
    public class BufferedPanel : Panel
    {
        public BufferedPanel()
        {
            DoubleBuffered = true;
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);
        }
    }

    // =============================================
    //  Modern Rounded Button (owner-drawn)
    // =============================================
    public class RoundedButton : Button
    {
        public int Radius { get; set; }
        public Color GradientStart { get; set; }
        public Color GradientEnd { get; set; }
        public bool UseGradient { get; set; }
        private bool hovering = false;

        public RoundedButton()
        {
            Radius = 12;
            UseGradient = false;
            GradientStart = Color.Transparent;
            GradientEnd = Color.Transparent;
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            FlatAppearance.MouseOverBackColor = Color.Transparent;
            FlatAppearance.MouseDownBackColor = Color.Transparent;
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.DoubleBuffer, true);
            Cursor = Cursors.Hand;
        }

        protected override void OnMouseEnter(EventArgs e) { hovering = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { hovering = false; Invalidate(); base.OnMouseLeave(e); }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            var rect = new Rectangle(0, 0, Width - 1, Height - 1);
            var path = CreateRoundedPath(rect, Radius);

            // Background
            if (UseGradient && GradientStart != Color.Transparent)
            {
                using (var brush = new LinearGradientBrush(rect, GradientStart, GradientEnd, 135f))
                    g.FillPath(brush, path);
            }
            else
            {
                using (var brush = new SolidBrush(BackColor))
                    g.FillPath(brush, path);
            }

            // Hover overlay
            if (hovering && Enabled)
            {
                using (var brush = new SolidBrush(Color.FromArgb(25, 255, 255, 255)))
                    g.FillPath(brush, path);
            }

            // Disabled overlay
            if (!Enabled)
            {
                using (var brush = new SolidBrush(Color.FromArgb(120, 12, 15, 28)))
                    g.FillPath(brush, path);
            }

            // Text
            using (var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
            {
                Color textCol = Enabled ? ForeColor : Color.FromArgb(100, ForeColor);
                using (var brush = new SolidBrush(textCol))
                    g.DrawString(Text, Font, brush, rect, sf);
            }

            path.Dispose();
        }

        private GraphicsPath CreateRoundedPath(Rectangle rect, int rad)
        {
            var path = new GraphicsPath();
            int d = rad * 2;
            path.AddArc(rect.X, rect.Y, d, d, 180, 90);
            path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
            path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
            path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }
    }

    // =============================================
    //  Windows API - Idle Detection
    // =============================================
    [StructLayout(LayoutKind.Sequential)]
    public struct LASTINPUTINFO
    {
        public uint cbSize;
        public uint dwTime;
    }

    // =============================================
    //  Main Application Form
    // =============================================
    public class MainForm : Form
    {
        [DllImport("user32.dll")]
        static extern bool GetLastInputInfo(ref LASTINPUTINFO plii);

        // --- State ---
        private bool timerRunning = false;
        private int remainingSeconds = 0;
        private int totalSeconds = 0;
        private string actionMode = "shutdown";
        private string timerMode = "countdown";
        private float timerProgress = 0f;
        private bool earlyWarningGiven = false;
        private long lastBytesReceived = 0;
        private int trackerConditionMetCount = 0;

        // --- Colors ---
        static readonly Color BgDark = Color.FromArgb(10, 12, 24);
        static readonly Color BgCard = Color.FromArgb(18, 24, 42);
        static readonly Color BgInput = Color.FromArgb(24, 32, 52);
        static readonly Color BgHover = Color.FromArgb(32, 40, 62);
        static readonly Color AccentBlue = Color.FromArgb(99, 102, 241);
        static readonly Color AccentBlueDark = Color.FromArgb(79, 70, 229);
        static readonly Color AccentBlueLight = Color.FromArgb(129, 140, 248);
        static readonly Color AccentCyan = Color.FromArgb(6, 182, 212);
        static readonly Color AccentOrange = Color.FromArgb(245, 158, 11);
        static readonly Color AccentOrangeDark = Color.FromArgb(217, 119, 6);
        static readonly Color AccentOrangeLight = Color.FromArgb(251, 191, 36);
        static readonly Color AccentRed = Color.FromArgb(239, 68, 68);
        static readonly Color AccentRedDark = Color.FromArgb(220, 38, 38);
        static readonly Color AccentGreen = Color.FromArgb(16, 185, 129);
        static readonly Color TextPrimary = Color.FromArgb(241, 245, 249);
        static readonly Color TextSecondary = Color.FromArgb(148, 163, 184);
        static readonly Color TextMuted = Color.FromArgb(100, 116, 139);
        static readonly Color BorderCol = Color.FromArgb(35, 45, 75);
        static readonly Color SelectedBg = Color.FromArgb(35, 99, 102, 241);

        // --- Controls ---
        private BufferedPanel timerRingPanel;
        private Label timerDigitsLabel;
        private Label timerSubLabel;
        private Label statusLabel;
        private Label infoLabel;
        private Label idleStatusLabel;
        private RoundedButton btnModeShutdown, btnModeRestart;
        private RoundedButton btnTimerCountdown, btnTimerIdle, btnTimerTracker;
        private TextBox inputHours, inputMinutes, inputSeconds;
        private RoundedButton btnStart, btnCancel;
        private RoundedButton[] presetButtons;
        private RoundedButton selectedPreset = null;
        private System.Windows.Forms.Timer countdownTimer;
        
        private NotifyIcon trayIcon;
        private BufferedPanel trackerPanel;
        private RadioButton radioProcess, radioNetwork;
        private TextBox inputProcessName, inputNetworkThreshold;
        private Label presetLabel, customLabel;

        public MainForm()
        {
            InitializeForm();
            BuildUI();
            SetupTimer();
        }

        // =============================================
        //  Idle Detection
        // =============================================
        private int GetSystemIdleSeconds()
        {
            LASTINPUTINFO lii = new LASTINPUTINFO();
            lii.cbSize = (uint)Marshal.SizeOf(typeof(LASTINPUTINFO));
            if (GetLastInputInfo(ref lii))
            {
                uint idleMs = (uint)Environment.TickCount - lii.dwTime;
                return (int)(idleMs / 1000);
            }
            return 0;
        }

        // =============================================
        //  Form Setup
        // =============================================
        private void InitializeForm()
        {
            Text = "Zamanlayici";
            Size = new Size(480, 830);
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            BackColor = BgDark;
            ForeColor = TextPrimary;
            DoubleBuffered = true;
            Font = new Font("Segoe UI", 9f);
            this.Resize += MainForm_Resize;
        }

        // =============================================
        //  Build UI
        // =============================================
        private void BuildUI()
        {
            var mainPanel = new BufferedPanel { Dock = DockStyle.Fill, BackColor = BgDark, AutoScroll = true };
            Controls.Add(mainPanel);

            int y = 15;

            // --- HEADER ---
            var header = new BufferedPanel { Location = new Point(15, y), Size = new Size(434, 66), BackColor = Color.Transparent };
            header.Paint += delegate(object s, PaintEventArgs ev) { PaintHeader(ev.Graphics, header.ClientRectangle); };
            mainPanel.Controls.Add(header);

            statusLabel = new Label
            {
                Location = new Point(326, 22), Size = new Size(96, 22),
                Text = "\u25CF  Hazir", ForeColor = AccentGreen,
                Font = new Font("Segoe UI", 8f, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleRight, BackColor = Color.Transparent
            };
            header.Controls.Add(statusLabel);
            y += 78;

            // --- TIMER RING ---
            timerRingPanel = new BufferedPanel { Location = new Point(15, y), Size = new Size(434, 210), BackColor = Color.Transparent };
            timerRingPanel.Paint += delegate(object s, PaintEventArgs ev) { PaintTimerRing(ev.Graphics, timerRingPanel.ClientRectangle); };
            mainPanel.Controls.Add(timerRingPanel);

            timerDigitsLabel = new Label
            {
                Location = new Point(0, 68), Size = new Size(434, 55),
                Text = "00:00:00", Font = new Font("Consolas", 40f, FontStyle.Bold),
                ForeColor = TextPrimary, TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent
            };
            timerRingPanel.Controls.Add(timerDigitsLabel);

            timerSubLabel = new Label
            {
                Location = new Point(0, 128), Size = new Size(434, 22),
                Text = "Sure Belirleyin", Font = new Font("Segoe UI", 9f),
                ForeColor = TextMuted, TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent
            };
            timerRingPanel.Controls.Add(timerSubLabel);
            y += 210;

            // --- TIMER MODE SELECTOR ---
            var timerModePanel = new BufferedPanel { Location = new Point(15, y), Size = new Size(434, 52), BackColor = Color.Transparent };
            timerModePanel.Paint += delegate(object s, PaintEventArgs ev) { PaintRoundedCard(ev.Graphics, timerModePanel.ClientRectangle, 14); };
            mainPanel.Controls.Add(timerModePanel);

            btnTimerCountdown = new RoundedButton
            {
                Location = new Point(6, 6), Size = new Size(138, 40),
                Text = "Zamanlayici", Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                BackColor = AccentBlue, ForeColor = Color.White,
                UseGradient = true, GradientStart = AccentBlue, GradientEnd = AccentBlueDark,
                Radius = 10
            };
            btnTimerCountdown.Click += delegate { SetTimerMode("countdown"); };
            timerModePanel.Controls.Add(btnTimerCountdown);

            btnTimerIdle = new RoundedButton
            {
                Location = new Point(148, 6), Size = new Size(138, 40),
                Text = "Hareketsizlik", Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                BackColor = BgCard, ForeColor = TextSecondary, Radius = 10
            };
            btnTimerIdle.Click += delegate { SetTimerMode("idle"); };
            timerModePanel.Controls.Add(btnTimerIdle);

            btnTimerTracker = new RoundedButton
            {
                Location = new Point(290, 6), Size = new Size(138, 40),
                Text = "Akilli Izleyici", Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                BackColor = BgCard, ForeColor = TextSecondary, Radius = 10
            };
            btnTimerTracker.Click += delegate { SetTimerMode("tracker"); };
            timerModePanel.Controls.Add(btnTimerTracker);
            y += 60;

            // --- ACTION MODE SELECTOR ---
            var modePanel = new BufferedPanel { Location = new Point(15, y), Size = new Size(434, 52), BackColor = Color.Transparent };
            modePanel.Paint += delegate(object s, PaintEventArgs ev) { PaintRoundedCard(ev.Graphics, modePanel.ClientRectangle, 14); };
            mainPanel.Controls.Add(modePanel);

            btnModeShutdown = new RoundedButton
            {
                Location = new Point(6, 6), Size = new Size(208, 40),
                Text = "Bilgisayari Kapat", Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                BackColor = AccentBlue, ForeColor = Color.White,
                UseGradient = true, GradientStart = AccentBlue, GradientEnd = AccentBlueDark,
                Radius = 10
            };
            btnModeShutdown.Click += delegate { SetActionMode("shutdown"); };
            modePanel.Controls.Add(btnModeShutdown);

            btnModeRestart = new RoundedButton
            {
                Location = new Point(220, 6), Size = new Size(208, 40),
                Text = "Yeniden Baslat", Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                BackColor = BgCard, ForeColor = TextSecondary, Radius = 10
            };
            btnModeRestart.Click += delegate { SetActionMode("restart"); };
            modePanel.Controls.Add(btnModeRestart);
            y += 60;

            // --- IDLE STATUS LABEL ---
            idleStatusLabel = new Label
            {
                Location = new Point(15, y), Size = new Size(434, 30),
                Font = new Font("Segoe UI", 8.5f), ForeColor = AccentOrange,
                TextAlign = ContentAlignment.MiddleCenter, BackColor = Color.Transparent,
                Text = "", Visible = false
            };
            mainPanel.Controls.Add(idleStatusLabel);

            // --- PRESETS ---
            presetLabel = new Label
            {
                Location = new Point(22, y), Size = new Size(200, 20),
                Text = "HIZLI AYAR", Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                ForeColor = TextMuted, BackColor = Color.Transparent
            };
            mainPanel.Controls.Add(presetLabel);
            y += 26;

            var presetData = new[] {
                new { Text = "15 dk", Min = 15 },
                new { Text = "30 dk", Min = 30 },
                new { Text = "45 dk", Min = 45 },
                new { Text = "1 saat", Min = 60 },
                new { Text = "1.5 saat", Min = 90 },
                new { Text = "2 saat", Min = 120 }
            };

            presetButtons = new RoundedButton[presetData.Length];
            for (int i = 0; i < presetData.Length; i++)
            {
                int col = i % 3, row = i / 3;
                var btn = MakePresetButton(presetData[i].Text, presetData[i].Min,
                    new Point(15 + col * 146, y + row * 58));
                mainPanel.Controls.Add(btn);
                presetButtons[i] = btn;
            }
            y += 128;

            // --- CUSTOM TIME ---
            customLabel = new Label
            {
                Location = new Point(22, y), Size = new Size(200, 20),
                Text = "OZEL SURE", Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                ForeColor = TextMuted, BackColor = Color.Transparent
            };
            mainPanel.Controls.Add(customLabel);
            y += 28;

            int sx = 75;
            inputHours = MakeTimeInput(new Point(sx, y));
            mainPanel.Controls.Add(inputHours);
            mainPanel.Controls.Add(MakeTimeLabel("SAAT", new Point(sx, y + 44)));
            mainPanel.Controls.Add(MakeTimeSep(new Point(sx + 87, y)));

            inputMinutes = MakeTimeInput(new Point(sx + 115, y));
            mainPanel.Controls.Add(inputMinutes);
            mainPanel.Controls.Add(MakeTimeLabel("DAKIKA", new Point(sx + 115, y + 44)));
            mainPanel.Controls.Add(MakeTimeSep(new Point(sx + 202, y)));

            inputSeconds = MakeTimeInput(new Point(sx + 230, y));
            mainPanel.Controls.Add(inputSeconds);
            mainPanel.Controls.Add(MakeTimeLabel("SANIYE", new Point(sx + 230, y + 44)));
            y += 74;

            // --- ACTION BUTTONS ---
            y += 10;
            
            // --- TRACKER PANEL ---
            trackerPanel = new BufferedPanel { Location = new Point(15, 395), Size = new Size(434, 250), BackColor = Color.Transparent, Visible = false };
            mainPanel.Controls.Add(trackerPanel);

            radioProcess = new RadioButton { Text = "Islem (Program) Izle", Location = new Point(10, 10), Size = new Size(200, 24), ForeColor = TextPrimary, Checked = true, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold) };
            trackerPanel.Controls.Add(radioProcess);
            trackerPanel.Controls.Add(new Label { Text = "Kapanmasini beklediginiz programin adini (exe haric) yazin.\nOrnek: steam, IDMan, chrome", Location = new Point(30, 40), Size = new Size(380, 36), ForeColor = TextMuted });
            
            inputProcessName = new TextBox { Location = new Point(30, 80), Size = new Size(300, 25), BackColor = BgInput, ForeColor = TextPrimary, BorderStyle = BorderStyle.FixedSingle, Font = new Font("Segoe UI", 10f) };
            trackerPanel.Controls.Add(inputProcessName);

            radioNetwork = new RadioButton { Text = "Ag (Internet) Izle", Location = new Point(10, 120), Size = new Size(200, 24), ForeColor = TextPrimary, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold) };
            trackerPanel.Controls.Add(radioNetwork);
            trackerPanel.Controls.Add(new Label { Text = "Internet hizi belirlenen KB/s altina duserse kapatir.\nOrnek: 100 (Indirme bitince hiz duser)", Location = new Point(30, 150), Size = new Size(380, 36), ForeColor = TextMuted });
            
            inputNetworkThreshold = new TextBox { Location = new Point(30, 190), Size = new Size(100, 25), Text = "100", BackColor = BgInput, ForeColor = TextPrimary, BorderStyle = BorderStyle.FixedSingle, Font = new Font("Segoe UI", 10f) };
            trackerPanel.Controls.Add(inputNetworkThreshold);
            trackerPanel.Controls.Add(new Label { Text = "KB/s", Location = new Point(140, 192), Size = new Size(50, 20), ForeColor = TextMuted });

            // --- TRAY ICON SETUP ---
            trayIcon = new NotifyIcon();
            trayIcon.Text = "Zamanlayici";
            try { trayIcon.Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath); } catch { trayIcon.Icon = SystemIcons.Information; }
            trayIcon.DoubleClick += (s, ev) => { this.Show(); this.WindowState = FormWindowState.Normal; trayIcon.Visible = false; };
            
            ContextMenuStrip menu = new ContextMenuStrip();
            menu.Items.Add("Goster", null, (s, ev) => { this.Show(); this.WindowState = FormWindowState.Normal; trayIcon.Visible = false; });
            menu.Items.Add("Iptal Et", null, BtnCancel_Click);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add("Cikis", null, (s, ev) => { Application.Exit(); });
            trayIcon.ContextMenuStrip = menu;

            btnStart = new RoundedButton
            {
                Location = new Point(15, y), Size = new Size(434, 52),
                Text = "\u25B6   BASLAT", Font = new Font("Segoe UI", 11.5f, FontStyle.Bold),
                BackColor = AccentBlue, ForeColor = Color.White,
                UseGradient = true, GradientStart = AccentBlue, GradientEnd = AccentBlueDark,
                Radius = 14
            };
            btnStart.Click += BtnStart_Click;
            mainPanel.Controls.Add(btnStart);

            btnCancel = new RoundedButton
            {
                Location = new Point(15, y), Size = new Size(434, 52),
                Text = "\u25A0   IPTAL ET", Font = new Font("Segoe UI", 11.5f, FontStyle.Bold),
                BackColor = AccentRed, ForeColor = Color.White,
                UseGradient = true, GradientStart = AccentRed, GradientEnd = AccentRedDark,
                Radius = 14, Visible = false
            };
            btnCancel.Click += BtnCancel_Click;
            mainPanel.Controls.Add(btnCancel);

            y += 60;
            infoLabel = new Label
            {
                Location = new Point(15, y), Size = new Size(434, 36),
                Font = new Font("Segoe UI", 9f), ForeColor = TextSecondary,
                TextAlign = ContentAlignment.MiddleCenter, BackColor = Color.Transparent,
                Visible = false
            };
            mainPanel.Controls.Add(infoLabel);
        }

        // =============================================
        //  Paint Helpers
        // =============================================
        private void MainForm_Resize(object sender, EventArgs e)
        {
            if (this.WindowState == FormWindowState.Minimized)
            {
                this.Hide();
                trayIcon.Visible = true;
                if (timerRunning && !earlyWarningGiven)
                {
                    trayIcon.ShowBalloonTip(3000, "Zamanlayici Arka Planda", "Program sag alt kosede calismaya devam ediyor.", ToolTipIcon.Info);
                }
            }
        }

        private GraphicsPath RoundedRect(Rectangle r, int rad)
        {
            var path = new GraphicsPath();
            int d = rad * 2;
            path.AddArc(r.X, r.Y, d, d, 180, 90);
            path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        private void PaintRoundedCard(Graphics g, Rectangle r, int rad)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var rect = new Rectangle(0, 0, r.Width - 1, r.Height - 1);
            using (var path = RoundedRect(rect, rad))
            using (var brush = new SolidBrush(BgCard))
            using (var pen = new Pen(BorderCol))
            {
                g.FillPath(brush, path);
                g.DrawPath(pen, path);
            }
        }

        private void PaintHeader(Graphics g, Rectangle r)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            var rect = new Rectangle(0, 0, r.Width - 1, r.Height - 1);
            using (var path = RoundedRect(rect, 16))
            using (var brush = new SolidBrush(BgCard))
            using (var pen = new Pen(BorderCol))
            {
                g.FillPath(brush, path);
                g.DrawPath(pen, path);
            }

            // Gradient icon box
            var iconRect = new Rectangle(16, 13, 40, 40);
            using (var iconPath = RoundedRect(iconRect, 12))
            using (var gb = new LinearGradientBrush(iconRect, AccentBlue, AccentCyan, 135f))
            {
                g.FillPath(gb, iconPath);
            }

            // Power icon
            using (var pen = new Pen(Color.White, 2.2f) { StartCap = LineCap.Round, EndCap = LineCap.Round })
            {
                g.DrawArc(pen, 25, 22, 22, 22, -55, 290);
                g.DrawLine(pen, 36, 20, 36, 30);
            }

            // Title
            using (var f1 = new Font("Segoe UI", 14f, FontStyle.Bold))
            using (var f2 = new Font("Segoe UI", 8f))
            {
                g.DrawString("Zamanlayici", f1, new SolidBrush(TextPrimary), 66, 13);
                g.DrawString("PC Kapatma & Yeniden Baslatma", f2, new SolidBrush(TextMuted), 67, 38);
            }
        }

        private void PaintTimerRing(Graphics g, Rectangle r)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            float cx = r.Width / 2f, cy = 102f, radius = 88f;

            Color ringColor = (timerMode == "idle" && timerRunning) ? AccentOrange : AccentBlue;

            // Outer glow circle
            using (var pen = new Pen(Color.FromArgb(10, ringColor), 20f))
                g.DrawEllipse(pen, cx - radius, cy - radius, radius * 2, radius * 2);

            // Background ring
            using (var pen = new Pen(Color.FromArgb(20, ringColor), 5f))
                g.DrawEllipse(pen, cx - radius, cy - radius, radius * 2, radius * 2);

            // Progress ring
            if (timerProgress > 0.001f)
            {
                float sweep = 360f * timerProgress;

                // Glow
                using (var pen = new Pen(Color.FromArgb(35, ringColor), 14f) { StartCap = LineCap.Round, EndCap = LineCap.Round })
                    g.DrawArc(pen, cx - radius, cy - radius, radius * 2, radius * 2, -90f, sweep);

                // Main arc
                using (var pen = new Pen(ringColor, 5f) { StartCap = LineCap.Round, EndCap = LineCap.Round })
                    g.DrawArc(pen, cx - radius, cy - radius, radius * 2, radius * 2, -90f, sweep);

                // Bright tip dot
                double angle = (-90 + sweep) * Math.PI / 180.0;
                float dotX = cx + (float)(radius * Math.Cos(angle));
                float dotY = cy + (float)(radius * Math.Sin(angle));
                using (var brush = new SolidBrush(Color.FromArgb(200, ringColor)))
                    g.FillEllipse(brush, dotX - 4, dotY - 4, 8, 8);
                using (var brush = new SolidBrush(Color.White))
                    g.FillEllipse(brush, dotX - 2, dotY - 2, 4, 4);
            }
        }

        // =============================================
        //  Control Factories
        // =============================================
        private RoundedButton MakePresetButton(string text, int minutes, Point loc)
        {
            var btn = new RoundedButton
            {
                Location = loc, Size = new Size(138, 50), Text = text,
                Font = new Font("Consolas", 13f, FontStyle.Bold),
                BackColor = BgCard, ForeColor = TextPrimary,
                Radius = 12, Tag = minutes
            };
            btn.Click += PresetBtn_Click;
            return btn;
        }

        private TextBox MakeTimeInput(Point loc)
        {
            var tb = new TextBox
            {
                Location = loc, Size = new Size(80, 42),
                Font = new Font("Consolas", 22f, FontStyle.Bold),
                BackColor = BgInput, ForeColor = TextPrimary,
                BorderStyle = BorderStyle.FixedSingle,
                TextAlign = HorizontalAlignment.Center,
                Text = "0", MaxLength = 2
            };
            tb.KeyPress += delegate(object s, KeyPressEventArgs ev) { if (!char.IsDigit(ev.KeyChar) && ev.KeyChar != '\b') ev.Handled = true; };
            tb.TextChanged += InputChanged;
            return tb;
        }

        private Label MakeTimeLabel(string text, Point loc)
        {
            return new Label
            {
                Location = loc, Size = new Size(80, 16), Text = text,
                Font = new Font("Segoe UI", 7.5f, FontStyle.Bold),
                ForeColor = TextMuted, TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent
            };
        }

        private Label MakeTimeSep(Point loc)
        {
            return new Label
            {
                Location = loc, Size = new Size(25, 42), Text = ":",
                Font = new Font("Consolas", 22f, FontStyle.Bold),
                ForeColor = Color.FromArgb(60, 70, 100), TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent
            };
        }

        // =============================================
        //  Timer
        // =============================================
        private void SetupTimer()
        {
            countdownTimer = new System.Windows.Forms.Timer { Interval = 1000 };
            countdownTimer.Tick += CountdownTick;
        }

        private void CountdownTick(object sender, EventArgs e)
        {
            if (timerMode == "idle") { IdleTick(); return; }
            if (timerMode == "tracker") { TrackerTick(); return; }

            if (remainingSeconds > 0)
            {
                remainingSeconds--;
                timerDigitsLabel.Text = FormatTime(remainingSeconds);

                if (totalSeconds > 0)
                    timerProgress = 1f - (float)remainingSeconds / totalSeconds;
                timerRingPanel.Invalidate();

                var shutdownTime = DateTime.Now.AddSeconds(remainingSeconds).ToString("HH:mm:ss");
                string verb = actionMode == "shutdown" ? "kapanacak" : "yeniden baslayacak";
                timerSubLabel.Text = "Saat " + shutdownTime + " - " + verb;

                if (remainingSeconds <= 60) timerDigitsLabel.ForeColor = AccentRed;

                if (remainingSeconds == 60 && !earlyWarningGiven)
                {
                    earlyWarningGiven = true;
                    SystemSounds.Exclamation.Play();
                    if (trayIcon != null && trayIcon.Visible) trayIcon.ShowBalloonTip(5000, "Dikkat!", "Bilgisayar 1 dakika icinde kapanacak/yeniden baslayacak!", ToolTipIcon.Warning);
                }
            }
            else
            {
                countdownTimer.Stop();
                timerRunning = false;
                timerDigitsLabel.Text = "00:00:00";
                timerDigitsLabel.ForeColor = TextPrimary;
                timerSubLabel.Text = "Tamamlandi!";
                timerProgress = 0f;
                timerRingPanel.Invalidate();
                ResetUI();
            }
        }

        private void IdleTick()
        {
            int idleSec = GetSystemIdleSeconds();
            timerDigitsLabel.Text = FormatTime(idleSec);

            if (totalSeconds > 0)
                timerProgress = Math.Min(1f, (float)idleSec / totalSeconds);
            timerRingPanel.Invalidate();

            int remaining = totalSeconds - idleSec;
            if (remaining > 0)
            {
                timerDigitsLabel.ForeColor = AccentOrange;
                string verb = actionMode == "shutdown" ? "kapanacak" : "yeniden baslayacak";
                timerSubLabel.Text = string.Format("Bosta: {0} | {1} sn sonra {2}", FormatTime(idleSec), remaining, verb);
                idleStatusLabel.Text = "Mouse/klavye hareketi sayaci sifirlar";
                idleStatusLabel.ForeColor = AccentGreen;
                if (remaining <= 60) 
                {
                    timerDigitsLabel.ForeColor = AccentRed;
                    if (remaining == 60 && !earlyWarningGiven)
                    {
                        earlyWarningGiven = true;
                        SystemSounds.Exclamation.Play();
                        if (trayIcon != null && trayIcon.Visible) trayIcon.ShowBalloonTip(5000, "Dikkat!", "Hareketsizlik suresi dolmak uzere!", ToolTipIcon.Warning);
                    }
                }
                else earlyWarningGiven = false;
            }
            else
            {
                countdownTimer.Stop();
                timerRunning = false;
                timerDigitsLabel.ForeColor = AccentRed;
                timerSubLabel.Text = "Hareketsizlik suresi doldu!";

                string flag = actionMode == "shutdown" ? "/s" : "/r";
                try
                {
                    Process.Start(new ProcessStartInfo("cmd.exe", "/c shutdown " + flag + " /t 30")
                    { WindowStyle = ProcessWindowStyle.Hidden, CreateNoWindow = true });
                    infoLabel.Text = "PC 30 saniye icinde kapanacak.";
                    infoLabel.Visible = true;
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Komut calistirilamadi: " + ex.Message, "Hata",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                timerProgress = 1f;
                timerRingPanel.Invalidate();
                ResetUI();
            }
        }

        private void StartTrackerMode()
        {
            if (radioProcess.Checked && string.IsNullOrWhiteSpace(inputProcessName.Text))
            {
                MessageBox.Show("Lutfen bir program adi girin (ornek: chrome)", "Uyari", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            int dummyOut1;
            if (radioNetwork.Checked && !int.TryParse(inputNetworkThreshold.Text, out dummyOut1))
            {
                MessageBox.Show("Lutfen gecerli bir KB/s siniri girin.", "Uyari", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string verb = actionMode == "shutdown" ? "kapanacak" : "yeniden baslayacak";
            string msg = "Akilli Izleyici baslatilacak.\nKosul saglandiginda PC 30 saniye icinde " + verb + ". Devam?";
            if (MessageBox.Show(msg, "Onay", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;

            timerRunning = true;
            earlyWarningGiven = false;
            trackerConditionMetCount = 0;
            lastBytesReceived = GetNetworkBytesReceived();
            
            timerDigitsLabel.Text = "IZLENIYOR";
            timerDigitsLabel.ForeColor = AccentBlueLight;
            timerSubLabel.Text = radioProcess.Checked ? "Program kapanmasi bekleniyor" : "Ag kullanimi dusmesi bekleniyor";
            statusLabel.Text = "\u25CF  Aktif";
            statusLabel.ForeColor = AccentBlueLight;
            
            btnStart.Visible = false;
            btnCancel.Visible = true;
            idleStatusLabel.Text = "Arka planda calisiyor...";
            idleStatusLabel.Visible = true;
            
            SetInputsEnabled(false);
            countdownTimer.Start();
        }

        private long GetNetworkBytesReceived()
        {
            if (!NetworkInterface.GetIsNetworkAvailable()) return 0;
            return NetworkInterface.GetAllNetworkInterfaces()
                .Where(n => n.OperationalStatus == OperationalStatus.Up && n.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                .Sum(n => n.GetIPv4Statistics().BytesReceived);
        }

        private void TrackerTick()
        {
            bool conditionMet = false;

            if (radioProcess.Checked)
            {
                string procName = inputProcessName.Text.Trim();
                if (procName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)) 
                    procName = procName.Substring(0, procName.Length - 4);
                
                var procs = Process.GetProcessesByName(procName);
                if (procs.Length == 0) conditionMet = true;
            }
            else if (radioNetwork.Checked)
            {
                long currentBytes = GetNetworkBytesReceived();
                long diff = currentBytes - lastBytesReceived;
                lastBytesReceived = currentBytes;
                
                int threshold;
                if (int.TryParse(inputNetworkThreshold.Text, out threshold))
                {
                    long kbps = diff / 1024;
                    if (kbps < threshold) conditionMet = true;
                }
            }

            if (conditionMet)
            {
                trackerConditionMetCount++;
                timerSubLabel.Text = string.Format("Kosul saglandi ({0}/10 saniye)", trackerConditionMetCount);
                if (trackerConditionMetCount >= 10) 
                {
                    countdownTimer.Stop();
                    ExecuteAction();
                }
            }
            else
            {
                trackerConditionMetCount = 0;
                timerSubLabel.Text = radioProcess.Checked ? "Program acik, izleniyor..." : "Ag kullanimi sinirin ustunde...";
            }
        }

        private void ExecuteAction()
        {
            timerRunning = false;
            timerDigitsLabel.Text = "00:00:00";
            timerDigitsLabel.ForeColor = AccentRed;
            timerSubLabel.Text = "Kosul gerceklesti, islem basladi!";
            
            string flag = actionMode == "shutdown" ? "/s" : "/r";
            try
            {
                Process.Start(new ProcessStartInfo("cmd.exe", "/c shutdown " + flag + " /t 30") { WindowStyle = ProcessWindowStyle.Hidden, CreateNoWindow = true });
                infoLabel.Text = "PC 30 saniye icinde kapanacak.";
                infoLabel.Visible = true;
                SystemSounds.Exclamation.Play();
                if (trayIcon != null && trayIcon.Visible) trayIcon.ShowBalloonTip(5000, "Islem Basladi", "Akilli Izleyici kosulu saglandi. Kapanma baslatildi.", ToolTipIcon.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Komut calistirilamadi: " + ex.Message, "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            ResetUI();
        }

        // =============================================
        //  Mode Switching
        // =============================================
        private void SetTimerMode(string newMode)
        {
            if (timerRunning) return;
            timerMode = newMode;

            btnTimerCountdown.BackColor = timerMode == "countdown" ? AccentBlue : BgCard;
            btnTimerCountdown.ForeColor = timerMode == "countdown" ? Color.White : TextSecondary;
            btnTimerCountdown.UseGradient = timerMode == "countdown";
            btnTimerCountdown.GradientStart = AccentBlue;
            btnTimerCountdown.GradientEnd = AccentBlueDark;
            btnTimerCountdown.Invalidate();

            btnTimerIdle.BackColor = timerMode == "idle" ? AccentOrange : BgCard;
            btnTimerIdle.ForeColor = timerMode == "idle" ? Color.White : TextSecondary;
            btnTimerIdle.UseGradient = timerMode == "idle";
            btnTimerIdle.GradientStart = AccentOrange;
            btnTimerIdle.GradientEnd = AccentOrangeDark;
            btnTimerIdle.Invalidate();
            
            if (btnTimerTracker != null)
            {
                btnTimerTracker.BackColor = timerMode == "tracker" ? AccentBlue : BgCard;
                btnTimerTracker.ForeColor = timerMode == "tracker" ? Color.White : TextSecondary;
                btnTimerTracker.UseGradient = timerMode == "tracker";
                btnTimerTracker.GradientStart = AccentBlue;
                btnTimerTracker.GradientEnd = AccentBlueDark;
                btnTimerTracker.Invalidate();
            }

            idleStatusLabel.Visible = (timerMode == "idle" || timerMode == "tracker");
            
            bool showTimeInputs = (timerMode == "countdown" || timerMode == "idle");
            if (presetLabel != null) presetLabel.Visible = showTimeInputs;
            if (customLabel != null) customLabel.Visible = showTimeInputs;
            if (inputHours != null) {
                inputHours.Visible = showTimeInputs;
                inputMinutes.Visible = showTimeInputs;
                inputSeconds.Visible = showTimeInputs;
                foreach (Control c in Controls) {
                   BufferedPanel bp = c as BufferedPanel;
                   if (bp != null) {
                      foreach(Control bpc in bp.Controls) {
                        Label lbl = bpc as Label;
                        if (lbl != null && (lbl.Text == ":" || lbl.Text == "SAAT" || lbl.Text == "DAKIKA" || lbl.Text == "SANIYE")) lbl.Visible = showTimeInputs;
                      }
                   }
                }
            }
            if (presetButtons != null) {
                foreach (var b in presetButtons) b.Visible = showTimeInputs;
            }
            if (trackerPanel != null) trackerPanel.Visible = (timerMode == "tracker");

            if (timerMode == "tracker")
            {
                timerSubLabel.Text = "Izlenecek kosulu secin";
                idleStatusLabel.Text = "Islem veya Ag hareketi izlenecek";
                idleStatusLabel.ForeColor = AccentBlueLight;
                btnStart.Text = "\u25B6   AKILLI IZLEMEYI BASLAT";
                btnStart.BackColor = AccentBlue;
                btnStart.UseGradient = true;
                btnStart.GradientStart = AccentBlue;
                btnStart.GradientEnd = AccentBlueDark;
                btnStart.Invalidate();
            }
            else if (timerMode == "idle")
            {
                timerSubLabel.Text = "Hareketsizlik suresi belirleyin";
                idleStatusLabel.Text = "Mouse/klavye hareketi izlenecek";
                idleStatusLabel.ForeColor = AccentOrange;
                btnStart.Text = "\u25B6   IZLEMEYI BASLAT";
                btnStart.BackColor = AccentOrange;
                btnStart.UseGradient = true;
                btnStart.GradientStart = AccentOrange;
                btnStart.GradientEnd = AccentOrangeDark;
                btnStart.Invalidate();
            }
            else
            {
                timerSubLabel.Text = "Sure Belirleyin";
                btnStart.Text = "\u25B6   BASLAT";
                btnStart.BackColor = AccentBlue;
                btnStart.UseGradient = true;
                btnStart.GradientStart = AccentBlue;
                btnStart.GradientEnd = AccentBlueDark;
                btnStart.Invalidate();
            }

            timerDigitsLabel.Text = "00:00:00";
            timerDigitsLabel.ForeColor = TextPrimary;
            timerProgress = 0f;
            timerRingPanel.Invalidate();
        }

        private void SetActionMode(string newMode)
        {
            if (timerRunning) return;
            actionMode = newMode;

            btnModeShutdown.BackColor = actionMode == "shutdown" ? AccentBlue : BgCard;
            btnModeShutdown.ForeColor = actionMode == "shutdown" ? Color.White : TextSecondary;
            btnModeShutdown.UseGradient = actionMode == "shutdown";
            btnModeShutdown.GradientStart = AccentBlue;
            btnModeShutdown.GradientEnd = AccentBlueDark;
            btnModeShutdown.Invalidate();

            btnModeRestart.BackColor = actionMode == "restart" ? AccentBlue : BgCard;
            btnModeRestart.ForeColor = actionMode == "restart" ? Color.White : TextSecondary;
            btnModeRestart.UseGradient = actionMode == "restart";
            btnModeRestart.GradientStart = AccentBlue;
            btnModeRestart.GradientEnd = AccentBlueDark;
            btnModeRestart.Invalidate();
        }

        // =============================================
        //  Events
        // =============================================
        private void PresetBtn_Click(object sender, EventArgs e)
        {
            var btn = (RoundedButton)sender;
            int min = (int)btn.Tag;

            if (selectedPreset != null)
            {
                selectedPreset.BackColor = BgCard;
                selectedPreset.ForeColor = TextPrimary;
                selectedPreset.UseGradient = false;
                selectedPreset.Invalidate();
            }

            btn.BackColor = SelectedBg;
            btn.ForeColor = AccentBlueLight;
            btn.Invalidate();
            selectedPreset = btn;

            inputHours.Text = (min / 60).ToString();
            inputMinutes.Text = (min % 60).ToString();
            inputSeconds.Text = "0";
        }

        private void InputChanged(object sender, EventArgs e)
        {
            if (timerRunning) return;
            int sec = GetTotalSeconds();
            timerDigitsLabel.Text = FormatTime(sec);
            if (timerMode == "idle")
                timerSubLabel.Text = sec > 0 ? "Izlemeye hazir" : "Hareketsizlik suresi belirleyin";
            else
                timerSubLabel.Text = sec > 0 ? "Baslatmaya hazir" : "Sure Belirleyin";
        }

        private void BtnStart_Click(object sender, EventArgs e)
        {
            int sec = GetTotalSeconds();
            if (sec < 1)
            {
                MessageBox.Show("Lutfen en az 1 saniyelik bir sure belirleyin.", "Uyari",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (timerMode == "tracker") StartTrackerMode();
            else if (timerMode == "idle") StartIdleMode(sec); 
            else StartCountdownMode(sec);
        }

        private void StartIdleMode(int sec)
        {
            string verb = actionMode == "shutdown" ? "kapanacak" : "yeniden baslayacak";
            string msg = string.Format(
                "Hareketsizlik modu baslatilacak.\n\n{0} boyunca mouse/klavye kullanilmazsa bilgisayar {1}.\nHerhangi bir hareket sayaci sifirlar.\n\nDevam?",
                FormatTime(sec), verb);
            if (MessageBox.Show(msg, "Onay", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;

            totalSeconds = sec;
            timerRunning = true;
            timerProgress = 0f;
            timerDigitsLabel.Text = "00:00:00";
            timerDigitsLabel.ForeColor = AccentOrange;
            timerSubLabel.Text = "Hareketsizlik izleniyor...";
            statusLabel.Text = "\u25CF  Izleniyor";
            statusLabel.ForeColor = AccentOrange;
            btnStart.Visible = false;
            btnCancel.Visible = true;
            infoLabel.Text = string.Format("{0} hareketsizlikte {1}", FormatTime(sec), verb);
            infoLabel.Visible = true;
            idleStatusLabel.Visible = true;
            idleStatusLabel.Text = "Sistem izleniyor...";
            idleStatusLabel.ForeColor = AccentOrange;
            SetInputsEnabled(false);
            countdownTimer.Start();
        }

        private void StartCountdownMode(int sec)
        {
            string verb = actionMode == "shutdown" ? "kapanacak" : "yeniden baslayacak";
            var shutdownTime = DateTime.Now.AddSeconds(sec).ToString("HH:mm:ss");
            string msg = string.Format("Bilgisayar {0} saniye sonra (saat {1}) {2}.\n\nDevam?", sec, shutdownTime, verb);
            if (MessageBox.Show(msg, "Onay", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;

            string flag = actionMode == "shutdown" ? "/s" : "/r";
            try
            {
                Process.Start(new ProcessStartInfo("cmd.exe", "/c shutdown " + flag + " /t " + sec)
                { WindowStyle = ProcessWindowStyle.Hidden, CreateNoWindow = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show("Komut calistirilamadi: " + ex.Message, "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            totalSeconds = sec;
            remainingSeconds = sec;
            timerRunning = true;
            timerProgress = 0f;
            timerDigitsLabel.Text = FormatTime(sec);
            timerDigitsLabel.ForeColor = TextPrimary;
            timerSubLabel.Text = "Geri sayim basladi...";
            statusLabel.Text = "\u25CF  Aktif";
            statusLabel.ForeColor = AccentRed;
            btnStart.Visible = false;
            btnCancel.Visible = true;
            infoLabel.Text = "Kapanma zamani: " + shutdownTime;
            infoLabel.Visible = true;
            SetInputsEnabled(false);
            countdownTimer.Start();
        }

        private void BtnCancel_Click(object sender, EventArgs e)
        {
            string cancelMsg = timerMode == "idle"
                ? "Hareketsizlik izlemeyi iptal etmek istiyor musunuz?"
                : "Zamanlayiciyi iptal etmek istiyor musunuz?";
            if (MessageBox.Show(cancelMsg, "Iptal Onayi", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;

            try { Process.Start(new ProcessStartInfo("cmd.exe", "/c shutdown /a") { WindowStyle = ProcessWindowStyle.Hidden, CreateNoWindow = true }); } catch { }

            countdownTimer.Stop();
            timerRunning = false;
            remainingSeconds = 0;
            timerProgress = 0f;
            earlyWarningGiven = false;
            timerDigitsLabel.Text = "00:00:00";
            timerDigitsLabel.ForeColor = TextPrimary;
            timerSubLabel.Text = "Iptal edildi";
            timerRingPanel.Invalidate();
            ResetUI();
            MessageBox.Show("Iptal edildi.", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (timerRunning)
            {
                string msg = timerMode == "idle"
                    ? "Hareketsizlik izleme aktif!\n\nEvet = Iptal et ve kapat\nHayir = Kapat (izleme duracak)\nIptal = Uygulamaya don"
                    : "Zamanlayici aktif!\n\nEvet = Iptal et ve kapat\nHayir = Kapanma devam etsin\nIptal = Uygulamaya don";
                var result = MessageBox.Show(msg, "Cikis Onayi", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Warning);
                if (result == DialogResult.Yes)
                {
                    try { Process.Start(new ProcessStartInfo("cmd.exe", "/c shutdown /a") { WindowStyle = ProcessWindowStyle.Hidden, CreateNoWindow = true }); } catch { }
                    countdownTimer.Stop();
                }
                else if (result == DialogResult.Cancel) { e.Cancel = true; return; }
            }
            if (countdownTimer != null) countdownTimer.Stop();
            if (countdownTimer != null) countdownTimer.Dispose();
            base.OnFormClosing(e);
        }

        // =============================================
        //  Helpers
        // =============================================
        private int GetTotalSeconds()
        {
            return ParseInput(inputHours) * 3600 + ParseInput(inputMinutes) * 60 + ParseInput(inputSeconds);
        }

        private int ParseInput(TextBox tb)
        {
            int val;
            return (tb != null && int.TryParse(tb.Text, out val)) ? val : 0;
        }

        private string FormatTime(int totalSec)
        {
            int h = totalSec / 3600, m = (totalSec % 3600) / 60, s = totalSec % 60;
            return string.Format("{0:D2}:{1:D2}:{2:D2}", h, m, s);
        }

        private void ResetUI()
        {
            statusLabel.Text = "\u25CF  Hazir";
            statusLabel.ForeColor = AccentGreen;
            btnStart.Visible = true;
            btnCancel.Visible = false;
            infoLabel.Visible = false;

            if (timerMode == "idle")
            {
                idleStatusLabel.Text = "Mouse/klavye hareketi izlenecek";
                idleStatusLabel.ForeColor = AccentOrange;
                btnStart.Text = "\u25B6   IZLEMEYI BASLAT";
                btnStart.BackColor = AccentOrange;
                btnStart.UseGradient = true;
                btnStart.GradientStart = AccentOrange;
                btnStart.GradientEnd = AccentOrangeDark;
            }
            else if (timerMode == "tracker")
            {
                idleStatusLabel.Text = "Islem veya Ag hareketi izlenecek";
                idleStatusLabel.ForeColor = AccentBlueLight;
                btnStart.Text = "\u25B6   AKILLI IZLEMEYI BASLAT";
                btnStart.BackColor = AccentBlue;
                btnStart.UseGradient = true;
                btnStart.GradientStart = AccentBlue;
                btnStart.GradientEnd = AccentBlueDark;
            }
            else
            {
                btnStart.Text = "\u25B6   BASLAT";
                btnStart.BackColor = AccentBlue;
                btnStart.UseGradient = true;
                btnStart.GradientStart = AccentBlue;
                btnStart.GradientEnd = AccentBlueDark;
            }
            btnStart.Invalidate();
            SetInputsEnabled(true);
        }

        private void SetInputsEnabled(bool enabled)
        {
            inputHours.Enabled = enabled;
            inputMinutes.Enabled = enabled;
            inputSeconds.Enabled = enabled;
            btnModeShutdown.Enabled = enabled;
            btnModeRestart.Enabled = enabled;
            btnTimerCountdown.Enabled = enabled;
            btnTimerIdle.Enabled = enabled;
            if (btnTimerTracker != null) btnTimerTracker.Enabled = enabled;
            if (inputProcessName != null) inputProcessName.Enabled = enabled;
            if (inputNetworkThreshold != null) inputNetworkThreshold.Enabled = enabled;
            if (radioProcess != null) radioProcess.Enabled = enabled;
            if (radioNetwork != null) radioNetwork.Enabled = enabled;
            foreach (var b in presetButtons) { b.Enabled = enabled; b.Invalidate(); }
            btnModeShutdown.Invalidate();
            btnModeRestart.Invalidate();
            btnTimerCountdown.Invalidate();
            btnTimerIdle.Invalidate();
            if (btnTimerTracker != null) btnTimerTracker.Invalidate();
        }
    }
}
