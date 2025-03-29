#!/usr/bin/env python
# Copyright (c) 2025 Microsoft
#
# Permission is hereby granted, free of charge, to any person obtaining a copy of
# this software and associated documentation files (the "Software"), to deal in
# the Software without restriction, including without limitation the rights to
# use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of
# the Software, and to permit persons to whom the Software is furnished to do so,
# subject to the following conditions:
#
# The above copyright notice and this permission notice shall be included in all
# copies or substantial portions of the Software.
#
# THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
# IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS
# FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR
# COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER
# IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN
# CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

import asyncio
from dotenv import load_dotenv
import os

from semantic_kernel import Kernel
from semantic_kernel.connectors.ai.open_ai import AzureChatCompletion
from semantic_kernel.connectors.ai.function_choice_behavior import (
    FunctionChoiceBehavior,
)
from semantic_kernel.contents.chat_history import ChatHistory

from semantic_kernel.connectors.ai.open_ai.prompt_execution_settings.azure_chat_prompt_execution_settings import (
    AzureChatPromptExecutionSettings,
)

from semantic_kernel_memory_plugin.memory_plugin import MemoryPlugin
from kernel_memory_client import AuthenticatedClient
from dotenv import find_dotenv


async def main():
    load_dotenv(find_dotenv())

    print("Starting the Memory Plugin test...")
    print(os.environ.get("MEMORY_SERVICE_URL"))
    print(os.environ.get("MEMORY_SERVICE_API_KEY"))

    memory_client = AuthenticatedClient(
        base_url=os.environ.get("MEMORY_SERVICE_URL"),
        token=os.environ.get("MEMORY_SERVICE_API_KEY"),
        verify_ssl=False,
    )

    # Initialize the kernel
    kernel = Kernel()

    # Add Azure OpenAI chat completion
    chat_completion = AzureChatCompletion(
        deployment_name="gpt-4o",
        api_key=os.environ.get("AZURE_OPENAI_API_KEY"),
        base_url=os.environ.get("AZURE_OPENAI_ENDPOINT"),
    )
    kernel.add_service(chat_completion)

    # Add a plugin (the MemoryPlugin class is defined below)
    kernel.add_plugin(
        MemoryPlugin(memory_client=memory_client),
        plugin_name="Memory",
    )

    # Enable planning
    execution_settings = AzureChatPromptExecutionSettings()
    execution_settings.function_choice_behavior = FunctionChoiceBehavior.Auto()

    # Create a history of the conversation
    history = ChatHistory()
    history.add_user_message(
        "Please search for the following information in index default: 'What is the latest Nasa news from project Artemis?'"
    )

    # Get the response from the AI
    result = await chat_completion.get_chat_message_content(
        chat_history=history,
        settings=execution_settings,
        kernel=kernel,
    )

    # Print the results
    print("Assistant > " + str(result))

    # Add the message from the agent to the chat history
    history.add_message(result)


# Run the main function
if __name__ == "__main__":
    asyncio.run(main())
