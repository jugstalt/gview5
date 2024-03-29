﻿using System;
using System.Windows.Forms;

namespace gView.Plugins.Network
{
    public partial class FormMaxDistance : Form
    {
        public FormMaxDistance()
        {
            InitializeComponent();
        }

        public double MaxDistance
        {
            get
            {
                if (btnUseMaxDistance.Checked)
                {
                    return Convert.ToDouble(numMaxDistance.Value);
                }
                else
                {
                    return double.MaxValue;
                }
            }
        }

        private void btnInfinite_CheckedChanged(object sender, EventArgs e)
        {
            numMaxDistance.Enabled = !btnInfinite.Checked;
        }
    }
}
