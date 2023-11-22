using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using System.Xml;

namespace gView.Framework.system.UI
{
    public partial class AboutBox : Form
    {
        private bool _showCredits = true;

        public AboutBox()
            : this(true)
        {

        }
        public AboutBox(bool showCredits)
        {
            _showCredits = showCredits;

            InitializeComponent();

            MakeGUI();
        }

        private void MakeGUI()
        {
            richTextBox1.Text = richTextBox1.Text.Replace("{YEAR}", DateTime.Now.Year.ToString());

            lblVersion.Text = "Version: " + SystemInfo.Version.ToString();
            lblBit.Text = (gView.Framework.system.Wow.Is64BitProcess ? "64" : "32") + " Bit";

            lbPath.Text = "Installation path: " + SystemVariables.ApplicationDirectory;
            MakeCredits();
        }

        private void MakeCredits()
        {
            if (!_showCredits)
            {
                return;
            }

            try
            {
                XmlDocument doc = new XmlDocument();
                doc.Load(SystemVariables.ApplicationDirectory + @"\credits.xml");

                int y = 25;
                foreach (XmlNode credit in doc.SelectNodes("credits/credit"))
                {
                    if (credit.Attributes["text"] != null)
                    {
                        Label lbl = new Label();
                        lbl.Text = credit.Attributes["text"].Value.Replace("\\n", "\n");
                        lbl.Width = gpCredits.Width - 20;
                        lbl.Height = lbl.Font.Height * lbl.Text.Split('\n').Length + 2;
                        lbl.Location = new Point(10, y);
                        gpCredits.Controls.Add(lbl);
                        y += lbl.Height;
                    }
                    if (credit.Attributes["hyperlink"] != null)
                    {
                        LinkLabel llbl = new LinkLabel();
                        llbl.Text = credit.Attributes["hyperlink"].Value;
                        llbl.Width = gpCredits.Width - 20;
                        llbl.Height = llbl.Font.Height;
                        llbl.Location = new Point(10, y);
                        string link = "http://" + llbl.Text;
                        llbl.Links.Add(0, link.Length, link);
                        llbl.LinkClicked += new LinkLabelLinkClickedEventHandler(llbl_LinkClicked);
                        gpCredits.Controls.Add(llbl);
                        y += llbl.Height;
                    }
                    y += 10;
                }
            }
            catch { }
        }

        void llbl_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start(e.Link.LinkData.ToString());
        }

        private void richTextBox1_TextChanged(object sender, EventArgs e)
        {

        }
    }
}