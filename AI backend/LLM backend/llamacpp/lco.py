from langchain.llms import Ollama
ollama =Ollama(base_url ="http://localhost:11434",model='llama2')
print(ollama('why the sky is blue'))