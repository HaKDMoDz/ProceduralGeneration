using System;
using System.Collections.Generic;
using System.Drawing;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using OpenTK.Input;
using QuickFont;

namespace CjClutter.OpenGl
{
    public class OpenGlWindow : GameWindow
    {
        private readonly Dictionary<Key, Action<GameWindow>> _keyboardInputActions = new Dictionary<Key, Action<GameWindow>>
                                                                                         {
                                                                                             { Key.Escape, w => w.Exit() },
                                                                                         };

        private QFont _qFont;
        private readonly FrameTimeCounter _frameTimeCounter = new FrameTimeCounter();

        public OpenGlWindow(int width, int height, string title, OpenGlVersion openGlVersion)
            : base(
            width, 
            height, 
            GraphicsMode.Default, 
            title, 
            GameWindowFlags.Default, 
            DisplayDevice.Default, 
            openGlVersion.Major, 
            openGlVersion.Minor, 
            GraphicsContextFlags.Default) {}

        protected override void OnLoad(EventArgs e)
        {
            var font = new Font(FontFamily.GenericSansSerif, 10);
            var config = new QFontBuilderConfiguration
                             {
                UseVertexBuffer = true,
                TextGenerationRenderHint = TextGenerationRenderHint.SystemDefault
            };

            _qFont = new QFont(font, config);
        }

        protected override void OnRenderFrame(FrameEventArgs e)
        {
            ProcessKeyboardInput();

            _frameTimeCounter.UpdateFrameTime(e.Time);

            GL.ClearColor(Color4.White);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            GL.Color3(Color.Green);

            GL.Rotate(e.Time * 10, 0, 0, 1);

            GL.Begin(BeginMode.Triangles);

            GL.Vertex3(-0.5, -0.5, 0);
            GL.Vertex3(0.5, -0.5, 0);
            GL.Vertex3(0.5, 0.5, 0);

            GL.End();

            DrawDebugText();

            SwapBuffers();
        }

        private void DrawDebugText()
        {
            QFontExtensions.RunInQFontScope(() =>
                {
                    _qFont.PrintToVBO(_frameTimeCounter.ToOutputString(), Vector2.Zero, Color.Black);
                    _qFont.LoadVBOs();
                    _qFont.DrawVBOs();
                    _qFont.ResetVBOs();
                });
        }

        private void ProcessKeyboardInput()
        {
            var keyboardState = OpenTK.Input.Keyboard.GetState();

            foreach (var keyboardInputActionPair in _keyboardInputActions)
            {
                if (keyboardState[keyboardInputActionPair.Key])
                {
                    keyboardInputActionPair.Value(this);
                }
            }
        }
    }
}