# Once launched open http://localhost:9444/ui to find your keys
docker run -it --rm --name s3-ninja \
  -p 9444:9000 scireum/s3-ninja:latest
