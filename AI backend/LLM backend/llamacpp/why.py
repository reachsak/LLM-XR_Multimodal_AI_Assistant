from transformers.models.llama import LLamaForCausalLM, LLamaTokenizer
from langchain.llms import HuggingFacePipeline
from langchain.prompts import PromptTemplate

# Load the model and tokenizer
model_name = "models/llama-2-13b-chat.Q8_0.gguf"
tokenizer = LLamaTokenizer.from_pretrained(model_name)
model = LLamaForCausalLM.from_pretrained(model_name)

# Create the LangChain pipeline
llm = HuggingFacePipeline(model=model, tokenizer=tokenizer)

# Define the prompt template
prompt_template = PromptTemplate(input_variables=["query"], template="{query}")

# Ask a question
query = "What is the capital of France?"
prompt = prompt_template.format(query=query)

# Generate the response
result = llm(prompt)
print(result[0]['text'])