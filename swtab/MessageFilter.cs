using System.Windows.Forms;

namespace swtab
{
    public class MessageFilter : IMessageFilter
    {
        readonly int WM_USER = 0x0400;

        private TabControl tabControl;
        public MessageFilter(TabControl tabControl)
        {
            this.tabControl = tabControl;
        }
        public bool PreFilterMessage(ref Message m)
        {
            if (m.Msg == WM_USER + 1)
            {
                for (var i = 0; i < tabControl.TabPages.Count; i++)
                {
                    if (tabControl.TabPages[i].Handle == m.HWnd)
                    {
                        tabControl.TabPages.Remove(tabControl.TabPages[i]);
                        return true;
                    }
                }
            }
            return false;
        }
    }
}
