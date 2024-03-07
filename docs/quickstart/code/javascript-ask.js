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
