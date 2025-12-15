

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace COMDBG
{
    static class Program
    {
        /// <summary>
        /// Entry point of the program
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            MainForm view = new MainForm();
            view.StartPosition = FormStartPosition.CenterScreen;
            IController controller = new IController(view);
            
            Application.Run(view);
        }
    }
}
