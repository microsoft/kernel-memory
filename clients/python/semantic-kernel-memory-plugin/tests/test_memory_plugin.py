#!/usr/bin/env python
# -*- coding: utf-8 -*-
import asyncio
from dotenv import load_dotenv
import os

from semantic_kernel import Kernel
from semantic_kernel.connectors.ai.open_ai import AzureChatCompletion
from semantic_kernel.connectors.ai.function_choice_behavior import FunctionChoiceBehavior
from semantic_kernel.contents.chat_history import ChatHistory

from semantic_kernel.connectors.ai.open_ai.prompt_execution_settings.azure_chat_prompt_execution_settings import (
    AzureChatPromptExecutionSettings, )

from semantic_kernel_memory_plugin.memory_plugin import MemoryPlugin
from kernel_memory_client import Client, AuthenticatedClient
from dotenv import load_dotenv, find_dotenv


async def main():
    load_dotenv(find_dotenv())

    print("Starting the Memory Plugin test...")
    print(os.environ.get("MEMORY_SERVICE_URL"))
    print(os.environ.get("MEMORY_SERVICE_API_KEY"))

    memory_client = AuthenticatedClient(
        base_url=os.environ.get("MEMORY_SERVICE_URL"),
        token=os.environ.get("MEMORY_SERVICE_API_KEY"),
        verify_ssl=False)

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
