from langchain.llms import Ollama
llm = Ollama(base_url ="http://localhost:11434",model="llama2")
print(llm.invoke("i love you"))
