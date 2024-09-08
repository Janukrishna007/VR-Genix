from flask import Flask, request, jsonify, send_file
import requests
import os
from flask_cors import CORS
from pydub import AudioSegment

app = Flask(__name__)
CORS(app)

GROQ_API_KEY = "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"
STABILITY_API_KEY = "your_stability_api_key"

if not os.path.exists('temp'):
    os.makedirs('temp')

@app.route('/transcribe', methods=['POST'])
def transcribe():
    try:
        if 'audio' not in request.files:
            return jsonify({'error': 'No audio file provided'}), 400

        audio_file = request.files['audio']

        if not audio_file.filename.endswith(('.wav', '.mp3', '.ogg')):
            return jsonify({'error': 'Unsupported audio format. Please upload a .wav, .mp3, or .ogg file.'}), 400
        
        original_file_path = os.path.join('temp', audio_file.filename)
        audio_file.save(original_file_path)
        
        if not audio_file.filename.endswith('.wav'):
            sound = AudioSegment.from_file(original_file_path)
            wav_file_path = os.path.join('temp', os.path.splitext(audio_file.filename)[0] + '.wav')
            sound.export(wav_file_path, format='wav')
        else:
            wav_file_path = original_file_path

        # Replace this with actual transcription logic
        transcription_text = perform_transcription(wav_file_path)
        
        generated_word = generate_word_from_description(transcription_text)

        # Generate image from the generated word
        image_path = generate_image_from_prompt(generated_word)

        return jsonify({
            'text': transcription_text,
            'generated_word': generated_word,
            'image_url': image_path
        })

    except Exception as e:
        return jsonify({'error': str(e)}), 500

def perform_transcription(file_path):
    # Implement your transcription logic here
    # For example, call an external API or service to get the transcription
    return "This is the actual transcription result"  # Placeholder for actual result

def generate_word_from_description(description):
    try:
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
            generated_word = response.json().get('choices', [{}])[0].get('message', {}).get('content', '').strip()
            return generated_word
        else:
            return 'Error generating word'

    except Exception as e:
        return f"Error: {str(e)}"

def generate_image_from_prompt(prompt):
    try:
        url = "https://api.stability.ai/v2beta/stable-image/generate/ultra"
        headers = {
            "authorization": f"Bearer {STABILITY_API_KEY}",
            "accept": "image/*"
        }

        response = requests.post(
            url,
            headers=headers,
            files={"none": ''},
            data={
                "prompt": prompt,
                "output_format": "webp",
            },
        )

        if response.status_code == 200:
            image_path = "./generated_image.webp"
            with open(image_path, 'wb') as file:
                file.write(response.content)
            return image_path
        else:
            raise Exception('Image generation failed')

    except Exception as e:
        return f"Error: {str(e)}"

if __name__ == '__main__':
    app.run(debug=True)
