using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PcrImageGen
{
    public partial class GroupName : UserControl
    {
        public GroupName()
        {
            InitializeComponent();
        }

        public string DisplayName
        {
            get => label2.Text;
            set => label2.Text = value;
        }
    }
}
