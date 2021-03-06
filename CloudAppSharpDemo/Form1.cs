﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Net;
using System.Windows.Forms;
using CloudAppSharp;
using CloudAppSharp.Auth;

namespace CloudAppSharpDemo
{
    public partial class Form1 : Form
    {
        private bool cloudAppLogged = false;
        private CloudApp cloudApp;
        private Dictionary<string, CloudAppItem> uploadsNameAssoc = new Dictionary<string,CloudAppItem>();

        public Form1()
        {
            Font = SystemFonts.MessageBoxFont;
            AutoScaleMode = AutoScaleMode.Font;
            InitializeComponent();
            labelDetailsName.Location = new Point(28, 21);
        }

        private void buttonLogin_Click(object sender, EventArgs e)
        {
            if (!cloudAppLogged)
            {
                cloudApp = new CloudApp(textBoxEmail.Text, textBoxPassword.Text);
                cloudAppLogged = true;
                textBoxEmail.Enabled = false;
                textBoxPassword.Enabled = false;
                groupBoxAddBookmark.Enabled = true;
                groupBoxUploadFile.Enabled = true;
                groupBoxUploads.Enabled = true;
                buttonLogin.Text = "Logout";
                DigestCredentials digestCredentials = cloudApp.GetCredentials();
                // Just an example of how to get HA1.
                MessageBox.Show(string.Format("Now logged in as {0} with login hash of {1}.", digestCredentials.Username,
                                              digestCredentials.Ha1));
            }
            else
            {
                cloudApp = null;
                cloudAppLogged = false;
                textBoxEmail.Enabled = true;
                textBoxPassword.Enabled = true;
                groupBoxAddBookmark.Enabled = false;
                groupBoxUploadFile.Enabled = false;
                groupBoxUploads.Enabled = false;
                buttonLogin.Text = "Login";
            }
        }

        private void buttonAddBookmark_Click(object sender, EventArgs e)
        {
            CloudAppItem bookmark = cloudApp.AddBookmark(new Uri(textBoxAddBookmark.Text));
        }

        private void buttonUploadFileBrowse_Click(object sender, EventArgs e)
        {
            OpenFileDialog openDialog = new OpenFileDialog();
            DialogResult openDialogResult = openDialog.ShowDialog();
            if (openDialogResult == DialogResult.OK)
            {
                textBoxUploadFile.Text = openDialog.FileName;
            }
        }

        private void buttonUploadFile_Click(object sender, EventArgs e)
        {
            string filePath = textBoxUploadFile.Text;
            CloudAppItem uploadedItem = null;

            if (File.Exists(filePath))
            {
                try
                {
                    uploadedItem = cloudApp.Upload(textBoxUploadFile.Text);
                }
                catch (WebException ex)
                {
                    MessageBox.Show("The file couldn't be uploaded!\n\n" + ex.Message, "CloudAppSharp Demo", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                finally
                {
                    if (uploadedItem != null)
                    {
                        UpdateDetailsArea(uploadedItem);
                    }
                }
            }
            else
            {
                MessageBox.Show("The file you tried to upload doesn't exist!", "CloudAppSharp Demo", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void buttonUploadsRefresh_Click(object sender, EventArgs e)
        {
            listViewUploads.Items.Clear();
            uploadsNameAssoc.Clear();

            List<CloudAppItem> items = cloudApp.GetItems();

            foreach (CloudAppItem item in items)
            {
                ListViewItem itemListViewItem = listViewUploads.Items.Add(item.Name);
                uploadsNameAssoc.Add(item.Name, item);
                itemListViewItem.SubItems.Add(""); // icon
                itemListViewItem.SubItems.Add(item.ViewCounter.ToString());
                itemListViewItem.SubItems.Add(item.CreatedAt);
                itemListViewItem.SubItems.Add(item.UpdatedAt);
            }
        }

        private void listViewUploads_SelectedIndexChanged(object sender, EventArgs e)
        {
            buttonUploadsDelete.Enabled = true;
            buttonUploadsDetails.Enabled = true;
        }

        private void buttonUploadsDelete_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("Are you sure you want to delete " + uploadsNameAssoc[listViewUploads.FocusedItem.SubItems[0].Text].Name + "?",
                "CloudAppSharp Demo", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == System.Windows.Forms.DialogResult.Yes)
            {
                cloudApp.DeleteItemFromUri(uploadsNameAssoc[listViewUploads.FocusedItem.SubItems[0].Text].Href);
                buttonUploadsRefresh.PerformClick();
            }
        }

        private void buttonUploadsDetails_Click(object sender, EventArgs e)
        {
            UpdateDetailsArea(uploadsNameAssoc[listViewUploads.FocusedItem.SubItems[0].Text]);
        }

        private void buttonDetailsFromUrl_Click(object sender, EventArgs e)
        {
            UpdateDetailsArea(CloudApp.GetItemFromUri(new Uri(textBoxDetailsFromUrl.Text)));
        }

        private void UpdateDetailsArea(CloudAppItem item)
        {
            pictureBoxDetails.Image = UriToBitmap(item.Icon);

            labelDetailsName.Text = String.Format("{0} ({1}, {2} views)", item.Name, item.ItemType, item.ViewCounter);

            textBoxDetails.Text = String.Format("Href: {0}\r\nURL: {1}\r\n{2}\r\nCreated: {3}\r\nUpdated: {4}",
                item.Href,
                item.Url,
                item.ItemType == CloudAppItemType.Bookmark ? "Redirect URL: " + item.RedirectUrl : "Remote URL: " + item.RemoteUrl,
                item.CreatedAt,
                item.UpdatedAt
            );
        }

        /// <summary>
        /// Downloads a given uri and returns it as a bitmap.
        /// Written by Marian, see http://bytes.com/topic/c-sharp/answers/471313-displaying-image-url-c#post1813162
        /// </summary>
        /// <param name="uri">A Uri to retrieve the bitmap from.</param>
        /// <returns>A bitmap from the given uri.</returns>
        private Bitmap UriToBitmap(string uri)
        {
            HttpWebRequest wreq;
            HttpWebResponse wresp;
            Stream mystream;
            Bitmap bmp;

            bmp = null;
            mystream = null;
            wresp = null;
            try
            {
                wreq = (HttpWebRequest)WebRequest.Create(uri);
                wreq.AllowWriteStreamBuffering = true;

                wresp = (HttpWebResponse)wreq.GetResponse();

                if ((mystream = wresp.GetResponseStream()) != null)
                    bmp = new Bitmap(mystream);
            }
            finally
            {
                if (mystream != null)
                    mystream.Close();

                if (wresp != null)
                    wresp.Close();
            }

            return (bmp);
        }
    }
}
