// Filename: Program.cs
using System;
using System.Threading.Tasks;

namespace AudioApiProject
{
    class Program
    {
        static async Task Main(string[] args)
        {
            string transcriptionApiUrl = "http://localhost:5000/transcribe"; // Replace with your transcription API endpoint
            string combineWordsApiUrl = "http://localhost:5000/combine_words"; // Replace with your combine words API endpoint

            using (var apiClient = new ApiClient(transcriptionApiUrl, combineWordsApiUrl))
            {
                using (var audioRecorder = new AudioRecorder(apiClient))
                {
                    await audioRecorder.StartRecordingAndSend(); // Start recording and sending audio dynamically
                }
            }
        }
    }
}
