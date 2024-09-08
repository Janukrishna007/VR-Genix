import React, { useState, useEffect } from 'react';
import axios from 'axios';
import { FaMicrophone } from 'react-icons/fa';

function App() {
  const [isRecording, setIsRecording] = useState(false);
  const [transcription, setTranscription] = useState('');
  const [combinedWord, setCombinedWord] = useState('');
  const [imageUrl, setImageUrl] = useState('');
  const [mediaRecorder, setMediaRecorder] = useState(null);
  const [audioChunks, setAudioChunks] = useState([]);
  const [isProcessing, setIsProcessing] = useState(false);
  const [errorMessage, setErrorMessage] = useState('');

  useEffect(() => {
    if (!navigator.mediaDevices || !navigator.mediaDevices.getUserMedia) {
      console.error("MediaRecorder not supported by this browser.");
      setErrorMessage("Your browser doesn't support recording audio.");
      return;
    }

    navigator.mediaDevices.getUserMedia({ audio: true })
      .then(stream => {
        const recorder = new MediaRecorder(stream);
        setMediaRecorder(recorder);

        recorder.ondataavailable = (event) => {
          setAudioChunks(prevChunks => [...prevChunks, event.data]);
        };

        recorder.onstop = () => {
          const audioBlob = new Blob(audioChunks, { type: 'audio/wav' });
          sendAudioFile(audioBlob);
          setAudioChunks([]);  // Clear chunks for the next recording
        };
      })
      .catch(error => {
        console.error("Error accessing microphone:", error);
        setErrorMessage("Error accessing the microphone. Please check permissions.");
      });
  }, []);

  useEffect(() => {
    if (mediaRecorder) {
      if (isRecording) {
        mediaRecorder.start();
      } else {
        mediaRecorder.stop();
      }
    }
  }, [isRecording, mediaRecorder]);

  const toggleRecording = () => {
    setIsRecording(prevState => !prevState);
  };

  const sendAudioFile = async (audioBlob) => {
    setIsProcessing(true);
    setErrorMessage('');
    
    const formData = new FormData();
    formData.append('audio', audioBlob, 'recording.wav');

    try {
      const response = await axios.post('http://127.0.0.1:5000/transcribe', formData, {
        headers: {
          'Content-Type': 'multipart/form-data',
        },
      });

      setTranscription(response.data.text);
      setCombinedWord(response.data.generated_word);
      setImageUrl(response.data.image_url); // Set the image URL
    } catch (error) {
      console.error("Error during transcription:", error);
      setErrorMessage("Error during transcription. Please try again.");
    } finally {
      setIsProcessing(false);
    }
  };

  return (
    <div style={{ textAlign: 'center', marginTop: '50px' }}>
      <h1>Audio Transcription App</h1>

      {errorMessage && (
        <div style={{ color: 'red', marginBottom: '20px' }}>
          <p>{errorMessage}</p>
        </div>
      )}

      <button
        onClick={toggleRecording}
        disabled={isProcessing}
        style={{
          backgroundColor: isRecording ? 'red' : 'green',
          border: 'none',
          borderRadius: '50%',
          padding: '20px',
          fontSize: '2rem',
          cursor: isProcessing ? 'not-allowed' : 'pointer',
          color: 'white',
        }}
      >
        <FaMicrophone />
      </button>

      {isRecording ? (
        <p>Recording... Press again to stop.</p>
      ) : (
        <p>Press the mic to start recording</p>
      )}

      {isProcessing && <p>Processing audio, please wait...</p>}

      {transcription && (
        <div>
          <h2>Transcription:</h2>
          <p>{transcription}</p>

          {combinedWord && (
            <div>
              <h2>Generated Word:</h2>
              <p>{combinedWord}</p>
            </div>
          )}

          {imageUrl && (
            <div>
              <h2>Generated Image:</h2>
              <img src={imageUrl} alt="Generated" style={{ maxWidth: '100%', height: 'auto' }} />
            </div>
          )}
        </div>
      )}
    </div>
  );
}

export default App;
