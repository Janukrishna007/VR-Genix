// Filename: AudioRecorder.cs
using System;
using System.IO;
using System.Threading.Tasks;
using NAudio.Wave;
using NAudio.Lame;

namespace AudioApiProject
{
    public class AudioRecorder : IDisposable
    {
        private readonly WaveInEvent waveIn;
        private readonly string audioFolderPath;
        private readonly ApiClient apiClient;
        private string tempMp3FilePath;
        private MemoryStream mp3Stream;

        public AudioRecorder(ApiClient apiClient)
        {
            this.apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));

            // Path to the audio files folder at the project's root directory
            audioFolderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "audiofiles");

            // Ensure the audio files folder is created
            if (!Directory.Exists(audioFolderPath))
            {
                try
                {
                    Directory.CreateDirectory(audioFolderPath);
                    Console.WriteLine("Audio files directory created at: " + audioFolderPath);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to create directory: {ex.Message}");
                    throw;
                }
            }

            // Generate a unique filename
            string uniqueId = Guid.NewGuid().ToString();
            tempMp3FilePath = Path.Combine(audioFolderPath, $"{uniqueId}.mp3");

            // Initialize a MemoryStream for MP3 encoding
            mp3Stream = new MemoryStream();

            // Set up the microphone recording
            waveIn = new WaveInEvent
            {
                DeviceNumber = 0, // Default microphone
                WaveFormat = new WaveFormat(44100, 16, 1) // 44.1kHz, 16-bit, Mono
            };

            // Set up the event handler for recording audio data
            waveIn.DataAvailable += (sender, e) =>
            {
                try
                {
                    // Write the audio data to the MP3 stream
                    if (mp3Stream != null)
                    {
                        using (var mp3Writer = new LameMP3FileWriter(mp3Stream, waveIn.WaveFormat, LAMEPreset.STANDARD))
                        {
                            mp3Writer.Write(e.Buffer, 0, e.BytesRecorded);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error writing to MP3 stream: {ex.Message}");
                }
            };
        }

        public async Task StartRecordingAndSend()
        {
            try
            {
                waveIn.StartRecording();
                Console.WriteLine("Recording... Press Enter to stop.");
                Console.ReadLine();
                waveIn.StopRecording();
                Console.WriteLine("Recording stopped.");

                // Finalize MP3 stream
                if (mp3Stream != null)
                {
                    mp3Stream.Position = 0; // Reset the position to the start of the stream

                    // Save MP3 stream to a file
                    using (var fileStream = new FileStream(tempMp3FilePath, FileMode.Create, FileAccess.Write))
                    {
                        await mp3Stream.CopyToAsync(fileStream);
                    }
                }

                // Send the recorded MP3 to the server
                var transcription = await SendRecordedAudio();

                // Send the transcription to combine words API
                if (!string.IsNullOrEmpty(transcription))
                {
                    var words = transcription.Split(' '); // Assuming transcription has multiple words
                    if (words.Length >= 2)
                    {
                        await apiClient.SendWordsToCombineApi(words[0], words[1]);
                    }
                    else
                    {
                        Console.WriteLine("Transcription did not return enough words.");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during recording: {ex.Message}");
            }
        }

        private async Task<string> SendRecordedAudio()
        {
            try
            {
                if (!File.Exists(tempMp3FilePath))
                {
                    Console.WriteLine("No MP3 file found to send.");
                    return null;
                }

                using (var fs = new FileStream(tempMp3FilePath, FileMode.Open, FileAccess.Read))
                {
                    return await apiClient.SendAudioToApi(fs, Path.GetFileName(tempMp3FilePath));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending audio: {ex.Message}");
                return null;
            }
        }

        public void Dispose()
        {
            waveIn.Dispose();
            mp3Stream?.Dispose();
        }
    }
}
