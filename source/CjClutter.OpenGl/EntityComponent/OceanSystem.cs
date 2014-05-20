﻿using System;
using System.Collections.Generic;
using System.Linq;
using CjClutter.OpenGl.Noise;
using CjClutter.OpenGl.OpenGl.VertexTypes;
using CjClutter.OpenGl.SceneGraph;
using OpenTK;

namespace CjClutter.OpenGl.EntityComponent
{
    public class OceanComponent : IEntityComponent
    {
        public OceanComponent(int width, int height)
        {
            Width = width;
            Height = height;
        }

        public int Width { get; private set; }
        public int Height { get; private set; }
    }

    public static class Gerstner
    {
        public static Vector3d A(List<WaveSetting> settings, Vector2d position, double time)
        {
            var offset = settings
                .Select(x => CalculateOffset(x, position, time))
                .Aggregate((x, y) => x + y);

            var height = settings
                .Select(x => CalculateHeight(x, position, time))
                .Sum();

            return new Vector3d(position.X + offset.X, height, position.Y + offset.Y);
        }

        private static double CalculateHeight(WaveSetting setting, Vector2d position, double time)
        {
            return setting.Amplitude * Math.Sin(Vector2d.Dot(setting.Frequency * setting.Direction, position) + setting.PhaseConstant * time);
        }

        private static Vector2d CalculateOffset(WaveSetting setting, Vector2d position, double time)
        {
            var periodic = Math.Cos(Vector2d.Dot(setting.Frequency * setting.Direction, position) + setting.PhaseConstant * time);
            var x = setting.Q * setting.Amplitude * setting.Direction.X * periodic;
            var y = setting.Q * setting.Amplitude * setting.Direction.Y * periodic;

            return new Vector2d(x, y);
        }

        public class WaveSetting
        {
            public double Q;
            public double Amplitude;
            public double Frequency;
            public double PhaseConstant;
            public Vector2d Direction;
        }
    }

    public class OceanSystem : IEntitySystem
    {
        private readonly INoiseGenerator _improvedPerlinNoise = new FractalBrownianMotion(new SimplexNoise(), FractalBrownianMotionSettings.Default);
        private List<Gerstner.WaveSetting> _waveSettings;

        public OceanSystem()
        {
            _waveSettings = CreateWaves(Math.PI * 0.3, 5, 0.01, 0.7);
        }

        public void Update(double elapsedTime, EntityManager entityManager)
        {
            foreach (var water in entityManager.GetEntitiesWithComponent<OceanComponent>())
            {
                var waterComponent = entityManager.GetComponent<OceanComponent>(water);
                var waterMesh = entityManager.GetComponent<StaticMesh>(water);
                if (waterMesh == null)
                {
                    waterMesh = new StaticMesh
                    {
                        Color = new Vector4(0f, 0f, 1f, 0f),
                        ModelMatrix = Matrix4.CreateTranslation(-5, 0, -5)
                        //ModelMatrix = Matrix4.Identity
                    };
                    entityManager.AddComponentToEntity(water, waterMesh);
                }

                //var waveSetting0 = new Gerstner.WaveSetting
                //{
                //    Amplitude = 0.2,
                //    Direction = new Vector2d(0.2, 0.4),
                //    Frequency = CalculateFrequency(10),
                //    PhaseConstant = 1,
                //    Q = 0.5
                //};

                //var waveSetting1 = new Gerstner.WaveSetting
                //{
                //    Amplitude = 0.1,
                //    Direction = new Vector2d(0.25, 0.5),
                //    Frequency = CalculateFrequency(2),
                //    PhaseConstant = 1,
                //    Q = 0.5
                //};

                //var waveSetting2 = new Gerstner.WaveSetting
                //{
                //    Amplitude = 0.05,
                //    Direction = new Vector2d(0.1, 0.7),
                //    Frequency = CalculateFrequency(0.25),
                //    PhaseConstant = 1,
                //    Q = 0.5
                //};

                var mesh = CreateMesh(waterComponent);
                waterMesh.Update(mesh);
                for (var i = 0; i < waterMesh.Mesh.Vertices.Length; i++)
                {
                    var position = waterMesh.Mesh.Vertices[i].Position;
                    var vector3D = Gerstner.A(_waveSettings, new Vector2d(position.X, position.Z), elapsedTime);

                    waterMesh.Mesh.Vertices[i] = new Vertex3V3N
                    {
                        Position = (Vector3)vector3D
                    };
                }
                waterMesh.Mesh.CalculateNormals();
                waterMesh.Update(waterMesh.Mesh);
            }
        }

