using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace swtab
{
    public partial class Prompt : Form
    {
        public Prompt()
        {
            InitializeComponent();
        }

        public new static string Show(string name)
        {
            var prompt = new Prompt();
            prompt.textBox1.Text = name;
            return prompt.ShowDialog() == DialogResult.OK ? prompt.textBox1.Text : null;
        }
    }
}
