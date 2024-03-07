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
