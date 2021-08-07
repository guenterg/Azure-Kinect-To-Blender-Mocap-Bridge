﻿using Microsoft.Azure.Kinect.BodyTracking;
using OpenGL;
using OpenGL.CoreUI;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Threading.Tasks;
using System.Text.Json;

namespace Csharp_3d_viewer
{
    public class Renderer
    {
        private SphereRenderer SphereRenderer;
        private CylinderRenderer CylinderRenderer;
        private PointCloudRenderer PointCloudRenderer;

        private readonly VisualizerData visualizerData;
        private List<Vertex> pointCloud = null;

        struct BoneMocapData
        {
            public BoneMocapData(double timestamp, string name, double x, double y, double z, double w, double xLoc, double yLoc, double zLoc, double pW, double pX, double pY, double pZ)
            {
                Timestamp = timestamp;
                Name = name;
                X = x;
                Y = y;
                Z = z;
                W = w;
                XLoc = xLoc;
                YLoc = yLoc;
                ZLoc = zLoc;
                PW = pW;
                PX = pX;
                PY = pY;
                PZ = pZ;
            }
            public double Timestamp { get; }
            public string Name { get; }
            public double X { get; }
            public double Y { get; }
            public double Z { get; }
            public double W { get; }
            public double XLoc { get; }
            public double YLoc { get; }
            public double ZLoc { get; }
            public double PW { get; }
            public double PX { get; }
            public double PY { get; }
            public double PZ { get; }
        }
        public Renderer(VisualizerData visualizerData)
        {
            this.visualizerData = visualizerData;
        }

        public bool IsActive { get; private set; }

        public void StartVisualizationThread()
        {
            Task.Run(() =>
            {
                using (NativeWindow nativeWindow = NativeWindow.Create())
                {
                    IsActive = true;
                    nativeWindow.ContextCreated += NativeWindow_ContextCreated;
                    nativeWindow.Render += NativeWindow_Render;
                    nativeWindow.KeyDown += (object obj, NativeWindowKeyEventArgs e) =>
                    {
                        switch (e.Key)
                        {
                            case KeyCode.Escape:
                                nativeWindow.Stop();
                                IsActive = false;
                                break;

                            case KeyCode.F:
                                nativeWindow.Fullscreen = !nativeWindow.Fullscreen;
                                break;
                        }
                    };
                    nativeWindow.Animation = true;

                    nativeWindow.Create(0, 0, 640, 480, NativeWindowStyle.Overlapped);

                    nativeWindow.Show();
                    nativeWindow.Run();
                }
            });
        }

        private void NativeWindow_ContextCreated(object sender, NativeWindowEventArgs e)
        {
            Gl.ReadBuffer(ReadBufferMode.Back);

           // Gl.ClearColor(0.0f, 0.0f, 0.0f, 1.0f);

            Gl.Enable(EnableCap.Blend);
            Gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

            Gl.LineWidth(2.5f);

            CreateResources();
        }

        private static float ToRadians(float degrees)
        {
            return degrees / 180.0f * (float)Math.PI;
        }

        private void NativeWindow_Render(object sender, NativeWindowEventArgs e)
        {
            using (var lastFrame = visualizerData.TakeFrameWithOwnership())
            {
                if (lastFrame == null)
                {
                    return;
                }

                NativeWindow nativeWindow = (NativeWindow)sender;

                Gl.Viewport(0, 0, (int)nativeWindow.Width, (int)nativeWindow.Height);
                Gl.Clear(ClearBufferMask.ColorBufferBit);

                // Update model/view/projective matrices in shader
                var proj = Matrix4x4.CreatePerspectiveFieldOfView(ToRadians(65.0f), (float)nativeWindow.Width / nativeWindow.Height, 0.1f, 150.0f);
                var view = Matrix4x4.CreateLookAt(Vector3.Zero, Vector3.UnitZ, -Vector3.UnitY);

                SphereRenderer.View = view;
                SphereRenderer.Projection = proj;

                CylinderRenderer.View = view;
                CylinderRenderer.Projection = proj;

                PointCloudRenderer.View = view;
                PointCloudRenderer.Projection = proj;

                PointCloud.ComputePointCloud(lastFrame.Capture.Depth, ref pointCloud);
                PointCloudRenderer.Render(pointCloud, new Vector4(1, 1, 1, 1));

                for (uint i = 0; i < lastFrame.NumberOfBodies; ++i)
                {
                    var skeleton = lastFrame.GetBodySkeleton(i);
                    var bodyId = lastFrame.GetBodyId(i);
                    var bodyColor = BodyColors.GetColorAsVector(bodyId);

                    for (int jointId = 0; jointId < (int)JointId.Count; ++jointId)
                    {
                        var joint = skeleton.GetJoint(jointId);

                        // Render the joint as a sphere.
                        const float radius = 0.024f;
                        SphereRenderer.Render(joint.Position / 1000, radius, bodyColor);
                        Quaternion parentQuat = new Quaternion();
                        if (JointConnections.JointParent.TryGetValue((JointId)jointId, out JointId parentId))
                        {
                            parentQuat = skeleton.GetJoint(parentId).Quaternion;
                            CylinderRenderer.Render(joint.Position / 1000, skeleton.GetJoint((int)parentId).Position / 1000, bodyColor);
                            // Render a bone connecting this joint and its parent as a cylinder.
                        }

                        Program.outputFile.WriteLine(JsonSerializer.Serialize(new BoneMocapData(lastFrame.DeviceTimestamp.TotalMilliseconds, Enum.GetName(typeof(JointId), jointId), joint.Quaternion.X, joint.Quaternion.Y, joint.Quaternion.Z, joint.Quaternion.W, joint.Position.X, joint.Position.Y, joint.Position.Z, parentQuat.W, parentQuat.X, parentQuat.Y,parentQuat.Z)));

                    }
                }
            }
        }

        private void CreateResources()
        {
            SphereRenderer = new SphereRenderer();
            CylinderRenderer = new CylinderRenderer();
            PointCloudRenderer = new PointCloudRenderer();
        }
    }
}
