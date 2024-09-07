using System;
using System.Threading.Tasks;

namespace AudioApiProject
{
    class Program
    {
        static async Task Main(string[] args)
        {
            string apiUrl = "http://localhost:5000/transcribe"; // Replace with your API endpoint

            using (var apiClient = new ApiClient(apiUrl))
            {
                using (var audioRecorder = new AudioRecorder(apiClient))
                {
                    await audioRecorder.StartRecordingAndSend(); // Start recording and sending audio dynamically
                }
            }
        }
    }
}
