using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using OpenTK.Graphics.OpenGL4;
using OpenTK;
using OpenTK.Graphics;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Xml.Linq;
using static System.Net.Mime.MediaTypeNames;

namespace StringArtGPU
{
    public partial class Form1 : Form
    {
        GLControl glControl;

        GPUImage TargetPic;
        GPUImage ReproducedPic;
        GPUImage BlurReproducedPic;
        GPUImage DiffPic;

        Shader CreateDiff;
        Shader Reduce;
        Shader AddLine;
        Shader SortBestConnection;

        List<Vector2> nodes = new List<Vector2>();
        int node_ssbo;
        int sortbuffer_ssbo;

        int nodecount;

        Timer timer = new Timer();
        int currentlinecount = 0;
        int targetlinecount = 8000;
        int lastnode = 0;

        int lastnodebuffersize = 20;
        int[] lastnodebuffer;

        Random rng = new Random();
        public Form1()
        {
            lastnodebuffer = new int[lastnodebuffersize];

            InitializeComponent();

            DoubleBuffered = true;
            pictureBox.SizeMode = PictureBoxSizeMode.Zoom;


        }

        public void Prepare()
        {

            string path = "C:\\Users\\felix\\Pictures\\test.jpg";
            TargetPic = new GPUImage(path);


            nodecount = 200;

            // Generate node positions
            float ds = 2 * (float)(TargetPic.width + TargetPic.height) / (float)(nodecount - 1);

            for (float y = 0; y < TargetPic.height; y += ds)
            {
                nodes.Add(new Vector2(0, y));
            }
            for (float x = 0; x < TargetPic.width; x += ds)
            {
                nodes.Add(new Vector2(x, TargetPic.height));
            }

            for (float y = TargetPic.height; y > 0; y -= ds)
            {
                nodes.Add(new Vector2(TargetPic.width, y));
            }
            for (float x = TargetPic.width; x > 0; x -= ds)
            {
                nodes.Add(new Vector2(x, 0));
            }

            nodecount = nodes.Count;


            node_ssbo = GL.GenBuffer();

            GL.BindBuffer(BufferTarget.ShaderStorageBuffer, node_ssbo);
            GL.BufferData(BufferTarget.ShaderStorageBuffer, 2*nodes.Count * sizeof(float), nodes.ToArray(), BufferUsageHint.StaticDraw);

            sortbuffer_ssbo = GL.GenBuffer();

            GL.BindBuffer(BufferTarget.ShaderStorageBuffer, sortbuffer_ssbo);
            GL.BufferData(BufferTarget.ShaderStorageBuffer, (nodecount - 1) * sizeof(int), new int[nodecount -1], BufferUsageHint.StaticDraw);


            ReproducedPic = new GPUImage(TargetPic.width, TargetPic.height, Color.White);
            BlurReproducedPic = new GPUImage(TargetPic.width, TargetPic.height, Color.White);
            DiffPic = new GPUImage(TargetPic.width, TargetPic.height * (nodecount-1), Color.White);

            CreateDiff = new Shader("Shaders\\create_diff.glsl");
            Reduce = new Shader("Shaders\\reduce.glsl");
            AddLine = new Shader("Shaders\\add_line.glsl");
            SortBestConnection = new Shader("Shaders\\sort_best_connection.glsl");


            timer.Interval = 50; // milliseconds (adjust as needed)
            timer.Tick += Timer_Tick;
            timer.Start();

        }

        public void Timer_Tick(object sender, EventArgs e)
        {
            timer.Stop();
            if (currentlinecount >= targetlinecount)
            {
                MessageBox.Show("Done");
                return;
            }


            DoBestConnection();
            timer.Start();

            pictureBox.Image = ReproducedPic.GetBitmap();

        }

