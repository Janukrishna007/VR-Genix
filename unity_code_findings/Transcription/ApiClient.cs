using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace AudioApiProject
{
    public class ApiClient : IDisposable
    {
        private readonly HttpClient httpClient;
        private readonly string apiUrl;

        public ApiClient(string apiUrl)
        {
            this.apiUrl = apiUrl ?? throw new ArgumentNullException(nameof(apiUrl));
            httpClient = new HttpClient();
        }

        public async Task SendAudioToApi(Stream audioStream, string fileName)
        {
            using (var content = new MultipartFormDataContent())
            {
                var audioContent = new StreamContent(audioStream);
                audioContent.Headers.ContentType = new MediaTypeHeaderValue("audio/wav");
                content.Add(audioContent, "audio", fileName); // "audio" is the form field name expected by Flask

                try
                {
                    var response = await httpClient.PostAsync(apiUrl, content);
                    response.EnsureSuccessStatusCode();
                    var responseData = await response.Content.ReadAsStringAsync();
                    Console.WriteLine("Transcription: " + responseData);
                }
                catch (HttpRequestException httpEx)
                {
                    Console.WriteLine($"HTTP error: {httpEx.Message}");
                }
                catch (TaskCanceledException taskEx)
                {
                    Console.WriteLine($"Task canceled: {taskEx.Message}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Unexpected error: {ex.Message}");
                }
            }
        }

        public void Dispose()
        {
            httpClient?.Dispose();
        }
    }
}
