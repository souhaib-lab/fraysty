﻿using System;
using System.IO;
using System.Windows.Forms;
using Freeserf.Renderer.OpenTK;
using Orientation = Freeserf.Renderer.OpenTK.Orientation;
using Freeserf.Render;

namespace Freeserf
{
    public partial class FreeserfForm : Form
    {
        GameView gameView = null;
        Game game = null;

        public FreeserfForm()
        {
            InitializeComponent();
        }

        private void FreeserfForm_Load(object sender, EventArgs e)
        {
            Log.SetFile(File.Create(Path.Combine(Program.ExecutablePath, "log.txt")));
            Log.SetLevel(Log.Level.Verbose);

            // TODO: for now we just load DOS data (test path)
            DataSourceDos dosData = new DataSourceDos(Path.Combine(Program.ExecutablePath, "SPAE.PA"));

            if (!dosData.Load())
            {
                MessageBox.Show(this, "Error loading DOS data.", "Error");
                Close();
            }

            gameView = new GameView(new Size(1024, 768), DeviceType.Desktop, SizingPolicy.FitRatio, OrientationPolicy.Fixed);

            gameView.Resize(RenderControl.Width, RenderControl.Height, Orientation.LandscapeLeftRight);

            TextureAtlasManager.RegisterFactory(new TextureAtlasBuilderFactory());
            TextureAtlasManager.Instance.AddAll(dosData);
            
            // TODO: create texture atlas for every layer
            Renderer.OpenTK.Texture textureDummy = null; // dummy

            // TODO: color keys?
            var layerLandscape = new RenderLayer(Layer.Landscape, Shape.Triangle, TextureAtlasManager.Instance.GetOrCreate((int)Layer.Landscape).Texture as Renderer.OpenTK.Texture);
            var layerGrid = new RenderLayer(Layer.Grid, Shape.Triangle, textureDummy);
            var layerPaths = new RenderLayer(Layer.Paths, Shape.Rect, textureDummy);
            var layerSerfsBehind = new RenderLayer(Layer.SerfsBehind, Shape.Rect, textureDummy);
            var layerObjects = new RenderLayer(Layer.Objects, Shape.Rect, textureDummy);
            var layerSerfs = new RenderLayer(Layer.Serfs, Shape.Rect, textureDummy);
            var layerBuilds = new RenderLayer(Layer.Builds, Shape.Rect, textureDummy);
            var layerCursor = new RenderLayer(Layer.Cursor, Shape.Rect, textureDummy);

            gameView.AddLayer(layerLandscape);
            gameView.AddLayer(layerGrid);
            gameView.AddLayer(layerPaths);
            gameView.AddLayer(layerSerfsBehind);
            gameView.AddLayer(layerObjects);
            gameView.AddLayer(layerSerfs);
            gameView.AddLayer(layerBuilds);
            gameView.AddLayer(layerCursor);

            // Example for adding a sprite
            // var serfSprite = new Sprite(32, 34, 0, 0);
            // serfSprite.Visible = true;
            // serfSprite.Layer = layerSerfs;

            var random = new Random();
            var gameInfo = new GameInfo(random);

            if (!GameManager.Instance.StartGame(gameInfo.GetMission(0), gameView))
                throw new ExceptionFreeserf("Failed to start game.");

            game = GameManager.Instance.GetCurrentGame();

            game.Map.AttachToRenderLayer(layerLandscape, dosData);

            FrameTimer.Start();
        }

        private void FrameTimer_Tick(object sender, EventArgs e)
        {
            RenderControl.MakeCurrent();

            gameView.Render();


            RenderControl.SwapBuffers();
        }

        int lastX = int.MinValue;
        int lastY = int.MinValue;

        private void RenderControl_MouseMove(object sender, MouseEventArgs e)
        {
            Position pos = gameView.ScreenToView(new Position(e.X, e.Y));

            if (e.Button == MouseButtons.Right)
            {
                if (pos == null || lastX == int.MinValue)
                    return;

                int diffX = pos.X - lastX;
                int diffY = pos.Y - lastY;
                int scrollX = diffX / 32;
                int scrollY = diffY / 20;

                game.Map.Scroll(-scrollX, -scrollY);

                int remainingX = diffX % 32;
                int remainingY = diffY % 20;

                lastX = pos.X - remainingX;
                lastY = pos.Y - remainingY;
            }
            else
            {
                lastX = int.MinValue;
            }
        }

        private void RenderControl_MouseDown(object sender, MouseEventArgs e)
        {
            lastX = int.MinValue;

            Position pos = gameView.ScreenToView(new Position(e.X, e.Y));

            if (pos == null)
                return;

            lastX = pos.X;
            lastY = pos.Y;
        }
    }
}
