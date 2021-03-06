using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using CjClutter.OpenGl.Camera;
using CjClutter.OpenGl.CoordinateSystems;
using CjClutter.OpenGl.EntityComponent;
using CjClutter.OpenGl.Input.Keboard;
using CjClutter.OpenGl.Input.Mouse;
using CjClutter.OpenGl.Noise;
using CjClutter.OpenGl.SceneGraph;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using OpenTK.Input;
using OpenTK.Platform;
using FrameEventArgs = OpenTK.FrameEventArgs;

namespace CjClutter.OpenGl.Gui
{
    public class OpenGlWindow : GameWindow
    {
        private readonly FrameTimeCounter _frameTimeCounter = new FrameTimeCounter();
        private Stopwatch _stopwatch;
        private readonly MouseInputProcessor _mouseInputProcessor;
        private readonly MouseInputObservable _mouseInputObservable;
        private readonly KeyboardInputProcessor _keyboardInputProcessor = new KeyboardInputProcessor();
        private readonly KeyboardInputObservable _keyboardInputObservable;
        private readonly ICamera _camera;
        private EntityManager _entityManager;
        private List<IEntitySystem> _systems;
        private LookAtCamera _lodCamera;
        private bool _synchronizeCameras = true;
        private Terrain _terrain;
        private Cube _cube;

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
            GraphicsContextFlags.Default)
        {
            _mouseInputProcessor = new MouseInputProcessor(this, new GuiToRelativeCoordinateTransformer());

            var buttonUpEventEvaluator = new ButtonUpActionEvaluator(_mouseInputProcessor);
            _mouseInputObservable = new MouseInputObservable(buttonUpEventEvaluator);

            _keyboardInputObservable = new KeyboardInputObservable(_keyboardInputProcessor);

            _camera = new LookAtCamera();
            _lodCamera = new LookAtCamera();
        }

        private void StartJobThread()
        {
            Context.MakeCurrent(null);
            var contextReady = new AutoResetEvent(false);
            var thread = new Thread(() =>
            {
                var window = new NativeWindow();
                var context = new GraphicsContext(Context.GraphicsMode, window.WindowInfo);
                context.MakeCurrent(window.WindowInfo);
                contextReady.Set();

                while (true)
                {
                    var action = JobDispatcher.Instance.Dequeue();
                    action();
                }
            });

            thread.IsBackground = true;
            thread.Start();
            contextReady.WaitOne();
            MakeCurrent();
        }

        protected override void OnLoad(EventArgs e)
        {
            StartJobThread();
            StartJobThread();
            StartJobThread();
            StartJobThread();
            StartJobThread();
            StartJobThread();
            StartJobThread();
            StartJobThread();

            _stopwatch = new Stopwatch();
            _stopwatch.Start();

            _keyboardInputObservable.SubscribeKey(KeyCombination.LeftAlt && KeyCombination.Enter, CombinationDirection.Down, ToggleFullScren);

            _keyboardInputObservable.SubscribeKey(KeyCombination.Esc, CombinationDirection.Down, Exit);
            _keyboardInputObservable.SubscribeKey(KeyCombination.P, CombinationDirection.Down, () => _camera.Projection = ProjectionMode.Perspective);
            _keyboardInputObservable.SubscribeKey(KeyCombination.O, CombinationDirection.Down, () => _camera.Projection = ProjectionMode.Orthographic);
            _keyboardInputObservable.SubscribeKey(KeyCombination.F, CombinationDirection.Down, () => _synchronizeCameras = !_synchronizeCameras);

            _entityManager = new EntityManager();

            var terrainSystem = new TerrainSystem(FractalBrownianMotionSettings.Default);
            _systems = new List<IEntitySystem>
            {
                terrainSystem,
                new FreeCameraSystem(_keyboardInputProcessor, _mouseInputProcessor,_camera),
                new LightMoverSystem(),
                new OceanSystem(),
                new CubeMeshSystem(),
                new InputSystem(_keyboardInputProcessor)
                //new ChunkedLODSystem(_lodCamera),
                //new RenderSystem(_camera),
            };

            var light = new Entity(Guid.NewGuid().ToString());
            _entityManager.Add(light);
            _entityManager.AddComponentToEntity(light, new PositionalLightComponent { Position = new Vector3d(0, 20, 0) });
            _entityManager.AddComponentToEntity(light, new InputComponent(Key.J, Key.L, Key.M, Key.N, Key.U, Key.I));

            const int numberOfChunksX = 20;
            const int numberOfChunksY = 20;
            for (var i = 0; i < numberOfChunksX; i++)
            {
                for (var j = 0; j < numberOfChunksY; j++)
                {
                    var entity = new Entity(Guid.NewGuid().ToString());
                    _entityManager.Add(entity);
                    _entityManager.AddComponentToEntity(entity, new ChunkComponent(i, j));
                    _entityManager.AddComponentToEntity(entity, new StaticMesh());
                }
            }

            var settingsViewModel = new SettingsViewModel(new NoiseFactory.NoiseParameters());
            settingsViewModel.SettingsChanged += () =>
            {
                var settings = settingsViewModel.Assemble();
                terrainSystem.SetTerrainSettings(new NoiseFactory.RidgedMultiFractal().Create(settings));
            };

            _terrain = new Terrain();
            _cube = new Cube();

        }

        private void ToggleFullScren()
        {
            if (WindowState == WindowState.Fullscreen)
            {
                WindowState = WindowState.Normal;
            }
            else if (WindowState == WindowState.Normal)
            {
                WindowState = WindowState.Fullscreen;
            }
        }

        protected override void OnResize(EventArgs e)
        {
            GL.Viewport(0, 0, Width, Height);
            _camera.Width = Width;
            _camera.Height = Height;
        }

        protected override void OnUpdateFrame(FrameEventArgs e)
        {
            ProcessMouseInput();

            ProcessKeyboardInput();
        }

        protected override void OnRenderFrame(FrameEventArgs e)
        {
            if (_synchronizeCameras)
            {
                _lodCamera.Position = _camera.Position;
                _lodCamera.Target = _camera.Target;
                _lodCamera.Up = _camera.Up;
                _lodCamera.Width = _camera.Width;
                _lodCamera.Height = _camera.Height;
            }

            _frameTimeCounter.UpdateFrameTime(e.Time);

            GL.Clear(ClearBufferMask.DepthBufferBit);

            foreach (var system in _systems)
            {
                system.Update(ElapsedTime.TotalSeconds, _entityManager);
            }

            _terrain.Render(_camera, _lodCamera, _entityManager.GetComponent<PositionalLightComponent>(_entityManager.GetEntitiesWithComponent<PositionalLightComponent>().Single()).Position);
            _cube.Render(_camera, _entityManager.GetComponent<PositionalLightComponent>(_entityManager.GetEntitiesWithComponent<PositionalLightComponent>().Single()).Position);

            SwapBuffers();
        }

        private void ProcessKeyboardInput()
        {
            if (!Focused)
            {
                return;
            }

            var keyboardState = OpenTK.Input.Keyboard.GetState();

            _keyboardInputProcessor.Update(keyboardState);
            _keyboardInputObservable.ProcessKeys();

        }

        private void ProcessMouseInput()
        {
            if (!Focused)
            {
                return;
            }

            var mouseState = OpenTK.Input.Mouse.GetState();

            _mouseInputProcessor.Update(mouseState);
            _mouseInputObservable.ProcessMouseButtons();
        }

        public TimeSpan ElapsedTime
        {
            get { return _stopwatch.Elapsed; }
        }
    }
}
