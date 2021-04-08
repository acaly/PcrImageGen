using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Media;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PcrImageGen
{
    public partial class MainWindow : Form
    {
        private class CharacterInfo
        {
            public int Id;
            public string Name;
            public int Range;
            public string Group;
            public string Alias;
        }

        private readonly List<(string name, CharacterInfo[] list)> _characterGroups = new();
        private readonly Dictionary<int, Bitmap> _icons = new();
        private readonly Dictionary<int, CharacterInfo> _characters = new();
        private readonly Dictionary<int, CharacterSelector> _selectors = new();
        private readonly List<int> _selectedSelectors = new();
        private readonly MemoryStream _serializeStream = new();
        private readonly StringBuilder _sb = new();
        private Bitmap _bitmap;
        private Graphics _g;
        private float _resolution = 96;
        private int _size = 50;

        private void LoadCharacterInfo()
        {
            static string BreakName(string name)
            {
                var index = name.IndexOf('（');
                if (index == -1)
                {
                    return name;
                }
                return name[..index] + "\n" + name[index..];
            }
            try
            {
                var list = new List<CharacterInfo>();
                var reader = new StreamReader("CharacterInfo.csv");
                reader.ReadLine();
                while (!reader.EndOfStream)
                {
                    var str = reader.ReadLine();
                    if (string.IsNullOrWhiteSpace(str)) continue;
                    var fields = str.Split(',');
                    var id = int.Parse(fields[0]);
                    var c = new CharacterInfo
                    {
                        Id = id,
                        Name = BreakName(fields[1]),
                        Range = int.Parse(fields[2]),
                        Group = fields[3],
                        Alias = fields[4],
                    };
                    _characters.Add(id, c);
                    list.Add(c);
                    var bitmap = new Bitmap($"CharacterIcons/{id}31.jpg");
                    _icons.Add(id, bitmap);
                }
                _characterGroups.AddRange(list
                    .GroupBy(c => c.Group)
                    .Select(g => (g.Key, g.OrderBy(c => c.Range).ToArray())));
            }
            catch
            {
            }
        }

        public MainWindow()
        {
            LoadCharacterInfo();
            InitializeComponent();
            InitializeCharacterSelectors();
            _bitmap = new Bitmap(_size * 5, _size);
            _bitmap.SetResolution(_resolution, _resolution);
            _g = Graphics.FromImage(_bitmap);
            _g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
            destImage.Image = _bitmap;
        }

        private void CopyHtml(string background, string border, string img, IEnumerable<string> names)
        {
            const string strStart = "<html><body><!--StartFragment -->";
            const string strEnd = "<!--EndFragment --></body></html>";
            const string strHead = "Version:1.0\r\nStartHTML:aaaaaaaaaa\r\nEndHTML:bbbbbbbbbb\r\n" +
                "StartFragment:cccccccccc\r\nEndFragment:dddddddddd\r\n";

            string style = $"background-color:#{background};font-size:10pt;border:.5pt solid #{border};" +
                "vertical-align:bottom;text-align:center;mso-number-format: '@'";

            _sb.Clear();
            _sb.Append("<table><tr>");

            bool isFirstCell = true;
            foreach (var n in names)
            {
                _sb.Append(@"<td style=""");
                _sb.Append(style);
                _sb.Append(@""">");
                _sb.Append(n);
                if (isFirstCell)
                {
                    isFirstCell = false;
                    _sb.Append(@"<img src=""data:image/png;base64,");
                    _sb.Append(img);
                    _sb.Append(@""" type=""tencent-sheet-float-img"" width=250 height=50");
                    _sb.Append(@"style=""width:250px;height:50px;margin:0"" data-clip =""0px 0px 0px 0px""/>");
                }
                _sb.Append("</td>");
            }

            _sb.Append("</tr></table>");
            string sHtmlFragment = _sb.ToString();

            var l1 = strHead.Length;
            var l2 = l1 + strStart.Length;
            var l3 = l2 + sHtmlFragment.Length;
            var l4 = l3 + strEnd.Length;

            var replacedDesc = strHead
                .Replace("aaaaaaaaaa", l1.ToString("0000000000"))
                .Replace("bbbbbbbbbb", l4.ToString("0000000000"))
                .Replace("cccccccccc", l2.ToString("0000000000"))
                .Replace("dddddddddd", l3.ToString("0000000000"));
            string sData = replacedDesc + strStart + sHtmlFragment + strEnd;
            Clipboard.SetDataObject(new DataObject(DataFormats.Html, sData), true);
        }

        private bool SetSelectorState(int id, bool newState)
        {
            if (newState)
            {
                if (_selectedSelectors.Count >= 5)
                {
                    return false;
                }
                _selectedSelectors.Add(id);
                RefreshBitmap();
                return true;
            }
            else
            {
                _selectedSelectors.Remove(id);
                RefreshBitmap();
                return true;
            }
        }

        private void RefreshBitmap()
        {
            _selectedSelectors.Sort((a, b) => _characters[b].Range - _characters[a].Range);

            _g.Clear(Color.Transparent);
            for (int i = 0; i < _selectedSelectors.Count; ++i)
            {
                var bitmap = _icons[_selectedSelectors[i]];
                _g.DrawImage(bitmap, _size * i, 0, _size, _size);
            }
            _g.Flush();
            destImage.Image = _bitmap;
        }

        private void InitializeCharacterSelectors()
        {
            selectorContainer.SuspendLayout();
            selectorContainer.RowCount = _characterGroups.Count * 2;
            while (selectorContainer.RowStyles.Count < selectorContainer.RowCount)
            {
                selectorContainer.RowStyles.Add(new());
            }
            for (int i = 0; i < _characterGroups.Count; ++i)
            {
                var (name, list) = _characterGroups[i];
                selectorContainer.RowStyles[i * 2].SizeType = SizeType.AutoSize;
                selectorContainer.RowStyles[i * 2 + 1].SizeType = SizeType.AutoSize;
                var groupName = new GroupName() { DisplayName = name };
                selectorContainer.Controls.Add(groupName, i * 2, 0);

                var flow = new FlowLayoutPanel();
                flow.SuspendLayout();
                flow.AutoSize = true;
                for (int j = 0; j < list.Length; ++j)
                {
                    var c = list[j];
                    var selector = new CharacterSelector(c.Id, SetSelectorState)
                    {
                        Icon = _icons[c.Id],
                        DisplayName = c.Name,
                    };
                    flow.Controls.Add(selector);
                    _selectors.Add(c.Id, selector);
                }
                flow.Dock = DockStyle.Fill;
                flow.Margin = new Padding(0);
                flow.Size = new Size(0, 0);
                flow.TabIndex = 1;
                flow.ResumeLayout(false);
                selectorContainer.Controls.Add(flow, i * 2 + 1, 0);
            }
            selectorContainer.ResumeLayout(true);
        }

        private void clearButton_Click(object sender, EventArgs e)
        {
            foreach (var s in _selectedSelectors)
            {
                _selectors[s].IsSelected = false;
            }
            _selectedSelectors.Clear();
            RefreshBitmap();
        }

        private void textBox2_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                if (!float.TryParse(textBox2.Text, out var r) || r < 10 || r > 300)
                {
                    textBox2.Text = _resolution.ToString();
                    SystemSounds.Beep.Play();
                }
                else
                {
                    _resolution = r;
                    _bitmap.SetResolution(r, r);
                    RefreshBitmap();
                }
            }
            else if (e.KeyCode == Keys.Escape)
            {
                textBox2.Text = _resolution.ToString();
            }
        }

        private void textBox1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                if (!int.TryParse(textBox1.Text, out var r) || r < 10 || r > 300)
                {
                    textBox1.Text = _size.ToString();
                    SystemSounds.Beep.Play();
                }
                else
                {
                    _size = r;
                    destImage.Image = null;
                    _g.Dispose();
                    _bitmap.Dispose();
                    _bitmap = new Bitmap(r * 5, r);
                    _bitmap.SetResolution(_resolution, _resolution);
                    _g = Graphics.FromImage(_bitmap);
                    _g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                    RefreshBitmap();
                }
            }
            else if (e.KeyCode == Keys.Escape)
            {
                textBox1.Text = _size.ToString();
            }
        }

        private void copyButton_Click(object sender, EventArgs e)
        {
            _serializeStream.SetLength(0);
            _bitmap.Save(_serializeStream, System.Drawing.Imaging.ImageFormat.Png);

            CopyHtml(textBox3.Text, textBox4.Text, Convert.ToBase64String(_serializeStream.ToArray()),
                _selectedSelectors.Select(s => _characters[s].Alias));
        }

        private void copyAliasButton_Click(object sender, EventArgs e)
        {
            _sb.Clear();
            for (int i = 0; i < _selectedSelectors.Count; ++i)
            {
                var name = _characters[_selectedSelectors[i]].Alias;
                if (i != 0)
                {
                    _sb.Append('\t');
                }
                _sb.Append(name);
            }
            Clipboard.SetText(_sb.ToString());
        }

        private void copyImageButton_Click(object sender, EventArgs e)
        {
            Clipboard.SetImage(_bitmap);
        }
    }
}
