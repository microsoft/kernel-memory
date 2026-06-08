docker run -it --rm --name elasticsearch \
  -p 9200:9200 -p 9300:9300 \
  -e "discovery.type=single-node" elasticsearch:8.11.3
