using NAudio.CoreAudioApi;
using NAudio.Wave;
using PadScope.Core.Diagnostics;

namespace PadScope.Hid.Audio;

public sealed class AudioStreamBridge : IDisposable
{
    private WasapiCapture? _capture;
    private WasapiOut? _playback;
    private bool _disposed;
    private readonly object _lock = new();

    public bool IsCapturing { get; private set; }
    public bool IsPlaying { get; private set; }

    public event Action<string>? Log;
    public event Action<byte[]>? MicDataCaptured;
    public event Action<int>? SpeakerVolumeChanged;
    public event Action<int>? MicVolumeChanged;

    public IReadOnlyList<AudioDeviceInfo> AvailableSpeakers { get; private set; } = Array.Empty<AudioDeviceInfo>();
    public IReadOnlyList<AudioDeviceInfo> AvailableMicrophones { get; private set; } = Array.Empty<AudioDeviceInfo>();

    public void RefreshDevices()
    {
        AvailableSpeakers = AudioProbe.FindControllerSpeakers();
        AvailableMicrophones = AudioProbe.FindControllerMicrophones();
        Log?.Invoke($"Refreshed audio devices: {AvailableSpeakers.Count} speaker(s), {AvailableMicrophones.Count} microphone(s).");
    }

    public bool StartCapture(int deviceIndex = 0)
    {
        lock (_lock)
        {
            if (IsCapturing)
            {
                Log?.Invoke("Capture already running.");
                return true;
            }

            var devices = AvailableMicrophones;
            if (devices.Count == 0)
            {
                Log?.Invoke("No controller microphone found. Connect DS4/DualSense via USB/Bluetooth.");
                return false;
            }

            if (deviceIndex < 0 || deviceIndex >= devices.Count)
            {
                deviceIndex = 0;
            }

            try
            {
                var enumerator = new MMDeviceEnumerator();
                var device = FindWasapiDevice(enumerator, devices[deviceIndex], DataFlow.Capture);

                if (device is null)
                {
                    Log?.Invoke($"WASAPI device not found for '{devices[deviceIndex].Name}'.");
                    return false;
                }

                _capture = new WasapiCapture(device, false, 100);
                _capture.DataAvailable += OnCaptureDataAvailable;
                _capture.RecordingStopped += OnCaptureStopped;
                _capture.StartRecording();

                IsCapturing = true;
                Log?.Invoke($"Capture started on '{devices[deviceIndex].Name}' ({_capture.WaveFormat}).");
                return true;
            }
            catch (Exception ex)
            {
                Log?.Invoke($"Capture failed: {ex.Message}");
                return false;
            }
        }
    }

    public void StopCapture()
    {
        lock (_lock)
        {
            if (!IsCapturing || _capture is null)
            {
                return;
            }

            try
            {
                _capture.StopRecording();
                _capture.DataAvailable -= OnCaptureDataAvailable;
                _capture.RecordingStopped -= OnCaptureStopped;
                _capture.Dispose();
                _capture = null;
                IsCapturing = false;
                Log?.Invoke("Capture stopped.");
            }
            catch (Exception ex)
            {
                Log?.Invoke($"Stop capture error: {ex.Message}");
            }
        }
    }

    public bool StartPlayback(int deviceIndex = 0)
    {
        lock (_lock)
        {
            if (IsPlaying)
            {
                Log?.Invoke("Playback already running.");
                return true;
            }

            var devices = AvailableSpeakers;
            if (devices.Count == 0)
            {
                Log?.Invoke("No controller speaker found. Connect DS4/DualSense via USB/Bluetooth.");
                return false;
            }

            if (deviceIndex < 0 || deviceIndex >= devices.Count)
            {
                deviceIndex = 0;
            }

            try
            {
                var enumerator = new MMDeviceEnumerator();
                var device = FindWasapiDevice(enumerator, devices[deviceIndex], DataFlow.Render);

                if (device is null)
                {
                    Log?.Invoke($"WASAPI device not found for '{devices[deviceIndex].Name}'.");
                    return false;
                }

                _playback = new WasapiOut(device, AudioClientShareMode.Shared, false, 100);
                IsPlaying = true;
                Log?.Invoke($"Playback ready on '{devices[deviceIndex].Name}' ({_playback.OutputWaveFormat}).");
                return true;
            }
            catch (Exception ex)
            {
                Log?.Invoke($"Playback failed: {ex.Message}");
                return false;
            }
        }
    }

