using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Сockroach.Properties;

namespace Сockroach
{
    enum MOVE_DIRECTION { LEFT, UP, RIGHT, DOWN };

    public partial class MainForm : Form
    {
        private int trackQnt = 4;
        private List<RaceTrack> tracks = new List<RaceTrack>();         //Беговые дорожки
        private List<PictureBox> cockroachs = new List<PictureBox>();   //Контролы - тараканы
        private Random rand = new Random();

        private EventWaitHandle runEvent = 
            new EventWaitHandle(false, EventResetMode.ManualReset);     //Событие бежим/стоим (флаг)
        
        private string winner = null;
        private bool isFinish;

        public MainForm()
        {
            InitializeComponent();

            this.Load += Form1_Load;

            this.panel1.Paint += panel1_Paint;
            this.panel1.SizeChanged += panel1_SizeChanged;

            this.timer1.Interval = 50;
            this.timer1.Start();
        }

        #region EVENTS
        
        private void Form1_Load(object sender, EventArgs e)
        {
            //Стадион и тараканы

            this.FillTracks();
            this.AddCockroach();

            //Потоки, семафоры

            Thread[] fwTherads = new Thread[4];
            Thread[] bcTherads = new Thread[4];
            Thread[] rgTherads = new Thread[4];
            Thread[] lfTherads = new Thread[4];
            Semaphore[] sMove = new Semaphore[4];
            
            for (int i = 0; i < 4; i++)
            {
                fwTherads[i] = new Thread(MoveForward);
                fwTherads[i].IsBackground = true;

                bcTherads[i] = new Thread(MoveBack);
                bcTherads[i].IsBackground = true;

                rgTherads[i] = new Thread(MoveRight);
                rgTherads[i].IsBackground = true;

                lfTherads[i] = new Thread(MoveLeft);
                lfTherads[i].IsBackground = true;

                sMove[i] = new Semaphore(2, 2);
            }

            //Запуск

            for (int i = 0; i < 4; i++)
            {
                this.cockroachs[i].Tag = sMove[i];
                fwTherads[i].Start(this.cockroachs[i]);
                bcTherads[i].Start(this.cockroachs[i]);
                rgTherads[i].Start(this.cockroachs[i]);
                lfTherads[i].Start(this.cockroachs[i]);
            }
        }

        private void panel1_SizeChanged(object sender, EventArgs e)
        {
            this.FillTracks();
            this.AddCockroach();
            this.panel1.Invalidate();
        }

        private void panel1_Paint(object sender, PaintEventArgs e)
        {
            Panel panel = sender as Panel;
            if (panel == null) { return; }
            
            Pen pen = new Pen(Color.Black, 2.0f);
            Pen penStrt = new Pen(Color.Blue, 2.0f);
            Pen penFnsh = new Pen(Color.Red, 2.0f);

            using (Graphics g = e.Graphics)
            {
                g.Clear(Color.White);
                foreach (RaceTrack rt in this.tracks)
                {
                    g.DrawLine(pen, rt.LeftCoord, rt.TopCoord, rt.RightCoord, rt.TopCoord);
                    g.DrawLine(pen, rt.LeftCoord, rt.BottomCoord, rt.RightCoord, rt.BottomCoord);
                    g.DrawLine(penStrt, rt.LeftCoord, rt.TopCoord, rt.LeftCoord, rt.BottomCoord);
                    g.DrawLine(penFnsh, rt.RightCoord, rt.TopCoord, rt.RightCoord, rt.BottomCoord);
                }
            }
        }

        private void mnStart_Click(object sender, EventArgs e)
        {
            this.runEvent.Reset();
            for (int pb = 0; pb < this.cockroachs.Count; pb++)
            {
                this.cockroachs[pb].Left = this.tracks[pb].LeftCoord + 1;
                this.cockroachs[pb].Top = (this.tracks[pb].BottomCoord + this.tracks[pb].TopCoord) / 2 - this.cockroachs[pb].Width / 2;
            }
            this.isFinish = false;
        }

        private void mnContinue_Click(object sender, EventArgs e)
        {
            if (!this.isFinish)
            {
                this.runEvent.Set();
            }
        }

        private void mnPause_Click(object sender, EventArgs e)
        {
            if (!this.isFinish)
            {
                this.runEvent.Reset();
            }
        }

