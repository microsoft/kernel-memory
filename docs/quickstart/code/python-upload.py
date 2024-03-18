import requests

files = {
    "file1": ("README.md", open("README.md", "rb")),
}

data = {
    "documentId": "doc01",
}

response = requests.post("http://127.0.0.1:9001/upload", files=files, data=data)

print(response.text)
