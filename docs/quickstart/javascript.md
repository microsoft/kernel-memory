---
nav_order: 6
parent: Quickstart
title: JavaScript example
permalink: /quickstart/javascript
layout: default
---
# <img src="javascript.png" width="48"> JavaScript / Node.js example

## Upload a document

Create a file named `upload.js` with the current content:

```javascript
const axios = require("axios");
const fs = require("fs");
const FormData = require("form-data");

async function run() {
  const fileBuffer = await fs.promises.readFile("README.md");

  const formData = new FormData();
  formData.append("file1", fileBuffer, { filename: "README.md" });
  formData.append("documentId", "doc01");

  axios
    .post("http://127.0.0.1:9001/upload", formData, {
      headers: {
        "Content-Type": "multipart/form-data",
      },
    })
    .then((response) => {
      console.log(response.data);
    })
    .catch((error) => {
      console.error(error);
    });
}

run();
```

Execute the script with `node`:

    node upload.js

You should see the following result:

{: .console }
> ```yaml
> {
>   index: '',
>   documentId: 'doc01',
>   message: 'Document upload completed, ingestion pipeline started'
> }
```

## Query

Create a file named `ask.js` with the current content:

```javascript
const axios = require("axios");

const data = {
  question: "Name one tool that I can use from command line",
};

axios
  .post("http://127.0.0.1:9001/ask", data, {
    headers: {
      "Content-Type": "application/json",
    },
  })
  .then((response) => {
    if ("text" in response.data) {
      console.log(response.data.text);
    } else {
      console.log("Response does not contain a 'text' property.");
    }
  })
  .catch((error) => {
    console.error(error.message);
  });
```

Execute the script with `node`:

    node ask.js

You should see the following result:

{: .console }
> _You can use the `upload-file.sh`, `ask.sh`, and `search.sh` tools from the command line._
