# 🔌 Semantic Kernel Memory Plugin

[![Python](https://img.shields.io/badge/Python-3.12+-blue.svg)](https://www.python.org/downloads/)
[![Semantic Kernel](https://img.shields.io/badge/Semantic%20Kernel-1.23.1+-green.svg)](https://github.com/microsoft/semantic-kernel)

A Python plugin that seamlessly integrates Microsoft Kernel Memory with Semantic Kernel, enabling powerful document ingestion, retrieval, and knowledge search capabilities in your AI applications.

## 📋 Overview

This plugin bridges Kernel Memory's document management capabilities with Semantic Kernel's orchestration framework, allowing your AI applications to:

- 🔍 Search across indexed documents with semantic relevance
- 🏷️ Filter search results using custom tags
- 🧩 Integrate memory capabilities within Semantic Kernel pipelines

## 🚀 Installation

```bash
# Using pip
pip install semantic-kernel-memory-plugin

# Using Poetry
poetry add semantic-kernel-memory-plugin
```

## 🔧 Usage

### Basic Setup

```python
import asyncio
from semantic_kernel import Kernel
from semantic_kernel_memory_plugin.memory_plugin import MemoryPlugin
from kernel_memory_client import AuthenticatedClient

# Create a Semantic Kernel instance
kernel = Kernel()

# Initialize the Kernel Memory client
memory_client = AuthenticatedClient(
    base_url="http://localhost:9001",
    token="your-api-key",
    verify_ssl=True
)

# Add the Kernel Memory plugin to your kernel
kernel.add_plugin(
    MemoryPlugin(memory_client=memory_client),
    plugin_name="Memory"
)
```

### Using with Chat Completion

```python
import asyncio
from semantic_kernel import Kernel
from semantic_kernel.connectors.ai.open_ai import AzureChatCompletion
from semantic_kernel.connectors.ai.function_choice_behavior import FunctionChoiceBehavior
from semantic_kernel.contents.chat_history import ChatHistory
from semantic_kernel.connectors.ai.open_ai.prompt_execution_settings.azure_chat_prompt_execution_settings import (
    AzureChatPromptExecutionSettings,
)

# Setup kernel with chat completion service
kernel = Kernel()
chat_completion = AzureChatCompletion(
    deployment_name="gpt-4o",
    api_key="your-api-key",
    base_url="https://your-endpoint.openai.azure.com/"
)
kernel.add_service(chat_completion)

# Configure execution settings to allow function calling
execution_settings = AzureChatPromptExecutionSettings()
execution_settings.function_choice_behavior = FunctionChoiceBehavior.Auto()

# Create a history of the conversation
history = ChatHistory()
history.add_user_message(
    "Please search for information in index my-index: 'What is quantum computing?'"
)

# Get the response from the AI
result = await chat_completion.get_chat_message_content(
    chat_history=history,
    settings=execution_settings,
    kernel=kernel,
)

print("Assistant > " + str(result))
```

## ⚙️ Configuration Options

The `MemoryPlugin` class accepts several configuration parameters:

| Parameter | Description | Default |
|-----------|-------------|---------|
| `memory_client` | The Kernel Memory client or service URL | Required |
| `default_index` | Default Memory Index to use | `""` (empty string) |
| `default_retrieval_tags` | Default tags to require when searching | `{}` |

## 🔎 Search Function

The plugin provides a `search` function and is automatically used by semantic-kernel:

```python
# Example usage in a prompt
history.add_user_message(
    "Please search for the following information in index my-index: 'What is quantum computing?'"
)

# The AI will automatically use the memory.search function with appropriate parameters
```

## Example

```shell
python -m tests.test_memory_plugin

Starting the Memory Plugin test...
http://localhost:9001
Searching for: What is the latest Nasa news from project Artemis?
query: What is the latest Nasa news from project Artemis?
index: default
minRelevance: 0.0
limit: 1
tags: None
SearchResult(query='What is the latest Nasa news from project Artemis?', no_result=False, results=[Citation(link='default/20250323.115649.fec26b5981d34d7e8a04df03d7fd2047/416e1c4e75574fb38b47209de5c97e65', index='default', document_id='20250323.115649.fec26b5981d34d7e8a04df03d7fd2047', file_id='416e1c4e75574fb38b47209de5c97e65', source_content_type='application/pdf', source_name='mydocs-NASA-news.pdf', source_url='/download?index=default&documentId=20250323.115649.fec26b5981d34d7e8a04df03d7fd2047&filename=mydocs-NASA-news.pdf', partitions=[Partition(text='Skip to main content\nJul 28, 2023\nMEDIA ADVISORY M23-095\nNASA Invites Media to See Recovery Craft for\nArtemis Moon Mission\n(/sites/default/ﬁles/thumbnails/image/ksc-20230725-ph-fmx01_0003orig.jpg)\nAboard the USS John P. Murtha, NASA and Department of Defense personnel practice recovery operations for Artemis II in July. A\ncrew module test article is used to help verify the recovery team will be ready to recovery the Artemis II crew and the Orion spacecraft.\nCredits: NASA/Frank Michaux\nMedia are invited to see the new test version of NASA’s Orion spacecraft and the hardware teams will use\nto recover the capsule and astronauts upon their return from space during the Artemis II\n(http://www.nasa.gov/artemis-ii) mission. The event will take place at 11 a.m. PDT on Wednesday, Aug. 2,\nat Naval Base San Diego.\nPersonnel involved in recovery operations from NASA, the U.S. Navy, and the U.S. Air Force will be\navailable to speak with media.\nU.S. media interested in attending must RSVP by 4\xa0p.m., Monday, July 31, to the Naval Base San Diego\nPublic Aﬀairs (mailto:nbsd.pao@us.navy.mil) or 619-556-7359.\nOrion Spacecraft (/exploration/systems/orion/index.html)\nNASA Invites Media to See Recovery Craft for Artemis Moon Miss... https://www.nasa.gov/press-release/nasa-invites-media-to-see-recov...\n1 of 3 7/28/23, 4:51 PMTeams are currently conducting the ﬁrst in a series of tests in the Paciﬁc Ocean to demonstrate and\nevaluate the processes, procedures, and hardware for recovery operations (https://www.nasa.gov\n/exploration/systems/ground/index.html) for crewed Artemis missions. The tests will help prepare the\nteam for Artemis II, NASA’s ﬁrst crewed mission under Artemis that will send four astronauts in Orion\naround the Moon to checkout systems ahead of future lunar missions.\nThe Artemis II crew – NASA astronauts Reid Wiseman, Victor Glover, and Christina Koch, and CSA\n(Canadian Space Agency) astronaut Jeremy Hansen – will participate in recovery testing at sea next year.\nFor more information about Artemis, visit:\nhttps://www.nasa.gov/artemis (https://www.nasa.gov/artemis)\n-end-\nRachel Kraft\nHeadquarters, Washington\n202-358-1100\nrachel.h.kraft@nasa.gov (mailto:rachel.h.kraft@nasa.gov)\nMadison Tuttle\nKennedy Space Center, Florida\n321-298-5868\nmadison.e.tuttle@nasa.gov (mailto:madison.e.tuttle@nasa.gov)\nLast Updated: Jul 28, 2023\nEditor: Claire O’Shea\nTags:\xa0 Artemis (/artemisprogram),Ground Systems (http://www.nasa.gov/exploration/systems/ground\n/index.html),Kennedy Space Center (/centers/kennedy/home/index.html),Moon to Mars (/topics/moon-to-\nmars/),Orion Spacecraft (/exploration/systems/orion/index.html)\nNASA Invites Media to See Recovery Craft for Artemis Moon Miss... https://www.nasa.gov/press-release/nasa-invites-media-to-see-recov...\n2 of 3 7/28/23, 4:51 PM', relevance=0.487779, partition_number=0, section_number=0, last_update=datetime.datetime(2025, 3, 23, 11, 56, 50, tzinfo=tzutc()), tags=PartitionTagsType0(additional_properties={'__document_id': ['20250323.115649.fec26b5981d34d7e8a04df03d7fd2047'], '__file_type': ['application/pdf'], '__file_id': ['416e1c4e75574fb38b47209de5c97e65'], '__file_part': ['3e75b559d5ab43dd97409c91e7d0336f'], '__part_n': ['0'], '__sect_n': ['0']}))])])

Assistant > Here's the latest NASA news regarding the Artemis project:

- **Title:** NASA Invites Media to See Recovery Craft for Artemis Moon Mission

- **Date:** July 28, 2023

- **Summary:** NASA, in cooperation with the Department of Defense, is practicing recovery operations for the Artemis II mission aboard the USS John P. Murtha. This involves using a crew module test article to verify readiness for recovering the Artemis II crew and the Orion spacecraft. Media has been invited to see the test version of NASA’s Orion spacecraft and the hardware that will be used to recover the capsule and astronauts upon their return during the Artemis II mission.

- **Location:** Naval Base San Diego.

- **Personnel Involved:** Personnel from NASA, the U.S. Navy, and the U.S. Air Force, including astronauts Reid Wiseman, Victor Glover, Christina Koch from NASA, and Jeremy Hansen from the Canadian Space Agency.

- **For More Information:** Visit [NASA's Artemis website](https://www.nasa.gov/artemis).

If you want more detailed information, you can [download the document](https://www.nasa.gov/press-release/nasa-invites-media-to-see-recov) containing this news.
```

## 📄 Requirements

- Python 3.12 or higher
- `semantic-kernel` 1.23.1 or higher
- `kernel-memory-client`

## 🤝 Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## 📝 License

This project is licensed under the MIT License - see the LICENSE file for details.