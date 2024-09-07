const express = require('express');
const axios = require('axios');
const cors = require('cors');
const multer = require('multer');
const fs = require('fs');
const path = require('path');

const app = express();
const port = 3000;

app.use(cors()); // Enable CORS for all routes
app.use(express.json()); // For parsing application/json

const upload = multer({ dest: 'uploads/' }); // Set up multer to handle file uploads

const GROQ_API_KEY = "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";

// Route for transcribing audio
app.post('/transcribe', upload.single('audio'), async (req, res) => {
    if (!req.file) {
        return res.status(400).json({ error: 'No audio file provided' });
    }

    const tempAudioPath = req.file.path;

    const url = "https://api.groq.com/openai/v1/audio/translations";

    const headers = {
        "Authorization": `Bearer ${GROQ_API_KEY}`
    };

    const data = {
        "model": "whisper-large-v3",
        "prompt": "Specify context or spelling",
        "temperature": 0,
        "response_format": "json"
    };

    try {
        const response = await axios.post(url, data, {
            headers: headers,
            params: { 'file': fs.createReadStream(tempAudioPath) }
        });

        // Clean up the temporary file
        fs.unlinkSync(tempAudioPath);

        if (response.status === 200) {
            return res.json(response.data);
        } else {
            return res.status(500).json({ error: 'Transcription failed', details: response.statusText });
        }
    } catch (error) {
        // Clean up the temporary file in case of error
        fs.unlinkSync(tempAudioPath);
        return res.status(500).json({ error: 'Transcription failed', details: error.message });
    }
});

// Route for combining words
app.post('/combine_words', async (req, res) => {
    const { word1, word2 } = req.body;

    if (!word1 || !word2) {
        return res.status(400).json({ error: 'Please provide two words' });
    }

    const url = "https://api.groq.com/openai/v1/chat/completions";

    const headers = {
        "Authorization": `Bearer ${GROQ_API_KEY}`,
        "Content-Type": "application/json"
    };

    const prompt = `
    Combine the words "${word1}" and "${word2}" to create a new, unique word that is related to both input words. 
    The new word should be creative and meaningful. 
    Provide only the new word as the response, without any additional explanation.
    The word should be meaningful, which can be found in a dictionary.
    Example:
    Input: wood, fire
    Output: campfire
    
    Now, combine these words: ${word1}, ${word2}
    `;

    const data = {
        "model": "llama3-8b-8192",
        "messages": [{ "role": "user", "content": prompt }],
        "temperature": 0.7,
        "max_tokens": 50
    };

    try {
        const response = await axios.post(url, data, { headers });

        if (response.status === 200) {
            const newWord = response.data.choices[0].message.content.trim();
            return res.json({ new_word: newWord });
        } else {
            return res.status(500).json({ error: 'Word combination failed', details: response.statusText });
        }
    } catch (error) {
        return res.status(500).json({ error: 'Word combination failed', details: error.message });
    }
});

// Start the server
app.listen(port, () => {
    console.log(`Server running at http://localhost:${port}/`);
});