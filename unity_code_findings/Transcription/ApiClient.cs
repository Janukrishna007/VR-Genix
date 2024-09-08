// Filename: ApiClient.cs
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
        private readonly string transcriptionApiUrl;
        private readonly string combineWordsApiUrl;

        public ApiClient(string transcriptionApiUrl, string combineWordsApiUrl)
        {
            this.transcriptionApiUrl = transcriptionApiUrl ?? throw new ArgumentNullException(nameof(transcriptionApiUrl));
            this.combineWordsApiUrl = combineWordsApiUrl ?? throw new ArgumentNullException(nameof(combineWordsApiUrl));
            httpClient = new HttpClient();
        }

        public async Task<string> SendAudioToApi(Stream audioStream, string fileName)
        {
            using (var content = new MultipartFormDataContent())
            {
                var audioContent = new StreamContent(audioStream);
                audioContent.Headers.ContentType = new MediaTypeHeaderValue("audio/wav");
                content.Add(audioContent, "audio", fileName); // "audio" is the form field name expected by Flask

                try
                {
                    var response = await httpClient.PostAsync(transcriptionApiUrl, content);
                    response.EnsureSuccessStatusCode();
                    var transcription = await response.Content.ReadAsStringAsync();
                    Console.WriteLine("Transcription: " + transcription);
                    return transcription;
                }
                catch (HttpRequestException httpEx)
                {
                    Console.WriteLine($"HTTP error: {httpEx.Message}");
                    return null;
                }
                catch (TaskCanceledException taskEx)
                {
                    Console.WriteLine($"Task canceled: {taskEx.Message}");
                    return null;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Unexpected error: {ex.Message}");
                    return null;
                }
            }
        }

        public async Task SendWordsToCombineApi(string word1, string word2)
        {
            var formData = new MultipartFormDataContent();
            formData.Add(new StringContent(word1), "word1");
            formData.Add(new StringContent(word2), "word2");

            try
            {
                var response = await httpClient.PostAsync(combineWordsApiUrl, formData);
                response.EnsureSuccessStatusCode();
                var combinedWord = await response.Content.ReadAsStringAsync();
                Console.WriteLine("Combined Word: " + combinedWord);
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

        public void Dispose()
        {
            httpClient?.Dispose();
        }
    }
}
