from flask import Flask, request, jsonify
import requests
import os
from flask_cors import CORS
from pydub import AudioSegment  # Library for handling audio format conversion

app = Flask(__name__)
CORS(app)  # Enable CORS for all routes

GROQ_API_KEY = "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"

# Ensure the 'temp' directory exists for saving files
if not os.path.exists('temp'):
    os.makedirs('temp')

@app.route('/transcribe', methods=['POST'])
def transcribe():
    try:
        # Ensure the file is part of the request
        if 'audio' not in request.files:
            return jsonify({'error': 'No audio file provided'}), 400

        audio_file = request.files['audio']

        # Validate that the file is an audio file
        if not audio_file.filename.endswith(('.wav', '.mp3', '.ogg')):
            return jsonify({'error': 'Unsupported audio format. Please upload a .wav, .mp3, or .ogg file.'}), 400
        
        # Save the file temporarily
        original_file_path = os.path.join('temp', audio_file.filename)
        audio_file.save(original_file_path)
        
        # Convert file to WAV format if not already in WAV format
        if not audio_file.filename.endswith('.wav'):
            sound = AudioSegment.from_file(original_file_path)
            wav_file_path = os.path.join('temp', os.path.splitext(audio_file.filename)[0] + '.wav')
            sound.export(wav_file_path, format='wav')
        else:
            wav_file_path = original_file_path

        # Run your transcription logic here
        # Example placeholder for actual transcription processing
        transcription_text = "This is a test transcription."  # Replace this with your actual transcription logic

        # After transcribing the audio, use the transcription as a description for word generation
        generated_word = generate_word_from_description(transcription_text)

        # Return both transcription and the generated word
        return jsonify({
            'text': transcription_text,
            'generated_word': generated_word
        })

    except Exception as e:
        return jsonify({'error': str(e)}), 500

def generate_word_from_description(description):
    try:
        # Generate a single word from description
        prompt = f"""
        Generate a single, real-world existing object that fits the description: "{description}". 
        The object should be meaningful and recognizable in the real world. 
        Provide only the name of the object as the response.
        Example:
        Input: something that keeps you warm
        Output: blanket
        Give an accurate object name instead of random names
        
        Now, for the description: {description}
        """

        url = "https://api.groq.com/openai/v1/chat/completions"
        headers = {
            "Authorization": f"Bearer {GROQ_API_KEY}",
            "Content-Type": "application/json"
        }

        data = {
            "model": "llama3-8b-8192",
            "messages": [{"role": "user", "content": prompt}],
            "temperature": 0.7,
            "max_tokens": 50
        }

        response = requests.post(url, headers=headers, json=data)

        if response.status_code == 200:
            # Extract and clean the generated word from the response
            generated_word = response.json().get('choices', [{}])[0].get('message', {}).get('content', '').strip()
            return generated_word
        else:
            return 'Error generating word'

    except Exception as e:
        return f"Error: {str(e)}"

if __name__ == '__main__':
    app.run(debug=True)