        private void mnExit_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            if (this.winner != null)
            {
                string mes = String.Format("Победил: {0}", this.winner);
                this.winner = null;
                MessageBox.Show(mes, "Finish", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        #endregion

        #region MOVE
        
        private void MovePicterBoxByStep(PictureBox pb, MOVE_DIRECTION direction)
        {
            switch (direction)
            {
                case MOVE_DIRECTION.LEFT:
                    pb.Left -= 1;
                    break;
                case MOVE_DIRECTION.UP:
                    pb.Top -= 1;
                    break;
                case MOVE_DIRECTION.RIGHT:
                    pb.Left += 1;
                    break;
                case MOVE_DIRECTION.DOWN:
                    pb.Top += 1;
                    break;
                default:
                    break;
            }
        }
        
        private void MoveForward(object obj)
        {
            PictureBox pb = obj as PictureBox;
            if (pb == null) { return; }

            RaceTrack track = this.GetTrackByControl(pb);
            if (track == null) { return; }

            Semaphore semaphore = pb.Tag as Semaphore;
            if (semaphore == null) { return; }

            while (true)
            {
                semaphore.WaitOne();
                int steps = this.rand.Next(10, 50);
                for (int i = 0; i < steps; i++)
                {
                    this.runEvent.WaitOne();
                    pb.Invoke(new Action<PictureBox, MOVE_DIRECTION>(MovePicterBoxByStep), pb, MOVE_DIRECTION.RIGHT);
                    if (pb.Left + pb.Width >= track.RightCoord - 1)
                    {
                        this.runEvent.Reset();
                        this.isFinish = true;
                        this.winner = pb.Name;
                        break;
                    }
                    Thread.Sleep(10);
                }
                semaphore.Release();
                Thread.Sleep(this.rand.Next(1, 10));
            }
        }

        private void MoveBack(object obj)
        {
            PictureBox pb = obj as PictureBox;
            if (pb == null) { return; }

            RaceTrack track = this.GetTrackByControl(pb);
            if (track == null) { return; }

            Semaphore semaphore = pb.Tag as Semaphore;
            if (semaphore == null) { return; }

            while (true)
            {
                semaphore.WaitOne();
                int steps = this.rand.Next(0, 10);
                for (int i = 0; i < steps; i++)
                {
                    this.runEvent.WaitOne();
                    pb.Invoke(new Action<PictureBox, MOVE_DIRECTION>(MovePicterBoxByStep), pb, MOVE_DIRECTION.LEFT);
                    if (pb.Left <= track.LeftCoord + 1)
                    {
                        break;
                    }
                    Thread.Sleep(30);
                }
                semaphore.Release();
                Thread.Sleep(this.rand.Next(1, 10));
            }
        }

        private void MoveRight(object obj)
        {
            PictureBox pb = obj as PictureBox;
            if (pb == null) { return; }

            RaceTrack track = this.GetTrackByControl(pb);
            if (track == null) { return; }

            Semaphore semaphore = pb.Tag as Semaphore;
            if (semaphore == null) { return; }

            while (true)
            {
                semaphore.WaitOne();
                int steps = this.rand.Next(0, 20);
                for (int i = 0; i < steps; i++)
                {
                    this.runEvent.WaitOne();
                    pb.Invoke(new Action<PictureBox, MOVE_DIRECTION>(MovePicterBoxByStep), pb, MOVE_DIRECTION.DOWN);
                    if (pb.Top + pb.Width >= track.BottomCoord - 1)
                    {
                        break;
                    }
                    Thread.Sleep(20);
                }
                semaphore.Release();
                Thread.Sleep(this.rand.Next(1, 10));
            }
        }

        private void MoveLeft(object obj)
        {
            PictureBox pb = obj as PictureBox;
            if (pb == null) { return; }

            RaceTrack track = this.GetTrackByControl(pb);
            if (track == null) { return; }

            Semaphore semaphore = pb.Tag as Semaphore;
            if (semaphore == null) { return; }

            while (true)
            {
                semaphore.WaitOne();
                int steps = this.rand.Next(0, 20);
                for (int i = 0; i < steps; i++)
                {
                    this.runEvent.WaitOne();
                    pb.Invoke(new Action<PictureBox, MOVE_DIRECTION>(MovePicterBoxByStep), pb, MOVE_DIRECTION.UP);
                    if (pb.Top <= track.TopCoord + 1)
                    {
                        break;
                    }
                    Thread.Sleep(20);
                }
                semaphore.Release();
                Thread.Sleep(this.rand.Next(1, 10));
            }
        }

        #endregion

        #region HELPERS
        
        private void FillTracks()
        {
            int offset = 20;
            int trackWidth = (this.panel1.Height - 2 * offset) / this.trackQnt;
            this.tracks.Clear();
            for (int i = 0; i < this.trackQnt; i++)
            {
                RaceTrack rt = new RaceTrack(trackWidth * i + offset,trackWidth * (i + 1) + offset, offset, this.panel1.Width - offset);
                this.tracks.Add(rt); 
            }
        }

        private void AddCockroach()
        {
            int num = 0;
            this.panel1.Controls.Clear();
            foreach (RaceTrack rt in this.tracks)
            {
                num++;
                PictureBox pb = new PictureBox();
                pb.Name = "Таракан " + num;
                pb.Width = 40;
                pb.Height = 30;
                pb.BackColor = Color.White;
                pb.Left = rt.LeftCoord + 1;
                pb.Top = (rt.BottomCoord + rt.TopCoord) / 2 - pb.Width / 2;

                pb.SizeMode = PictureBoxSizeMode.Zoom;
                
                pb.Image = Resources.cimage_cr;

                this.panel1.Controls.Add(pb);
                this.cockroachs.Add(pb);
            }
        }

        private RaceTrack GetTrackByControl(Control c)
        {
            foreach (RaceTrack rt in this.tracks)
            {
                if (rt.Contains(c)) { return rt; }
            }
            return null;
        }

        #endregion
    }
}