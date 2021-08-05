﻿using Microsoft.Azure.Kinect.BodyTracking;
using Microsoft.Azure.Kinect.Sensor;
using System;
using System.IO;

namespace Csharp_3d_viewer
{
    class Program
    {
        public static StreamWriter outputFile;
        static void Main()
        {
            outputFile = new StreamWriter("C:\\Users\\grant\\Kinect DK Scanning Software\\Azure-Kinect-Samples-master\\Azure-Kinect-Samples-master\\body-tracking-samples\\csharp_3d_viewer\\output.txt");
            using (var visualizerData = new VisualizerData())
            {
                var renderer = new Renderer(visualizerData);

                renderer.StartVisualizationThread();

                // Open device.
                using (Device device = Device.Open())
                {
                    device.StartCameras(new DeviceConfiguration()
                    {
                        CameraFPS = FPS.FPS30,
                        ColorResolution = ColorResolution.Off,
                        DepthMode = DepthMode.NFOV_Unbinned,
                        WiredSyncMode = WiredSyncMode.Standalone,
                    });

                    var deviceCalibration = device.GetCalibration();
                    PointCloud.ComputePointCloudCache(deviceCalibration);
                    Tracker tracker = Tracker.Create(deviceCalibration, new TrackerConfiguration() { ProcessingMode = TrackerProcessingMode.Gpu, SensorOrientation = SensorOrientation.Default});


                    using (tracker)
                    {
                        while (renderer.IsActive)
                        {
                            using (Capture sensorCapture = device.GetCapture())
                            {
                                // Queue latest frame from the sensor.
                                tracker.EnqueueCapture(sensorCapture);
                            }

                            // Try getting latest tracker frame.
                            using (Frame frame = tracker.PopResult(TimeSpan.Zero, throwOnTimeout: false))
                            {
                                if (frame != null)
                                {
                                    // Save this frame for visualization in Renderer.

                                    // One can access frame data here and extract e.g. tracked bodies from it for the needed purpose.
                                    // Instead, for simplicity, we transfer the frame object to the rendering background thread.
                                    // This example shows that frame popped from tracker should be disposed. Since here it is used
                                    // in a different thread, we use Reference method to prolong the lifetime of the frame object.
                                    // For reference on how to read frame data, please take a look at Renderer.NativeWindow_Render().
                                    visualizerData.Frame = frame.Reference();
                                }
                            }
                        }
                    }
                }
                outputFile.Flush();
                outputFile.Close();
            }
        }
    }
}