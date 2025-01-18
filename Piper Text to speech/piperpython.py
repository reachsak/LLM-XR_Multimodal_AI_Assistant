import subprocess
import io
from pydub import AudioSegment
from pydub.playback import play

def text_to_speech_and_play(text, model):
    try:
        # Construct the command
        command = f"echo '{text}' | piper --model {model} --output_file -"
        
        # Execute the command and capture the output
        result = subprocess.run(command, shell=True, check=True, stdout=subprocess.PIPE, stderr=subprocess.PIPE)
        
        if result.returncode == 0:
            # Load the audio data from the stdout
            audio_data = io.BytesIO(result.stdout)
            audio_segment = AudioSegment.from_file(audio_data, format="wav")
            
            # Play the audio
            play(audio_segment)
        else:
            print(f"Failed to synthesize speech: {result.stderr.decode()}")
    
    except subprocess.CalledProcessError as e:
        print(f"Error occurred: {e.stderr.decode()}")

# Define the text and model
text = 'Now, we’re ushering in a new era with open source leading the way'
model = 'en_US-lessac-medium'

# Call the function
text_to_speech_and_play(text, model)
