# Setup

Before running the code, you should start Kernel Memory web service
and pipeline service.

The example points to http://127.0.0.1:9001 so by default you should
run the services locally, though you can also deploy them to Azure
and update the script accordingly.

# Run the example: upload a file

```bash
./upload-example.sh
```

Content of [upload-example.sh](upload-example.sh):

```bash
../../tools/upload-file.sh -f test.pdf \
                           -s http://127.0.0.1:9001 \
                           -u curlUser \
                           -t "type:test" \
                           -i curlExample01
```

# Run the example: ask a question

```bash
./ask-example.sh
```

Content of [ask-example.sh](ask-example.sh):

```bash
../../tools/ask.sh -s http://127.0.0.1:9001 \
                   -u curlUser \
                   -q "tell me about Semantic Kernel" \
                   -f '"type":["test"]'
```