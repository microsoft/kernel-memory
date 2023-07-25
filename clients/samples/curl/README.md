# Setup

Before running the code, you should start Semantic Memory web service
and pipeline service.

The example points to http://127.0.0.1:9001 so by default you should
run the services locally, though you can also deploy them to Azure
and update the script accordingly.

# Run the example

```bash
./example.sh
```

Content of [example.sh](example.sh):

```bash
../../curl/upload-file.sh -f test.pdf \
                          -s http://127.0.0.1:9001/upload \
                          -u curlUser \
                          -c curlDataCollection \
                          -i curlExample01
```