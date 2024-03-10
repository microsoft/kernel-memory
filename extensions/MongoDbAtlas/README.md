# Developing with MongoDB Atlas

While MongoDB Atlas is a cloud only deployment, recently MongoDb added the ability to create local installation of Atlas thanks to Atlas Command line.

You can install Atlas Command Line [Directly from MongoDB Web Site](https://www.mongodb.com/docs/atlas/cli/stable/install-atlas-cli/). Thanks to this tool you can create a local MongoDB atlas installation with few lines of CLI.

This local cluster is managed by podman, we need to investigate if we can create one on Docker.

Local cluster offers all the Search Capabilities of MongoDB Atlas, including Atlas Search.

## Docker support

You can find all detail for recent docker support [here](https://www.mongodb.com/docs/atlas/cli/stable/atlas-cli-deploy-docker/)

```bash
docker run -p 27777:27017 --privileged -it mongodb/atlas sh \
  -c "atlas deployments setup --bindIpAll --username root --password root --type local --force && tail -f /dev/null"
```

The original 27017 port is mapped to 27777, so it does not conflict with standard MongoDb installation you can have in the local machine.

You can change the username and password if you like, then the connection string will be

```
mongodb://user:password@localhost:27777/?authSource=admin
```

## Better docker support

If you start container as shown in previous chapter, the problem is that, after you stop and restart container another instance of atlas will be created. To have a better docker support you need to create a Dockerfile with the following content

```Dockerfile
FROM mongodb/atlas

COPY startatlas.sh /usr/startatlas.sh 

CMD /usr/startatlas.sh 
```

Then you can create a startatlas.sh file with the following content

```bash
#!/bin/bash

# The name of the deployment to search for

# Run the command and save the output
OUTPUT=$(atlas deployments list)

echo "Output: "
echo $OUTPUT

# count line of output
LINE=$(echo "$OUTPUT" | wc -l)
echo "Count line of output: $LINE "

if [ $LINE -lt 2 ]; then
    echo "No deployment found. create a new one"
    atlas deployments setup local --bindIpAll --username root --password root --type local --force
else
    echo "Deployment found. Start it"
    atlas deployments start local
fi

function pause_atlas() {
    atlas deployments pause local
}
# This will call the 'on_exit' function when the container exits
trap pause_atlas EXIT

tail -f /dev/null
```

This will create a base image that can support stop/start of the container.

## Creating local cluster

Once you installed Atlas CLI you can create a local MongoDB Atlas cluster with this simple instruction

```bash
 atlas deployments setup --type local
```

You can follow instruction, you can use both 6 or 7 version of MongoDB Atlas.

You can then list all of your environment with

```bash
atlas deployments list
```

And you can start/stop atlas deployment with 

```bash
atlas deployment start <deployment-name>
atlas deployment pause <deployment-name>
```

You can then connect with the standard connection string

```
mongodb://localhost:27017/?directConnection=true&serverSelectionTimeoutMS=2000
```

## Some useful commands

If in local atlas installation tests fails or you have some strange error, it could happen that the search index is corrupted. To manually delete an index, first of all list all available vector and search indexes inside the collection

```
db.getCollection("_ix__kernel_memory_single_index").aggregate([

{"$listSearchIndexes" : {}}
])
```

This will return the list of all indexes that are defined in the collection, you can delete an index using the command

```
db.runCommand({"dropSearchIndex" : "_ix__kernel_memory_single_index", "id" : "65e4ae1623dd55119d74571e"})
```