    public void StopPlayback()
    {
        lock (_lock)
        {
            if (!IsPlaying || _playback is null)
            {
                return;
            }

            try
            {
                _playback.Stop();
                _playback.Dispose();
                _playback = null;
                IsPlaying = false;
                Log?.Invoke("Playback stopped.");
            }
            catch (Exception ex)
            {
                Log?.Invoke($"Stop playback error: {ex.Message}");
            }
        }
    }

    private BufferedWaveProvider? _routeBuffer;

    public void RouteMicToSpeaker()
    {
        if (!IsCapturing || _capture is null)
        {
            Log?.Invoke("Start capture first before routing.");
            return;
        }

        if (!IsPlaying || _playback is null)
        {
            Log?.Invoke("Start playback first before routing.");
            return;
        }

        _routeBuffer = new BufferedWaveProvider(_capture.WaveFormat)
        {
            BufferDuration = TimeSpan.FromSeconds(2),
            DiscardOnBufferOverflow = true
        };

        MicDataCaptured += OnMicDataForRoute;

        var resampler = new MediaFoundationResampler(_routeBuffer, _playback.OutputWaveFormat);
        _playback.Init(resampler);
        _playback.Play();

        Log?.Invoke($"Routing active: mic ({_capture.WaveFormat}) → speaker ({_playback.OutputWaveFormat}).");
    }

    public void StopRoute()
    {
        MicDataCaptured -= OnMicDataForRoute;
        _routeBuffer = null;

        lock (_lock)
        {
            _playback?.Stop();
        }

        Log?.Invoke("Audio route stopped.");
    }

    private void OnMicDataForRoute(byte[] data)
    {
        _routeBuffer?.AddSamples(data, 0, data.Length);
    }

    public void SetSpeakerVolume(int volumePercent)
    {
        volumePercent = Math.Clamp(volumePercent, 0, 100);
        SpeakerVolumeChanged?.Invoke(volumePercent);
        Log?.Invoke($"Speaker volume set to {volumePercent}%.");
    }

    public void SetMicVolume(int volumePercent)
    {
        volumePercent = Math.Clamp(volumePercent, 0, 100);
        MicVolumeChanged?.Invoke(volumePercent);
        Log?.Invoke($"Microphone volume set to {volumePercent}%.");
    }

    public string DescribeStatus()
    {
        string captureStatus = IsCapturing ? "ACTIVE" : "stopped";
        string playbackStatus = IsPlaying ? "ACTIVE" : "stopped";
        int speakers = AvailableSpeakers.Count;
        int mics = AvailableMicrophones.Count;

        return $"Audio Lab: capture={captureStatus}, playback={playbackStatus}, speakers={speakers}, mics={mics}.";
    }

    private void OnCaptureDataAvailable(object? sender, WaveInEventArgs e)
    {
        if (e.BytesRecorded > 0)
        {
            byte[] buffer = new byte[e.BytesRecorded];
            Array.Copy(e.Buffer, buffer, e.BytesRecorded);
            MicDataCaptured?.Invoke(buffer);
        }
    }

    private void OnCaptureStopped(object? sender, StoppedEventArgs e)
    {
        if (e.Exception is not null)
        {
            Log?.Invoke($"Capture stopped with error: {e.Exception.Message}");
        }

        lock (_lock)
        {
            IsCapturing = false;
        }
    }

    private static MMDevice? FindWasapiDevice(MMDeviceEnumerator enumerator, AudioDeviceInfo info, DataFlow flow)
    {
        try
        {
            var devices = enumerator.EnumerateAudioEndPoints(flow, DeviceState.Active);

            foreach (var device in devices)
            {
                if (device.FriendlyName.Contains(info.Name, StringComparison.OrdinalIgnoreCase) ||
                    device.DeviceID.Contains(info.PnpDeviceId, StringComparison.OrdinalIgnoreCase))
                {
                    return device;
                }

                device.Dispose();
            }
        }
        catch
        {
            // WASAPI enumeration failure — fall through.
        }

        return null;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        StopRoute();
        StopCapture();
        StopPlayback();
    }
}
