import requests
import json

data = {"question": "Name one tool that I can use from command line"}

response = requests.post(
    "http://127.0.0.1:9001/ask",
    headers={"Content-Type": "application/json"},
    data=json.dumps(data),
).json()

if "text" in response:
    print(response["text"])
else:
    print("Response does not contain a 'text' property.")
