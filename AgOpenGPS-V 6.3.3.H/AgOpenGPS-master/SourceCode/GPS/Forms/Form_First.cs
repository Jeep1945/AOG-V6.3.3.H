﻿using System;
using System.Globalization;
using System.Windows.Forms;

namespace AgOpenGPS
{
    public partial class Form_First : Form
    {
        private readonly FormGPS mf = null;

        public Form_First(Form callingForm)
        {
            mf = callingForm as FormGPS;

            InitializeComponent();
        }

        private void linkLabelGit_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            System.Diagnostics.Process.Start(e.Link.LinkData.ToString());
        }

        private void linkLabelCombineForum_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            System.Diagnostics.Process.Start(e.Link.LinkData.ToString());
        }

        private void Form_About_Load(object sender, EventArgs e)
        {
            lblVersion.Text = "Version " + Application.ProductVersion.ToString(CultureInfo.InvariantCulture);

            // Add a link to the LinkLabel.
            LinkLabel.Link link = new LinkLabel.Link { LinkData = "https://github.com/farmerbriantee/AgOpenGPS" };
            linkLabelGit.Links.Add(link);

            // Add a link to the LinkLabel.
            LinkLabel.Link linkCf = new LinkLabel.Link
            {
                LinkData = "https://discourse.agopengps.com/"
            };
            linkLabelCombineForum.Links.Add(linkCf);

            if (!mf.IsOnScreen(Location, Size, 1))
            {
                Top = 0;
                Left = 0;
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            mf.isTermsAccepted = true;
            Close();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            Properties.Settings.Default.setDisplay_isTermsAccepted = false;
            Properties.Settings.Default.Save();
            //Close();
            Environment.Exit(0);
        }

        private void button3_Click(object sender, EventArgs e)
        {
            Properties.Settings.Default.setDisplay_isTermsAccepted = true;
            Properties.Settings.Default.Save();
            mf.isTermsAccepted = true;
        }

        private void lblVersion_Click(object sender, EventArgs e)
        {

        }
    }
}