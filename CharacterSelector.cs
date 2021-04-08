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
    public partial class CharacterSelector : UserControl
    {
        private readonly int _id;
        private readonly Func<int, bool, bool> _check;

        public CharacterSelector(int id, Func<int, bool, bool> check)
        {
            _id = id;
            _check = check;
            InitializeComponent();
            label1.Click += CharacterSelector_Click;
            pictureBox1.Click += CharacterSelector_Click;
        }

        private bool _isSelected = false;
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (value != _isSelected)
                {
                    _isSelected = value;
                    label1.BorderStyle = value ? BorderStyle.FixedSingle : BorderStyle.None;
                    label1.Padding = new(value ? 0 : 1);
                }
            }
        }

        private void CharacterSelector_Click(object sender, EventArgs e)
        {
            if (_check(_id, !IsSelected))
            {
                IsSelected = !IsSelected;
            }
        }

        public Image Icon
        {
            get => pictureBox1.Image;
            set => pictureBox1.Image = value;
        }

        public string DisplayName
        {
            get => label2.Text;
            set => label2.Text = value;
        }
    }
}
