﻿using System.Windows;

namespace SDL.TridionVSRazorExtension
{
    public partial class DiffDialogWindow
    {
        public DiffDialogWindow()
        {
            InitializeComponent();
        }

        public string StartItemInfo { private get; set; }
        public string StartItemText { private get; set; }
        public string EndItemInfo { private get; set; }
        public string EndItemText { private get; set; }
        public SyncState SyncState { get; set; }
        public bool Tridion2VSEnabled { private get; set; }
        public bool VS2TridionEnabled { private get; set; }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            this.lblStartItem.Content = StartItemInfo;
            this.lblEndItem.Content = EndItemInfo;

            string html = "<div style='font-size: 11px; font-family: Verdana; white-space:nowrap;'>" + Functions.GetDiffHtml(StartItemText, EndItemText).Replace("&para;", "") + "</div>";

            this.webBrowser1.NavigateToString(html);

            if (this.SyncState == SyncState.Tridion2VS)
            {
                this.btnUseTridion.IsDefault = true;
                this.btnUseTridion.FontWeight = FontWeights.Bold;

                this.btnUseVS.IsDefault = false;
                this.btnUseVS.FontWeight = FontWeights.Normal;
            }
            if (this.SyncState == SyncState.VS2Tridion)
            {
                this.btnUseVS.IsDefault = true;
                this.btnUseVS.FontWeight = FontWeights.Bold;

                this.btnUseTridion.IsDefault = false;
                this.btnUseTridion.FontWeight = FontWeights.Normal;
            }

            this.btnUseTridion.IsEnabled = this.Tridion2VSEnabled;
            this.btnUseVS.IsEnabled = this.VS2TridionEnabled;
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.SyncState = SyncState.None;
            this.Close();
        }

        private void btnUseTridion_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            this.SyncState = SyncState.Tridion2VS;
            this.Close();
        }

        private void btnUseVS_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            this.SyncState = SyncState.VS2Tridion;
            this.Close();
        }
    }
}