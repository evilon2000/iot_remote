using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace COMDBG.View
{
    public partial class PulseInputForm : Form
    {
        public delegate void TransfDelegate(String value);
        public event TransfDelegate TransfEvent;
        public MainForm fatherForm;
        public PulseInputForm(MainForm mainForm)
        {
            InitializeComponent();
            fatherForm = mainForm;
        }

        private void btnConfirm_Click(object sender, EventArgs e)
        {
            if (!int.TryParse(textBox1.Text, out _)) 
            {
                MessageBox.Show("First input format is wrong");
                textBox1.Focus();
                return;
            };
            if (!int.TryParse(textBox2.Text, out _))
            {
                MessageBox.Show("Second input format is wrong");
                textBox2.Focus();
                return;
            };
            if (!int.TryParse(textBox3.Text, out _))
            {
                MessageBox.Show("Third input format is wrong");
                textBox3.Focus();
                return;
            };
            if (!int.TryParse(textBox4.Text, out _))
            {
                MessageBox.Show("Fourth input format is wrong");
                textBox4.Focus();
                return;
            };
            TransfEvent?.Invoke($"{textBox1.Text}-{textBox2.Text}-{textBox3.Text}-{textBox4.Text}");

            fatherForm.sendbtn_Click(null, null);

            this.Close();
        }
    }
}
