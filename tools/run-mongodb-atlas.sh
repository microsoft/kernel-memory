#!/usr/bin/env bash

# Based on: https://www.mongodb.com/docs/atlas/cli/stable/atlas-cli-deploy-docker/

docker run -p 27777:27017 --rm --privileged -it mongodb/atlas sh \
  -c "atlas deployments setup --bindIpAll --username root --password root --type local --force && tail -f /dev/null"

# Then you can connect with mongodb://root:root@localhost:27777/?authSource=admin

# The problem of this approach is that if you stop and restart container you will encounter problem
# because it will create another deployment.