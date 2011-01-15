#if !_WIN32
	#define MONO
#endif

#region --- Using directives ---

using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using System.Threading;

using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using OpenTK.Platform;
using OpenTK.Input;
using System.Drawing;
using System.Linq;
using Ninject;
using Ninject.Modules;

using JollyBit.BS.Rendering;
using JollyBit.BS.World;
using JollyBit.BS.Utility;

#endregion

namespace JollyBit.BS
{
	public class BSClient : GameWindow
    {
		private IList<IRenderable> _renderList = new List<IRenderable>();
		private Camera _camera = new Camera();
        
		
		public IList<IRenderable> RenderList {
			get { return _renderList; }
		}
		public Camera Camera {
			get { return _camera; }
		}

        public BSClient() : base(800, 600) { }


        GLTextureObject tex;
		protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            //Setup Ninject
            IKernel kenel = new StandardKernel();
            kenel.Load(new INinjectModule[] { new BSCoreNinjectModule(), new BSClientNinjectModule() });

			// Handle mouse and keyboard events
			new Input(this);
				
            // Set OpenGL options
			GL.ClearColor(System.Drawing.Color.Black);
            GL.Enable(EnableCap.DepthTest);
            GL.Enable(EnableCap.CullFace);
            GL.Enable(EnableCap.Texture2D);
            GL.CullFace(CullFaceMode.Back);

            _camera.Position = new Vector3(0, 0, 5);

            using (System.IO.Stream stream = kenel.Get<IFileSystem>().OpenFile("smile.jpg"))
            {
                tex = new GLTextureObject(new Bitmap(stream));
            }

            // Create World Renderer
            //MapRenderer mapRenderer = kenel.Get<MapRenderer>();
            //_renderList.Add(mapRenderer);
            //_camera.Position = new Vector3(0, 0, 60);
            //IChunk c = mapRenderer.Map[new Utility.Point3L(0, 0, 0)];
            //c = mapRenderer.Map[new Utility.Point3L(-1, 0, 0)];
            //c = mapRenderer.Map[new Utility.Point3L(0, -1, 0)];
            //c = mapRenderer.Map[new Utility.Point3L(0, 0, -1)];
			
			// Build trident and add to the render list
			_renderList.Add( new Trident() );
        }    

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
			
            GL.Viewport(0, 0, Width, Height);

            float aspect_ratio = Width / (float)Height;
            Matrix4 perpective = Matrix4.CreatePerspectiveFieldOfView(MathHelper.PiOver4, aspect_ratio, 1, 64);
            GL.MatrixMode(MatrixMode.Projection);
            GL.LoadMatrix(ref perpective);
        }

		private double _fps = 0; // render frequency adder
		private int _fpsCount = 0; // Frames since last FPS update
		private readonly int _maxFpsCount = 60; // Max number of frames between FPS updates
        protected override void OnRenderFrame(FrameEventArgs e)
        {
            base.OnRenderFrame(e);

			// Put FPS in title bar
			_fps += this.RenderFrequency;
			_fpsCount += 1;
			if(_fpsCount == _maxFpsCount) {
				this.Title = string.Format("BlockSpaces - {0:0d}FPS",_fps/_fpsCount);
				_fps = 0;
				_fpsCount = 0;
			}

			// Remove previous rendering
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

			// Render the camera
            _camera.Render();

            //Render a texture
            tex.Render();
            GL.Color3(Color.White);
            GL.Begin(BeginMode.Quads);
            //GL.Color3(Color.Pink); 
            GL.Vertex2(-0.6f, 0.4f); GL.TexCoord2(0.0f, 0.0f);
            //GL.Color3(Color.Blue);
            GL.Vertex2(0.6f, 0.4f); GL.TexCoord2(1.0f, 0.0f);
            //GL.Color3(Color.Green);
            GL.Vertex2(0.6f, -0.4f); GL.TexCoord2(1.0f, 1.0f);
            //GL.Color3(Color.Red);
            GL.Vertex2(-0.6f, -0.4f); GL.TexCoord2(0.0f, 1.0f);

            GL.End();

			// Render each item in the render list
			foreach(var renderable in _renderList) {
				renderable.Render();
			}

           
            SwapBuffers();
        }

        /// <summary>
        /// Entry point of this example.
        /// </summary>
        [STAThread]
        public static void Main()
        {
            using (BSClient example = new BSClient())
            {
                example.Run(30.0, 0.0);
            }
        }
    }
}