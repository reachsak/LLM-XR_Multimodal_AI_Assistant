from langchain.llms import Ollama
from langchain.agents import initialize_agent, Tool
import subprocess

# Function to execute a Python script
def execute_python_script():
    try:
        # Execute the Python script without capturing its output
        subprocess.run(["python3", "./example.py"])
        return "Python script executed successfully."
    except subprocess.CalledProcessError as e:
        return f"Error executing script: {e}"
    except Exception as e:
        return f"Error executing script: {e}"

# Initializing LangChain LLM
llm = Ollama(base_url="http://localhost:11434", model="llama2")

# Defining the tool to execute the Python script
tool_execute_script = Tool(
    name="execute_python_script",
    func=execute_python_script,
    description="This tool executes a Python script."
)

# Initializing LangChain agent with the tool
agent = initialize_agent([tool_execute_script], llm, agent='zero-shot-react-description', verbose=True)

# Running the agent to execute the Python script
while True:
    command = input("Please enter a command: ")
    response = agent.run(command)
    print(response)