        private List<Gerstner.WaveSetting> CreateWaves(double angle, double wavelength, double amplitude, double steepness)
        {
            var waveSettings = new List<Gerstner.WaveSetting>();
            var random = new Random(4711);
            int waves = 20;
            for (var i = 0; i < waves; i++)
            {
                var waveLengthFactor = wavelength * 2 - wavelength / 2;
                var minWaveLength = wavelength / 2;
                var nextDouble = random.NextDouble();
                var waveLength2 = minWaveLength + nextDouble * waveLengthFactor;

                var amplitudeSpan = amplitude*2 - amplitude/2;
                var minAmplitude = amplitude/2;
                var amplitude2 = minAmplitude + nextDouble*amplitudeSpan;

                var frequency = CalculateFrequency(waveLength2);

                var directionRad = angle + (random.NextDouble() - 0.5);
                var direction = new Vector2d(Math.Cos(directionRad), Math.Sin(directionRad));

                //How to handle the amplitude is a matter of opinion. Although derivations of wave amplitude as a function of wavelength and current weather conditions probably exist, 
                //we use a constant (or scripted) ratio, specified at authoring time. More exactly, along with a median wavelength, the artist specifies a median amplitude. 
                //For a wave of any size, the ratio of its amplitude to its wavelength will match the ratio of the median amplitude to the median wavelength.

                var q = steepness / (frequency * amplitude2 * waves);

                var s = new Gerstner.WaveSetting
                {
                    Direction = direction,
                    Frequency = frequency,
                    PhaseConstant = 2,
                    Q = q,
                    Amplitude = amplitude2,
                };

                waveSettings.Add(s);
            }

            return waveSettings;
        }

        private static double CalculateFrequency(double waveLength)
        {
            const double g = 9.82;
            var frequency = Math.Sqrt(g * Math.PI * 2 / waveLength);
            return frequency;
        }

        private static Mesh3V3N CreateMesh(OceanComponent oceanComponent)
        {
            var vertices = new List<Vertex3V3N>();
            for (var i = 0; i <= oceanComponent.Width; i++)
            {
                for (var j = 0; j <= oceanComponent.Height; j++)
                {
                    var xin = i / (double)oceanComponent.Width * 10;
                    var yin = j / (double)oceanComponent.Height * 10;

                    var position = new Vector3((float)xin, 0, (float)yin);
                    var vertex = new Vertex3V3N { Position = position };
                    vertices.Add(vertex);
                }
            }

            var faces = new List<Face3>();
            for (var i = 0; i < oceanComponent.Width; i++)
            {
                for (var j = 0; j < oceanComponent.Height; j++)
                {
                    var verticesInColumn = (oceanComponent.Height + 1);
                    var v0 = i * verticesInColumn + j;
                    var v1 = (i + 1) * verticesInColumn + j;
                    var v2 = (i + 1) * verticesInColumn + j + 1;
                    var v3 = i * verticesInColumn + j + 1;

                    var f0 = new Face3 { V0 = v0, V1 = v1, V2 = v2 };
                    var f1 = new Face3 { V0 = v0, V1 = v2, V2 = v3 };

                    faces.Add(f0);
                    faces.Add(f1);
                }
            }

            return new Mesh3V3N(vertices, faces);
        }
    }
}