using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;

using System.Windows.Forms;

namespace swtab
{
    public partial class frmMain : Form
    {
        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wp, IntPtr lp);
        private const int TCM_SETMINTABWIDTH = 0x1300 + 49;
        private Config config;
        public frmMain()
        {
            InitializeComponent();
            Application.AddMessageFilter(new MessageFilter(tabControl1));
            config = new Config(Application.StartupPath + "\\config.ini");
            AppLocale(appLocaleToolStripMenuItem.Checked = config.AppLocale);
            newGame();
        }

        private bool newGame()
        {
            var gamePath = config.GamePath;
            var gameFileName = gamePath + "\\Bin\\sw3main.exe";
            if (!File.Exists(gameFileName))
            {
                return false;
            }
            var tabPage = new TabPage("未登录");
            tabPage.Tag = new GameAgent(gameFileName, gamePath, tabPage.Handle);
            tabControl1.TabPages.Add(tabPage);
            tabControl1.SelectedIndex = tabControl1.TabPages.Count - 1;
            return true;
        }

        private void tabControl1_MouseDown(object sender, MouseEventArgs e)
        {
            for (var i = 0; i < tabControl1.TabPages.Count; i++)
            {
                var tabRect = tabControl1.GetTabRect(i);
                tabRect.Inflate(-2, -2);
                var closeImage = Properties.Resources.DeleteButton_Image;
                var imageRect = new Rectangle(
                    (tabRect.Right - closeImage.Width),
                    tabRect.Top + (tabRect.Height - closeImage.Height) / 2,
                    closeImage.Width,
                    closeImage.Height);
                if (imageRect.Contains(e.Location))
                {
                    var tagPage = tabControl1.TabPages[i];
                    var ga = (GameAgent)tagPage.Tag;
                    ga.close();
                    break;
                }
            }
        }

        private void tabControl1_DrawItem(object sender, DrawItemEventArgs e)
        {
            var tabPage = this.tabControl1.TabPages[e.Index];
            var tabRect = this.tabControl1.GetTabRect(e.Index);
            var closeImage = Properties.Resources.DeleteButton_Image;
            tabRect.Inflate(-2, -2);
            e.Graphics.DrawImage(closeImage,
                (tabRect.Right - closeImage.Width),
                tabRect.Top + (tabRect.Height - closeImage.Height) / 2);
            TextRenderer.DrawText(e.Graphics, tabPage.Text, tabPage.Font,
                tabRect, tabPage.ForeColor, TextFormatFlags.Left);
        }

        private void tabControl1_HandleCreated(object sender, EventArgs e)
        {
            SendMessage(this.tabControl1.Handle, TCM_SETMINTABWIDTH, IntPtr.Zero, (IntPtr)16);
        }

        private void NewToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!newGame())
            {
                MessageBox.Show("找不到游戏主程序, 请配置正确的游戏路径");
            }

        }

        private void tabControl1_Selected(object sender, TabControlEventArgs e)
        {

            ((GameAgent)e.TabPage.Tag).active();
        }

        private void frmMain_FormClosed(object sender, FormClosedEventArgs e)
        {
            for (var i =0; i < tabControl1.TabPages.Count; i++)
            {
                ((GameAgent)tabControl1.TabPages[i].Tag).kill();
            }
        }

        private void setTabPageTitle(TabPage tp)
        {
            var title = ((GameAgent)tp.Tag).id;
            if (title == null)
            {
                title = "未登录";
            }
            title = config.GetName(title);
            if (tp.Text != title)
            {
                tp.Text = title;
            }
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            for (var i = 0; i < tabControl1.TabPages.Count; i++)
            {
                setTabPageTitle(tabControl1.TabPages[i]);
            }
        }

        private void GamePathToolStripMenuItem_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog dialog = new FolderBrowserDialog();
            dialog.Description = "请选择游戏路径";
            DialogResult result = dialog.ShowDialog();
            if (result == DialogResult.Cancel)
            {
                return;
            }
            string gamePath = dialog.SelectedPath.Trim().TrimEnd('\\');
            if (Directory.Exists(gamePath))
            {
                config.GamePath = gamePath;
            }
        }

        private void AppLocale(bool enable)
        {

            Environment.SetEnvironmentVariable("__COMPAT_LAYER", enable ? "#APPLICATIONLOCALE" : null);
            Environment.SetEnvironmentVariable("AppLocaleID", enable ? "0404" : null);
        }

        private void appLocaleToolStripMenuItem_CheckedChanged(object sender, EventArgs e)
        {

            AppLocale(config.AppLocale = ((ToolStripMenuItem)sender).Checked);
        }

        private void tabControl1_DoubleClick(object sender, EventArgs e)
        {
            var tp = tabControl1.SelectedTab;
            var ga = (GameAgent)tp.Tag;
            var id = ga.id;
            if (id == null) return;
            var name = Prompt.Show(config.GetName(id));
            if (name == null) return;
            config.SetName(id, name);
            setTabPageTitle(tp);
        }
    }
}
