using InfHelper;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;

namespace osu_songsgrabber
{
    public partial class Form1 : Form
    {
        CancellationTokenSource cancelSource = new CancellationTokenSource();
        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            var res = folderBrowserDialog1.ShowDialog();
            if (res == DialogResult.OK)
            {
                textBox1.Text = folderBrowserDialog1.SelectedPath;
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            var res = folderBrowserDialog2.ShowDialog();
            if (res == DialogResult.OK)
            {
                textBox2.Text = folderBrowserDialog2.SelectedPath;
            }
        }



        private void button3_Click(object sender, EventArgs e)
        {
            new Thread(new ThreadStart(() => {
                try
                {
                    CopySongs(cancelSource.Token); //).Start();
                }
                catch (OperationCanceledException)
                {
                    Console.WriteLine("Canceled!");
                }
            })).Start();
        }

        void CopySongs(CancellationToken cancel)
        {
            string sourcePath = textBox1.Text;
            string outputPath = textBox2.Text;
            //check if folders exists
            if (!Directory.Exists(sourcePath))
            {
                MessageBox.Show("Check your osu! installation!", "osu!songsgrabber", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (!Directory.Exists(outputPath))
            {
                MessageBox.Show("Check your output folder!", "osu!songsgrabber", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            string songsPath = Path.Combine(sourcePath, "Songs");

            if (!Directory.Exists(songsPath))
            {
                MessageBox.Show("Check your osu! installation!", "osu!songsgrabber", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            //get total songs
            string[] songsdirs = Directory.GetDirectories(songsPath).ToArray();
            int totalSongs = songsdirs.Length;
            if (label3.InvokeRequired)
            {
                label3.Invoke(new SetStuff(() => { label3.Text = $"0/{totalSongs} songs copied"; }));
            }
            else
            {
                label3.Text = $"0/{totalSongs} songs copied";
            }
            if (progressBar1.InvokeRequired)
            {
                progressBar1.Invoke(new SetStuff(() => { progressBar1.Maximum = totalSongs; }));
            }
            else
            {
                progressBar1.Maximum = totalSongs;
            }

            List<Song> Songs = new List<Song>();

            for (int i = 0; i < totalSongs; i++)
            {
                string songDirName = songsdirs[i].Replace(songsPath, "").Remove(0, 1);

                //if the song folder name starts with a number, it's valid
                if (Regex.IsMatch(songDirName, @"^\d+"))
                {
                    string songName = songDirName.Remove(0, songDirName.IndexOf(" "));
                    //Console.WriteLine(songName);
                    string osuFile = Directory.GetFiles(songsdirs[i], "*.osu").First();
                    string[] lines = File.ReadAllLines(osuFile);

                    Song song = new Song();
                    for(int j = 1; j < lines.Length; j++)
                    {
                        if (lines[j].StartsWith("Artist:"))
                        {
                            song.Artist = lines[j].Replace("Artist:", "");
                        }
                        else if (lines[j].StartsWith("ArtistUnicode:"))
                        {
                            song.ArtistUnicode = lines[j].Replace("ArtistUnicode:", "");
                        }
                        else if (lines[j].StartsWith("Title:"))
                        {
                            song.Title = lines[j].Replace("Title:", "");
                        }
                        else if (lines[j].StartsWith("TitleUnicode:"))
                        {
                            song.TitleUnicode = lines[j].Replace("TitleUnicode:", "");
                        }
                        else if (lines[j].StartsWith("Creator:"))
                        {
                            song.Mapper = lines[j].Replace("Creator:", "");
                        }
                        else if (lines[j].StartsWith("AudioFilename:"))
                        {
                            string file = lines[j].Replace("AudioFilename:", "");
                            if (file.StartsWith(" ")) file = file.Remove(0, 1);
                            song.File = Path.Combine(songsdirs[i], file);
                        }
                    }
                    Songs.Add(song);
                    /*
                    var helper = new InfUtil();
                    var data = helper.Parse(inf);
                    Console.WriteLine(data["Metadata"]["Author"]);*/
                }
            }

            for(int i = 0; i < Songs.Count; i++)
            {
                Song s = Songs.ElementAt(i);
                string file = $"{s.Artist} - {s.Title}.mp3";
                char[] invalid = Path.GetInvalidFileNameChars();
                foreach (char c in invalid)
                {
                    file = file.Replace(c.ToString(), "");
                }
                string newPath = Path.Combine(outputPath, file);
                try
                {
                    File.Copy(s.File, newPath);
                    if (checkBox1.Checked)
                    {
                        var v = TagLib.File.Create(newPath);
                        if (v.Tag.Title == null || v.Tag.Title.Equals(""))
                        {
                            v.Tag.Title = s.TitleUnicode;
                        }
                        if (v.Tag.Subtitle == null || v.Tag.Subtitle.Equals(""))
                        {
                            v.Tag.Subtitle = s.Title;
                        }
                        if (v.Tag.FirstPerformer == null || v.Tag.FirstPerformer.Equals(""))
                        {
                            v.Tag.Performers = new string[] { s.ArtistUnicode, s.Artist };
                        }
                        v.Tag.Publisher = s.Mapper;
                        if (checkBox2.Checked)
                        {
                            try
                            {
                                if (v.Tag.Pictures == null)
                                {
                                    v.Tag.Pictures = new TagLib.IPicture[] { CopyImage(Directory.GetParent(s.File).FullName) };
                                }
                                else if (v.Tag.Pictures.Length < 1)
                                {
                                    v.Tag.Pictures = new TagLib.IPicture[] { CopyImage(Directory.GetParent(s.File).FullName) };
                                }
                                else
                                {
                                    v.Tag.Pictures = new TagLib.IPicture[] { v.Tag.Pictures[0], CopyImage(Directory.GetParent(s.File).FullName) };
                                }
                            }
                            catch
                            {

                            }
                        }
                        v.Save();
                    }
                }
                catch (IOException)
                {

                }

                if (label3.InvokeRequired)
                {
                    label3.Invoke(new SetStuff(() => { label3.Text = $"{i + 1}/{totalSongs} songs copied"; }));
                }
                else
                {
                    label3.Text = $"{i + 1}/{totalSongs} songs copied";
                }
                if (progressBar1.InvokeRequired)
                {
                    progressBar1.Invoke(new SetStuff(() => { progressBar1.Value = i + 1; }));
                }
                else
                {
                    progressBar1.Value = i + 1;
                }
            }

            if (label3.InvokeRequired)
            {
                label3.Invoke(new SetStuff(() => { label3.Text = $"{totalSongs}/{totalSongs} songs copied"; }));
            }
            else
            {
                label3.Text = $"{totalSongs}/{totalSongs} songs copied";
            }
            if (progressBar1.InvokeRequired)
            {
                progressBar1.Invoke(new SetStuff(() => { progressBar1.Value = totalSongs; }));
            }
            else
            {
                progressBar1.Value = totalSongs;
            }
        }


        TagLib.Picture CopyImage(string beatmapPath)
        {
            //get all jpg images
            string[] fs = Directory.GetFiles(beatmapPath, "*.jpg");
            FileInfo[] files = new FileInfo[fs.Length];

            for(int i = 0; i < files.Length; i++)
            {
                files[i] = new FileInfo(fs[i]);
            }
            //find the biggest one (the background)
            FileInfo info = files[0];
            for (int i = 0; i < files.Length; i++)
            {
                if (files[i].Length > info.Length)
                    info = files[i];
            }
            TagLib.Picture pic = new TagLib.Picture(info.FullName);
            return pic;
        }

        delegate void SetStuff();

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            cancelSource.Cancel(false);
        }
    }
}
