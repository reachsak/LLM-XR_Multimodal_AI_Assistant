from langchain_community.chat_models import ChatOllama
from langchain_core.messages import HumanMessage
from langchain_core.output_parsers import StrOutputParser
from langchain_core.prompts import ChatPromptTemplate
from langchain.tools import Tool
import example  # Import your Python script file
import subprocess

# Create the ChatOllama LLM instance
llm = ChatOllama(model="gemma:2b")

# Define a function to execute the example.py script
def execute_python_script():
    try:
        # Execute the Python script without capturing its output
        subprocess.run(["python3", "./example.py"])
    except subprocess.CalledProcessError as e:
        print(f"Error executing script: {e}")
    except Exception as e:
        print(f"Error executing script: {e}")
# Create a prompt template



# Create a tool using the execute_python_script function
tool = Tool(
    name="Execute Python Script",
    description="Execute a Python script with the provided input",
    func=execute_python_script,
    coroutine=False,
)

# Get user input and execute the script
human_input = input("can you execute the script for me pelase i need you ")
result = tool.run(human_input)
print(result)