        public void CreateDifferenceImages(int startnode)
        {
            CreateDiff.Use();

            CreateDiff.SetInt("width", TargetPic.width);
            CreateDiff.SetInt("height", TargetPic.height);
            CreateDiff.SetInt("imagecount", nodecount - 1);

            CreateDiff.SetInt("nodeindex", startnode);

            GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, 0, TargetPic.ssbo);
            GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, 1, BlurReproducedPic.ssbo);
            GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, 2, DiffPic.ssbo);
            GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, 3, node_ssbo);
            GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, 4, sortbuffer_ssbo);


            GL.DispatchCompute((TargetPic.width + 7) / 8, (TargetPic.height + 7) / 8, (nodecount - 1 + 7) / 8);
            GL.MemoryBarrier(MemoryBarrierFlags.ShaderStorageBarrierBit);
        }

        public int GetBestConnection()
        {
            Reduce.Use();

            Reduce.SetInt("datasize", TargetPic.width * TargetPic.height);
            Reduce.SetInt("setcount", nodecount - 1);

            GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, 0, DiffPic.ssbo);

            int blocksize = (int)Math.Pow(2, Math.Ceiling(Math.Log(TargetPic.width * TargetPic.height, 2) - 1));
            for (int b = blocksize; b >= 1; b /= 2)
            {
                Reduce.SetInt("blocksize", b);

                GL.DispatchCompute((b + 63) / 64, (nodecount - 1 + 7) / 8, 1);
                GL.MemoryBarrier(MemoryBarrierFlags.ShaderStorageBarrierBit);
            }


            SortBestConnection.Use();

            SortBestConnection.SetInt("datasize", TargetPic.width * TargetPic.height);
            SortBestConnection.SetInt("setcount", nodecount - 1);

            GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, 0, DiffPic.ssbo);
            GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, 1, sortbuffer_ssbo);

            blocksize = (int)Math.Pow(2, Math.Ceiling(Math.Log(nodecount - 1, 2) - 1));
            for (int b = blocksize; b >= 1; b /= 2)
            {
                SortBestConnection.SetInt("blocksize", b);

                GL.DispatchCompute((b + 127) / 128, 1, 1);
                GL.MemoryBarrier(MemoryBarrierFlags.ShaderStorageBarrierBit);
            }
            if (currentlinecount % 10 == 0) {
                GL.BindBuffer(BufferTarget.ShaderStorageBuffer, DiffPic.ssbo);
                IntPtr ptrf = GL.MapBuffer(BufferTarget.ShaderStorageBuffer, BufferAccess.ReadOnly);
                float[] dataf = new float[1];
                Marshal.Copy(ptrf, dataf, 0, 1);
                GL.UnmapBuffer(BufferTarget.ShaderStorageBuffer);

                textBox1.AppendText(dataf[0].ToString() + "\r\n");
            }


            GL.BindBuffer(BufferTarget.ShaderStorageBuffer, sortbuffer_ssbo);
            IntPtr ptr = GL.MapBuffer(BufferTarget.ShaderStorageBuffer, BufferAccess.ReadOnly);
            int[] data = new int[9];
            Marshal.Copy(ptr, data, 0, 9);
            GL.UnmapBuffer(BufferTarget.ShaderStorageBuffer);
            //MessageBox.Show(String.Join(", ", data));
            return data[0];

        }

        public void DoBestConnection()
        {
            currentlinecount++;


            CreateDifferenceImages(lastnode);
            
            int best = GetBestConnection();

            while(lastnodebuffer.Contains(best)) {
                best = rng.Next(0,nodecount);
            }


            AddLine.Use();

            AddLine.SetInt("width", TargetPic.width);
            AddLine.SetInt("height", TargetPic.height);

            GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, 0, ReproducedPic.ssbo);
            GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, 1, BlurReproducedPic.ssbo);

            AddLine.SetVec2("node1", nodes[lastnode]);
            AddLine.SetVec2("node2", nodes[best]);

            GL.DispatchCompute((TargetPic.width + 31) / 32, (TargetPic.height + 31) / 32, 1);
            GL.MemoryBarrier(MemoryBarrierFlags.ShaderStorageBarrierBit);


            lastnode = best;
            lastnodebuffer[currentlinecount % lastnodebuffersize] = best;



        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            glControl = new GLControl();
            glControlPanel.Controls.Add(glControl);
            glControl.Parent = glControlPanel;
            glControl.Dock = DockStyle.Fill;
            glControl.Resize += glControl_Resize;
            GL.Viewport(0, 0, 0, 0);

            GL.ClearColor(0, 0, 0, 1.0f);

            Prepare();
        }
        private void glControl_Resize(object sender, EventArgs e)
        {

            glControl.MakeCurrent();    // Tell OpenGL to use MyGLControl.

            GL.Viewport(0, 0, 0, 0);
        }
    }
}
